// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.SecurityCompliance;

/// <summary>
/// SecurityComplianceDeviceChangeEvent
/// <para>变更前数据</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "SecurityCompliance")]
public class SecurityComplianceDeviceChangeEvent
{
    /// <summary>
    /// <para>设备认证编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_record_id")]
    public string? DeviceRecordId { get; set; }

    /// <summary>
    /// <para>版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("current_user_id")]
    public UserIdInfo? CurrentUserId { get; set; }

    /// <summary>
    /// <para>设备名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// <para>设备型号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// <para>操作系统</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：Windows</para>
    /// <para>- 2：macOS</para>
    /// <para>- 3：Linux</para>
    /// <para>- 4：Android</para>
    /// <para>- 5：iOS</para>
    /// <para>- 6：OpenHarmony</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_system")]
    public int? DeviceSystem { get; set; }

    /// <summary>
    /// <para>生产序列号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("serial_number")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// <para>硬盘序列号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("disk_serial_number")]
    public string? DiskSerialNumber { get; set; }

    /// <summary>
    /// <para>主板UUID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// <para>MAC地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mac_address")]
    public string? MacAddress { get; set; }

    /// <summary>
    /// <para>Android标识符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("android_id")]
    public string? AndroidId { get; set; }

    /// <summary>
    /// <para>iOS供应商标识符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("idfv")]
    public string? Idfv { get; set; }

    /// <summary>
    /// <para>Harmony供应商标识符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("aaid")]
    public string? Aaid { get; set; }

    /// <summary>
    /// <para>设备归属</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知设备</para>
    /// <para>- 1：个人设备</para>
    /// <para>- 2：企业设备</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_ownership")]
    public int? DeviceOwnership { get; set; }

    /// <summary>
    /// <para>可信状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知状态</para>
    /// <para>- 1：信任设备</para>
    /// <para>- 2：非信任设备</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_status")]
    public int? DeviceStatus { get; set; }

    /// <summary>
    /// <para>认证方式</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知</para>
    /// <para>- 1：飞书认证</para>
    /// <para>- 2：终端管理认证</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("certification_level")]
    public int? CertificationLevel { get; set; }

    /// <summary>
    /// <para>设备类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知</para>
    /// <para>- 1：移动端</para>
    /// <para>- 2：桌面端</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_terminal_type")]
    public int? DeviceTerminalType { get; set; }

    /// <summary>
    /// <para>是否为受管控设备</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_managed")]
    public bool? IsManaged { get; set; }

    /// <summary>
    /// <para>MDM设备ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mdm_device_id")]
    public string? MdmDeviceId { get; set; }

    /// <summary>
    /// <para>MDM厂商名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mdm_provider_name")]
    public string? MdmProviderName { get; set; }

    /// <summary>
    /// <para>设备环境检测状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知</para>
    /// <para>- 1：安全</para>
    /// <para>- 2：不安全</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_env_detect_status")]
    public int? DeviceEnvDetectStatus { get; set; }

    /// <summary>
    /// <para>是否为公共设备</para>
    /// <para>**默认值**：`false`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_public")]
    public bool? IsPublic { get; set; }
}
