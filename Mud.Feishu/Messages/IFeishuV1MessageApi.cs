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
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV1MessageService))]
public interface IFeishuV1MessageApi
{
    #region 消息管理
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
    Task<FeishuApiResult<MessageDataResult>?> SendMessageAsync(
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
    Task<FeishuApiResult<MessageDataResult>?> ReplyMessageAsync(
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
    Task<FeishuApiResult<MessageDataResult>?> EditMessageAsync(
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
    Task<FeishuApiResult<ReceiveMessageResult>?> ReceiveMessageAsync(
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
    Task<FeishuApiResult<MergeReceiveMessageResult>?> MergeReceiveMessageAsync(
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
    Task<FeishuApiResult<ThreadResult>?> ReceiveThreadsAsync(
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
    Task<FeishuNullDataApiResult?> RevokeMessageAsync(
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
    Task<FeishuApiPageListResult<ReadMessageUser>> GetMessageReadUsesAsync(
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
    Task<FeishuApiPageListResult<HistoryMessageData>> GetHistoryMessageAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Query("container_id_type")] string container_id_type,
         [Query("container_id")] string container_id,
         [Query("start_time")] string? start_time = null,
         [Query("end_time")] string? end_time = null,
         [Query("sort_type")] string? sort_type = "ByCreateTimeAsc",
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定消息内包含的资源文件，包括音频、视频、图片和文件。成功调用后，返回二进制文件流下载文件。
    /// <para>注意：该函数适用于获取小文件。</para>
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="message_id">待查询的消息 ID。</param>
    /// <param name="file_key">待查询资源的 Key。可以调用<see href="https://open.feishu.cn/document/server-docs/im-v1/message/get">获取指定消息的内容接口</see>，通过消息 ID 获取消息内容中的资源 Key。
    /// <para>示例值："file_456a92d6-c6ea-4de4-ac3f-7afcf44ac78g"</para>
    /// </param>
    /// <param name="type">资源类型.
    /// <para>可选值有：</para>
    /// <para>image：对应消息中的图片或富文本消息中的图片。</para>
    /// <para>file：对应消息中的文件、音频、视频（表情包除外）。</para>
    /// </param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/resources/{file_key}")]
    Task<byte[]> GetMessageFile(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string message_id,
        [Path] string file_key,
        [Query("type")] string type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定消息内包含的资源文件，包括音频、视频、图片和文件。成功调用后，返回二进制文件流下载文件。
    /// <para>注意：该函数适用于获取大文件。</para>
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="message_id">待查询的消息 ID。</param>
    /// <param name="file_key">待查询资源的 Key。可以调用<see cref="GetContentListByMessageIdAsync">获取指定消息的内容接口</see>，通过消息 ID 获取消息内容中的资源 Key。
    /// <para>示例值："file_456a92d6-c6ea-4de4-ac3f-7afcf44ac78g"</para>
    /// </param>
    /// <param name="type">资源类型.
    /// <para>可选值有：</para>
    /// <para>image：对应消息中的图片或富文本消息中的图片。</para>
    /// <para>file：对应消息中的文件、音频、视频（表情包除外）。</para>
    /// </param>
    /// <param name="localFilePath">用于保存获取二进制文件的本地文件路径。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/resources/{file_key}")]
    Task GetMessageLargeFile(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string message_id,
        [Path] string file_key,
        [Query("type")] string type,
        [FilePath] string localFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过消息的 message_id 查询指定消息的内容。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="message_id">待查询的消息 ID。</param>  
    /// <param name="user_id_type">用户 ID 类型，示例值："open_id"，默认值：open_id</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/messages")]
    Task<FeishuApiListResult<MessageContentData>> GetContentListByMessageIdAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string message_id,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
    #endregion

    #region 文件管理
    /// <summary>
    /// 通过已<see cref="UploadFileAsync">上传文件</see>的 Key 下载文件(此函数适应于下载1MB以内的小文件)。
    /// </summary>
    /// <param name="file_key">文件的 Key，通过上传文件接口上传文件后，从返回结果中获取。
    /// <para>示例值："file_456a92d6-c6ea-4de4-ac3f-7afcf44ac78g"。</para>
    /// </param>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/files/{file_key}")]
    Task<byte[]?> DownFileAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string file_key,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过已<see cref="UploadFileAsync">上传文件</see>的 Key 下载文件(此函数适应于下载大于1MB的大文件)。
    /// </summary>
    /// <param name="file_key">文件的 Key，通过上传文件接口上传文件后，从返回结果中获取。
    /// <para>示例值："file_456a92d6-c6ea-4de4-ac3f-7afcf44ac78g"。</para>
    /// </param>
    /// <param name="localFile">保存至本地的文件全路径。</param>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/files/{file_key}")]
    Task DownLargeFileAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string file_key,
       [FilePath] string localFile,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过已<see cref="UploadImageAsync">上传图片</see>的 Key 值下载图片。(此函数适应于下载1MB以内的小文件)。
    /// </summary>
    /// <param name="image_key">图片的 Key，通过上传图片接口上传图片后，在返回结果中获取。
    /// <para>示例值："img_8d5181ca-0aed-40f0-b0d1-b1452132afbg"。</para>
    /// </param>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/images/{image_key}")]
    Task<byte[]?> DownImageAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string image_key,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过已<see cref="UploadImageAsync">上传图片</see>的 Key 值下载图片。(此函数适应于下载1MB以内的小文件)。
    /// </summary>
    /// <param name="image_key">图片的 Key，通过上传图片接口上传图片后，在返回结果中获取。
    /// <para>示例值："img_8d5181ca-0aed-40f0-b0d1-b1452132afbg"。</para>
    /// </param>
    /// <param name="localFile">保存至本地的文件全路径。</param>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/images/{image_key}")]
    Task DownLargeImageAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string image_key,
       [FilePath] string localFile,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将本地文件上传至开放平台，支持上传音频、视频、文档等文件类型。
    /// <para>上传后接口会返回文件的 Key，使用该 Key 值可以调用其他 OpenAPI。例如，调用发送消息接口，发送文件。</para>
    /// </summary>
    /// <param name="uploadFileRequest">文件上传请求体。</param>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/files")]
    [IgnoreImplement]
    Task<FeishuApiResult<FileUploadResult>?> UploadFileAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Body] UploadFileRequest uploadFileRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将图片上传至飞书开放平台，支持上传 JPG、JPEG、PNG、WEBP、GIF、BMP、ICO、TIFF、HEIC 格式的图片，但需要注意 TIFF、HEIC 上传后会被转为 JPG 格式。
    /// </summary>
    /// <param name="uploadImageRequest">文件图片请求体。</param>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/images")]
    [IgnoreImplement]
    Task<FeishuApiResult<ImageUpdateResult>?> UploadImageAsync(
     [Token][Header("Authorization")] string tenant_access_token,
     [Body] UploadImageRequest uploadImageRequest,
     CancellationToken cancellationToken = default);

    #endregion

    #region 消息加急
    /// <summary>
    /// 把指定消息加急给目标用户，加急仅在飞书客户端内通知。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="sendMessageRequest">消息加急请求体。</param>
    /// <param name="message_id">待加急的消息 ID。</param>
    /// <param name="user_id_type">用户 ID 类型 示例值："open_id"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/urgent_app")]
    Task<FeishuApiResult<MessageUrgentResult>?> MessageUrgentAppAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string message_id,
      [Body] MessageUrgentRequest sendMessageRequest,
      [Query("receive_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 把指定消息加急给目标用户，加急将通过飞书客户端和短信进行通知。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="sendMessageRequest">消息加急请求体。</param>
    /// <param name="message_id">待加急的消息 ID。</param>
    /// <param name="user_id_type">用户 ID 类型 示例值："open_id"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/urgent_sms")]
    Task<FeishuApiResult<MessageUrgentResult>?> MessageUrgentSMSAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string message_id,
      [Body] MessageUrgentRequest sendMessageRequest,
      [Query("receive_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 把指定消息加急给目标用户，加急将通过飞书客户端和电话进行通知。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="sendMessageRequest">消息加急请求体。</param>
    /// <param name="message_id">待加急的消息 ID。</param>
    /// <param name="user_id_type">用户 ID 类型 示例值："open_id"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/urgent_phone")]
    Task<FeishuApiResult<MessageUrgentResult>?> MessageUrgentPhoneAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string message_id,
      [Body] MessageUrgentRequest sendMessageRequest,
      [Query("receive_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
    #endregion
}
