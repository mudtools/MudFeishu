// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 职位聚合详情（获取职位详情响应体）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class JobDetail
{
    /// <summary>
    /// <para>职位基本信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public JobDetailBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>职位负责人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruiter")]
    public JobUserInfo? Recruiter { get; set; }

    /// <summary>
    /// <para>职位助理列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assistant_list")]
    public JobUserInfo[]? AssistantList { get; set; }

    /// <summary>
    /// <para>职位用人经理列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hiring_manager_list")]
    public JobUserInfo[]? HiringManagerList { get; set; }

    /// <summary>
    /// <para>招聘需求列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_requirement_list")]
    public JobRequirementSimple[]? JobRequirementList { get; set; }

    /// <summary>
    /// <para>职位地址列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address_list")]
    public CommonAddress[]? AddressList { get; set; }

    /// <summary>
    /// <para>职位设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_config")]
    public JobConfigDetail? JobConfig { get; set; }

    /// <summary>
    /// <para>门店列表，仅当 storefront_mode 值为 2 的时候会填充</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("storefront_list")]
    public JobStorefront[]? StorefrontList { get; set; }

    /// <summary>
    /// <para>职位标签列表，根据标签顺序降序排列返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tag_list")]
    public JobDetailTag[]? TagList { get; set; }

    /// <summary>
    /// <para>招聘进展阶段统计数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage_count_list")]
    public StageCountInfo[]? StageCountList { get; set; }
}
