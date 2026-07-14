// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;


/// <summary>
/// <para>连接</para>
/// </summary>
public class ConnectorAttachedObject
{
    /// <summary>
    /// <para>连接图形的 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：o1:1</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>连接图形的方向</para>
    /// <para>必填：否</para>
    /// <para>示例值：auto</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>auto：连接方向自动匹配</item>
    /// <item>top：连接图形顶部方向</item>
    /// <item>right：连接图形右边方向</item>
    /// <item>bottom：连接图形底部方向</item>
    /// <item>left：连接图形左边方向</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("snap_to")]
    public string? SnapTo { get; set; }

    /// <summary>
    /// <para>连接图形的相对坐标，0-1</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position")]
    public Point? Position { get; set; }
}