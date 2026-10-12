// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationUserAssignInfo
/// <para>用户席位列表。</para>
/// <para>**字段权限要求**：</para>
/// <para>查询用户席位信息(contact:user.assign_info:read)</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationUserAssignInfo
{
    /// <summary>
    /// <para>席位 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// <para>席位的许可证（license plan key）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("license_plan_key")]
    public string? LicensePlanKey { get; set; }

    /// <summary>
    /// <para>席位名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    /// <summary>
    /// <para>国际化名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public OrganizationProductI18nName? I18nName { get; set; }

    /// <summary>
    /// <para>席位起始时间。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>席位结束时间。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
