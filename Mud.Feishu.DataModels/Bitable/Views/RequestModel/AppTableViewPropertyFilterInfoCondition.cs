// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>筛选条件集合</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppTableViewPropertyFilterInfoCondition
{
    /// <summary>
    /// <para>用于筛选的字段的 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：fldmeqmpVA</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string FieldId { get; set; } = string.Empty;

    /// <summary>
    /// <para>筛选操作的类型，条件运算符</para>
    /// <para>必填：是</para>
    /// <para>示例值：is</para>
    /// <para>可选值：<list type="bullet">
    /// <item>is：等于</item>
    /// <item>isNot：不等于（不支持日期字段）</item>
    /// <item>contains：包含（不支持日期字段）</item>
    /// <item>doesNotContain：不包含（不支持日期字段）</item>
    /// <item>isEmpty：为空</item>
    /// <item>isNotEmpty：不为空</item>
    /// <item>isGreater：大于</item>
    /// <item>isGreaterEqual：大于等于（不支持日期字段）</item>
    /// <item>isLess：小于</item>
    /// <item>isLessEqual：小于等于（不支持日期字段）</item>
    /// </list></para>
    /// <para>默认值：is</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// <para>条件的值，可以是单个值或多个值的数组。不同字段类型和不同的 operator 可填的值不同。</para>
    /// <para>必填：否</para>
    /// <para>示例值：`[\"text content\"]`</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
