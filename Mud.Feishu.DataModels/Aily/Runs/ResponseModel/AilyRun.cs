// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>运行（Run）信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AilyRun
{
    /// <summary>
    /// <para>运行 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：run_4dfrxvctjqzzj</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：5</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>运行的创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1711975665710</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>应用 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：spring_xxx__c</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：session_4dfunz7sp1g8m</para>
    /// </summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    /// <summary>
    /// <para>运行状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：IN_PROGRESS</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>运行的开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1711975665710</para>
    /// </summary>
    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    /// <summary>
    /// <para>运行的结束时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1711975665710</para>
    /// </summary>
    [JsonPropertyName("ended_at")]
    public string? EndedAt { get; set; }

    /// <summary>
    /// <para>运行失败时的错误信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("error")]
    public AilyRunError? Error { get; set; }

    /// <summary>
    /// <para>其他透传信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}
