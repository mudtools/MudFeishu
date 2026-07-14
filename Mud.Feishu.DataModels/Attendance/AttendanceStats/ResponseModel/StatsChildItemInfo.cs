// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceStats;

/// <summary>
/// <para>子标题</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class StatsChildItemInfo
{
    /// <summary>
    /// <para>子标题编号</para>
    /// <para>必填：是</para>
    /// <para>示例值：50101</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// <para>开关字段，0：关闭，1：开启</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// <para>子标题名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：工号</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>列类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("column_type")]
    public int? ColumnType { get; set; }

    /// <summary>
    /// <para>是否只读</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("read_only")]
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// <para>最小值</para>
    /// <para>必填：否</para>
    /// <para>示例值：""</para>
    /// </summary>
    [JsonPropertyName("min_value")]
    public string? MinValue { get; set; }

    /// <summary>
    /// <para>最大值</para>
    /// <para>必填：否</para>
    /// <para>示例值：""</para>
    /// </summary>
    [JsonPropertyName("max_value")]
    public string? MaxValue { get; set; }
}
