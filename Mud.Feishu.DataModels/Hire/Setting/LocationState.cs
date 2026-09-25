// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 省份/州地址码信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class LocationState
{
    /// <summary>
    /// <para>省份/州 Code 码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("state_code")]
    public string? StateCode { get; set; }

    /// <summary>
    /// <para>国家 Code 码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>
    /// <para>省份/州名称信息聚合</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("state_name_info")]
    public LocationNameInfo? StateNameInfo { get; set; }
}
