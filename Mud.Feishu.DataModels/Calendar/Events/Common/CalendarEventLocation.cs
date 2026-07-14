// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// <para>日程地点，不传值则默认为空。</para>
/// </summary>
public class CalendarEventLocation
{
    /// <summary>
    /// <para>地点名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：地点名称</para>
    /// <para>最大长度：512</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>地点地址。</para>
    /// <para>必填：否</para>
    /// <para>示例值：地点地址</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// <para>地点坐标纬度信息。</para>
    /// <para>- 对于国内的地点，采用 GCJ-02 标准。</para>
    /// <para>- 对于海外的地点，采用 WGS84 标准。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1.100000023841858</para>
    /// </summary>
    [JsonPropertyName("latitude")]
    public float? Latitude { get; set; }

    /// <summary>
    /// <para>地点坐标经度信息。</para>
    /// <para>- 对于国内的地点，采用 GCJ-02 标准。</para>
    /// <para>- 对于海外的地点，采用 WGS84 标准。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2.200000047683716</para>
    /// </summary>
    [JsonPropertyName("longitude")]
    public float? Longitude { get; set; }
}