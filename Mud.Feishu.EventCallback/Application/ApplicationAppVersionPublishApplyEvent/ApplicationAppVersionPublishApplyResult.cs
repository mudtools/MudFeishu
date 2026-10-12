// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 申请发布应用
/// <para>通过订阅该事件，可接收应用提交发布申请事件</para>
/// <para>事件类型:application.application.app_version.publish_apply_v6</para>
/// <para>使用时请继承：<see cref="ApplicationAppVersionPublishApplyEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/application-app_version/events/publish_apply</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationAppVersionPublishApply, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppVersionPublishApplyResult : IEventResult
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>当前线上版本信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("online_version")]
    public ApplicationAppVersionEvent? OnlineVersion { get; set; }

    /// <summary>
    /// <para>当前在审核的版本信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("under_audit_version")]
    public ApplicationAppVersionEvent? UnderAuditVersion { get; set; }

    /// <summary>
    /// <para>应用状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：停用状态</para>
    /// <para>- 1：启用状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_status")]
    public int? AppStatus { get; set; }
}
