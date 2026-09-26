// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// <para>查询设备信息查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class ListDeviceRecordsQuery : IQueryParameter
{
    /// <summary>
    /// <para>分页大小（必填），默认 100，取值范围 1~100</para>
    /// <para>必填：是</para>
    /// <para>示例值：100</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：7394463407091023892</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>设备认证编码</para>
    /// <para>必填：否</para>
    /// <para>示例值：7089353870308032531</para>
    /// </summary>
    public string? DeviceRecordId { get; set; }

    /// <summary>
    /// <para>当前登录用户 ID，ID 类型必须与 <see cref="UserIdType"/> 的取值一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_b25e90585ef8c1adac4b379c2e257906</para>
    /// </summary>
    public string? CurrentUserId { get; set; }

    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id，默认 open_id；取 user_id 时需字段权限 contact:user.employee_id:readonly</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>设备名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Q9C6RYMFDK</para>
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// <para>生产序列号</para>
    /// <para>必填：否</para>
    /// <para>示例值：C02DTHRMML7H</para>
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// <para>硬盘序列号</para>
    /// <para>必填：否</para>
    /// <para>示例值：CC344362-5990-5A68-8DDD-64A23C99FA0C</para>
    /// </summary>
    public string? DiskSerialNumber { get; set; }

    /// <summary>
    /// <para>MAC 地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：ac:de:48:00:11:21</para>
    /// </summary>
    public string? MacAddress { get; set; }

    /// <summary>
    /// <para>Android 标识符</para>
    /// <para>必填：否</para>
    /// <para>示例值：02a11ac4a83b918e</para>
    /// </summary>
    public string? AndroidId { get; set; }

    /// <summary>
    /// <para>主板 UUID</para>
    /// <para>必填：否</para>
    /// <para>示例值：4C4C4544-0052-5A10-804E-B6C04F324433</para>
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// <para>iOS 供应商标识符</para>
    /// <para>必填：否</para>
    /// <para>示例值：968F0E5C-C297-4122-ACB6-102494DEFD9A</para>
    /// </summary>
    public string? Idfv { get; set; }

    /// <summary>
    /// <para>Harmony 供应商标识符</para>
    /// <para>必填：否</para>
    /// <para>示例值：ff3c2237-cd76-4331-9d72-0a4470854567</para>
    /// </summary>
    public string? Aaid { get; set; }

    /// <summary>
    /// <para>设备归属：0（未知设备）/ 1（个人设备）/ 2（企业设备）</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    public int? DeviceOwnership { get; set; }

    /// <summary>
    /// <para>可信状态：0（未知状态）/ 1（信任设备）/ 2（非信任设备）</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    public int? DeviceStatus { get; set; }

    /// <summary>
    /// <para>设备类型：0（未知）/ 1（移动端）/ 2（桌面端）</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    public int? DeviceTerminalType { get; set; }

    /// <summary>
    /// <para>设备操作系统：0（未知）/ 1（Windows）/ 2（macOS）/ 3（Linux）/ 4（Android）/ 5（iOS）/ 6（OpenHarmony）</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    public int? Os { get; set; }

    /// <summary>
    /// <para>最近登录用户 ID，ID 类型必须与 <see cref="UserIdType"/> 的取值一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_b25e90585ef8c1adac4b379c2e257906</para>
    /// </summary>
    public string? LatestUserId { get; set; }

    /// <summary>
    /// <para>设备指纹</para>
    /// <para>必填：否</para>
    /// <para>示例值：7089353870308032531</para>
    /// </summary>
    public string? Did { get; set; }

    /// <summary>
    /// <para>是否为受管控设备</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    public bool? IsManaged { get; set; }

    /// <summary>
    /// <para>MDM 设备 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：123abc</para>
    /// </summary>
    public string? MdmDeviceId { get; set; }

    /// <summary>
    /// <para>MDM 厂商名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Workspace_ONE</para>
    /// </summary>
    public string? MdmProviderName { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (!string.IsNullOrEmpty(DeviceRecordId))
        {
            yield return new KeyValuePair<string, string?>("device_record_id", DeviceRecordId);
        }

        if (!string.IsNullOrEmpty(CurrentUserId))
        {
            yield return new KeyValuePair<string, string?>("current_user_id", CurrentUserId);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }

        if (!string.IsNullOrEmpty(DeviceName))
        {
            yield return new KeyValuePair<string, string?>("device_name", DeviceName);
        }

        if (!string.IsNullOrEmpty(SerialNumber))
        {
            yield return new KeyValuePair<string, string?>("serial_number", SerialNumber);
        }

        if (!string.IsNullOrEmpty(DiskSerialNumber))
        {
            yield return new KeyValuePair<string, string?>("disk_serial_number", DiskSerialNumber);
        }

        if (!string.IsNullOrEmpty(MacAddress))
        {
            yield return new KeyValuePair<string, string?>("mac_address", MacAddress);
        }

        if (!string.IsNullOrEmpty(AndroidId))
        {
            yield return new KeyValuePair<string, string?>("android_id", AndroidId);
        }

        if (!string.IsNullOrEmpty(Uuid))
        {
            yield return new KeyValuePair<string, string?>("uuid", Uuid);
        }

        if (!string.IsNullOrEmpty(Idfv))
        {
            yield return new KeyValuePair<string, string?>("idfv", Idfv);
        }

        if (!string.IsNullOrEmpty(Aaid))
        {
            yield return new KeyValuePair<string, string?>("aaid", Aaid);
        }

        if (DeviceOwnership.HasValue)
        {
            yield return new KeyValuePair<string, string?>("device_ownership", DeviceOwnership.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (DeviceStatus.HasValue)
        {
            yield return new KeyValuePair<string, string?>("device_status", DeviceStatus.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (DeviceTerminalType.HasValue)
        {
            yield return new KeyValuePair<string, string?>("device_terminal_type", DeviceTerminalType.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Os.HasValue)
        {
            yield return new KeyValuePair<string, string?>("os", Os.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(LatestUserId))
        {
            yield return new KeyValuePair<string, string?>("latest_user_id", LatestUserId);
        }

        if (!string.IsNullOrEmpty(Did))
        {
            yield return new KeyValuePair<string, string?>("did", Did);
        }

        if (IsManaged.HasValue)
        {
            yield return new KeyValuePair<string, string?>("is_managed", IsManaged.Value ? "true" : "false");
        }

        if (!string.IsNullOrEmpty(MdmDeviceId))
        {
            yield return new KeyValuePair<string, string?>("mdm_device_id", MdmDeviceId);
        }

        if (!string.IsNullOrEmpty(MdmProviderName))
        {
            yield return new KeyValuePair<string, string?>("mdm_provider_name", MdmProviderName);
        }
    }
}
