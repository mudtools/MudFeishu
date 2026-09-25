// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 查询猎头保护期信息响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class SearchAgencyProtectionResult
{
    /// <summary>
    /// <para>是否已入职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_onboarded")]
    public bool? IsOnboarded { get; set; }

    /// <summary>
    /// <para>是否在猎头保护期内入职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboarded_in_protection")]
    public bool? OnboardedInProtection { get; set; }

    /// <summary>
    /// <para>入职时所在保护期，当且仅当 is_onboarded 与 onboarded_in_protection 均为 true 时有值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboarded_protection")]
    public AgencyProtection? OnboardedProtection { get; set; }

    /// <summary>
    /// <para>保护期列表，返回空表明人才没有任何保护期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("protection_list")]
    public AgencyProtection[]? ProtectionList { get; set; }
}
