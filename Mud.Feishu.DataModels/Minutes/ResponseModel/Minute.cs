// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Minutes;

/// <summary>
/// 妙记基本信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Minutes")]
public class Minute
{
    /// <summary>
    /// <para>妙记 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：obcnq3b9jl72l83w4f14xxxx</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>所有者 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_612b787ccd3259fb3c816b3f678dxxxx</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>妙记创建时间戳（ms 级别）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1669098360477</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>妙记标题</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>妙记封面链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cover")]
    public string? Cover { get; set; }

    /// <summary>
    /// <para>妙记时长（ms 级别）</para>
    /// <para>必填：否</para>
    /// <para>示例值：314000</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    /// <summary>
    /// <para>妙记链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>纪要 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7616590025794260496</para>
    /// </summary>
    [JsonPropertyName("note_id")]
    public string? NoteId { get; set; }
}
