// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 记录，详情与按投递查询共用（offer_type 仅详情接口返回），offer_status 枚举：1 未申请 / 2 审批中 / 3 审批已撤回 / 4 审批通过 / 5 审批不通过 / 6 已发出 / 7 被接受 / 8 被拒绝 / 9 已失效 / 10 未审批或已创建 / 11 实习待入职 / 12 实习已入职 / 13 实习已离职
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class Offer
{
    /// <summary>
    /// <para>Offer ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>Offer 基本信息（响应形态）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public ApplicationOfferBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>薪资计划（响应形态）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("salary_plan")]
    public ApplicationOfferSalaryPlan? SalaryPlan { get; set; }

    /// <summary>
    /// <para>Offer 申请表模板 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schema_id")]
    public string? SchemaId { get; set; }

    /// <summary>
    /// <para>Offer 状态，枚举：1 未申请 / 2 审批中 / 3 审批已撤回 / 4 审批通过 / 5 审批不通过 / 6 已发出 / 7 被接受 / 8 被拒绝 / 9 已失效 / 10 未审批或已创建 / 11 实习待入职 / 12 实习已入职 / 13 实习已离职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_status")]
    public int? OfferStatus { get; set; }

    /// <summary>
    /// <para>Offer 类型：1 正式 Offer，2 实习 Offer（仅详情接口返回）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_type")]
    public int? OfferType { get; set; }

    /// <summary>
    /// <para>职位信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_info")]
    public OfferJobInfo? JobInfo { get; set; }

    /// <summary>
    /// <para>自定义模块列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_module_list")]
    public OfferCustomizedModule[]? CustomizedModuleList { get; set; }

    /// <summary>
    /// <para>招聘需求 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_requirement_id")]
    public string? JobRequirementId { get; set; }

    /// <summary>
    /// <para>Offer 发送记录列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_send_record_list")]
    public OfferSendRecord[]? OfferSendRecordList { get; set; }
}
