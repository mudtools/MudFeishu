// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试评价（面试评价 v2 新版结构）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewRecordV2
{
    /// <summary>
    /// <para>面试评价 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>面试反馈表 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_form_id")]
    public string? FeedbackFormId { get; set; }

    /// <summary>
    /// <para>提交状态：1 已提交 / 2 未提交</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("commit_status")]
    public int? CommitStatus { get; set; }

    /// <summary>
    /// <para>提交时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("submit_time")]
    public string? SubmitTime { get; set; }

    /// <summary>
    /// <para>总得分</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("record_score")]
    public InterviewRecordScore? RecordScore { get; set; }

    /// <summary>
    /// <para>面试官</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interviewer")]
    public IdNameObject? Interviewer { get; set; }

    /// <summary>
    /// <para>附件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public InterviewRecordAttachmentV2[]? Attachments { get; set; }

    /// <summary>
    /// <para>模块评价列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("module_assessments")]
    public InterviewModuleAssessment[]? ModuleAssessments { get; set; }
}
