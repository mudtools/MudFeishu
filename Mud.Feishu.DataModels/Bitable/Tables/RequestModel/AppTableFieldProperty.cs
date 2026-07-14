// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>字段属性</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppTableFieldProperty
{
    /// <summary>
    /// <para>单选、多选字段的选项信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public AppTableFieldPropertyOption[]? Options { get; set; }


    /// <summary>
    /// <para>数字、公式字段的显示格式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("formatter")]
    public string? Formatter { get; set; }

    /// <summary>
    /// <para>日期、创建时间、最后更新时间字段的显示格式。默认为 "yyyy/MM/dd"。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2021/01/30</para>
    /// </summary>
    [JsonPropertyName("date_formatter")]
    public string? DateFormatter { get; set; }

    /// <summary>
    /// <para>日期字段中新纪录自动填写创建时间。默认为 false</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("auto_fill")]
    public bool? AutoFill { get; set; }

    /// <summary>
    /// <para>人员字段中允许添加多个成员，单向关联、双向关联中允许添加多个记录</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("multiple")]
    public bool? Multiple { get; set; }

    /// <summary>
    /// <para>单向关联、双向关联字段中关联的数据表的id</para>
    /// <para>必填：否</para>
    /// <para>示例值：tblsRc9GRRXKqhvW</para>
    /// </summary>
    [JsonPropertyName("table_id")]
    public string? TableId { get; set; }

    /// <summary>
    /// <para>单向关联、双向关联字段中关联的数据表的名字</para>
    /// <para>必填：否</para>
    /// <para>示例值：table2</para>
    /// </summary>
    [JsonPropertyName("table_name")]
    public string? TableName { get; set; }

    /// <summary>
    /// <para>双向关联字段中关联的数据表中对应的双向关联字段的名字</para>
    /// <para>必填：否</para>
    /// <para>示例值：table1-双向关联</para>
    /// </summary>
    [JsonPropertyName("back_field_name")]
    public string? BackFieldName { get; set; }

    /// <summary>
    /// <para>自动编号类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("auto_serial")]
    public AppFieldPropertyAutoSerial? AutoSerial { get; set; }



    /// <summary>
    /// <para>地理位置输入方式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("location")]
    public AppFieldPropertyLocation? Location { get; set; }

    /// <summary>
    /// <para>公式字段的表达式</para>
    /// <para>必填：否</para>
    /// <para>示例值：bitable::$table[tblNj92WQBAasdEf].$field[fldMV60rYs]*2</para>
    /// </summary>
    [JsonPropertyName("formula_expression")]
    public string? FormulaExpression { get; set; }

    /// <summary>
    /// <para>字段支持的编辑模式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allowed_edit_modes")]
    public AppTableFieldPropertyAllowedEditModes? AllowedEditModes { get; set; }



    /// <summary>
    /// <para>进度、评分等字段的数据范围最小值</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("min")]
    public float? Min { get; set; }

    /// <summary>
    /// <para>进度、评分等字段的数据范围最大值</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("max")]
    public float? Max { get; set; }

    /// <summary>
    /// <para>进度等字段是否支持自定义范围</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("range_customize")]
    public bool? RangeCustomize { get; set; }

    /// <summary>
    /// <para>货币币种</para>
    /// <para>必填：否</para>
    /// <para>示例值：CNY</para>
    /// </summary>
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// <para>评分字段的相关设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rating")]
    public AppTableFieldPropertyRating? Rating { get; set; }


}
