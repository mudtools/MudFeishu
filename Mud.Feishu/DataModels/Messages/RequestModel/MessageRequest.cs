// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 发送消息请求体。
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 消息接收者的 ID，ID 类型与查询参数 receive_id_type 的取值一致。
    /// <para>示例值："ou_7d8a6e6df7621556ce0d21922b676706ccs"</para>
    /// </summary>
    [JsonPropertyName("receive_id")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? ReceiveId
    { get; set; }

    /// <summary>
    /// 消息类型。
    /// 可选值有：
    /// <para> text：文本</para>
    /// <para> post：富文本</para>
    /// <para> image：图片</para>
    /// <para> file：文件</para>
    /// <para> audio：语音</para>
    /// <para> media：视频</para>
    /// <para> sticker：表情包</para>
    /// <para> interactive：卡片</para>
    /// <para> share_chat：分享群名片（被分享的群名片有效期为 7 天）</para>
    /// <para> share_user：分享个人名片</para>
    /// <para> system：系统消息。该类型仅支持在机器人单聊内推送系统消息，不支持在群聊内使用，例如下图所示突出新会话。</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? MsgType
    { get; set; }

    /// <summary>
    /// 消息内容，JSON 结构序列化后的字符串。
    /// <para>该参数的取值与 msg_type 对应，例如 msg_type 取值为 text，则该参数需要传入文本类型的内容。</para>
    /// <para>如何设置消息内容请参见<see href="https://open.feishu.cn/document/server-docs/im-v1/message-content-description/create_json"/></para>
    /// </summary>
    [JsonPropertyName("content")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? Content
    { get; set; }

    /// <summary>
    /// 自定义设置的唯一字符串序列，用于在发送消息时请求去重。持有相同 uuid 的请求，在 1 小时内至多成功发送一条消息。
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }
}
