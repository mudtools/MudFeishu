// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调的展示场景上下文。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardActionContext
{
    /// <summary>
    /// <para>链接地址</para>
    /// <para>链接预览场景返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>链接预览 token</para>
    /// <para>链接预览场景返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("preview_token")]
    public string? PreviewToken { get; set; }

    /// <summary>
    /// <para>消息 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_message_id")]
    public string? OpenMessageId { get; set; }

    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_chat_id")]
    public string? OpenChatId { get; set; }
}
