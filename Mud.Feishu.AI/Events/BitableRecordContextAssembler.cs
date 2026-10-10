// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 多维表格记录变更事件的上下文装配器（R7 / C2 · T3-5）：把
/// <c>drive.file.bitable_record_changed_v1</c> 载荷转成结构化 prompt 片段——
/// <b>只含变化字段的前后 diff</b>（不含全量字段值），让模型"看到变更就知道发生了什么"。
/// </summary>
/// <remarks>
/// <para>
/// <b>触发条件</b>：<see cref="ConversationRequest.EventKey"/> ==
/// <see cref="FeishuEventKeys.BitableRecordChanged"/>（由
/// <c>BitableRecordChangedConversationalEventHandler</c> 填充）。其他事件返回 <see langword="null"/>。
/// </para>
/// <para>
/// <b>为什么不灌全量字段</b>：一张表的整行可能有几十个字段、字段值又是 JSON 字符串（长文本/附件/人员/关联），
/// 全量灌入会①吃掉 prompt 预算②淹没"到底哪个字段变了"这个唯一有用的信号。故生产者<b>只登记变化字段</b>
/// （见 <see cref="DiffPrefix"/> 约定），装配器再逐行截断。
/// </para>
/// <para>
/// <b>事实键约定</b>（生产者 → 本装配器，单一源为本类的常量）：
/// <list type="bullet">
/// <item><term><see cref="FileTokenKey"/> / <see cref="TableIdKey"/> / <see cref="RevisionKey"/> / <see cref="OperatorKey"/></term>
/// <description>标量事实（多维表格 token / 数据表 / 版本号 / 操作人）。</description></item>
/// <item><term><see cref="ActionPrefix"/>n</term>
/// <description>行操作：值为 <c>&lt;action&gt; &lt;record_id&gt;</c>（如 <c>record_edited recABC</c>），
/// action 的官方取值是 <c>record_added</c>/<c>record_deleted</c>/<c>record_edited</c>。</description></item>
/// <item><term><see cref="DiffPrefix"/>n</term>
/// <description>字段差异：值为 <c>&lt;field_id&gt;: &lt;旧值&gt; → &lt;新值&gt;</c>（空侧渲染为 <see cref="EmptyValueToken"/>）。</description></item>
/// </list>
/// 未登记的键按原样展示键名（不臆造语义）。
/// </para>
/// <para>
/// <b>预算与截断</b>：单行 ≤ <see cref="ContextBudgets.BitableRecordFieldPreviewLength"/>，
/// 整段 ≤ <see cref="ContextBudgets.BitableRecordTotalLength"/>，行数 ≤
/// <see cref="ContextBudgets.MaxFactsPerAssembler"/>；超限<b>截断</b>并留 <see cref="ContextBudgets.TruncationMarker"/>。
/// </para>
/// <para>
/// <b>untrusted 标注</b>：字段值是<b>表格内容</b>（他人可写），整段以
/// <see cref="ContextBudgets.UntrustedHeader"/> 开头（防提示注入）。
/// </para>
/// </remarks>
public sealed class BitableRecordContextAssembler : IContextAssembler
{
    /// <summary>多维表格 token 事实键。</summary>
    public const string FileTokenKey = "file_token";

    /// <summary>数据表 ID 事实键。</summary>
    public const string TableIdKey = "table_id";

    /// <summary>版本号事实键。</summary>
    public const string RevisionKey = "revision";

    /// <summary>操作人事实键。</summary>
    public const string OperatorKey = "operator";

    /// <summary>行操作事实键前缀（<c>action.0</c> / <c>action.1</c>…）。</summary>
    public const string ActionPrefix = "action.";

    /// <summary>字段差异事实键前缀（<c>diff.0</c> / <c>diff.1</c>…）。</summary>
    public const string DiffPrefix = "diff.";

    /// <summary>diff 值里表示"该侧为空"的占位（生产者渲染，装配器只展示）。</summary>
    public const string EmptyValueToken = "(空)";

    /// <summary>默认装配顺序（与审批装配器同级：事件事实是本轮问题的背景）。</summary>
    public const int DefaultOrder = 200;

    /// <summary>事实键 → 展示名（未登记的键原样展示）。</summary>
    private static readonly Dictionary<string, string> FactLabels = new(StringComparer.Ordinal)
    {
        [FileTokenKey] = "多维表格",
        [TableIdKey] = "数据表",
        [RevisionKey] = "版本号",
        [OperatorKey] = "操作人",
    };

    /// <inheritdoc />
    public int Order => DefaultOrder;

    /// <inheritdoc />
    public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Task.FromResult<string?>(null);
        }

        if (!string.Equals(request.EventKey, FeishuEventKeys.BitableRecordChanged, StringComparison.Ordinal)
            || request.EventFacts is null
            || request.EventFacts.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var builder = new StringBuilder();
        builder.AppendLine(ContextBudgets.UntrustedHeader);

        var budget = ContextBudgets.BitableRecordTotalLength;
        var written = 0;

        foreach (var fact in request.EventFacts)
        {
            if (written == ContextBudgets.MaxFactsPerAssembler || budget <= 0)
            {
                builder.AppendLine(ContextBudgets.TruncationMarker);
                break;
            }

            if (string.IsNullOrWhiteSpace(fact.Value))
            {
                // 缺字段降级：跳过空值（不输出"数据表: "这样的半截行）。
                continue;
            }

            var line = $"- {LabelOf(fact.Key)}: {Truncate(RenderValue(fact.Key, fact.Value!), ContextBudgets.BitableRecordFieldPreviewLength)}";

            if (line.Length > budget)
            {
                line = line.Substring(0, budget) + ContextBudgets.TruncationMarker;
            }

            builder.AppendLine(line);
            budget -= line.Length;
            written++;
        }

        if (written == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var hint = "- 建议: 用 bitable.get_records_by_ids 取变更记录详情，或用 bitable.query_records 在该表内检索。";
        if (hint.Length <= budget)
        {
            builder.AppendLine(hint);
        }

        return Task.FromResult<string?>(builder.ToString().TrimEnd());
    }

    private static string LabelOf(string key)
        => FactLabels.TryGetValue(key, out var known)
            ? known
            : key.StartsWith(ActionPrefix, StringComparison.Ordinal) ? "变更"
            : key.StartsWith(DiffPrefix, StringComparison.Ordinal) ? "字段差异"
            : key;

    /// <summary>
    /// 值渲染：把平台枚举码翻成中文（<b>装配层职责</b>——生产者只登记平台事实，不做措辞）。
    /// </summary>
    private static string RenderValue(string key, string value)
    {
        if (!key.StartsWith(ActionPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        // 形如 "record_edited recABC"：只翻第一个词，其余（记录 ID）原样保留。
        var separator = value.IndexOf(' ');
        var action = separator < 0 ? value : value.Substring(0, separator);
        var rest = separator < 0 ? string.Empty : value.Substring(separator);

        var label = action switch
        {
            "record_added" => "新增记录",
            "record_deleted" => "删除记录",
            "record_edited" => "修改记录",
            _ => action,
        };

        return label + rest;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";
}
