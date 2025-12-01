// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Cards;

/// <summary>
/// 局部更新卡片实体请求体
/// </summary>
public class PartialUpdateCardRequest
{

    /// <summary>
    /// <para>幂等 ID，可通过传入唯一的 UUID 以保证相同批次的操作只进行一次。</para>
    /// <para>必填：否</para>
    /// <para>示例值：a0d69e20-1dd1-458b-k525-dfeca4015204</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// <para>操作卡片的序号。用于保证多次更新的时序性。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    /// <summary>
    /// <para>操作列表。参考示例更新配置或组件。支持的操作有：</para>
    /// <para>- `partial_update_setting`：更新卡片配置，支持更新卡片的 config 和 card_link 字段。参数结构可参考[更新卡片配置](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/cardkit-v1/card/settings)；</para>
    /// <para>- `add_elements`：添加组件，支持 type、 target_element_id、elements 字段。参数结构可参考[新增组件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/cardkit-v1/card-element/create)接口请求体；</para>
    /// <para>- `delete_elements`：删除组件，支持 element_ids 字段。参数值为组件 ID 数组。参数结构可参考[删除组件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/cardkit-v1/card-element/delete)；</para>
    /// <para>- `partial_update_element`：更新组件的属性，支持 element_id 和 partial_element 字段。参数结构可参考[更新组件属性](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/cardkit-v1/card-element/patch)接口的路径参数 element_id 和请求体 partial_element 字段 ;</para>
    /// <para>- `update_element`：全量更新组件，支持 element_id 和 element 字段。参数结构可参考[全量更新组件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/cardkit-v1/card-element/update)接口的路径参数 element_id 和请求体 element 字段</para>
    /// <para>必填：是</para>
    /// <para>示例值：[{\"action\":\"partial_update_setting\",\"params\":{\"settings\":{\"config\":{\"streaming_mode\":true}}}},{\"action\":\"add_elements\",\"params\":{\"type\":\"insert_before\",\"target_element_id\":\"markdown_1\",\"elements\":[{\"tag\":\"markdown\",\"element_id\":\"md_1\",\"content\":\"欢迎使用[飞书卡片搭建工具](https://open.feishu.cn/cardkit?from=open_docs)。\"}]}},{\"action\":\"delete_elements\",\"params\":{\"element_ids\":[\"text_1\",\"text_2\"]}},{\"action\":\"partial_update_element\",\"params\":{\"element_id\":\"markdown_2\",\"partial_element\":{\"content\":\"详情参考飞书卡片相关文档。\"}}},{\"action\":\"update_element\",\"params\":{\"element_id\":\"markdown_3\",\"element\":{\"tag\":\"button\",\"text\":{\"tag\":\"plain_text\",\"content\":\"有帮助\"},\"size\":\"medium\",\"icon\":{\"tag\":\"standard_icon\",\"token\":\"emoji_outlined\"}}}}]</para>
    /// </summary>
    [JsonPropertyName("actions")]
    public string Actions { get; set; } = string.Empty;
}
