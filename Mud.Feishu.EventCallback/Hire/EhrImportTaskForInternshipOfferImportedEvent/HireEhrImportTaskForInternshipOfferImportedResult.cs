// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 实习 Offer 导入 e-HR
/// <para>用户在飞书招聘系统中选择实习 Offer 并导入 e-HR 系统后，候选人信息将推送至订阅系统。</para>
/// <para>事件类型:hire.ehr_import_task_for_internship_offer.imported_v1</para>
/// <para>使用时请继承：<see cref="HireEhrImportTaskForInternshipOfferImportedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/onboard/events/imported</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireEhrImportTaskForInternshipOfferImported, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireEhrImportTaskForInternshipOfferImportedResult : IEventResult
{
    /// <summary>
    /// <para>导入任务 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>Offer ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }

    /// <summary>
    /// <para>实习 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pre_onboard_id")]
    public string? PreOnboardId { get; set; }

    /// <summary>
    /// <para>导入部门 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ehr_department_id")]
    public string? EhrDepartmentId { get; set; }

    /// <summary>
    /// <para>操作人的 SaaS 用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// <para>操作人用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_user_id")]
    public UserIdInfo? OperatorUserId { get; set; }

    /// <summary>
    /// <para>SaaS 部门 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ehr_department")]
    public HireDepartmentIdInfo? EhrDepartment { get; set; }
}
