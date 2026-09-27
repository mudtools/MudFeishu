// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 关键结果（Key Result）信息
/// </summary>
public class KeyResult
{
    /// <summary>
    /// <para>关键结果 id</para>
    /// <para>示例值：7342342398472398473</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>创建时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>更新时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>关键结果归属者</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public Owner? Owner { get; set; }

    /// <summary>
    /// <para>所属 Objective 的 id</para>
    /// <para>示例值：7342342398472398473</para>
    /// </summary>
    [JsonPropertyName("objective_id")]
    public string? ObjectiveId { get; set; }

    /// <summary>
    /// <para>关键结果序号，从 1 开始</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }

    /// <summary>
    /// <para>关键结果内容（富文本）</para>
    /// </summary>
    [JsonPropertyName("content")]
    public ContentBlockV2? Content { get; set; }

    /// <summary>
    /// <para>关键结果得分，取值范围 [0,1]，保留一位小数</para>
    /// <para>示例值：0.5</para>
    /// </summary>
    [JsonPropertyName("score")]
    public double? Score { get; set; }

    /// <summary>
    /// <para>关键结果在所属 Objective 中的权重，取值范围 [0,1]，保留三位小数</para>
    /// <para>示例值：0.5</para>
    /// </summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    /// <summary>
    /// <para>关键结果截止时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }
}
