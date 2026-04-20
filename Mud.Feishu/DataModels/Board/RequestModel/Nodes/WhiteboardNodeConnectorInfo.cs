// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>连线端点信息</para>
/// </summary>
public class WhiteboardNodeConnectorInfo
{
    /// <summary>
    /// <para>连接图形信息，与position参数二选一，同时设置时attached_object生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attached_object")]
    public ConnectorAttachedObject? AttachedObject { get; set; }

    /// <summary>
    /// <para>连线端点在画布内的坐标，position与attached_object二选一，position与attached_object 同时设置时 attched_object 生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position")]
    public Point? Position { get; set; }

    /// <summary>
    /// <para>连线端点箭头样式</para>
    /// <para>必填：否</para>
    /// <para>示例值：line_arrow</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>none：无箭头样式</item>
    /// <item>line_arrow：线型箭头</item>
    /// <item>triangle_arrow：三角形箭头</item>
    /// <item>empty_triangle_arrow：空心三角形箭头</item>
    /// <item>circle_arrow：圆形箭头</item>
    /// <item>empty_circle_arrow：空心圆形箭头</item>
    /// <item>diamond_arrow：菱形箭头</item>
    /// <item>empty_diamond_arrow：空心菱形箭头</item>
    /// <item>single_arrow：单箭头</item>
    /// <item>multi_arrow：多箭头</item>
    /// <item>exact_single_arrow：精确单箭头</item>
    /// <item>zero_or_multi_arrow：零个或多个箭头</item>
    /// <item>zero_or_single_arrow：零个或单个箭头</item>
    /// <item>single_or_multi_arrow：单个或多个箭头</item>
    /// <item>x_arrow：x型箭头</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("arrow_style")]
    public string? ArrowStyle { get; set; }
}
