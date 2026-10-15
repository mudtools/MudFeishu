// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调的交互信息。
/// </summary>
/// <remarks>
/// <para>各输入字段按交互组件是否嵌入 form 容器区分：</para>
/// <list type="bullet">
/// <item>嵌入 form 容器：提交数据统一在 <see cref="FormValue"/> 中返回，键为组件的「表单项标识」（<c>name</c>）。</item>
/// <item>未嵌入 form 容器：输入框取 <see cref="InputValue"/>；fold button groups / dropdown selections / person select 取
/// <see cref="Option"/>；<c>dropdown-select-multiselect</c> / <c>person-select-multiselect</c> 取 <see cref="Options"/>。</item>
/// </list>
/// </remarks>
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardActionInfo
{
    /// <summary>
    /// <para>开发者绑定到交互组件的自定义数据，对应组件的 <c>value</c> 属性</para>
    /// <para>可为 string 或 object，故以 <see cref="object"/> 承载（反序列化后为 JSON 元素）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// <para>交互组件标签</para>
    /// <para>如 <c>button</c>、<c>select_static</c>、<c>input</c> 等</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>
    /// <para>用户当前区域时区</para>
    /// <para>当用户操作 date picker、time picker、date-time picker 时返回，如 <c>Asia/Shanghai</c></para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// <para>组件自定义唯一标识，用于识别 form 容器内的特定组件</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- form 容器内不可为空或重复，否则客户端报错 200530</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>用户在 form 容器内提交的数据</para>
    /// <para>键为组件的「表单项标识」（<c>name</c>），值为该组件的提交值（字符串或字符串数组）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("form_value")]
    public Dictionary<string, object?>? FormValue { get; set; }

    /// <summary>
    /// <para>未嵌入 form 容器时，输入框组件提交的数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("input_value")]
    public string? InputValue { get; set; }

    /// <summary>
    /// <para>未嵌入 form 容器时，fold button groups、dropdown selections、person select 组件选中选项的回调值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("option")]
    public string? Option { get; set; }

    /// <summary>
    /// <para>未嵌入 form 容器时，<c>dropdown-select-multiselect</c>、<c>person-select-multiselect</c> 组件选中选项的回调值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public string[]? Options { get; set; }
}
