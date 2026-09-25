// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试信息（获取面试信息、获取人才面试信息响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewExtend
{
    /// <summary>
    /// <para>面试 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>面试开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("begin_time")]
    public long? BeginTime { get; set; }

    /// <summary>
    /// <para>面试结束时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public long? EndTime { get; set; }

    /// <summary>
    /// <para>面试轮次</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("round")]
    public int? Round { get; set; }

    /// <summary>
    /// <para>面试官评价列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_record_list")]
    public InterviewRecord[]? InterviewRecordList { get; set; }

    /// <summary>
    /// <para>面试评价提交时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_submit_time")]
    public long? FeedbackSubmitTime { get; set; }

    /// <summary>
    /// <para>招聘流程状态 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage_id")]
    public string? StageId { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>招聘流程状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage")]
    public IdNameObject? Stage { get; set; }

    /// <summary>
    /// <para>创建者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public IdNameObject? Creator { get; set; }

    /// <summary>
    /// <para>创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("biz_create_time")]
    public long? BizCreateTime { get; set; }

    /// <summary>
    /// <para>更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("biz_modify_time")]
    public long? BizModifyTime { get; set; }

    /// <summary>
    /// <para>面试状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_round_summary")]
    public int? InterviewRoundSummary { get; set; }

    /// <summary>
    /// <para>面试安排 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_arrangement_id")]
    public string? InterviewArrangementId { get; set; }

    /// <summary>
    /// <para>面试形式：1 现场面试 / 2 电话面试 / 3 视频面试</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_type")]
    public int? InterviewType { get; set; }

    /// <summary>
    /// <para>候选人时区</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_time_zone")]
    public CodeNameObject? TalentTimeZone { get; set; }

    /// <summary>
    /// <para>联系人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact_user")]
    public IdNameObject? ContactUser { get; set; }

    /// <summary>
    /// <para>联系电话</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact_mobile")]
    public string? ContactMobile { get; set; }

    /// <summary>
    /// <para>备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>面试地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address")]
    public InterviewAddress? Address { get; set; }

    /// <summary>
    /// <para>视频面试类型：1 Zoom / 2 牛客技术 / 3 牛客非技术 / 4 赛码 / 5 飞书 / 8 Hackerrank / 9 飞书含代码 / 100 不使用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("video_type")]
    public int? VideoType { get; set; }

    /// <summary>
    /// <para>面试安排状态：1 未开始 / 2 进行中 / 3 已结束</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("arrangement_status")]
    public int? ArrangementStatus { get; set; }

    /// <summary>
    /// <para>面试安排类型：1 社招单面 / 2 集中面试</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("arrangement_type")]
    public int? ArrangementType { get; set; }

    /// <summary>
    /// <para>面试安排方式：1 直接安排 / 2 自助约面</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("arrangement_appointment_kind")]
    public int? ArrangementAppointmentKind { get; set; }

    /// <summary>
    /// <para>会议室列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_room_list")]
    public InterviewMeetingRoom[]? MeetingRoomList { get; set; }

    /// <summary>
    /// <para>面试轮次类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_round_type")]
    public IdNameObject? InterviewRoundType { get; set; }
}
