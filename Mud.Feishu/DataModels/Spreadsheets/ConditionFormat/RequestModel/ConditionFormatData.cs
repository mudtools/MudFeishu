// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>条件格式的详细信息</summary>
public class ConditionFormatData
{
    /// <summary>
    /// <para>条件格式应用的范围，支持以下五种写法。</para>
    /// <para>- `sheetId`：填写工作表 ID，表示将条件格式应用于整表</para>
    /// <para>- `sheetId!{开始行索引}:{结束行索引}`：填写工作表 ID 和行数区间，表示将条件格式应用于整行</para>
    /// <para>- `sheetId!{开始列索引}:{结束列索引}`：填写工作表 ID 和列的区间，表示将条件格式应用于整列</para>
    /// <para>- `sheetId!{开始单元格}:{结束单元格}`：填写工作表 ID 和单元格区间，表示将条件格式应用于单元格选定的区域中</para>
    /// <para>- `sheetId!{开始单元格}:{结束列索引}`：填写工作表 ID、起始单元格和结束列，表示省略结束行，使用表格的最后行作为结束行</para>
    /// <para>**注意**：</para>
    /// <para>- 每个范围的区间不可超过表格的行总数和列总数</para>
    /// <para>- 每个范围的 sheetId 的值必须与 `sheet_id` 参数的值一致</para>
    /// <para>**示例值**：["40a7b0!C3:C3"]</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("ranges")]
    public string[] Ranges { get; set; } = [];

    /// <summary>
    /// <para>创建条件时的规则类型。可选值：</para>
    /// <para>- containsBlanks：为空</para>
    /// <para>- notContainsBlanks：不为空</para>
    /// <para>- duplicateValues：重复值</para>
    /// <para>- uniqueValues：唯一值</para>
    /// <para>- cellIs：限定值范围</para>
    /// <para>- containsText：包含内容</para>
    /// <para>- timePeriod：日期</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("rule_type")]
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// <para>`rule_type` 参数对应的具体属性信息</para>
    /// <para>**注意**：</para>
    /// <para>当 `rule_type` 为 containsBlanks（为空）、notContainsBlanks（不为空）、duplicateValues（重复值）或 uniqueValues（唯一值）时，无需传入 `attrs` 参数。了解更多，参考[条件格式指南](https://open.feishu.cn/document/ukTMukTMukTM/uATMzUjLwEzM14CMxMTN/conditionformat/condition-format-guide)。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("attrs")]
    public ConditionFormatAttr[] Attrs { get; set; } = [];


    /// <summary>
    /// <para>条件格式的样式。支持设置字体样式、文本装饰、字体颜色和背景颜色。</para>
    /// <para>**注意**：</para>
    /// <para>`style` 不可设置为 `""`。默认不传该值，即不设置样式。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("style")]
    public ConditionFormatStyle? Style { get; set; }


}