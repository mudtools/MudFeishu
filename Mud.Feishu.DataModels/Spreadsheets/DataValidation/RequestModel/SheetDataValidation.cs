// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 工作表数据验证
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetDataValidation
{
    /// <summary>
    /// <para>下拉列表选项的值。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 单个值需为字符串类型且不能包含 ","</para>
    /// <para>- 单个值的长度不可超过 100 字符</para>
    /// <para>- 选项值的个数不可超过 500 个</para>
    /// <para>**示例值**：["2", "89", "3","2"]</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("conditionValues")]
    public string[] ConditionValues { get; set; } = [];

    /// <summary>
    /// <para>下拉选项其它配置，包括是否支持多选、是否设置下拉选项样式等。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public SheetDataValidationOption? Options { get; set; }


}
