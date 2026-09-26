// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志的 Android 环境信息（字段键名保留飞书原始大小写，如 IP）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditAndroidContext
{
    /// <summary>
    /// <para>设备唯一标识</para>
    /// </summary>
    [JsonPropertyName("udid")]
    public string? Udid { get; set; }

    /// <summary>
    /// <para>设备标识</para>
    /// </summary>
    [JsonPropertyName("did")]
    public string? Did { get; set; }

    /// <summary>
    /// <para>应用版本</para>
    /// </summary>
    [JsonPropertyName("app_ver")]
    public string? AppVer { get; set; }

    /// <summary>
    /// <para>客户端版本</para>
    /// </summary>
    [JsonPropertyName("ver")]
    public string? Ver { get; set; }

    /// <summary>
    /// <para>地域</para>
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// <para>设备标识 I（飞书原始字段键名为 id_i）</para>
    /// </summary>
    [JsonPropertyName("id_i")]
    public string? IdI { get; set; }

    /// <summary>
    /// <para>设备标识 R（飞书原始字段键名为 id_r）</para>
    /// </summary>
    [JsonPropertyName("id_r")]
    public string? IdR { get; set; }

    /// <summary>
    /// <para>硬件品牌</para>
    /// </summary>
    [JsonPropertyName("hw_brand")]
    public string? HwBrand { get; set; }

    /// <summary>
    /// <para>硬件厂商</para>
    /// </summary>
    [JsonPropertyName("hw_manuf")]
    public string? HwManuf { get; set; }

    /// <summary>
    /// <para>WiFi IP（飞书原始字段键名为 wifip）</para>
    /// </summary>
    [JsonPropertyName("wifip")]
    public string? Wifip { get; set; }

    /// <summary>
    /// <para>路由内网 IP</para>
    /// </summary>
    [JsonPropertyName("route_iip")]
    public string? RouteIip { get; set; }

    /// <summary>
    /// <para>路由网关 IP</para>
    /// </summary>
    [JsonPropertyName("route_gip")]
    public string? RouteGip { get; set; }

    /// <summary>
    /// <para>环境 su 权限状态</para>
    /// </summary>
    [JsonPropertyName("env_su")]
    public string? EnvSu { get; set; }

    /// <summary>
    /// <para>环境时区</para>
    /// </summary>
    [JsonPropertyName("env_tz")]
    public string? EnvTz { get; set; }

    /// <summary>
    /// <para>环境语言</para>
    /// </summary>
    [JsonPropertyName("env_ml")]
    public string? EnvMl { get; set; }

    /// <summary>
    /// <para>位置信息</para>
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>
    /// <para>活跃 IP</para>
    /// </summary>
    [JsonPropertyName("active_ip")]
    public string? ActiveIp { get; set; }

    /// <summary>
    /// <para>活跃 IP 详情</para>
    /// </summary>
    [JsonPropertyName("active_ip_detail")]
    public string? ActiveIpDetail { get; set; }

    /// <summary>
    /// <para>基站信息</para>
    /// </summary>
    [JsonPropertyName("cell_base_station")]
    public string? CellBaseStation { get; set; }

    /// <summary>
    /// <para>IP 地址（飞书原始字段键名为 IP）</para>
    /// </summary>
    [JsonPropertyName("IP")]
    public string? Ip { get; set; }
}
