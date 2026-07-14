// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 服务台机器人向工单绑定的群内发送消息请求体
/// </summary>
public class CreateBotMessageRequest
{
    /// <summary>
    /// <para>消息类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：post</para>
    /// <para>可选值：<list type="bullet">
    /// <item>text：普通文本</item>
    /// <item>post：富文本</item>
    /// <item>image：图片</item>
    /// <item>interactive：卡片消息</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public string MsgType { get; set; } = string.Empty;

    /// <summary>
    /// <para>消息内容，json格式结构序列化成string。格式说明参考: [发送消息content说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/im-v1/message/create_json)</para>
    /// <para>必填：是</para>
    /// <para>示例值：{\"post\":{\"zh_cn\":{\"title\":\"sometitle\",\"content\":[[{\"tag\":\"text\",\"text\":\"somecontent\"}]]}}}</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>接收消息用户id</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_7346484524</para>
    /// </summary>
    [JsonPropertyName("receiver_id")]
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// <para>接收消息方式，chat(服务台专属服务群)或user(服务台机器人私聊)。若选择专属服务群，用户有正在处理的工单将会发送失败。默认以chat方式发送。</para>
    /// <para>必填：否</para>
    /// <para>示例值：chat</para>
    /// <para>可选值：<list type="bullet">
    /// <item>chat：通过服务台专属群发送</item>
    /// <item>user：通过服务台机器人私聊发送</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("receive_type")]
    public string? ReceiveType { get; set; }
}
