// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>条件格式的属性</summary>
public class ConditionFormatAttr
{
    /// <summary>
    /// <para>操作方法。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    /// <summary>
    /// <para>时间范围。当 `rule_type` 为 `timePeriod` 时，该参数必填，且 `operator` 参数仅支持 `is`。可选值：</para>
    /// <para>- yesterday：昨天</para>
    /// <para>- today：今天</para>
    /// <para>- tomorrow：明天</para>
    /// <para>- last7Days：最近 7 天</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("time_period")]
    public string? TimePeriod { get; set; }

    /// <summary>
    /// <para>公式。当 `rule_type` 为 `cellIs` 时，该参数必填。</para>
    /// <para>**注意**：</para>
    /// <para>- 当 `operator` 为 `between` 或 `notBetween` 时，需要填写两个元素，其他情况下只需填一个元素，值为用户自定义。</para>
    /// <para>- 填写的值若是数字类型，需填写为如 `"1"` 的格式；若是文本类型，需填写为 `"\"aaaaa\""` 格式。即文本需要用 "" 包裹并转义。了解更多示例，请参考[条件格式指南](https://open.feishu.cn/document/ukTMukTMukTM/uATMzUjLwEzM14CMxMTN/conditionformat/condition-format-guide)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("formula")]
    public string[]? Formula { get; set; }

    /// <summary>
    /// <para>文本。当 `rule_type` 为 `containsText` 时，该参数必填。值为用户自定义。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
