// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 导入 e-HR
/// <para>用户在飞书招聘系统中选择 Offer 并导入 e-HR 系统后触发此事件。注意：该事件仅通知相关 ID 的变更，实际数据需配合其他查询接口获取。</para>
/// <para>事件类型:hire.ehr_import_task.imported_v1</para>
/// <para>使用时请继承：<see cref="HireEhrImportTaskImportedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/onboard/events/imported-2</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireEhrImportTaskImported, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireEhrImportTaskImportedResult : IEventResult
{
    /// <summary>
    /// <para>导入任务 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>投递 ID，需配合「获取投递信息」接口查询详情</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>导入部门 ID。已废弃，请改用 <see cref="EhrDepartment"/> 中的部门数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ehr_department_id")]
    public string? EhrDepartmentId { get; set; }

    /// <summary>
    /// <para>招聘需求 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ehr_requirement_id")]
    public string? EhrRequirementId { get; set; }

    /// <summary>
    /// <para>操作人的飞书招聘用户 ID。已废弃，请改用 <see cref="OperatorUserId"/> 中的用户数据</para>
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
    /// <para>导入部门</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ehr_department")]
    public HireDepartmentIdInfo? EhrDepartment { get; set; }
}
