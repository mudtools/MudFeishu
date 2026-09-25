// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Minutes;

/// <summary>
/// 创建妙记剪辑请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Minutes")]
public class ClipMinuteRequest
{
    /// <summary>
    /// <para>妙记剪辑时间段列表，长度范围 1～50；每个时间段须大于 1000 毫秒，重叠或相邻区间会自动合并</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("time_ranges")]
    public MinutesTimeRange[] TimeRanges { get; set; } = Array.Empty<MinutesTimeRange>();

    /// <summary>
    /// <para>妙记剪辑标题，留空时自动生成默认标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：妙记剪辑标题</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
