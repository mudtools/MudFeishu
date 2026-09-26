// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志的 PC 环境信息（字段键名保留飞书原始大小写，如 IP）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditPcContext
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
    /// <para>操作系统</para>
    /// </summary>
    [JsonPropertyName("os")]
    public string? Os { get; set; }

    /// <summary>
    /// <para>WiFi IP（飞书原始字段键名为 wifip）</para>
    /// </summary>
    [JsonPropertyName("wifip")]
    public string? Wifip { get; set; }

    /// <summary>
    /// <para>地域</para>
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// <para>IP 地址（飞书原始字段键名为 IP）</para>
    /// </summary>
    [JsonPropertyName("IP")]
    public string? Ip { get; set; }
}
