// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>画笔属性</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Board")]
public class WhiteboardNodePaint
{
    /// <summary>
    /// <para>画笔类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：marker</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>marker：马克笔</item>
    /// <item>highlight：高亮笔</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>画板线段，由系列坐标点表示</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100000000</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("lines")]
    public Point[]? Lines { get; set; }

    /// <summary>
    /// <para>画笔粗细，单位px</para>
    /// <para>必填：否</para>
    /// <para>示例值：7</para>
    /// <para>最大值：23</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// <para>画笔颜色</para>
    /// <para>必填：否</para>
    /// <para>示例值：#ffffff</para>
    /// <para>最大长度：7</para>
    /// <para>最小长度：7</para>
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }
}
