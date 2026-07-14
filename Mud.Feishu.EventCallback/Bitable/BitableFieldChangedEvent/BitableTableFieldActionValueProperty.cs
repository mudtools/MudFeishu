// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Bitable;

/// <summary>字段属性</summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class BitableTableFieldActionValueProperty
{
    /// <summary>
    /// <para>数字、公式字段的显示格式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("formatter")]
    public string? Formatter { get; set; }

    /// <summary>
    /// <para>日期、创建时间、最后更新时间字段的显示格式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("date_formatter")]
    public string? DateFormatter { get; set; }

    /// <summary>
    /// <para>日期字段中新纪录自动填写创建时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("auto_fill")]
    public bool? AutoFill { get; set; }

    /// <summary>
    /// <para>人员字段中允许添加多个成员，单向关联、双向关联中允许添加多个记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("multiple")]
    public bool? Multiple { get; set; }

    /// <summary>
    /// <para>单向关联、双向关联字段中关联的数据表的ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("table_id")]
    public string? TableId { get; set; }

    /// <summary>
    /// <para>单向关联、双向关联字段中关联的数据表的名字</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("table_name")]
    public string? TableName { get; set; }

    /// <summary>
    /// <para>双向关联字段中关联的数据表中对应的双向关联字段的名字</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("back_field_name")]
    public string? BackFieldName { get; set; }

    /// <summary>
    /// <para>地理位置输入限制</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("input_type")]
    public string? InputType { get; set; }

    /// <summary>
    /// <para>双向关联字段中关联的数据表中对应的双向关联字段的id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("back_field_id")]
    public string? BackFieldId { get; set; }

    /// <summary>
    /// <para>自动编号类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("auto_serial")]
    public BitableTableFieldActionValuePropertyAutoSerial? AutoSerial { get; set; }


    /// <summary>
    /// <para>单选、多选字段的选项信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public BitableTableFieldActionValuePropertyOption[]? Options { get; set; }

    /// <summary>
    /// <para>公式字段的公式表达式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("formula_expression")]
    public string? FormulaExpression { get; set; }
}
