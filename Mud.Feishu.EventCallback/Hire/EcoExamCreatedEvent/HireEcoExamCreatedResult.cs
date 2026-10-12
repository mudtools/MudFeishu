// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 创建笔试
/// <para>招聘系统客户创建笔试后，系统向对应笔试服务商推送「创建笔试」事件。</para>
/// <para>事件类型:hire.eco_exam.created_v1</para>
/// <para>使用时请继承：<see cref="HireEcoExamCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/hire-v1/ecological-docking/eco_exam/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireEcoExamCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireEcoExamCreatedResult : IEventResult
{
    /// <summary>
    /// <para>笔试 ID，招聘系统内唯一</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("exam_id")]
    public string? ExamId { get; set; }

    /// <summary>
    /// <para>账号 ID，招聘系统内唯一，可通过账号绑定事件获取</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>
    /// <para>试卷 ID，通过创建试卷列表接口传入</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("paper_id")]
    public string? PaperId { get; set; }

    /// <summary>
    /// <para>候选人信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("candidate_info")]
    public HireEcoExamCandidateInfo? CandidateInfo { get; set; }

    /// <summary>
    /// <para>人才 ID。仅灰度租户返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>投递 ID。仅灰度租户返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>职位信息。可选字段，仅灰度租户返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_info")]
    public HireEcoExamJobInfo? JobInfo { get; set; }
}
