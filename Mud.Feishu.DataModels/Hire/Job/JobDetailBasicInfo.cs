// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 职位基本信息（职位聚合详情子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class JobDetailBasicInfo
{
    /// <summary>
    /// <para>职位 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>职位名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>职位描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>职位编号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// <para>职位要求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("requirement")]
    public string? Requirement { get; set; }

    /// <summary>
    /// <para>职位雇佣类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruitment_type")]
    public JobDetailRecruitmentType? RecruitmentType { get; set; }

    /// <summary>
    /// <para>职位部门</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department")]
    public JobDetailDepartment? Department { get; set; }

    /// <summary>
    /// <para>最低职级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_job_level")]
    public JobDetailLevel? MinJobLevel { get; set; }

    /// <summary>
    /// <para>最高职级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_job_level")]
    public JobDetailLevel? MaxJobLevel { get; set; }

    /// <summary>
    /// <para>职位亮点列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("highlight_list")]
    public JobDetailHighlight[]? HighlightList { get; set; }

    /// <summary>
    /// <para>职位序列</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_category")]
    public JobDetailCategory? JobCategory { get; set; }

    /// <summary>
    /// <para>职位类别</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_type")]
    public JobDetailType? JobType { get; set; }

    /// <summary>
    /// <para>启用状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }

    /// <summary>
    /// <para>创建人 ID，与入参 user_id_type 类型一致，若为空则为系统或其他对接系统创建</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>职位流程类型：1 社招流程 / 2 校招流程</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_type")]
    public int? ProcessType { get; set; }

    /// <summary>
    /// <para>职位流程 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>职位流程名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_name")]
    public I18n? ProcessName { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public JobCustomizedData[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>职能分类</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_function")]
    public IdNameObject? JobFunction { get; set; }

    /// <summary>
    /// <para>职位科目</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subject")]
    public IdNameObject? Subject { get; set; }

    /// <summary>
    /// <para>招聘数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("head_count")]
    public int? HeadCount { get; set; }

    /// <summary>
    /// <para>工作年限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("experience")]
    public int? Experience { get; set; }

    /// <summary>
    /// <para>到期日期，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expiry_time")]
    public string? ExpiryTime { get; set; }

    /// <summary>
    /// <para>月薪范围-最低薪资，单位：千</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_salary")]
    public int? MinSalary { get; set; }

    /// <summary>
    /// <para>月薪范围-最高薪资，单位：千</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_salary")]
    public int? MaxSalary { get; set; }

    /// <summary>
    /// <para>学历要求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("required_degree")]
    public int? RequiredDegree { get; set; }

    /// <summary>
    /// <para>工作地点列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("city_list")]
    public CodeNameObject[]? CityList { get; set; }

    /// <summary>
    /// <para>职位属性：1 实体职位 / 2 虚拟职位</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_attribute")]
    public int? JobAttribute { get; set; }

    /// <summary>
    /// <para>目标专业列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_major_list")]
    public JobDetailTargetMajorInfo[]? TargetMajorList { get; set; }

    /// <summary>
    /// <para>标志是否门店职位</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("storefront_mode")]
    public int? StorefrontMode { get; set; }
}
