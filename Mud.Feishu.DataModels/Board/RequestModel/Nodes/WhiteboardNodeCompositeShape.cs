// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>基础图形属性</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Board")]
public class WhiteboardNodeCompositeShape
{
    /// <summary>
    /// <para>基础图形的具体类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：circle</para>
    /// <para>可选值：<list type="bullet">
    /// <item>round_rect2：全圆角矩形</item>
    /// <item>ellipse：圆形</item>
    /// <item>hexagon：六边形</item>
    /// <item>cylinder：圆柱体</item>
    /// <item>parallelogram：平行四边形</item>
    /// <item>trapezoid：梯形</item>
    /// <item>triangle：三角形</item>
    /// <item>round_rect：圆角矩形</item>
    /// <item>step：步骤</item>
    /// <item>diamond：菱形</item>
    /// <item>rect：基础矩形</item>
    /// <item>star：五角星</item>
    /// <item>bubble：气泡</item>
    /// <item>pentagon：五边形</item>
    /// <item>forward_arrow：单向箭头</item>
    /// <item>document_shape：文档图形</item>
    /// <item>condition_shape：组合片段</item>
    /// <item>cloud：云朵</item>
    /// <item>cross：十字形</item>
    /// <item>step2：步骤图形2</item>
    /// <item>predefined_process：预定义流程</item>
    /// <item>delay_shape：延迟图形</item>
    /// <item>off_page_connector：跨页引用</item>
    /// <item>note_shape：注释图形</item>
    /// <item>data_process：数据处理</item>
    /// <item>data_store：数据存储</item>
    /// <item>data_store2：数据存储2</item>
    /// <item>data_store3：数据存储3</item>
    /// <item>star2：爆炸星型</item>
    /// <item>star3：四角形</item>
    /// <item>star4：六角形</item>
    /// <item>actor：角色小人</item>
    /// <item>brace：花括号</item>
    /// <item>condition_shape2：组合片段2</item>
    /// <item>double_arrow：双向箭头</item>
    /// <item>data_flow_round_rect3：数据处理（正方圆角矩形）</item>
    /// <item>rect_bubble：矩形气泡</item>
    /// <item>manual_input：手动输入图形</item>
    /// <item>flow_chart_round_rect：流程图圆角矩形</item>
    /// <item>flow_chart_round_rect2：流程图全圆角矩形</item>
    /// <item>flow_chart_diamond：流程图判定</item>
    /// <item>flow_chart_parallelogram：流程图数据</item>
    /// <item>flow_chart_cylinder：流程图数据库</item>
    /// <item>flow_chart_trapezoid：流程图手动操作</item>
    /// <item>flow_chart_hexagon：流程图准备</item>
    /// <item>data_flow_round_rect：数据流外部实体</item>
    /// <item>data_flow_ellipse：数据流数据处理</item>
    /// <item>backward_arrow：反向箭头（左箭头）</item>
    /// <item>brace_reverse：反向花括号（左括号）</item>
    /// <item>flow_chart_mq：消息队列</item>
    /// <item>horiz_cylinder：水平方向圆柱体</item>
    /// <item>class_interface：类图，接口</item>
    /// <item>classifier：类图，类目</item>
    /// <item>circular_ring：圆环</item>
    /// <item>pie：扇形</item>
    /// <item>right_triangle：直角三角形</item>
    /// <item>octagon：八边形</item>
    /// <item>state_start：状态图，开始</item>
    /// <item>state_end：状态图，结束</item>
    /// <item>state_concurrence：状态图，并发</item>
    /// <item>component_shape：组件</item>
    /// <item>component_shape2：组件2</item>
    /// <item>component_interface：组件，接口</item>
    /// <item>component_required_interface：组件，需求接口</item>
    /// <item>component_assembly：组件，组装</item>
    /// <item>cube：立方体</item>
    /// <item>boundary：边界</item>
    /// <item>control：控制</item>
    /// <item>entity：实体</item>
    /// <item>data_base：数据库</item>
    /// <item>boundary：边界</item>
    /// <item>queue：队列</item>
    /// <item>collection：集合</item>
    /// <item>actor_lifeline：角色生命线</item>
    /// <item>object_lifeline：对象生命线</item>
    /// <item>mind_node_full_round_rect：思维导图全圆角矩形</item>
    /// <item>mind_node_round_rect：思维导图圆角矩形</item>
    /// <item>mind_node_text：思维导图文本图形</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>饼图属性，type=pie时需要设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pie")]
    public WhiteboardNodeCompositeShapePie? Pie { get; set; }


    /// <summary>
    /// <para>圆环属性，type=circular_ring时需要设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("circular_ring")]
    public WhiteboardNodePie? CircularRing { get; set; }


}
