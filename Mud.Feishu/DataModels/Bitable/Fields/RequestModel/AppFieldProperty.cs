// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>字段属性，了解如何填写字段，参考[字段编辑指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-field/guide)。</para>
/// </summary>
public class AppFieldProperty
{
    /// <summary>
    /// <para>单选、多选字段的选项信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public AppTableFieldPropertyOption[]? Options { get; set; }


    /// <summary>
    /// <para>数字和公式字段的显示格式。详情参考[字段编辑指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-field/guide)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("formatter")]
    public string? Formatter { get; set; }

    /// <summary>
    /// <para>日期、创建时间、最后更新时间字段的显示格式。默认为 "yyyy/MM/dd"。枚举值如下所示：</para>
    /// <para>- "yyyy/MM/dd"：2021/1/30</para>
    /// <para>- "yyyy-MM-dd HH:mm"：2021/1/30 14:00</para>
    /// <para>- "MM-dd"：1月30日</para>
    /// <para>- "MM/dd/yyyy"：2021/1/30</para>
    /// <para>- "dd/MM/yyyy"：2021/1/30"</para>
    /// <para>必填：否</para>
    /// <para>示例值：yyyy/MM/dd</para>
    /// </summary>
    [JsonPropertyName("date_formatter")]
    public string? DateFormatter { get; set; }

    /// <summary>
    /// <para>对于新记录，是否自动填写创建时间。默认为 false。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("auto_fill")]
    public bool? AutoFill { get; set; }

    /// <summary>
    /// <para>人员字段中是否允许添加多个成员，或单向关联、双向关联字段中是否允许添加多个记录。默认为 true。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("multiple")]
    public bool? Multiple { get; set; }

    /// <summary>
    /// <para>单向关联、双向关联字段中关联的数据表的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：tblsRc9GRRXKqhvW</para>
    /// </summary>
    [JsonPropertyName("table_id")]
    public string? TableId { get; set; }

    /// <summary>
    /// <para>双向关联字段中，关联的数据表中对应的双向关联字段名称</para>
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
    /// <para>公式字段的表达式。参考[飞书帮助中心文档](https://www.feishu.cn/hc/zh-CN/articles/360049067853-%E5%A4%9A%E7%BB%B4%E8%A1%A8%E6%A0%BC%E5%85%AC%E5%BC%8F%E5%AD%97%E6%AE%B5%E6%A6%82%E8%BF%B0)了解如何设置公式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：bitable::$table[tblNj92WQBAasdEf].$field[fldMV60rYs]*2</para>
    /// </summary>
    [JsonPropertyName("formula_expression")]
    public string? FormulaExpression { get; set; }

    /// <summary>
    /// <para>条码展示类型字段支持的配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allowed_edit_modes")]
    public AppTableFieldPropertyAllowedEditModes? AllowedEditModes { get; set; }

    /// <summary>
    /// <para>进度和评分字段的数据范围最小值。不同字段类型中，该参数的必填属性和取值范围不同，详情参考[字段编辑指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-field/guide)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("min")]
    public float? Min { get; set; }

    /// <summary>
    /// <para>进度和评分字段的数据范围最大值。不同字段类型中，该参数的必填属性和取值范围不同，详情参考[字段编辑指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-field/guide)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("max")]
    public float? Max { get; set; }

    /// <summary>
    /// <para>进度字段是否允许自定义进度条值，默认为 false。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("range_customize")]
    public bool? RangeCustomize { get; set; }

    /// <summary>
    /// <para>货币的具体类型，枚举值如下所示：</para>
    /// <para>- CNY：人民币，货币符号为 ¥</para>
    /// <para>- USD：美元，货币符号为 $</para>
    /// <para>- EUR：欧元，货币符号为 €</para>
    /// <para>- GBP：英镑，货币符号为 £</para>
    /// <para>- AED：阿联酋迪拉姆，货币符号为 dh</para>
    /// <para>- AUD：澳大利亚元，货币符号为 $</para>
    /// <para>- BRL：巴西雷亚尔，货币符号为 R$</para>
    /// <para>- CAD：加拿大元，货币符号为 $</para>
    /// <para>- CHF：瑞士法郎，货币符号为 CHF</para>
    /// <para>- HKD：港元，货币符号为 $</para>
    /// <para>- INR：印度卢比，货币符号为 ₹</para>
    /// <para>- IDR：印尼盾，货币符号为 Rp</para>
    /// <para>- JPY：日元，货币符号为 ¥</para>
    /// <para>- KRW：韩元，货币符号为 ₩</para>
    /// <para>- MOP：澳门元，货币符号为 MOP$</para>
    /// <para>- MXN：墨西哥比索，货币符号为 $</para>
    /// <para>- MYR：马来西亚令吉，货币符号为 RM</para>
    /// <para>- PHP：菲律宾比索，货币符号为 ₱</para>
    /// <para>- PLN：波兰兹罗提，货币符号为 zł</para>
    /// <para>- RUB：俄罗斯卢布，货币符号为 ₽</para>
    /// <para>- SGD：新加坡元，货币符号为 $</para>
    /// <para>- THB：泰国铢，货币符号为 ฿</para>
    /// <para>- TRY：土耳其里拉，货币符号为 ₺</para>
    /// <para>- TWD：新台币，货币符号为 NT$</para>
    /// <para>- VND：越南盾，货币符号为 ₫</para>
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

    /// <summary>
    /// <para>设置公式字段的数据类型</para>
    /// <para>**注意**：非所有多维表格都支持该能力。请参考[获取多维表格元数据](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app/get)接口返回的formula_type 判断，当 `formula_type` 等于 2 时，表示需要设置该字段。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public AppTableFieldPropertyType? Type { get; set; }
}