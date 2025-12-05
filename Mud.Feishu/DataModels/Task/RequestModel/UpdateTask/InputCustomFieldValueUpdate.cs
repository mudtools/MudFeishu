// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;


/// <summary>
/// <para>自定义字段值。</para>
/// <para>如要更新，每个字段的值根据字段类型填写相应的字段。</para>
/// <para>* 当`type`为"number"时，应使用`number_value`字段，表示数字类型自定义字段的值；</para>
/// <para>* 当`type`为"member"时，应使用`member_value`字段，表示人员类型自定义字段的值；</para>
/// <para>* 当`type`为"datetime"时，应使用`datetime_value`字段，表示日期类型自定义字段的值；</para>
/// <para>* 当`type`为"single_select"时，应使用`single_select_value`字段，表示单选类型自定义字段的值；</para>
/// <para>* 当`type`为"multi_select"时，应使用`multi_select_value`字段，表示多选类型自定义字段的值；</para>
/// <para>* 当`type`为"text"时，应使用`text_value`字段，表示文本类型自定义字段的值。</para>
/// </summary>
public class InputCustomFieldValueUpdate
{
    /// <summary>
    /// <para>自定义字段guid</para>
    /// <para>必填：是</para>
    /// <para>示例值：73b21903-0041-4796-a11e-f8be919a7063</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    /// <summary>
    /// <para>数字类型的自定义字段值，填写一个合法数字的字符串表示，空字符串表示设为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：10.23</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("number_value")]
    public string? NumberValue { get; set; }

    /// <summary>
    /// <para>人员类型的自定义字段值。可以设置1个或多个用户的id（遵循member格式，只支持user类型）。当字段设为只不能多选时只能输入一个值。设为空数组表示设为空。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：50</para>
    /// </summary>
    [JsonPropertyName("member_value")]
    public TaskMemberUpdate[]? MemberValues { get; set; }

    /// <summary>
    /// <para>日期类型自定义字段值，可以输入一个表示日期的以毫秒为单位的字符串。设为空字符串表示设为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1698192000000</para>
    /// </summary>
    [JsonPropertyName("datetime_value")]
    public string? DatetimeValue { get; set; }

    /// <summary>
    /// <para>单选类型字段值，填写一个字段选项的option_guid。设置为空字符串表示设为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：73b21903-0041-4796-a11e-f8be919a7063</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("single_select_value")]
    public string? SingleSelectValue { get; set; }

    /// <summary>
    /// <para>多选类型字段值，可以填写一个或多个本字段的option_guid。设为空数组表示设为空。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：50</para>
    /// </summary>
    [JsonPropertyName("multi_select_value")]
    public string[]? MultiSelectValue { get; set; }

    /// <summary>
    /// <para>文本类型字段值。可以填写最多3000字符。使用空字符串表示设为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：文本类型字段值。可以输入一段文本。空字符串表示清空。</para>
    /// </summary>
    [JsonPropertyName("text_value")]
    public string? TextValue { get; set; }
}