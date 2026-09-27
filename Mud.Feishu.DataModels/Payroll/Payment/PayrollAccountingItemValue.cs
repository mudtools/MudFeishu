// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 算薪项值（accounting_item_value）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollAccountingItemValue
{
    /// <summary>
    /// <para>算薪项数据原始值；当发薪明细的数据来源为「人工导入」时，如果当前算薪项类型为引用类型，那么算薪项原始值可能为空</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("original_value")]
    public string? OriginalValue { get; set; }

    /// <summary>
    /// <para>引用类型算薪项展示值</para>
    /// </summary>
    [JsonPropertyName("reference_values")]
    public I18nContent[]? ReferenceValues { get; set; }
}
