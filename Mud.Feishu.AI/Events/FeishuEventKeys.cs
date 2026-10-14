// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 飞书事件族判别键单一源（<c>ConversationRequest.EventKey</c> 的取值）。
/// </summary>
/// <remarks>
/// <para>
/// 生产者（飞书事件处理器）与消费者（上下文装配器）必须引用同一常量——否则
/// "装配器永远不触发"这类缺陷在编译期完全无声（字符串字面量两处各写一遍）。
/// </para>
/// <para>
/// <b>为什么这个类型留在产品侧（N1 决策）</b>：事实载体
/// （<c>Mud.AI.Abstractions.Context.EventFact</c>）已中立化，但本类型的<b>取值</b>
/// （<c>approval_task</c> / <c>bitable_record_changed</c>）是<b>飞书事件族的产品词汇</b>——
/// 换一个生态（企微/钉钉）后这些键必然不同。把它搬进中立包等于把产品事件模型焊进包族底层，
/// 与「共享结构、不共享语义」相悖。
/// </para>
/// </remarks>
public static class FeishuEventKeys
{
    /// <summary>审批任务状态变更（<c>approval_task</c>）。</summary>
    public const string ApprovalTask = "approval_task";

    /// <summary>多维表格记录变更（<c>drive.file.bitable_record_changed_v1</c>）。</summary>
    public const string BitableRecordChanged = "bitable_record_changed";
}
