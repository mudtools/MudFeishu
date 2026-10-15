// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 反馈更新
/// <para>当反馈的处理状态被更新时，触发该事件</para>
/// <para>事件类型:application.application.feedback.updated_v6</para>
/// <para>使用时请继承：<see cref="ApplicationFeedbackUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/application-feedback/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationFeedbackUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationFeedbackUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>反馈id列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_ids")]
    public string[]? FeedbackIds { get; set; }

    /// <summary>
    /// <para>反馈处理状态（枚举值，0: 未处理  1: 已处理  2: 处理中 3: 已关闭）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>应用的 app_id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>反馈处理时间，格式为yyyy-mm-dd hh:mm:ss</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>操作者用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }
}
