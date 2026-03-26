// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 数据验证信息
/// </summary>
public class SheetDataValidationInfo
{
    /// <summary>
    /// <para>数据验证的类型。下拉列表为 "list"。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dataValidationType")]
    public string? DataValidationType { get; set; }

    /// <summary>
    /// <para>下拉列表选项的值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("conditionValues")]
    public string[]? ConditionValues { get; set; }

    /// <summary>
    /// <para>下拉选项其它配置，包括是否支持多选、是否设置下拉选项样式等。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public SheetDataValidationOptionInfo? Options { get; set; }


}