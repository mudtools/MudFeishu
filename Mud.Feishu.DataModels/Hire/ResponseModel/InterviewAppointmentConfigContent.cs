// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 自助约面配置详情（请求与响应共用）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewAppointmentConfigContent
{
    /// <summary>
    /// <para>面试类型：1 现场面试 / 2 视频面试 / 3 电话面试</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_type")]
    public int? InterviewType { get; set; }

    /// <summary>
    /// <para>候选人时区，基于 IANA 标准时区码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_timezone_code")]
    public string? TalentTimezoneCode { get; set; }

    /// <summary>
    /// <para>面试联系人 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact_user_id")]
    public string? ContactUserId { get; set; }

    /// <summary>
    /// <para>面试联系人电话</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact_mobile")]
    public string? ContactMobile { get; set; }

    /// <summary>
    /// <para>面试联系人邮箱</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact_email")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// <para>面试地点 ID，详情请查看获取地址列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address_id")]
    public string? AddressId { get; set; }

    /// <summary>
    /// <para>视频面试类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("video_type")]
    public int? VideoType { get; set; }

    /// <summary>
    /// <para>抄送人 ID 列表，与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cc")]
    public string[]? Cc { get; set; }

    /// <summary>
    /// <para>面试配置备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>面试通知模板 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_notification_template_id")]
    public string? InterviewNotificationTemplateId { get; set; }

    /// <summary>
    /// <para>预邀通知模板 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("appointment_notification_template_id")]
    public string? AppointmentNotificationTemplateId { get; set; }

    /// <summary>
    /// <para>取消面试通知模板 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cancel_interview_notification_template_id")]
    public string? CancelInterviewNotificationTemplateId { get; set; }
}
