// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.IM;

/// <summary>
/// 用户进群事件处理器
/// <para>新用户进群（包含话题群）时触发此事件，在群组内的、已订阅该事件的机器人会收到事件数据。{使用示例}(url=/api/tools/api_explore/api_explore_config?project=im&amp;version=v1&amp;resource=chat.member.user&amp;event=added)</para>
/// <para>事件类型:im.chat.member.user.added_v1</para>
/// <para>使用时请继承：<see cref="ChatMemberUserAddedEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/group/chat-member/event/added"/> </para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ChatMemberUserAdd, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class ChatMemberUserAddedResult : IEventResult
{
    /// <summary>
    /// <para>群组 ID。</para>    
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>操作者的 ID。</para>    
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>是否是外部群</para>    
    /// </summary>
    [JsonPropertyName("external")]
    public bool? External { get; set; }

    /// <summary>
    /// <para>操作者的租户 Key，为租户在飞书上的唯一标识，用来换取对应的 tenant_access_token，也可以用作租户在应用中的唯一标识</para>
    /// </summary>
    [JsonPropertyName("operator_tenant_key")]
    public string? OperatorTenantKey { get; set; }

    /// <summary>
    /// <para>被添加的用户列表</para>    
    /// </summary>
    [JsonPropertyName("users")]
    public ChatMemberUser[]? Users { get; set; }

    /// <summary>
    /// <para>群名称</para>    
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>群国际化名称</para>    
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nNames? I18nNames { get; set; }
}
