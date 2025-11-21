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

    /// <summary>
    /// 撤回指定消息。调用接口的身份不同（身份通过 Authorization 请求头参数指定），可实现的效果不同：
    /// <para> 机器人可以撤回该机器人自己发送的消息。</para>
    /// <para> 群聊的群主可以撤回群内指定的消息。</para>
    /// </summary>
    /// <param name="access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="message_id">待撤回的消息 ID。示例值："om_dc13264520392913993dd051dba21dcf"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}")]
    Task<FeishuNullDataApiResult> RevokeMessageAsync(
        [Token(TokenType.Both)][Header("Authorization")] string access_token,
        [Path] string message_id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在最新一条消息下方添加气泡样式的内容，当消息接收者点击气泡或者新消息到达后，气泡消失。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="message_id">机器人发送的消息 ID。示例值："om_dc13264520392913993dd051dba21dcf"</param>
    /// <param name="messageFollowUpRequest">跟随气泡请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/push_follow_up")]
    Task<FeishuNullDataApiResult> CreateMessageFollowUpAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string message_id,
      [Body] MessageFollowUpRequest messageFollowUpRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定消息是否已读。接口只返回已读用户的信息，不返回未读用户的信息。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="message_id">待查询的消息 ID。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/read_users")]
    Task<FeishuApiListResult<ReadMessageUser>> GetMessageReadUsesAsync(
     [Token][Header("Authorization")] string tenant_access_token,
     [Path] string message_id,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定会话（包括单聊、群组）内的历史消息（即聊天记录）。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="container_id_type">容器类型。示例值："chat"，可选值有：
    /// <para>chat：包含单聊（p2p）和群聊（group） </para>
    /// <para>thread：话题</para></param>
    /// <param name="container_id">容器 ID。ID 类型与 container_id_type 取值一致。示例值："oc_234jsi43d3ssi993d43545f"</param>
    /// <param name="start_time">待查询历史信息的起始时间，秒级时间戳。示例值："1608594809"</param>
    /// <param name="end_time">待查询历史信息的结束时间，秒级时间戳。示例值："1609296809"</param>
    /// <param name="sort_type">消息排序方式。示例值："ByCreateTimeAsc"，可选值有：
    /// <para>ByCreateTimeAsc：按消息创建时间升序排列</para>
    /// <para>ByCreateTimeDesc：按消息创建时间降序排列</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/messages")]
    Task<FeishuApiListResult<HistoryMessageData>> GetHistoryMessageAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Query("container_id_type")] string container_id_type,
         [Query("container_id")] string container_id,
         [Query("start_time")] string? start_time = null,
         [Query("end_time")] string? end_time = null,
         [Query("sort_type")] string? sort_type = "ByCreateTimeAsc",
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);
}
