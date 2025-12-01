// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardElements;

/// <summary>
/// 新增卡片组件请求体
/// </summary>
public class CreateCardElementRequest
{
    /// <summary>
    /// <para>添加组件的方式。</para>
    /// <para>必填：是</para>
    /// <para>示例值：insert_after</para>
    /// <para>可选值：<list type="bullet">
    /// <item>insert_before：在目标组件前插入</item>
    /// <item>insert_after：在目标组件后插入</item>
    /// <item>append：在卡片或容器组件末尾添加</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string Type
    { get; set; } = string.Empty;

    /// <summary>
    /// <para>目标组件的 ID。 填写规则如下所示：</para>
    /// <para>- 当 `type` 为 `insert_before`、`insert_after` 时，字段必填，为用于定位的目标组件</para>
    /// <para>- 当 `type` 为 `append` 时，该字段必填且仅支持容器类组件，为用于指定末尾添加的目标组件</para>
    /// <para>- 未填写默认为在卡片 body 末尾添加</para>
    /// <para>必填：否</para>
    /// <para>示例值：markdown_1</para>
    /// </summary>
    [JsonPropertyName("target_element_id")]
    public string? TargetElementId { get; set; }

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
    public
#if NET7_0_OR_GREATER
        required
#endif
        int Sequence
    { get; set; }

    /// <summary>
    /// <para>添加的组件列表。</para>
    /// <para>必填：是</para>
    /// <para>示例值：[{\"tag\":\"button\",\"element_id\":\"button_1\",\"text\":{\"tag\":\"plain_text\",\"content\":\"查看更多\"},\"type\":\"default\",\"width\":\"default\",\"size\":\"medium\",\"behaviors\":[{\"type\":\"open_url\",\"default_url\":\"https://open.feishu.cn/?lang=zh-CN\",\"pc_url\":\"\",\"ios_url\":\"\",\"android_url\":\"\"}]}]</para>
    /// <para>最大长度：1000000</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("elements")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string Elements
    { get; set; } = string.Empty;
}
