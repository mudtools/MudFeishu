// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Message;

/// <summary>
/// 撤回消息事件处理器
/// <para>机器人所在会话内的消息被撤回时触发此事件。</para>
/// <para>事件类型:im.message.recalled_v1</para>
/// <para>使用时请继承：<see cref="MessageRecalledEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/im-v1/message/events/recalled"/></para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.MessageRecalled, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class MessageRecalledResult : IEventResult
{
    /// <summary>
    /// <para>被撤回的消息 ID。</para>    
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>消息所在的群组 ID。</para>    
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>撤回的时间，毫秒级时间戳。</para>    
    /// </summary>
    [JsonPropertyName("recall_time")]
    public string? RecallTime { get; set; }

    /// <summary>
    /// <para>撤回类型</para>
    /// <para>**可选值有**：</para>
    /// <para>message_owner:消息发送者撤回,group_owner:群主撤回,group_manager:群管理员撤回,enterprise_manager:企业管理员撤回</para>
    /// <para>可选值：<list type="bullet">
    /// <item>message_owner：消息发送者撤回</item>
    /// <item>group_owner：群主撤回</item>
    /// <item>group_manager：群管理员撤回</item>
    /// <item>enterprise_manager：企业管理员撤回</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("recall_type")]
    public string? RecallType { get; set; }
}
