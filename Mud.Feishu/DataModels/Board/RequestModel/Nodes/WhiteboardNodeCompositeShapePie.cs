// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>饼图属性，type=pie时需要设置</para>
/// </summary>
public class WhiteboardNodeCompositeShapePie
{
    /// <summary>
    /// <para>开始径向边角度，水平向右x轴正方向为0度，顺时针方向角度值递增，单位度</para>
    /// <para>必填：是</para>
    /// <para>示例值：30.0</para>
    /// <para>最大值：360.0</para>
    /// <para>最小值：0.0</para>
    /// </summary>
    [JsonPropertyName("start_radial_line_angle")]
    public float StartRadialLineAngle { get; set; }

    /// <summary>
    /// <para>圆心角角度，角度方向为始径向边逆时针方向，单位度</para>
    /// <para>必填：是</para>
    /// <para>示例值：40.0</para>
    /// <para>最大值：360.0</para>
    /// <para>最小值：0.0</para>
    /// </summary>
    [JsonPropertyName("central_angle")]
    public float CentralAngle { get; set; }

    /// <summary>
    /// <para>半径长度，单位 px</para>
    /// <para>必填：是</para>
    /// <para>示例值：10</para>
    /// <para>最大值：10000000000</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    /// <summary>
    /// <para>扇区占比，0为一个圆周线，1为一个圆盘</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// <para>默认值：1</para>
    /// </summary>
    [JsonPropertyName("sector_ratio")]
    public float? SectorRatio { get; set; }
}
