// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// <para>数字类型的字段设置</para>
/// </summary>
public class NumberSettingData
{
    /// <summary>
    /// <para>数字类型的自定义字段的值在App展示的格式。</para>
    /// <para>注意本设置仅影响App中的数字类型字段的字段值的显示格式，并不会影响openapi中输入/输出的字段值的格式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：normal</para>
    /// <para>可选值：<list type="bullet">
    /// <item>normal：常规数字</item>
    /// <item>percentage：百分比格式</item>
    /// <item>cny：人民币格式</item>
    /// <item>usd：美元格式</item>
    /// <item>custom：自定义符号</item>
    /// </list></para>
    /// <para>默认值：normal</para>
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// <para>当`format`设为"custom"时，设置具体的自定义符号。</para>
    /// <para>注意本设置仅影响App中的数字类型字段的字段值的显示格式，并不会影响openapi输入/输出的字段值的格式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：自定义符号</para>
    /// </summary>
    [JsonPropertyName("custom_symbol")]
    public string? CustomSymbol { get; set; }

    /// <summary>
    /// <para>当`format`设为"custom"时，自定义符号相对于数字的显示位置。</para>
    /// <para>注意本设置仅影响App中的数字类型字段的字段值的显示格式，并不会影响openapi输入/输出的字段值的格式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：left</para>
    /// <para>可选值：<list type="bullet">
    /// <item>left：自定义符号显示在数字左边</item>
    /// <item>right：自定义符号显示在数字右边</item>
    /// </list></para>
    /// <para>默认值：right</para>
    /// </summary>
    [JsonPropertyName("custom_symbol_position")]
    public string? CustomSymbolPosition { get; set; }

    /// <summary>
    /// <para>数字类型自定义字段整数部分的分隔符样式。</para>
    /// <para>注意本设置仅影响App中的数字类型字段的字段值的显示格式，并不会影响openapi输入/输出的字段值的格式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：thousand</para>
    /// <para>可选值：<list type="bullet">
    /// <item>none：无分隔符</item>
    /// <item>thousand：千分位分隔符</item>
    /// </list></para>
    /// <para>默认值：none</para>
    /// </summary>
    [JsonPropertyName("separator")]
    public string? Separator { get; set; }

    /// <summary>
    /// <para>数字类型自定义字段的值保留的小数位数。多余的位数将被四舍五入。</para>
    /// <para>默认为0。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>最大值：6</para>
    /// <para>最小值：0</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("decimal_count")]
    public int? DecimalCount { get; set; }
}
