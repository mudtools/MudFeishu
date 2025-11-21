// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu;

/// <summary>
/// 消息即飞书聊天中的一条消息。可以使用消息管理 API 对消息进行发送、回复、编辑、撤回、转发以及查询等操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/im-v1/message/intro"/></para>
/// </summary> 
[HttpClientApi(RegistryGroupName = "Message")]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV1Message))]
public interface IFeishuV1MessageApi
{
    /// <summary>
    /// 向指定用户或者群聊发送消息。
    /// <para>支持发送的消息类型包括文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件以及表情包等。</para>
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="sendMessageRequest">发送消息请求体。</param>
    /// <param name="receive_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages")]
    Task<FeishuApiResult<MessageDataResult>> SendMessageAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Body] SendMessageRequest sendMessageRequest,
       [Query("receive_id_type")] string receive_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 回复指定消息。
    /// <para>回复的内容支持文本、富文本、卡片、群名片、个人名片、图片、视频、文件等多种类型。</para>
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="replyMessageRequest">回复消息请体求。</param>
    /// <param name="message_id">待回复的消息的 ID。示例值："om_dc13264520392913993dd051dba21dcf"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/reply")]
    Task<FeishuApiResult<MessageDataResult>> ReplyMessageAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string message_id,
         [Body] ReplyMessageRequest replyMessageRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 编辑已发送的消息内容，支持编辑文本、富文本消息。
    /// <para>如需编辑卡片消息，请使用更新应用发送的消息卡片<see href="https://open.feishu.cn/document/server-docs/im-v1/message-card/patch"/>接口。</para>
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="editMessageRequest">编辑消息请求体。</param>
    /// <param name="message_id">待编辑的消息的 ID。示例值："om_dc13264520392913993dd051dba21dcf"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}")]
    Task<FeishuApiResult<MessageDataResult>> EditMessageAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string message_id,
         [Body] EditMessageRequest editMessageRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 将一条指定的消息转发给用户、群聊或话题。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="message_id">待转发的消息 ID。示例值："om_dc13264520392913993dd051dba21dcf"</param>
    /// <param name="receiveMessageRequest">转发消息请求体。</param>
    /// <param name="receive_id_type">消息接收者 ID 类型。</param>
    /// <param name="uuid">自定义设置的唯一字符串序列，用于在转发消息时请求去重。持有相同 uuid 的请求，在 1 小时内向同一目标的转发只可成功一次。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/forward")]
    Task<FeishuApiResult<ReceiveMessageResult>> ReceiveMessageAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string message_id,
        [Body] ReceiveMessageRequest receiveMessageRequest,
        [Query("receive_id_type")] string receive_id_type = Consts.User_Id_Type,
        [Query("uuid")] string? uuid = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将来自同一个会话内的多条消息，合并转发给指定的用户、群聊或话题。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="mergeReceiveMessageRequest">合并转发消息请求体。</param>
    /// <param name="receive_id_type">消息接收者 ID 类型。</param>
    /// <param name="uuid">自定义设置的唯一字符串序列，用于在转发消息时请求去重。持有相同 uuid 的请求，在 1 小时内向同一目标的转发只可成功一次。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages/merge_forward")]
    Task<FeishuApiResult<MergeReceiveMessageResult>> MergeReceiveMessageAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Body] MergeReceiveMessageRequest mergeReceiveMessageRequest,
        [Query("receive_id_type")] string receive_id_type = Consts.User_Id_Type,
        [Query("uuid")] string? uuid = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将话题转发至指定的用户、群聊或话题。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="thread_id">要转发的话题ID。示例值："omt_dc132645203"</param>
    /// <param name="receiveMessageRequest">转发消息请求体。</param>
    /// <param name="receive_id_type">消息接收者 ID 类型。</param>
    /// <param name="uuid">自定义设置的唯一字符串序列，用于在转发消息时请求去重。持有相同 uuid 的请求，在 1 小时内向同一目标的转发只可成功一次。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/threads/{thread_id}/forward")]
    Task<FeishuApiResult<ThreadResult>> ReceiveThreadsAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string thread_id,
       [Body] ReceiveMessageRequest receiveMessageRequest,
       [Query("receive_id_type")] string receive_id_type = Consts.User_Id_Type,
       [Query("uuid")] string? uuid = null,
       CancellationToken cancellationToken = default);
}
