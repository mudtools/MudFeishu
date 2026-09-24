// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 获取智能体对话结果（Get Agent Chat）响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AgentChatResult
{
    /// <summary>
    /// <para>Agent 回复内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public AgentChatMessage[]? Content { get; set; }

    /// <summary>
    /// <para>对话结束原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：stop</para>
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    /// <summary>
    /// <para>状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：Cancelled</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
