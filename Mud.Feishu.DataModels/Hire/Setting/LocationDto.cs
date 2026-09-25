// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 地址码信息（查询地址响应体）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class LocationDto
{
    /// <summary>
    /// <para>国家信息，仅当 location_type=1 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("country")]
    public LocationCountry? Country { get; set; }

    /// <summary>
    /// <para>省份/州信息，仅当 location_type=2 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("state")]
    public LocationState? State { get; set; }

    /// <summary>
    /// <para>市信息，仅当 location_type=3 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("city")]
    public LocationCity? City { get; set; }

    /// <summary>
    /// <para>区/县信息，仅当 location_type=4 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("district")]
    public LocationDistrict? District { get; set; }
}
