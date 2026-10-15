// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Context;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 审批事件的上下文装配器：把审批事件载荷转成<b>结构化 prompt 片段</b>，
/// 让模型"打开事件就知道上下文"，不必先调工具拼装。
/// </summary>
/// <remarks>
/// <b>触发条件</b>：<see cref="ConversationRequest.EventKey"/> 为 <see cref="ApprovalEventKey"/>
/// （由 <c>ApprovalTaskConversationalEventHandler</c> 填充）；其他事件或无载荷返回 <see langword="null"/>
/// ——「本装配器对本次事件无贡献」，与既有装配器的空片段语义一致（不污染 prompt）。
/// <para>
/// <b>预算与截断（硬约束）</b>：单字段值 ≤ <see cref="ContextBudgets.ApprovalFieldPreviewLength"/>、
/// 整段 ≤ <see cref="ContextBudgets.ApprovalTotalLength"/>、事实条数 ≤ <see cref="AssemblerBudgets.MaxFactsPerAssembler"/>；
/// 超限<b>截断</b>而非整段丢弃（整段丢弃会让"事件触发的提问"彻底失去上下文，比截断更差）。
/// </para>
/// <para>
/// <b>untrusted 标注</b>：载荷含平台/用户数据，整段以 <see cref="ContextBudgets.UntrustedHeader"/> 开头
/// 防提示注入（与知识切片同款纪律）。
/// </para>
/// <para>
/// <b>失败隔离与顺序</b>：本类不抛业务异常，载荷缺字段即返回 <see langword="null"/>（降级为既有行为），
/// 异常隔离由事件层统一承接；<c>Order = 200</c> 排在发送者信息（10）/ 引用消息（20）/ 知识切片（100）之后——
/// 事件事实是"本轮问题的背景"，问题文本（<c>MentionedText</c>）在装配器之后拼入。
/// </para>
/// </remarks>
public sealed class ApprovalContextAssembler : FactListContextAssembler
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

    /// <summary>初始化审批事件装配器（产品事实注入中立机制）。</summary>
    public ApprovalContextAssembler()
        : base(
            eventKey: ApprovalEventKey,
            order: DefaultOrder,
            totalLength: ContextBudgets.ApprovalTotalLength,
            fieldPreviewLength: ContextBudgets.ApprovalFieldPreviewLength,
            untrustedHeader: ContextBudgets.UntrustedHeader,
            factLabels: FactLabels,
            hint: "- 建议: 用 approval.get_instance 取详情，或用 approval.list_pending_tasks 看待办。")
    {
    }
}
