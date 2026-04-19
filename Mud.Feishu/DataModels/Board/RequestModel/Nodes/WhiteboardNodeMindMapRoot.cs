// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>思维导图根节点属性</para>
/// </summary>
public class WhiteboardNodeMindMapRoot
{
    /// <summary>
    /// <para>思维导图布局方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：left_right</para>
    /// <para>可选值：<list type="bullet">
    /// <item>up_down：上下布局</item>
    /// <item>left_right：左右布局</item>
    /// <item>tree_left：左树布局</item>
    /// <item>tree_right：右树布局</item>
    /// <item>tree_balance：左右交替平衡树布局</item>
    /// <item>vertical_time_line：垂直时间线布局</item>
    /// <item>horizontal_time_line：水平时间线布局</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("layout")]
    public string? Layout { get; set; }

    /// <summary>
    /// <para>思维导图根节点图形类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：mind_map_round_rect</para>
    /// <para>可选值：<list type="bullet">
    /// <item>mind_map_text：思维导图文本节点类型</item>
    /// <item>mind_map_full_round_rect：思维导图全圆角矩形节点类型</item>
    /// <item>mind_map_round_rect：思维导图矩形节点类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>思维导图图形连接线样式</para>
    /// <para>必填：否</para>
    /// <para>示例值：round_angle</para>
    /// <para>可选值：<list type="bullet">
    /// <item>curve：曲线</item>
    /// <item>right_angle：直角折线</item>
    /// <item>round_angle：圆角折线</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("line_style")]
    public string? LineStyle { get; set; }
}