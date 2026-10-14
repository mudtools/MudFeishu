// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Conversations;
using Mud.AI.Abstractions.Knowledge;

namespace Mud.AI.Agent.Context;

/// <summary>
/// 知识检索上下文装配器（AI-FD-D12 P2D-4a，注入模式）的<b>中立实现</b>（N4 · F5 拆分）：
/// 把 <see cref="IRetriever"/> 召回切片以编号参考块形式注入 Prompt——「检索 → Prompt 注入桥」。
/// </summary>
/// <remarks>
/// <para>
/// <b>拆分说明（方案 v2 §5.5 F5）</b>：机制（检索 → minScore 过滤 → 编号 → 三级预算 →
/// untrusted 头）在此一份实现；头部/尾部文案是产品事实（⑤ 式），由构造参数注入——
/// 产品子类传入自有文案即保行为逐字节一致。
/// </para>
/// <para>
/// <b>三级预算（R3-8，取值单一源 <see cref="RetrievalBudgets"/>）</b>：条数闸、总长闸、
/// 单条闸各自可达——短切片撞条数闸、长切片撞总预算、超长单条撞单条闸；
/// 每条以 <c>[n] source=&lt;来源&gt; 文本</c> 成对输出——模型按编号引用时宿主可回链原文。
/// </para>
/// <para>
/// <b>失败隔离</b>：装配器自身失败由事件层既有异常隔离承接（记日志跳过，不阻断对话）；
/// 检索无结果时返回 <see langword="null"/>（本装配器对本次事件无贡献）。
/// </para>
/// <para>
/// <see cref="IContextAssembler.Order"/> = 100（缺省）：默认装配器之后，问题文本先于知识注入。
/// </para>
/// </remarks>
public class RetrievalContextAssembler : IContextAssembler
{
    private readonly IRetriever _retriever;
    private readonly string _header;
    private readonly string _footer;
    private readonly int _order;
    private readonly double? _minScore;

    /// <summary>
    /// 初始化 <see cref="RetrievalContextAssembler"/>。
    /// </summary>
    /// <param name="retriever">知识检索门面。</param>
    /// <param name="header">注入块头部（<b>产品事实</b>：untrusted 标注文案）。</param>
    /// <param name="footer">注入块尾部（<b>产品事实</b>）。</param>
    /// <param name="order">装配顺序（缺省 100）。</param>
    /// <param name="minScore">
    /// T9 / D9：相关性门槛——<see cref="RetrievedChunk.Score"/> 低于此值的切片被过滤。
    /// <see langword="null"/>（默认）= 不过滤；无分值的切片保留（A15 零差异）。
    /// </param>
    public RetrievalContextAssembler(
        IRetriever retriever,
        string header,
        string footer,
        int order = 100,
        double? minScore = null)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _header = string.IsNullOrEmpty(header)
            ? throw new ArgumentException("注入块头部不能为空（untrusted 标注是防提示注入的硬约束）", nameof(header))
            : header;
        _footer = footer ?? throw new ArgumentNullException(nameof(footer));
        _order = order;
        _minScore = minScore;
    }

    /// <inheritdoc />
    public int Order => _order;

    /// <inheritdoc />
    public virtual async Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // 用户问题取 @提及指令文本（可空时本轮不检索——无问题即无检索语义）。
        var query = request.MentionedText;
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        IEnumerable<RetrievedChunk> chunks = await _retriever.RetrieveAsync(query!, cancellationToken).ConfigureAwait(false);

        // T9 / D9：相关性门槛过滤——minScore 非空时过滤低分切片；
        // Score 为 null 的切片保留（A15 零差异）。
        if (_minScore is { } threshold)
        {
            chunks = chunks.Where(c => !c.Score.HasValue || c.Score.Value >= threshold).ToList();
        }

        // R3-8 ①：先按条数收敛（同时跳过空白切片）——条数与长度是两条独立的预算闸。
        var adopted = new List<RetrievedChunk>();
        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk.Text))
            {
                continue;
            }

            adopted.Add(chunk);
            if (adopted.Count == RetrievalBudgets.MaxChunks)
            {
                break;
            }
        }

        if (adopted.Count == 0)
        {
            return null;
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine(_header);

        // R3-8 ②：总长度预算逐条扣减——每条先按单条闸截断，再按剩余预算截断。
        var budget = RetrievalBudgets.TotalLength;
        for (var i = 0; i < adopted.Count && budget > 0; i++)
        {
            var text = TruncateChunk(adopted[i].Text!);
            if (text.Length > budget)
            {
                text = text.Substring(0, budget);
            }

            budget -= text.Length;

            // R3-8 ③：编号与来源**成对**输出 ⇒ 模型引用 [n] 时宿主可回链原文（无来源时退化为纯编号）。
            builder.Append('[').Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("] ");
            if (!string.IsNullOrWhiteSpace(adopted[i].Source))
            {
                builder.Append("source=").Append(adopted[i].Source).Append(' ');
            }

            // RAG-B：标题层级路径同样属于回链信息（有 Source 的 URL 往往很长，模型据此定位到"哪一节"更实用）。
            if (!string.IsNullOrWhiteSpace(adopted[i].TitlePath))
            {
                builder.Append("title=").Append(adopted[i].TitlePath).Append(' ');
            }

            builder.AppendLine(text);
        }

        builder.Append(_footer);
        return builder.ToString();
    }

    private static string TruncateChunk(string text)
        => text.Length <= RetrievalBudgets.ChunkPreviewLength ? text : text.Substring(0, RetrievalBudgets.ChunkPreviewLength) + "…";
}
