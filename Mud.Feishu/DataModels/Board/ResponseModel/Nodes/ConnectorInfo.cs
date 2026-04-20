// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>连线属性</para>
/// </summary>
public class ConnectorInfo
{
    /// <summary>
    /// <para>开始连接节点信息（兼容线上数据，只读，写操作使用 start 字段，start_object 设置也不会生效）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_object")]
    public ConnectorAttachedObject? StartObject { get; set; }

    /// <summary>
    /// <para>结束连接点信息（兼容线上数据， 只读，写操作使用 end 字段，写入时设置字段也不会生效）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_object")]
    public ConnectorAttachedObject? EndObject { get; set; }

    /// <summary>
    /// <para>连线端点信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start")]
    public WhiteboardNodeConnectorInfo? Start { get; set; }

    /// <summary>
    /// <para>连线端点信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end")]
    public WhiteboardNodeConnectorInfo? End { get; set; }

    /// <summary>
    /// <para>连线文本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("captions")]
    public ConnectorCaption? Captions { get; set; }

    /// <summary>
    /// <para>连线类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：straight</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>straight：直线</item>
    /// <item>polyline：折线</item>
    /// <item>curve：曲线</item>
    /// <item>right_angled_polyline：直角折线</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("shape")]
    public string? Shape { get; set; }

    /// <summary>
    /// <para>连线转向点</para>
    /// <para>必填：否</para>
    /// <para>最大长度：1000000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("turning_points")]
    public Point[]? TurningPoints { get; set; }

    /// <summary>
    /// <para>连线上的文本方向是否自动跟随连线方向</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("caption_auto_direction")]
    public bool? CaptionAutoDirection { get; set; }

    /// <summary>
    /// <para>文本在连线上的相对位置，范围0-1，0表示在连线的起始点，1表示在连线的终点</para>
    /// <para>必填：否</para>
    /// <para>示例值：0.5</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("caption_position")]
    public double? CaptionPosition { get; set; }
}
