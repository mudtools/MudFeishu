// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>智能体会话信息（创建会话/获取指定会话信息响应体）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AgentChatSessionOopsResult
{
    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：conversation_4kj6xxrp2jqk2</para>
    /// </summary>
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// <para>会话名</para>
    /// <para>必填：否</para>
    /// <para>示例值：新会话</para>
    /// <para>最大长度：200</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>会话状态（获取指定会话信息时返回）</para>
    /// <para>必填：否</para>
    /// <para>示例值：opening</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>会话创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1783424638072</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>会话上次对话时间，毫秒时间戳（获取指定会话信息时返回）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1783424638072</para>
    /// </summary>
    [JsonPropertyName("last_chat_at")]
    public string? LastChatAt { get; set; }

    /// <summary>
    /// <para>会话的对话轮次（获取指定会话信息时返回）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("turns")]
    public AgentChatTurn? Turns { get; set; }
}
