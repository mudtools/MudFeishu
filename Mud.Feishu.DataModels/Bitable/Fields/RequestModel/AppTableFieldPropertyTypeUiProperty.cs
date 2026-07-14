// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>公式数据属性信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppTableFieldPropertyTypeUiProperty
{
    /// <summary>
    /// <para>货币币种</para>
    /// <para>必填：否</para>
    /// <para>示例值：CNY</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// <para>数字、公式字段的显示格式</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("formatter")]
    public string? Formatter { get; set; }

    /// <summary>
    /// <para>进度等字段是否支持自定义范围</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("range_customize")]
    public bool? RangeCustomize { get; set; }

    /// <summary>
    /// <para>进度、评分等字段的数据范围最小值</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("min")]
    public float? Min { get; set; }

    /// <summary>
    /// <para>进度、评分等字段的数据范围最大值</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最大值：100</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("max")]
    public float? Max { get; set; }

    /// <summary>
    /// <para>日期、创建时间、最后更新时间字段的显示格式</para>
    /// <para>必填：否</para>
    /// <para>示例值：yyyy/MM/dd</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("date_formatter")]
    public string? DateFormatter { get; set; }

    /// <summary>
    /// <para>评分字段的相关设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rating")]
    public AppTableFieldPropertyTypeUiPropertyRating? Rating { get; set; }
}
