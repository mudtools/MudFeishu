// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// 工作表数据验证选项
/// </summary>
public class SheetDataValidationOption
{
    /// <summary>
    /// <para>是否支持多选选项。可选值：</para>
    /// <para>- false：不支持多选</para>
    /// <para>- true：支持多选</para>
    /// <para>**默认值**：false，即不支持多选选项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("multipleValues")]
    public bool? MultipleValues { get; set; }

    /// <summary>
    /// <para>是否为下拉选项设置颜色。可选值：</para>
    /// <para>- false：不设置颜色</para>
    /// <para>- true：为下拉选项设置颜色。需进一步配置 colors 参数</para>
    /// <para>**默认值**：false，即不设置颜色</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("highlightValidData")]
    public bool? HighlightValidData { get; set; }

    /// <summary>
    /// <para>指定下拉选项的颜色。格式为 RGB 16 进制，如 "#fffd00"。当 `highlightValidData` 为 true 时，该参数必填。颜色将与 conditionValues 中的值按顺序一一对应。</para>
    /// <para>**示例值**：["#1FB6C1", "#F006C2", "#FB16C3","#FFB6C1"]</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("colors")]
    public string[]? Colors { get; set; }
}