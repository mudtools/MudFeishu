// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 审批定义更新事件体
/// <para>审批定义的基础信息、表单设计或流程设计等信息发生变更时，触发该事件。</para>
/// <para>事件类型:approval.approval.updated_v4</para>
/// <para>使用时请继承：<see cref="ApprovalApprovalUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/common-event/custom-approval-event</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ApprovalApprovalUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class ApprovalApprovalUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>事件详细数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object")]
    public ObjectSuffix? Object { get; set; }
}
