// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>记录筛选条件，用于指定可阅读的记录。</para>
/// </summary>
public class RecRuleCondition
{
    /// <summary>
    /// <para>条件字段的名称。记录筛选条件是“创建人包含访问者本人”时，此参数值为 ""。</para>
    /// <para>必填：是</para>
    /// <para>示例值：单选</para>
    /// </summary>
    [JsonPropertyName("field_name")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// <para>条件运算符</para>
    /// <para>必填：否</para>
    /// <para>示例值：is</para>
    /// <para>可选值：<list type="bullet">
    /// <item>is：等于</item>
    /// <item>isNot：不等于</item>
    /// <item>contains：包含</item>
    /// <item>doesNotContain：不包含</item>
    /// <item>isEmpty：为空</item>
    /// <item>isNotEmpty：不为空</item>
    /// </list></para>
    /// <para>默认值：is</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    /// <summary>
    /// <para>条件的值，可以是单个值或多个值的数组。详情参考[字段目标值（value）填写说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-record/record-filter-guide#3e0fd644)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：["optbdVHf4q", "optrpd3eIJ"]</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string[]? Value { get; set; }
}