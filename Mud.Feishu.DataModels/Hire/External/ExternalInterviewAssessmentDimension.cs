// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 外部面试评价维度（题目）信息（请求与响应共用）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ExternalInterviewAssessmentDimension
{
    /// <summary>
    /// <para>打分题分数（当题目类型为「打分题」时使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：99</para>
    /// </summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>
    /// <para>单选选项（当题目类型为「单选题」时使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：opt</para>
    /// </summary>
    [JsonPropertyName("option")]
    public string? Option { get; set; }

    /// <summary>
    /// <para>多选选项（当题目类型为「多选题」时使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["opt1"]</para>
    /// </summary>
    [JsonPropertyName("options")]
    public string[]? Options { get; set; }

    /// <summary>
    /// <para>描述内容（当题目类型为「描述题」时使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：content</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>题目类型：1 打分题 / 2 单选题 / 3 描述题 / 4 多选题</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("assessment_type")]
    public int? AssessmentType { get; set; }

    /// <summary>
    /// <para>题目标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：title</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>题目描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：desc</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
