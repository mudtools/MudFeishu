// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>工单消息列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class TicketMessage
{
    /// <summary>
    /// <para>工单消息ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6948728206392295444</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>chat消息ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6949088236610273307</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>消息类型；text：纯文本；post：富文本；image：图像；file：文件；media：视频</para>
    /// <para>必填：是</para>
    /// <para>示例值：text</para>
    /// </summary>
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// <para>创建时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1617960686000</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    /// <summary>
    /// <para>内容</para>
    /// <para>必填：是</para>
    /// <para>示例值：{\"content\":\"进入人工服务。 @李宁 为你提供服务，开始聊起来吧~\",\"msg_type\":\"text\"}</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户名</para>
    /// <para>必填：否</para>
    /// <para>示例值：李宁</para>
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// <para>用户图片url</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://internal-api-lark-file.feishu-boe.cn/static-resource/v1/3e73cdce-54b0-4c6a-8226-b131fb2825dj~?image_size=72x72&amp;cut_type=&amp;quality=&amp;format=image&amp;sticker_format=.webp</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>用户open ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_37019b7c830210acd88fdce886e25c71</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
