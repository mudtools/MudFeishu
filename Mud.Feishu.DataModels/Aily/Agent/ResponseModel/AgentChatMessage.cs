// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 智能体对话回复消息（Agent Chat Message）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AgentChatMessage
{
    /// <summary>
    /// <para>类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：text</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>文本内容</para>
    /// <para>必填：否</para>
    /// <para>示例值：你好，我是你的专属智能体，请问有什么可以帮你？</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>产物 ID，可通过下载智能体产物接口获取下载地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：3d058789-6952-4697-bf9c-1add1ebc206e</para>
    /// </summary>
    [JsonPropertyName("agent_artifact_id")]
    public string? AgentArtifactId { get; set; }

    /// <summary>
    /// <para>产物类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：sandbox_file</para>
    /// </summary>
    [JsonPropertyName("artifact_type")]
    public string? ArtifactType { get; set; }
}
