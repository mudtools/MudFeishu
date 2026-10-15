// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 应用审核
/// <para>通过订阅该事件，可接收应用审核（通过 / 拒绝）事件</para>
/// <para>事件类型:application.application.app_version.audit_v6</para>
/// <para>使用时请继承：<see cref="ApplicationAppVersionAuditEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/application-app_version/events/audit</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationAppVersionAudit, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppVersionAuditResult : IEventResult
{
    /// <summary>
    /// <para>通过 / 拒绝应用审核的管理员 id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>被审核的应用版本 id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version_id")]
    public string? VersionId { get; set; }

    /// <summary>
    /// <para>应用创建者的 id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public UserIdInfo? CreatorId { get; set; }

    /// <summary>
    /// <para>审核应用的 id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>审核通过 / 拒绝</para>
    /// <para>**可选值有**：</para>
    /// <para>- audited：审核通过</para>
    /// <para>- reject：审核拒绝</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operation")]
    public string? Operation { get; set; }

    /// <summary>
    /// <para>审核信息，当审核拒绝时，管理员填写的拒绝理由</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>应用审核的方式</para>
    /// <para>**可选值有**：</para>
    /// <para>- administrator：管理员审核</para>
    /// <para>- auto：自动免审</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("audit_source")]
    public string? AuditSource { get; set; }
}
