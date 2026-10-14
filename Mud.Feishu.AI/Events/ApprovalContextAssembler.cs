// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Context;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 审批事件的上下文装配器（R7 / C2 · T3-5）：把审批事件载荷转成<b>结构化 prompt 片段</b>，
/// 让模型"打开事件就知道上下文"，不必先调工具拼装。
/// </summary>
/// <remarks>
/// <para>
/// <b>N4 拆分（E5）</b>：机制（事实排序/空值跳过/逐行扣预算/untrusted 头/截断标记/条数闸）
/// 已上移中立 <see cref="FactListContextAssembler"/>；本类只提供<b>产品事实</b>——
/// 事件族判别键、预算实测值、中文标签映射、下一步建议（工具名是产品词汇）。
/// 行为与拆分前逐字节一致。
/// </para>
/// <para>
/// <b>触发条件</b>：<see cref="ConversationRequest.EventKey"/> 为
/// <see cref="ApprovalEventKey"/>（由 <c>ApprovalTaskConversationalEventHandler</c> 填充）。
/// 其他事件（或无事件载荷）返回 <see langword="null"/>。
/// </para>
/// <para>
/// <b>Order = 200</b>：在发送者信息（10）/ 引用消息（20）/ 知识切片（100）之后——
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
