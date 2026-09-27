// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 设备记录（device_record）信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityDeviceRecord
{
    /// <summary>
    /// <para>设备认证编码</para>
    /// <para>示例值：7089353870308032531</para>
    /// </summary>
    [JsonPropertyName("device_record_id")]
    public string? DeviceRecordId { get; set; }

    /// <summary>
    /// <para>版本号</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>当前登录用户 ID</para>
    /// <para>示例值：ou_b25e90585ef8c1adac4b379c2e257906</para>
    /// </summary>
    [JsonPropertyName("current_user_id")]
    public string? CurrentUserId { get; set; }

    /// <summary>
    /// <para>设备名称</para>
    /// <para>示例值：Q9C6RYMFDK</para>
    /// </summary>
    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// <para>设备型号</para>
    /// <para>示例值：iPhone13,2</para>
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// <para>操作系统：1（Windows）/ 2（macOS）/ 3（Linux）/ 4（Android）/ 5（iOS）/ 6（OpenHarmony）</para>
    /// </summary>
    [JsonPropertyName("device_system")]
    public int? DeviceSystem { get; set; }

    /// <summary>
    /// <para>生产序列号</para>
    /// <para>示例值：C02DTHRMML7H</para>
    /// </summary>
    [JsonPropertyName("serial_number")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// <para>硬盘序列号</para>
    /// <para>示例值：CC344362-5990-5A68-8DDD-64A23C99FA0C</para>
    /// </summary>
    [JsonPropertyName("disk_serial_number")]
    public string? DiskSerialNumber { get; set; }

    /// <summary>
    /// <para>主板 UUID</para>
    /// <para>示例值：621CDFF0-13D0-5AB1-9ADC-5F560095F6ED</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// <para>MAC 地址</para>
    /// <para>示例值：ac:de:48:00:11:21</para>
    /// </summary>
    [JsonPropertyName("mac_address")]
    public string? MacAddress { get; set; }

    /// <summary>
    /// <para>Android 标识符</para>
    /// <para>示例值：02a11ac4a83b918e</para>
    /// </summary>
    [JsonPropertyName("android_id")]
    public string? AndroidId { get; set; }

    /// <summary>
    /// <para>iOS 供应商标识符</para>
    /// <para>示例值：968F0E5C-C297-4122-ACB6-102494DEFD9A</para>
    /// </summary>
    [JsonPropertyName("idfv")]
    public string? Idfv { get; set; }

    /// <summary>
    /// <para>Harmony 供应商标识符</para>
    /// <para>示例值：ff3c2237-cd76-4331-9d72-0a4470854567</para>
    /// </summary>
    [JsonPropertyName("aaid")]
    public string? Aaid { get; set; }

    /// <summary>
    /// <para>设备归属：0（未知设备）/ 1（个人设备）/ 2（企业设备）</para>
    /// </summary>
    [JsonPropertyName("device_ownership")]
    public int? DeviceOwnership { get; set; }

    /// <summary>
    /// <para>可信状态：0（未知状态）/ 1（信任设备）/ 2（非信任设备）</para>
    /// </summary>
    [JsonPropertyName("device_status")]
    public int? DeviceStatus { get; set; }

    /// <summary>
    /// <para>认证方式：0（未知）/ 1（飞书认证）/ 2（终端管理认证）</para>
    /// </summary>
    [JsonPropertyName("certification_level")]
    public int? CertificationLevel { get; set; }

    /// <summary>
    /// <para>设备类型：0（未知）/ 1（移动端）/ 2（桌面端）</para>
    /// </summary>
    [JsonPropertyName("device_terminal_type")]
    public int? DeviceTerminalType { get; set; }

    /// <summary>
    /// <para>最近登录用户 ID</para>
    /// <para>示例值：ou_b25e90585ef8c1adac4b379c2e257906</para>
    /// </summary>
    [JsonPropertyName("latest_user_id")]
    public string? LatestUserId { get; set; }

    /// <summary>
    /// <para>设备指纹列表</para>
    /// </summary>
    [JsonPropertyName("dids")]
    public string[]? Dids { get; set; }

    /// <summary>
    /// <para>是否为受管控设备</para>
    /// </summary>
    [JsonPropertyName("is_managed")]
    public bool? IsManaged { get; set; }

    /// <summary>
    /// <para>MDM 设备 ID</para>
    /// <para>示例值：123abc</para>
    /// </summary>
    [JsonPropertyName("mdm_device_id")]
    public string? MdmDeviceId { get; set; }

    /// <summary>
    /// <para>MDM 厂商名称</para>
    /// <para>示例值：Workspace_ONE</para>
    /// </summary>
    [JsonPropertyName("mdm_provider_name")]
    public string? MdmProviderName { get; set; }
}
