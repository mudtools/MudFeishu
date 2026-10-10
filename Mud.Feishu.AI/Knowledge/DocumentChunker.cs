// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// RAG-B 切片参数（结构优先切片的三个闸）。默认值面向"技术文档 / wiki"这类中文长文本。
/// </summary>
/// <remarks>
/// <b>为什么不引入分词器依赖</b>：真实分词器会引入模型/词典依赖并威胁 AOT 面；本仓用
/// <see cref="TokenEstimator"/> 的<b>可解释近似</b>做预算判定（见其文档）。若宿主已有分词器，
/// 可自行按 <see cref="DocumentChunker.Chunk(CorpusDocument, ChunkingOptions?)"/> 的同一输入产出更准的预算。
/// </remarks>
public sealed class ChunkingOptions
{
    /// <summary>单块字符上限（块级切分闸）。</summary>
    public int MaxChunkLength { get; set; } = 800;

    /// <summary>相邻块的<b>重叠</b>字符数（避免答案正好落在切口上）。</summary>
    public int OverlapLength { get; set; } = 100;

    /// <summary>单文档 token 预算上限（估算值；达到即停止产出后续块）。</summary>
    public int MaxTokens { get; set; } = 1200;

    /// <summary>短于此长度的块直接丢弃（标题行、空行残留等噪声块）。</summary>
    public int MinChunkLength { get; set; } = 24;

    /// <summary>
    /// 校验参数自洽性。<b>非法即抛</b>——静默回落默认值会让"预算失控"变成看不见的回归。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">参数非法（含重叠 ≥ 上限这类"无法前进"的配置）。</exception>
    public void Validate()
    {
        if (MaxChunkLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxChunkLength), MaxChunkLength, "单块字符上限必须为正");
        }

        if (OverlapLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(OverlapLength), OverlapLength, "重叠长度不能为负");
        }

        if (OverlapLength >= MaxChunkLength)
        {
            // 这是"永远无法前进"的配置：定长窗口的步长 = MaxChunkLength - OverlapLength ≤ 0 ⇒ 死循环。
            throw new ArgumentOutOfRangeException(
                nameof(OverlapLength), OverlapLength, $"重叠长度必须小于单块上限（当前上限 {MaxChunkLength}）");
        }

        if (MaxTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxTokens), MaxTokens, "token 预算必须为正");
        }

        if (MinChunkLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinChunkLength), MinChunkLength, "最短块长度不能为负");
        }
    }
}

/// <summary>
/// Token 估算（<b>刻意不精确</b>，但确定、可测、AOT 友好）。
/// </summary>
/// <remarks>
/// <para>
/// <b>口径</b>：CJK / 全角字符按 1 token 计，其余（ASCII 等）按 4 字符 ≈ 1 token 计（向上取整）。
/// </para>
/// <para>
/// <b>为什么允许不精确</b>：它只用于<b>切片预算判定</b>（"这段是不是已经太长"），
/// 而非计费或截断展示——此处偏保守 10~20% 完全可接受。引入真分词器的成本（依赖/AOT/体积）
/// 与收益不成比例，故按近似实现并把口径写死在此处。
/// </para>
/// </remarks>
public static class TokenEstimator
{
    /// <summary>估算一段文本的 token 数（空串返回 0）。</summary>
    public static int Estimate(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var wide = 0;
        var narrow = 0;
        foreach (var ch in value)
        {
            if (IsWide(ch))
            {
                wide++;
            }
            else
            {
                narrow++;
            }
        }

        return wide + (int)Math.Ceiling(narrow / 4d);
    }

    /// <summary>是否按"宽字符"计 1 token（CJK / 全角 / 常用中文标点）。</summary>
    private static bool IsWide(char ch) => ch >= 0x2E80 && ch <= 0x9FFF ||
        ch >= 0xF900 && ch <= 0xFAFF ||
        ch >= 0xFF00 && ch <= 0xFF60 ||
        ch >= 0x3000 && ch <= 0x303F;
}

/// <summary>
/// RAG-B 结构优先切片器：<b>标题层级 → 块边界 → 定长窗口（含重叠）</b>，纯函数、零依赖、零 IO。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么"结构优先"而不是"按长度硬切"</b>：硬切会把句子拦腰截断，检索命中后半段时
/// 语义不完整（"…请参见下表" 前面缺主语）。先按 Markdown 标题与空行分块、只在<b>单块超长</b>时才
/// 定长切，才能让绝大多数块在语义上是"一个完整小节"。
/// </para>
/// <para>
/// <b>标题层级保留</b>：<c>#</c>~<c>######</c> 维护层级栈，切片记录其<b>首个块</b>的标题路径
/// （<see cref="CorpusChunk.TitlePath"/>）——跨标题的块保留首个而非拼接，避免编造层级。
/// </para>
/// <para>
/// <b>确定性</b>：同一输入 + 同一参数 ⇒ 逐块相同的输出（无字典/随机/时间依赖），
/// 这是"重建索引不产生抖动"的前提。
/// </para>
/// </remarks>
public static class DocumentChunker
{
    private const string TitleSeparator = " / ";

    /// <summary>
    /// 切片。
    /// </summary>
    /// <param name="document">待切分文档（<see cref="CorpusDocument.SourceToken"/> 为空即视为无来源 ⇒ 抛错）。</param>
    /// <param name="options">切片参数（可空 ⇒ 默认值）。</param>
    /// <returns>切片序列（顺序稳定；空/全空白正文返回空序列）。</returns>
    /// <exception cref="ArgumentException">来源标识缺失。</exception>
    /// <exception cref="ArgumentOutOfRangeException">参数非法（见 <see cref="ChunkingOptions.Validate"/>）。</exception>
    public static IReadOnlyList<CorpusChunk> Chunk(CorpusDocument document, ChunkingOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(document.SourceToken))
        {
            // 缺来源标识 ⇒ 切片不可回链 ⇒ 直接拒绝（"先入库再说"在这里就是数据污染）。
            throw new ArgumentException("语料文档缺少 SourceToken——切片将无法回链到来源", nameof(document));
        }

        var effective = options ?? new ChunkingOptions();
        effective.Validate();

        var chunks = new List<CorpusChunk>();
        if (string.IsNullOrWhiteSpace(document.Text))
        {
            return chunks;
        }

        var tokens = 0;
        var index = 0;

        foreach (var segment in Pack(SplitBlocks(document.Text), effective.MaxChunkLength, effective.OverlapLength))
        {
            var text = segment.Text.Trim();
            if (text.Length < effective.MinChunkLength)
            {
                continue;
            }

            var estimated = TokenEstimator.Estimate(text);
            if (tokens + estimated > effective.MaxTokens && chunks.Count > 0)
            {
                // 预算已用尽 ⇒ 停止（不在首块上生效：否则任何文档都出不来内容）。
                break;
            }

            tokens += estimated;
            chunks.Add(new CorpusChunk(
                document.SourceToken,
                text,
                segment.TitlePath,
                document.Url,
                document.ScopeKey,
                index));

            index++;
        }

        return chunks;
    }

    /// <summary>带标题路径的候选块。</summary>
    private readonly record struct Block(string Text, string? TitlePath);

    private readonly record struct Segment(string Text, string? TitlePath);

    /// <summary>按行扫描：维护标题层级栈，空行作为块边界。</summary>
    private static List<Block> SplitBlocks(string text)
    {
        var blocks = new List<Block>();
        var headings = new string?[7];
        var buffer = new StringBuilder();

        void Flush()
        {
            if (buffer.Length == 0)
            {
                return;
            }

            blocks.Add(new Block(buffer.ToString().TrimEnd(), CurrentPath(headings)));
            buffer.Clear();
        }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();

            var level = HeadingLevel(line);
            if (level > 0)
            {
                Flush();
                headings[level] = line.Substring(level).Trim();
                for (var deeper = level + 1; deeper < headings.Length; deeper++)
                {
                    // 进入新层级时清空更深层（否则 "#A / ###B" 会拼出 "#A / ###B" 之后再挂 ###C 的错位路径）。
                    headings[deeper] = null;
                }

                continue;
            }

            if (line.Length == 0)
            {
                Flush();
                continue;
            }

            buffer.Append(line).Append('\n');
        }

        Flush();
        return blocks;
    }

    private static int HeadingLevel(string line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        // 只认 1~6 级标题，且 `#` 后必须跟空格（避免把 `#######` 或 `#hashtag` 误判为标题）。
        return level is >= 1 and <= 6 && level < line.Length && line[level] == ' ' ? level : 0;
    }

    private static string? CurrentPath(string?[] headings)
    {
        var parts = new List<string>(3);
        foreach (var heading in headings)
        {
            if (!string.IsNullOrEmpty(heading))
            {
                parts.Add(heading!);
            }
        }

        return parts.Count == 0 ? null : string.Join(TitleSeparator, parts);
    }

    /// <summary>块装箱：能整块放下就整块放，放不下且当前桶非空则先收口；单块超长则定长切（含重叠）。</summary>
    private static IEnumerable<Segment> Pack(List<Block> blocks, int maxLength, int overlap)
    {
        var bucket = new StringBuilder();
        string? bucketTitle = null;

        foreach (var block in blocks)
        {
            if (block.Text.Length > maxLength)
            {
                if (bucket.Length > 0)
                {
                    yield return new Segment(bucket.ToString(), bucketTitle);
                    bucket.Clear();
                    bucketTitle = null;
                }

                foreach (var piece in SplitFixed(block.Text, maxLength, overlap))
                {
                    // 定长窗口内的片段共享该块的标题路径（不臆造更细的层级）。
                    yield return new Segment(piece, block.TitlePath);
                }

                continue;
            }

            if (bucket.Length > 0 && bucket.Length + block.Text.Length + 1 > maxLength)
            {
                yield return new Segment(bucket.ToString(), bucketTitle);
                bucket.Clear();
            }

            if (bucket.Length == 0)
            {
                bucketTitle = block.TitlePath;
            }

            bucket.Append(block.Text).Append('\n');
        }

        if (bucket.Length > 0)
        {
            yield return new Segment(bucket.ToString(), bucketTitle);
        }
    }

    /// <summary>
    /// 定长窗口切分：步长 = 窗口 - 重叠，<b>保证步长为正</b>（由 <see cref="ChunkingOptions.Validate"/> 兜底）。
    /// </summary>
    private static IEnumerable<string> SplitFixed(string text, int window, int overlap)
    {
        var step = window - overlap;
        for (var start = 0; start < text.Length; start += step)
        {
            var length = Math.Min(window, text.Length - start);
            yield return text.Substring(start, length);
            if (start + length >= text.Length)
            {
                yield break;
            }
        }
    }
}