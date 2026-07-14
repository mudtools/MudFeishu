// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>思维导图节点属性</para>
/// </summary>
public class WhiteboardNodeMindMapNode
{
    /// <summary>
    /// <para>思维导图节点的父节点，必须为思维导图节点</para>
    /// <para>必填：是</para>
    /// <para>示例值：z1:1</para>
    /// <para>最大长度：10000000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>思维导图节点图形类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：mind_map_text</para>
    /// <para>可选值：<list type="bullet">
    /// <item>mind_map_text：思维导图文本节点类型</item>
    /// <item>mind_map_full_round_rect：思维导图全圆角矩形节点类型</item>
    /// <item>mind_map_round_rect：思维导图矩形节点类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>思维导图节点在兄弟节点中的位置index</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>最大值：10000</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("z_index")]
    public int? ZIndex { get; set; }

    /// <summary>
    /// <para>子节点相对根节点的方向（根节点下的子节点设置才生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：left</para>
    /// <para>可选值：<list type="bullet">
    /// <item>left：思维导图节点在根节点左侧</item>
    /// <item>right：思维导图节点在根节点右侧</item>
    /// <item>up：思维导图节点在根节点上方</item>
    /// <item>down：思维导图节点在根节点下方</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("layout_position")]
    public string? LayoutPosition { get; set; }

    /// <summary>
    /// <para>是否收起子节点</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("collapsed")]
    public bool? Collapsed { get; set; }
}