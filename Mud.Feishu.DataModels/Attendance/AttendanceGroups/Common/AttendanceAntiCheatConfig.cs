// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceGroups;

/// <summary>
/// <para>防作弊打卡配置，不传入时默认关闭/不更新（仅灰度租户有效，如需使用请联系技术支持）</para>
/// </summary>
public record AttendanceAntiCheatConfig
{
    /// <summary>
    /// <para>是否拦截疑似作弊打卡，不传入时默认关闭/不更新；关闭时，其余防作弊开关都会关闭</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("intercept_suspected_cheat_punch")]
    public bool InterceptSuspectedCheatPunch { get; set; }

    /// <summary>
    /// <para>是否校验疑似作弊软件打卡，不传入时默认关闭/不更新</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("check_cheat_software_punch")]
    public bool? CheckCheatSoftwarePunch { get; set; }

    /// <summary>
    /// <para>是否校验疑似他人代打卡，不传入时默认关闭/不更新</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("check_buddy_punch")]
    public bool? CheckBuddyPunch { get; set; }

    /// <summary>
    /// <para>是否校验疑似模拟 WI-FI 打卡，不传入时默认关闭/不更新（仅灰度租户有效，如需使用请联系技术支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("check_simulate_wifi_punch")]
    public bool? CheckSimulateWifiPunch { get; set; }

    /// <summary>
    /// <para>是否校验更换设备打卡，不传入时默认关闭/不更新</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("check_change_device_punch")]
    public bool? CheckChangeDevicePunch { get; set; }

    /// <summary>
    /// <para>同一考勤人员最多可绑定打卡设备数量上限，开启校验更换设备打卡时必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：3</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("allow_change_device_num")]
    public int? AllowChangeDeviceNum { get; set; }

    /// <summary>
    /// <para>疑似作弊打卡时的处理方式，开启拦截疑似作弊打卡时必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：2</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：使用人脸识别打卡</item>
    /// <item>2：仅记录疑似作弊信息</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("suspected_cheat_handle_method")]
    public int? SuspectedCheatHandleMethod { get; set; }
}