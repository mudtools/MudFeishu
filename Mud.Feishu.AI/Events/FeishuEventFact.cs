// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 事件上下文事实（<b>有序键值对</b>）：事件处理器（生产者）→ 上下文装配器（消费者）之间的最小载体。
/// </summary>
/// <param name="Key">事实键（约定见各装配器；未登记的键按原样展示，不臆造语义）。</param>
/// <param name="Value">事实值（可空；空值由装配器跳过——不输出"审批实例: "这样的半截行）。</param>
/// <remarks>
/// <para>
/// <b>为什么是"有序列表"而不是字典</b>：装配结果会进 prompt，<b>顺序必须是确定的</b>；
/// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/> 的枚举顺序在契约上未定义
/// （实现上虽近似插入序，但不得依赖）。列表同时天然表达"同一字段的变更前后配对"
/// （<c>diff.0</c> / <c>diff.1</c>…），字典则需要额外的序号约定。
/// </para>
/// <para>
/// <b>值可能含用户内容</b>（表格字段值、审批表单），故消费它的装配器<b>必须</b>带
/// <see cref="ContextBudgets.UntrustedHeader"/> 标注并逐行扣预算。
/// </para>
/// </remarks>
public readonly record struct FeishuEventFact(string Key, string? Value);

/// <summary>
/// 事件键单一源（<see cref="ConversationRequest.EventKey"/> 的取值）。
/// </summary>
/// <remarks>
/// 生产者（事件处理器）与消费者（上下文装配器）必须引用同一常量——否则"装配器永远不触发"
/// 这类缺陷在编译期完全无声（字符串字面量两处各写一遍）。
/// </remarks>
public static class FeishuEventKeys
{
    /// <summary>审批任务状态变更（<c>approval_task</c>）。</summary>
    public const string ApprovalTask = "approval_task";

    /// <summary>多维表格记录变更（<c>drive.file.bitable_record_changed_v1</c>）。</summary>
    public const string BitableRecordChanged = "bitable_record_changed";
}
