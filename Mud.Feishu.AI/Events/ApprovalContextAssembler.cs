// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 审批事件的上下文装配器（R7 / C2 · T3-5）：把审批事件载荷转成<b>结构化 prompt 片段</b>，
/// 让模型"打开事件就知道上下文"，不必先调工具拼装。
/// </summary>
/// <remarks>
/// <para>
/// <b>触发条件</b>：<see cref="ConversationRequest.EventKey"/> 为
/// <see cref="ApprovalEventKey"/>（由 <c>ApprovalTaskConversationalEventHandler</c> 填充）。
/// 其他事件（或无事件载荷）返回 <see langword="null"/>——「本装配器对本次事件无贡献」，
/// 与既有装配器的空片段语义一致（不污染 prompt）。
/// </para>
/// <para>
/// <b>预算与截断（硬约束）</b>：单字段值 ≤ <see cref="ContextBudgets.ApprovalFieldPreviewLength"/>，
/// 整段 ≤ <see cref="ContextBudgets.ApprovalTotalLength"/>，事实条数 ≤
/// <see cref="ContextBudgets.MaxFactsPerAssembler"/>；超限<b>截断</b>而非整段丢弃
/// （整段丢弃会让"事件触发的提问"彻底失去上下文，比截断更差）。
/// </para>
/// <para>
/// <b>untrusted 标注</b>：事件载荷含平台/用户数据，整段以
/// <see cref="ContextBudgets.UntrustedHeader"/> 开头（防提示注入，与知识切片同款纪律）。
/// </para>
/// <para>
/// <b>失败隔离</b>：本类不抛业务异常；载荷不完整（缺字段）返回 <see langword="null"/>
/// （降级为既有行为），异常隔离由事件层统一承接。
/// </para>
/// <para>
/// <b>Order = 200</b>：在发送者信息（10）/ 引用消息（20）/ 知识切片（100）之后——
/// 事件事实是"本轮问题的背景"，问题文本（<c>MentionedText</c>）在装配器之后拼入。
/// </para>
/// </remarks>
public sealed class ApprovalContextAssembler : IContextAssembler
{
    /// <summary>审批任务事件键（由审批事件处理器写入 <see cref="ConversationRequest.EventKey"/>；取值单一源）。</summary>
    public const string ApprovalEventKey = FeishuEventKeys.ApprovalTask;

    /// <summary>默认装配顺序（知识切片之后）。</summary>
    public const int DefaultOrder = 200;

    /// <summary>事实键 → 展示名（未登记的键原样展示键名——不臆造语义）。</summary>
    private static readonly Dictionary<string, string> FactLabels = new(StringComparer.Ordinal)
    {
        ["instance_code"] = "审批实例",
        ["task_id"] = "审批任务",
        ["status"] = "状态",
        ["approval_code"] = "审批定义",
        ["operator"] = "操作人",
        ["node_name"] = "当前节点",
        ["pending_approvers"] = "待办人",
        ["form_summary"] = "表单要点",
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

        if (!string.Equals(request.EventKey, ApprovalEventKey, StringComparison.Ordinal)
            || request.EventFacts is null
            || request.EventFacts.Count == 0)
        {
            // 非审批事件 / 无载荷：本装配器无贡献（不得注入"空标题"噪声）。
            return Task.FromResult<string?>(null);
        }

        var builder = new StringBuilder();
        builder.AppendLine(ContextBudgets.UntrustedHeader);

        var budget = ContextBudgets.ApprovalTotalLength;
        var written = 0;

        // ⚠️ 不用 `foreach (var (k, v) in dict)`：KeyValuePair 的 Deconstruct 扩展在
        // netstandard2.0 目标上不存在（Release 全 TFM 构建会 CS8129/CS8130）。
        foreach (var fact in request.EventFacts)
        {
            var key = fact.Key;
            var value = fact.Value;

            if (written == ContextBudgets.MaxFactsPerAssembler || budget <= 0)
            {
                builder.AppendLine(ContextBudgets.TruncationMarker);
                break;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                // 缺字段降级：跳过空值（而不是输出"审批实例: "这样的半截行）。
                continue;
            }

            var label = FactLabels.TryGetValue(key, out var known) ? known : key;
            var line = $"- {label}: {Truncate(value!, ContextBudgets.ApprovalFieldPreviewLength)}";

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
            // 全部字段为空 ⇒ 等价于"无载荷"（降级为既有行为，不注入空片段）。
            return Task.FromResult<string?>(null);
        }

        // 下一步建议：把"事件已到、该怎么办"写进片段（T3-5 的交付目标）。
        var hint = "- 建议: 用 approval.get_instance 取详情，或用 approval.list_pending_tasks 看待办。";
        if (hint.Length <= budget)
        {
            builder.AppendLine(hint);
        }

        return Task.FromResult<string?>(builder.ToString().TrimEnd());
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";
}
