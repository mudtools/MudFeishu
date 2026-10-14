// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Context;

/// <summary>
/// 「事件事实列表 → 结构化 prompt 片段」装配器的<b>中立基类</b>：机制（事实排序、空值跳过、逐行扣预算、
/// untrusted 头、截断标记、条数闸）一份实现，<b>产品事实</b>（事件族判别键、预算实测值、中文标签映射、
/// 建议文案）由产品子类以构造参数注入。
/// </summary>
/// <remarks>
/// <para>
/// 机制单体化的理由：审批事件与多维表格记录变更装配器的主体是同一份「事实列表 → 行列表」机制；
/// 各自实现则「每个装配器都记得扣一遍预算」只能靠人记——单体化后新增事件族只提供事实。
/// </para>
/// <para>
/// <b>预算与截断（硬约束，行为逐字节保留）</b>：单行 ≤ 字段预览上限，整段 ≤ 总预算，条数 ≤ <see cref="AssemblerBudgets.MaxFactsPerAssembler"/>；
/// 超限<b>截断</b>而非整段丢弃并留 <see cref="AssemblerBudgets.TruncationMarker"/>。
/// </para>
/// <para>
/// <b>untrusted 标注</b>：事件载荷含平台/用户数据，整段以 untrusted 头开头（防提示注入）；头部文案是产品事实。
/// <b>失败隔离</b>：本类不抛业务异常；事件键不匹配 / 无载荷 / 全部字段为空返回 <see langword="null"/>。
/// </para>
/// </remarks>
public abstract class FactListContextAssembler : IContextAssembler
{
    private readonly string _eventKey;
    private readonly int _totalLength;
    private readonly int _fieldPreviewLength;
    private readonly string _untrustedHeader;
    private readonly IReadOnlyDictionary<string, string>? _factLabels;
    private readonly string? _hint;

    /// <summary>
    /// 初始化事实列表装配器基类。
    /// </summary>
    /// <param name="eventKey">事件族判别键（<b>产品事实</b>：取值单一源留产品侧）。</param>
    /// <param name="order">装配顺序（小者在前）。</param>
    /// <param name="totalLength">整段预算（<b>产品事实</b>：实测值留产品侧）。</param>
    /// <param name="fieldPreviewLength">单行字段值预览上限（<b>产品事实</b>）。</param>
    /// <param name="untrustedHeader">untrusted 标注文案（<b>产品事实</b>，含平台载荷表述）。</param>
    /// <param name="factLabels">事实键 → 展示名映射（可空；未登记键原样展示——不臆造语义）。</param>
    /// <param name="hint">下一步建议文案（可空；工具名等产品词汇归产品侧）。</param>
    protected FactListContextAssembler(
        string eventKey,
        int order,
        int totalLength,
        int fieldPreviewLength,
        string untrustedHeader,
        IReadOnlyDictionary<string, string>? factLabels = null,
        string? hint = null)
    {
        _eventKey = string.IsNullOrEmpty(eventKey)
            ? throw new ArgumentException("事件族判别键不能为空（产品事实须显式注入）", nameof(eventKey))
            : eventKey;
        Order = order;
        _totalLength = totalLength;
        _fieldPreviewLength = fieldPreviewLength;
        _untrustedHeader = untrustedHeader;
        _factLabels = factLabels;
        _hint = hint;
    }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Task.FromResult<string?>(null);
        }

        if (!string.Equals(request.EventKey, _eventKey, StringComparison.Ordinal)
            || request.EventFacts is null
            || request.EventFacts.Count == 0)
        {
            // 非本族事件 / 无载荷：本装配器无贡献（不得注入"空标题"噪声）。
            return Task.FromResult<string?>(null);
        }

        var builder = new StringBuilder();
        builder.AppendLine(_untrustedHeader);

        var budget = _totalLength;
        var written = 0;

        // ⚠️ 不用 `foreach (var (k, v) in dict)`：KeyValuePair 的 Deconstruct 扩展在
        // netstandard2.0 目标上不存在（Release 全 TFM 构建会 CS8129/CS8130）。
        foreach (var fact in request.EventFacts)
        {
            if (written == AssemblerBudgets.MaxFactsPerAssembler || budget <= 0)
            {
                builder.AppendLine(AssemblerBudgets.TruncationMarker);
                break;
            }

            if (string.IsNullOrWhiteSpace(fact.Value))
            {
                // 缺字段降级：跳过空值（而不是输出"标签: "这样的半截行）。
                continue;
            }

            var line = $"- {LabelOf(fact.Key)}: {Truncate(RenderValue(fact.Key, fact.Value!), _fieldPreviewLength)}";

            if (line.Length > budget)
            {
                line = line.Substring(0, budget) + AssemblerBudgets.TruncationMarker;
            }

            builder.AppendLine(line);
            budget -= line.Length;
            written++;
        }

        if (written == 0)
        {
            // 全部字段为空 ⇒ 等价于"无载荷"（降级为既有行为，不注入空片段）。
            return Task.FromResult<string?>(null);
        }

        // 下一步建议（"事件已到、该怎么办"写进片段；可空 = 无建议）。
        var hint = _hint;
        if (!string.IsNullOrEmpty(hint) && hint!.Length <= budget)
        {
            builder.AppendLine(hint);
        }

        return Task.FromResult<string?>(builder.ToString().TrimEnd());
    }

    /// <summary>事实键 → 展示名（默认：登记表命中用展示名，否则原样键名）。</summary>
    protected virtual string LabelOf(string key)
        => _factLabels is not null && _factLabels.TryGetValue(key, out var known) ? known : key;

    /// <summary>值渲染钩子（默认原样；产品子类可把平台枚举码翻成展示措辞）。</summary>
    protected virtual string RenderValue(string key, string value) => value;

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";
}
