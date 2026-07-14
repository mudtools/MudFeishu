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
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class RecRuleConditionInfo : RecRuleCondition
{

    /// <summary>
    /// <para>字段类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// <para>最大值：2000</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("field_type")]
    public int? FieldType { get; set; }
}
