// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 新增应用反馈
/// <para>当应用收到新反馈时，触发该事件</para>
/// <para>事件类型:application.application.feedback.created_v6</para>
/// <para>使用时请继承：<see cref="ApplicationFeedbackCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/application-feedback/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationFeedbackCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationFeedbackCreatedResult : IEventResult
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>被反馈应用 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>反馈提交时间，格式为yyyy-mm-dd hh:mm:ss</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_time")]
    public string? FeedbackTime { get; set; }

    /// <summary>
    /// <para>反馈用户的租户名</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tenant_name")]
    public string? TenantName { get; set; }

    /// <summary>
    /// <para>反馈类型（枚举值，1：故障反馈，2：产品建议）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_type")]
    public int? FeedbackType { get; set; }

    /// <summary>
    /// <para>故障类型列表：1: 黑屏 2: 白屏 3: 无法打开小程序  4: 卡顿 5: 小程序闪退 6: 页面加载慢 7: 死机 8: 其他异常</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fault_type")]
    public int[]? FaultType { get; set; }

    /// <summary>
    /// <para>故障时间，格式为yyyy-mm-dd hh:mm:ss</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fault_time")]
    public string? FaultTime { get; set; }

    /// <summary>
    /// <para>反馈来源：1： 小程序 2：网页应用 3：机器人 4：webSDK</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("source")]
    public int? Source { get; set; }

    /// <summary>
    /// <para>用户填写的联系方式</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>获取用户邮箱信息(contact:user.email:readonly)</para>
    /// <para>获取用户手机号(contact:user.phone:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact")]
    public string? Contact { get; set; }

    /// <summary>
    /// <para>反馈详情</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>反馈图片url列表，url 过期时间三天</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("images")]
    public string[]? Images { get; set; }

    /// <summary>
    /// <para>应用反馈 ID，应用反馈记录唯一标识</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_id")]
    public string? FeedbackId { get; set; }

    /// <summary>
    /// <para>反馈页面路径</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_path")]
    public string? FeedbackPath { get; set; }
}
