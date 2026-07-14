// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>查询结果</para>
/// </summary>
public class WhiteboardNodeInfo
{
    /// <summary>
    /// <para>节点 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：o1:1</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>节点图形类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：composite_shape</para>
    /// <para>可选值：<list type="bullet">
    /// <item>image：图片属性</item>
    /// <item>text_shape：文本</item>
    /// <item>group：组合</item>
    /// <item>composite_shape：基础图形</item>
    /// <item>svg：svg 图形</item>
    /// <item>connector：连线</item>
    /// <item>table：表格</item>
    /// <item>life_line：对象生命线</item>
    /// <item>activation：控制焦点</item>
    /// <item>section：分区</item>
    /// <item>table_uml：类图</item>
    /// <item>table_er：实体关系图</item>
    /// <item>sticky_note：便签</item>
    /// <item>mind_map：思维导图</item>
    /// <item>paint：画笔</item>
    /// <item>combined_fragment：组合片段</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>父节点 id，为空是表示根节点</para>
    /// <para>必填：否</para>
    /// <para>示例值：o1:1</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// <para>子节点</para>
    /// <para>必填：否</para>
    /// <para>最大长度：3000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("children")]
    public string[]? Children { get; set; }

    /// <summary>
    /// <para>图形相对画布的 x 轴位置信息（存在父容器时为相对父容器的坐标，父容器为组合图形 group 时，坐标是穿透的），单位为 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最大值：132070</para>
    /// <para>最小值：-132070</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("x")]
    public double? X { get; set; }

    /// <summary>
    /// <para>图形相对画布的 y 轴位置信息（存在父容器时为相对父容器的坐标，父容器为组合图形 group 时，坐标是穿透的），单位为 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最大值：132070</para>
    /// <para>最小值：-132070</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("y")]
    public double? Y { get; set; }

    /// <summary>
    /// <para>图形旋转角度，单位度</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最大值：180</para>
    /// <para>最小值：-180</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("angle")]
    public double? Angle { get; set; }

    /// <summary>
    /// <para>图形高度，单位为 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最大值：132070</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("height")]
    public double? Height { get; set; }

    /// <summary>
    /// <para>图形内文字</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text")]
    public WhiteboardNodeText? Text { get; set; }

    /// <summary>
    /// <para>图形样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("style")]
    public WhiteboardNodeStyle? Style { get; set; }


    /// <summary>
    /// <para>图片</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("image")]
    public WhiteboardNodeImage? Image { get; set; }

    /// <summary>
    /// <para>基础图形属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("composite_shape")]
    public WhiteboardNodeCompositeShape? CompositeShape { get; set; }

    /// <summary>
    /// <para>连线属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("connector")]
    public ConnectorInfo? Connector { get; set; }

    /// <summary>
    /// <para>图形宽度，单位为 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最大值：132070</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("width")]
    public double? Width { get; set; }

    /// <summary>
    /// <para>分区属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("section")]
    public WhiteboardNodeSection? Section { get; set; }

    /// <summary>
    /// <para>表格属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("table")]
    public TableInfo? Table { get; set; }


    /// <summary>
    /// <para>图形是否锁定</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("locked")]
    public bool? Locked { get; set; }

    /// <summary>
    /// <para>图形在兄弟节点中的层级，层级大的会覆盖层级小的</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：10000</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("z_index")]
    public int? ZIndex { get; set; }

    /// <summary>
    /// <para>生命对象属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("lifeline")]
    public WhiteboardNodeLifeline? Lifeline { get; set; }

    /// <summary>
    /// <para>画笔属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("paint")]
    public WhiteboardNodePaint? Paint { get; set; }

    /// <summary>
    /// <para>svg图形属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("svg")]
    public WhiteboardNodeSvg? Svg { get; set; }

    /// <summary>
    /// <para>便签图形属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sticky_note")]
    public WhiteboardNodeStickyNote? StickyNote { get; set; }


    /// <summary>
    /// <para>思维导图节点属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mind_map_node")]
    public MindMapNodeInfo? MindMapNode { get; set; }



    /// <summary>
    /// <para>思维导图根节点属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mind_map_root")]
    public MindMapRootInfo? MindMapRoot { get; set; }

    /// <summary>
    /// <para>思维导图节点（v1版本，只读，写操作请使用mind_map_root/mind_map_node结构）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mind_map")]
    public MindMapInfo? MindMap { get; set; }
}
