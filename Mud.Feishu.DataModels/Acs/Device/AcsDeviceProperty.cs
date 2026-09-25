// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// 智能门禁设备属性
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsDeviceProperty
{
    /// <summary>
    /// <para>设备版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：2.3.10</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>当前设备人脸数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：300</para>
    /// </summary>
    [JsonPropertyName("current_device_face_count")]
    public int? CurrentDeviceFaceCount { get; set; }

    /// <summary>
    /// <para>设备最大人脸容量</para>
    /// <para>必填：否</para>
    /// <para>示例值：5000</para>
    /// </summary>
    [JsonPropertyName("max_face_capacity")]
    public int? MaxFaceCapacity { get; set; }

    /// <summary>
    /// <para>在线状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("online_status")]
    public int? OnlineStatus { get; set; }

    /// <summary>
    /// <para>设备名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：南门</para>
    /// </summary>
    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// <para>是否是打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_clock_in")]
    public bool? IsClockIn { get; set; }
}
