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
public class MindMapRootInfo : WhiteboardNodeMindMapRoot
{

    /// <summary>
    /// <para>思维导图上布局子节点关系树</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("up_children")]
    public string[]? UpChildren { get; set; }

    /// <summary>
    /// <para>思维导图下布局子节点关系树</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("down_children")]
    public string[]? DownChildren { get; set; }

    /// <summary>
    /// <para>思维导图左布局子节点关系树</para>
    /// <para>必填：否</para>
    /// <para>最大长度：1000000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("left_children")]
    public string[]? LeftChildren { get; set; }

    /// <summary>
    /// <para>思维导图右布局子节点关系树</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("right_children")]
    public string[]? RightChildren { get; set; }
}