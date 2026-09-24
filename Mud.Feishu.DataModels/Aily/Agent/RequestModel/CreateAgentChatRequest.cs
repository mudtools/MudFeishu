// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 发起智能体对话（Create Agent Chat）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class CreateAgentChatRequest
{
    /// <summary>
    /// <para>用户请求对话内容</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("user_message")]
    public AgentUserMessage UserMessage { get; set; } = new();

    /// <summary>
    /// <para>是否流式输出；为 true 时响应为 Server-sent Events（SSE）流，超时时间 5 分钟</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    /// <summary>
    /// <para>会话 ID；传入既有会话 ID 可复用该会话进行多轮对话</para>
    /// <para>必填：否</para>
    /// <para>示例值：conversation_6521651561561</para>
    /// </summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
}
