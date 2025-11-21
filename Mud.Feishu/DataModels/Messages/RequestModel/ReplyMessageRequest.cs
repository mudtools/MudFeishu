// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 回复消息请体求
/// </summary>
public class ReplyMessageRequest
{
    /// <summary>
    /// 消息内容，JSON 结构序列化后的字符串。
    /// <para>该参数的取值与 msg_type 对应，例如 msg_type 取值为 text，则该参数需要传入文本类型的内容。</para>
    /// </summary>
    [JsonPropertyName("content")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? Content
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
    /// <para> share_chat：分享群名片</para>
    /// <para> share_user：分享个人名片</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? MsgType
    { get; set; }

    /// <summary>
    /// 是否以话题形式回复。取值为 true 时将以话题形式回复。
    /// </summary>
    [JsonPropertyName("reply_in_thread")]
    public bool ReplyInThread { get; set; } = false;

    /// <summary>
    /// 自定义设置的唯一字符串序列，用于在回复消息时请求去重。不填则表示不去重。
    /// <para>持有相同 uuid 的请求，在 1 小时内至多成功回复一条消息。</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }
}