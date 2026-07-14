// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceGroups;

/// <summary>
/// <para>地址列表（仅追加，不会覆盖之前的列表）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class AttendanceLocation
{
    /// <summary>
    /// <para>地址名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：浙江省杭州市余杭区五常街道木桥头西溪八方城</para>
    /// </summary>
    [JsonPropertyName("location_name")]
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// <para>地址类型</para>
    /// <para>**可选值有：**</para>
    /// <para>* 1：GPS</para>
    /// <para>* 2：Wi-Fi</para>
    /// <para>* 8：IP</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("location_type")]
    public int LocationType { get; set; }

    /// <summary>
    /// <para>地址纬度（需配合gps_range使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：30.28994</para>
    /// </summary>
    [JsonPropertyName("latitude")]
    public float? Latitude { get; set; }

    /// <summary>
    /// <para>地址经度（需配合gps_range使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：120.04509</para>
    /// </summary>
    [JsonPropertyName("longitude")]
    public float? Longitude { get; set; }

    /// <summary>
    /// <para>Wi-Fi 名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：TP-Link-af12ca</para>
    /// </summary>
    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }

    /// <summary>
    /// <para>Wi-Fi 的 MAC 地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：08:00:20:0A:8C:6D</para>
    /// </summary>
    [JsonPropertyName("bssid")]
    public string? Bssid { get; set; }

    /// <summary>
    /// <para>地图类型，1：高德， 2：谷歌</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("map_type")]
    public int? MapType { get; set; }

    /// <summary>
    /// <para>地址名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：北京市海淀区中航广场</para>
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// <para>IP 地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：122.224.123.146</para>
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// <para>额外信息，例如：运营商信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：中国电信</para>
    /// </summary>
    [JsonPropertyName("feature")]
    public string? Feature { get; set; }

    /// <summary>
    /// <para>GPS 打卡的有效范围（历史无效字段）</para>
    /// <para>必填：否</para>
    /// <para>示例值：300</para>
    /// </summary>
    [JsonPropertyName("gps_range")]
    public int? GpsRange { get; set; }
}

/// <summary>
/// <para>地址列表（仅追加，不会覆盖之前的列表）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class AttendanceLocationInfo : AttendanceLocation
{
    /// <summary>
    /// <para>地址 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6921213751454744578</para>
    /// </summary>
    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }
}
