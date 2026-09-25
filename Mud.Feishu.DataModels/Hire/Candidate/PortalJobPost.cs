// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 内推官网职位（获取内推官网职位列表/详情响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class PortalJobPost
{
    /// <summary>
    /// <para>职位广告 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>标题</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>职位 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    /// <summary>
    /// <para>职位编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_code")]
    public string? JobCode { get; set; }

    /// <summary>
    /// <para>职位过期时间，「null」代表「长期有效」</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_expire_time")]
    public string? JobExpireTime { get; set; }

    /// <summary>
    /// <para>职位状态：1 启用 / 2 停用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_active_status")]
    public int? JobActiveStatus { get; set; }

    /// <summary>
    /// <para>职位流程类型：1 社会招聘 / 2 校园招聘</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_process_type")]
    public int? JobProcessType { get; set; }

    /// <summary>
    /// <para>职位雇佣类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_recruitment_type")]
    public IdNameObject? JobRecruitmentType { get; set; }

    /// <summary>
    /// <para>职位部门</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_department")]
    public IdNameObject? JobDepartment { get; set; }

    /// <summary>
    /// <para>职位类别</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_type")]
    public IdNameObject? JobType { get; set; }

    /// <summary>
    /// <para>最低职级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_job_level")]
    public IdNameObject? MinJobLevel { get; set; }

    /// <summary>
    /// <para>最高职级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_job_level")]
    public IdNameObject? MaxJobLevel { get; set; }

    /// <summary>
    /// <para>职位地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address")]
    public CommonAddress? Address { get; set; }

    /// <summary>
    /// <para>月薪范围-最低薪资</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_salary")]
    public string? MinSalary { get; set; }

    /// <summary>
    /// <para>月薪范围-最高薪资</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_salary")]
    public string? MaxSalary { get; set; }

    /// <summary>
    /// <para>学历要求：1 小学及以上 / 2 初中及以上 / 3 中专及以上 / 4 高中及以上 / 5 大专及以上 / 6 本科及以上 / 7 硕士及以上 / 8 博士及以上 / 20 不限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("required_degree")]
    public int? RequiredDegree { get; set; }

    /// <summary>
    /// <para>经验：1 不限 / 2 应届生 / 3 1 年以下 / 4 1-3 年 / 5 3-5 年 / 6 5-7 年 / 7 7-10 年 / 8 10 年以上</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("experience")]
    public int? Experience { get; set; }

    /// <summary>
    /// <para>数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("headcount")]
    public int? Headcount { get; set; }

    /// <summary>
    /// <para>职位亮点</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("high_light_list")]
    public IdNameObject[]? HighLightList { get; set; }

    /// <summary>
    /// <para>职位描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>职位要求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("requirement")]
    public string? Requirement { get; set; }

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
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>修改时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public string? ModifyTime { get; set; }

    /// <summary>
    /// <para>自定义字段</para>
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
    /// <para>职位地址列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address_list")]
    public CommonAddress[]? AddressList { get; set; }
}
