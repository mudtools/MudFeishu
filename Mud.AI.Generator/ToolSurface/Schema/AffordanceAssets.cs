// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Threading;
using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.ToolSurface.Schema;

/// <summary>
/// per-tool affordance 资产的<b>采集 / 解析 / 校验</b>（<see cref="SdkToolProfileModel.AffordanceDirectory"/> 槽驱动）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么用 AdditionalFiles 而非源码内 XML 文档</b>：与 <c>GuidanceEmitter</c> 同款判断——
/// 生成器工程受 <c>netstandard2.0</c> 约束，解析 XML doc 需手写解析器（复杂且脆）；
/// AdditionalFiles 的扫描、读取、比对全部复用既有管线。
/// </para>
/// <para>
/// <b>为什么与 guidance 分两套代码而不是复用一份解析器</b>：两者的<b>键空间与结构约束完全不同</b>——
/// guidance 是「路径 → 自由文本」的字典（键 = 域/主题，无结构约束），affordance 是
/// 「工具名 → 四节数组」的结构化契约（缺节/未知节/空节都是策展错误，必须报红）。
/// 强行抽象会把"结构校验"这条核心价值稀释成可选钩子。
/// </para>
/// <para>
/// <b>失败模式与 guidance 不同且更隐蔽</b>：guidance 写错文件名只是"某域少了一段文本"，
/// 而 affordance 写错文件名/节名<b>不会触发任何编译错误</b>，表现为"模型还是不看这些提示"，
/// 从构建日志上完全看不出来 ⇒ 必须由 Error 级诊断机械拦下（028/029/030）。
/// </para>
/// <para>
/// <b>为什么在本层报错而不是抛异常</b>：<c>GuidanceEmitter</c> 的"键冲突抛异常"是既有取舍，
/// 但那条路径的失败是「两个文件争一个键」（无法归属到单个资产）；affordance 的三类失败都能
/// 精确归属到<b>某一份资产</b>，报诊断能一次列全所有坏资产（抛异常只能报第一个），
/// 且诊断可被消费方按精确 ID 配置/审计。
/// </para>
/// </remarks>
internal static class AffordanceAssets
{
    /// <summary>单份资产的字符数上界（进模型 token 预算；超限走 Warning 级基线，不阻断构建）。</summary>
    public const int MaxAssetLength = 1024;

    /// <summary>单个小节的字符数上界（防"某一节被写成一篇文档"）。</summary>
    public const int MaxSectionLength = 320;

    /// <summary>超限资产在诊断消息里的列举上限（照搬 <c>ReportOutputSchemaTruncations</c> 的聚合纪律）。</summary>
    private const int MaxListedAssets = 5;

    /// <summary>四个固定小节：标题文本（大小写不敏感匹配）→ 发射键。</summary>
    /// <remarks>
    /// 顺序即<see cref="AffordanceEntry.ToJson"/> 的键顺序（golden 确定性的一部分）。
    /// 标题文本按<b>人类可读</b>书写（<c>Avoid when</c>），发射键按<b>机器可读</b> snake_case
    /// （<c>avoid_when</c>），与 <c>x-{sdk}</c> 既有键风格一致。
    /// </remarks>
    private static readonly ImmutableArray<SectionDefinition> SectionTable =
    [
        new("WHEN", "when"),
        new("Avoid when", "avoid_when"),
        new("Prerequisites", "prerequisites"),
        new("Tips", "tips"),
    ];

    /// <summary>
    /// 判断 AdditionalFile 是否为该剖面的 affordance 资产（正斜杠归一后按目录片段匹配）。
    /// </summary>
    /// <remarks>
    /// <see cref="SdkToolProfileModel.AffordanceDirectory"/> 是<b>非必填槽</b>：空串时本方法恒为
    /// <see langword="false"/>。否则 <c>IndexOf("")</c> 恒为 0，消费方工程里任意 <c>.md</c>
    /// （README、CHANGELOG、Guidance 资产）都会被吸收为本剖面的 affordance，
    /// 进而以"文件名不是工具名"批量产出零容忍的归属诊断（028/029）——升级即构建失败。
    /// </remarks>
    public static bool IsAffordanceFile(string path, SdkToolProfileModel profile)
    {
        var directory = profile.AffordanceDirectory;
        if (directory.Length == 0)
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');
        return normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            && normalized.IndexOf(directory, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>从资产路径取契约工具名（末段文件名去扩展名；取不到返回 <see langword="null"/>）。</summary>
    public static string? AssetToolName(string path)
    {
        var normalized = path.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        var tail = lastSlash >= 0 ? normalized.Substring(lastSlash + 1) : normalized;
        var dot = tail.LastIndexOf('.');
        var name = dot > 0 ? tail.Substring(0, dot) : tail;
        return name.Length == 0 ? null : name;
    }

    /// <summary>
    /// 采集并解析全部 affordance 资产：返回<b>可发射</b>的条目与<b>待上报</b>的诊断。
    /// </summary>
    /// <param name="files">已按 <see cref="IsAffordanceFile"/> 过滤的资产集合。</param>
    /// <param name="models">本剖面的工具模型（提供工具名契约表与草稿描述符）。</param>
    /// <param name="profile">当前剖面（提供目录槽与诊断 ID 前缀）。</param>
    /// <param name="factory">该剖面的诊断工厂（拼 <c>{prefix}{slot:D3}</c> 描述符）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <remarks>
    /// <b>确定性</b>：资产按路径序处理，条目按工具名 <c>SortedDictionary</c> 归集——
    /// 与 golden 的逐字节可比性要求一致（同一输入必须产出同一 JSON）。
    /// </remarks>
    public static (ImmutableArray<AffordanceEntry> Entries, ImmutableArray<PendingDiagnostic> Diagnostics) Collect(
        ImmutableArray<AdditionalText> files,
        ImmutableArray<ToolSchemaModel> models,
        SdkToolProfileModel profile,
        ToolSurfaceDiagnostics.Factory factory,
        CancellationToken cancellationToken)
    {
        if (files.IsDefaultOrEmpty)
        {
            return (ImmutableArray<AffordanceEntry>.Empty, ImmutableArray<PendingDiagnostic>.Empty);
        }

        var knownTools = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in models)
        {
            knownTools.Add(model.Entry.ToolName);
        }

        var entries = new SortedDictionary<string, AffordanceEntry>(StringComparer.Ordinal);
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        var diagnostics = ImmutableArray.CreateBuilder<PendingDiagnostic>();
        var overBudget = new List<string>();

        foreach (var file in files.OrderBy(static f => f.Path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var toolName = AssetToolName(file.Path);
            var text = file.GetText(cancellationToken)?.ToString();
            if (toolName is null)
            {
                continue;
            }

            // 归属先于结构：文件名不是工具名时，资产整体不会被消费，继续做结构校验只会产生噪声。
            if (!knownTools.Contains(toolName))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAffordanceOrphan],
                    file.Path,
                    toolName));
                continue;
            }

            if (owners.TryGetValue(toolName, out var previousOwner))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAffordanceStructure],
                    toolName,
                    file.Path,
                    $"同一工具名被两份资产占用（'{previousOwner}' 与 '{file.Path}'）"));
                continue;
            }

            owners[toolName] = file.Path;

            if (string.IsNullOrWhiteSpace(text))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAffordanceStructure],
                    toolName,
                    file.Path,
                    "资产为空（四个小节全部缺失）"));
                continue;
            }

            var raw = text!;
            if (!TryParse(raw, out var sections, out var structureError))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAffordanceStructure],
                    toolName,
                    file.Path,
                    structureError));
                continue;
            }

            // 引用校验（030）：仅校验反引号包裹的 `域.工具` 形态——这是资产里"指名道姓"的显式约定，
            // 裸文本不参与（否则正常散文里的 "e.g. foo.bar" 会变成假红，守卫的可信度随之归零）。
            var dangling = FindDanglingReferences(sections, knownTools);
            if (dangling.Count > 0)
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAffordanceDanglingReference],
                    toolName,
                    file.Path,
                    string.Join(", ", dangling.Select(static r => "'" + r + "'"))));
                continue;
            }

            var entry = new AffordanceEntry(
                toolName,
                sections[0],
                sections[1],
                sections[2],
                sections[3]);

            var measured = raw.Length;
            var longestSection = sections.Max(static section => section.Sum(static item => item.Length));
            if (measured > MaxAssetLength || longestSection > MaxSectionLength)
            {
                overBudget.Add($"{toolName}({measured}/{longestSection})");
            }

            entries[toolName] = entry;
        }

        // 031 聚合单条：逐资产报会随资产数线性放大日志（与 009/018/021 同款取舍）。
        // 消息文本携带"份数"，以便消费方按 009-tools 同款补 `031-assets=<N>` 语义量基线。
        if (overBudget.Count > 0)
        {
            var listed = overBudget.Count <= MaxListedAssets
                ? string.Join(" | ", overBudget)
                : string.Join(" | ", overBudget.Take(MaxListedAssets)) + $" | …另有 {overBudget.Count - MaxListedAssets} 份";

            diagnostics.Add(PendingDiagnostic.Create(
                factory[ToolSurfaceDiagnostics.SlotAffordanceBudget],
                overBudget.Count,
                MaxAssetLength,
                MaxSectionLength,
                listed));
        }

        return (entries.Values.ToImmutableArray(), diagnostics.ToImmutable());
    }

    /// <summary>
    /// 解析资产正文为四节条目数组（顺序与 <see cref="SectionTable"/> 一致）。
    /// </summary>
    /// <remarks>
    /// <para><b>词法规则（确定性优先，全部可机械复现）</b>：</para>
    /// <list type="bullet">
    /// <item>小节标题 = 行首（允许空白）以 <c>#</c> 开头的行，去掉全部 <c>#</c> 与空白后
    /// <b>大小写不敏感</b>匹配 <see cref="SectionTable"/> 的标题文本；未命中即"未知小节"；</item>
    /// <item>首个小节标题之前的正文被忽略（容错：资产可以有一级标题或说明抬头）；</item>
    /// <item>小节体内：以 <c>-</c> / <c>*</c> / <c>+</c> 开头的行是<b>一个条目</b>（标记剥离）；
    /// 非项目符号行并入当前条目（续行以 <c>\n</c> 连接）；空行忽略；</item>
    /// <item>四节必须<b>都存在且各至少一个条目</b>——"空节"与"缺节"同为策展错误。</item>
    /// </list>
    /// <para>
    /// <b>为什么"空节"也要报错</b>：affordance 的价值在于"逼迫策展者显式回答四个问题"。
    /// 允许留空会让资产退化为"只写 WHEN 的半个描述"，而模型拿到 `"avoid_when":[]` 无法区分
    /// "确实没有反例"与"策展者忘了写"。
    /// </para>
    /// </remarks>
    private static bool TryParse(
        string text,
        out ImmutableArray<ImmutableArray<string>> sections,
        out string error)
    {
        var bodies = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        sections = ImmutableArray<ImmutableArray<string>>.Empty;

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string? current = null;

        foreach (var rawLine in normalized.Split('\n'))
        {
            if (TryReadHeading(rawLine, out var heading))
            {
                var key = KeyOf(heading);
                if (key is null)
                {
                    error = $"未知小节 '{heading}'（可用小节：{string.Join(" / ", SectionTable.Select(static s => s.Heading))}）";
                    return false;
                }

                if (bodies.ContainsKey(key))
                {
                    error = $"小节 '{heading}' 重复出现";
                    return false;
                }

                bodies[key] = [];
                current = key;
                continue;
            }

            if (current is not null)
            {
                bodies[current].Add(rawLine);
            }
        }

        if (bodies.Count == 0)
        {
            error = $"未识别到任何小节（须为 {string.Join(" / ", SectionTable.Select(static s => s.Heading))}）";
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<ImmutableArray<string>>(SectionTable.Length);
        foreach (var definition in SectionTable)
        {
            if (!bodies.TryGetValue(definition.Key, out var body))
            {
                error = $"缺少小节 '{definition.Heading}'（四节均为必填）";
                return false;
            }

            var items = ToItems(body);
            if (items.Length == 0)
            {
                error = $"小节 '{definition.Heading}' 为空（四节均须至少一个条目）";
                return false;
            }

            builder.Add(items);
        }

        sections = builder.ToImmutable();
        error = string.Empty;
        return true;
    }

    /// <summary>读小节标题（行首空白 + 一个或多个 <c>#</c>；非标题返回 false）。</summary>
    private static bool TryReadHeading(string line, out string heading)
    {
        heading = string.Empty;
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '#')
        {
            return false;
        }

        heading = trimmed.TrimStart('#').Trim();
        return true;
    }

    private static string? KeyOf(string heading)
    {
        foreach (var definition in SectionTable)
        {
            if (string.Equals(definition.Heading, heading, StringComparison.OrdinalIgnoreCase))
            {
                return definition.Key;
            }
        }

        return null;
    }

    /// <summary>把小节体切成条目（项目符号各成一条；普通行并入当前条目）。</summary>
    private static ImmutableArray<string> ToItems(List<string> body)
    {
        var items = ImmutableArray.CreateBuilder<string>();
        StringBuilder? current = null;

        foreach (var line in body)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (IsBullet(trimmed))
            {
                Flush(items, current);
                current = new StringBuilder(trimmed.Substring(1).Trim());
                continue;
            }

            if (current is null)
            {
                current = new StringBuilder(trimmed);
            }
            else
            {
                current.Append('\n').Append(trimmed);
            }
        }

        Flush(items, current);
        return items.ToImmutable();
    }

    private static bool IsBullet(string trimmed)
        => trimmed.Length > 1
            && (trimmed[0] == '-' || trimmed[0] == '*' || trimmed[0] == '+')
            && char.IsWhiteSpace(trimmed[1]);

    private static void Flush(ImmutableArray<string>.Builder items, StringBuilder? current)
    {
        if (current is not null && current.Length > 0)
        {
            items.Add(current.ToString());
        }
    }

    /// <summary>
    /// 找出资产内反引号包裹且形如 <c>域.工具</c>、但不在工具名契约表内的引用。
    /// </summary>
    private static List<string> FindDanglingReferences(
        ImmutableArray<ImmutableArray<string>> sections,
        HashSet<string> knownTools)
    {
        var dangling = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var section in sections)
        {
            foreach (var item in section)
            {
                foreach (var reference in ExtractBacktickSpans(item))
                {
                    if (!LooksLikeToolName(reference)
                        || knownTools.Contains(reference)
                        || !seen.Add(reference))
                    {
                        continue;
                    }

                    dangling.Add(reference);
                }
            }
        }

        return dangling;
    }

    /// <summary>提取反引号对之间的文本（单反引号；未闭合的尾段忽略）。</summary>
    private static List<string> ExtractBacktickSpans(string text)
    {
        var spans = new List<string>();
        var index = 0;
        while (index < text.Length)
        {
            var open = text.IndexOf('`', index);
            if (open < 0)
            {
                break;
            }

            var close = text.IndexOf('`', open + 1);
            if (close < 0)
            {
                break;
            }

            var span = text.Substring(open + 1, close - open - 1).Trim();
            if (span.Length > 0)
            {
                spans.Add(span);
            }

            index = close + 1;
        }

        return spans;
    }

    /// <summary>形态判定：<c>域.工具</c>（两侧均为小写标识符，允许下划线与数字）。</summary>
    private static bool LooksLikeToolName(string value)
    {
        var dot = value.IndexOf('.');
        if (dot <= 0 || dot == value.Length - 1 || value.IndexOf('.', dot + 1) >= 0)
        {
            return false;
        }

        return IsIdentifierSegment(value.Substring(0, dot))
            && IsIdentifierSegment(value.Substring(dot + 1));
    }

    private static bool IsIdentifierSegment(string value)
    {
        if (value.Length == 0 || value[0] < 'a' || value[0] > 'z')
        {
            return false;
        }

        foreach (var ch in value)
        {
            var isLower = ch >= 'a' && ch <= 'z';
            var isDigit = ch >= '0' && ch <= '9';
            if (!isLower && !isDigit && ch != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>小节定义（标题文本 → 发射键）。</summary>
    private sealed class SectionDefinition
    {
        public SectionDefinition(string heading, string key)
        {
            Heading = heading;
            Key = key;
        }

        /// <summary>资产里书写的小节标题（大小写不敏感匹配）。</summary>
        public string Heading { get; }

        /// <summary>发射进 <c>x-{sdk}.affordance</c> 的 JSON 键。</summary>
        public string Key { get; }
    }
}