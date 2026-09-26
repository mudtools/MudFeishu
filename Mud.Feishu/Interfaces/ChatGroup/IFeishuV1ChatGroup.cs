// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatGroup;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书群组 OpenAPI 提供了群组管理能力，包括创建群、解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/group/chat/intro"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1ChatGroup : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建群聊，创建时支持设置群头像、群名称、群主以及群类型等配置，同时支持邀请群成员、群机器人入群。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/create">接口文档</see></para>
    /// </summary>
    /// <param name="createChatRequest">创建群聊请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="set_bot_manager">如果在请求体的 owner_id 字段指定了某个用户为群主，可以选择是否同时设置创建此群的机器人为管理员，此标志位用于标记是否设置创建群的机器人为管理员。
    ///  <para>示例值：false</para>
    /// </param>
    /// <param name="uuid">由开发者生成的唯一字符串序列，用于创建群组请求去重；持有相同 uuid + owner_id（若有） 的请求 10 小时内只可成功创建 1 个群聊。不传值表示不进行请求去重，每一次请求成功后都会创建一个群聊。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats")]
    Task<FeishuApiResult<CreateUpdateChatResult>?> CreateChatGroupAsync(
        [Body] CreateChatRequest createChatRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("set_bot_manager")] bool? set_bot_manager = false,
        [Query("uuid")] string? uuid = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定群的信息，包括群头像、群名称、群描述、群配置以及群主等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/update">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="updateChatRequest">更新群聊请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("/open-apis/im/v1/chats/{chat_id}")]
    Task<FeishuApiResult<CreateUpdateChatResult>?> UpdateChatGroupByIdAsync(
        [Path] string chat_id,
        [Body] UpdateChatRequest updateChatRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过 chat_id 解散指定群组。通过 API 解散群组后，群聊天记录将不会保存。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/delete">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/im/v1/chats/{chat_id}")]
    Task<FeishuNullDataApiResult?> DeleteChatGroupAsync(
       [Path] string chat_id,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定群组的发言权限，可设置为所有群成员可发言、仅群主或管理员可发言、指定群成员可发言。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/moderation/update">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="updateChatModerationRequest">更新群发言权限请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("/open-apis/im/v1/chats/{chat_id}/moderation")]
    Task<FeishuNullDataApiResult?> UpdateChatModerationAsync(
        [Path] string chat_id,
        [Body] UpdateChatModerationRequest updateChatModerationRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取指定群的基本信息，包括群名称、群描述、群头像、群主 ID 以及群权限配置等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/get">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/im/v1/chats/{chat_id}")]
    Task<FeishuApiResult<GetChatGroupInfoResult>?> GetChatGroupInoByIdAsync(
       [Path] string chat_id,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新群组中的群置顶信息，可以将群中的某一条消息，或群公告置顶展示。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-top_notice/put_top_notice">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="chatTopNoticeRequest">群置顶操作请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/top_notice/put_top_notice")]
    Task<FeishuNullDataApiResult?> PutChatGroupTopNoticeAsync(
      [Path] string chat_id,
      [Body] ChatTopNoticeRequest chatTopNoticeRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销指定群组中的置顶消息或群公告。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-top_notice/delete_top_notice">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/top_notice/delete_top_notice")]
    Task<FeishuNullDataApiResult?> DeleteChatGroupTopNoticeAsync(
      [Path] string chat_id,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取当前 access_token 所代表的用户或者机器人所在的群列表。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="sort_type">群组排序方式  示例值："ByCreateTimeAsc"
    /// <para>可选值有：</para>
    /// <para>ByCreateTimeAsc：按群组创建时间升序排列</para>
    /// <para>ByActiveTimeDesc：按群组活跃时间降序排列。因群组活跃时间变动频繁，使用 ByActiveTimeDesc 排序方式可能会造成群组遗漏。</para>
    /// <para>例如，设置分页大小为 10，发起第一次请求获取到第一页数据后，原本排在第 11 位的群组中有群成员发送了一条消息，那么该群组将被排列到第 1 位，此时发起请求获取第二页数据时，该群组将不能被获取到，需要再从第一页开始获取。</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/im/v1/chats")]
    Task<FeishuApiPageListResult<ChatItemInfo>?> GetChatGroupPageListAsync(
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       [Query("sort_type")] string sort_type = "ByCreateTimeAsc",
       [Query("page_size")] int? page_size = Consts.PageSize_10,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取当前身份（用户或机器人）可见的群列表，包括当前身份所在的群、对当前身份公开的群。支持关键词搜索、分页搜索。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/search">接口文档</see></para>
    /// </summary>
    /// <param name="query">关键词 示例值："abc"</param>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="sort_type">群组排序方式  示例值："ByCreateTimeAsc"
    /// <para>可选值有：</para>
    /// <para>ByCreateTimeAsc：按群组创建时间升序排列</para>
    /// <para>ByActiveTimeDesc：按群组活跃时间降序排列。因群组活跃时间变动频繁，使用 ByActiveTimeDesc 排序方式可能会造成群组遗漏。</para>
    /// <para>例如，设置分页大小为 10，发起第一次请求获取到第一页数据后，原本排在第 11 位的群组中有群成员发送了一条消息，那么该群组将被排列到第 1 位，此时发起请求获取第二页数据时，该群组将不能被获取到，需要再从第一页开始获取。</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/im/v1/chats/search")]
    Task<FeishuApiPageListResult<ChatItemInfo>?> GetChatGroupPageListByKeywordAsync(
       [Query("query")] string? query = "",
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       [Query("sort_type")] string sort_type = "ByCreateTimeAsc",
       [Query("page_size")] int? page_size = Consts.PageSize_10,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取指定群组的发言模式、可发言用户名单等信息。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/moderation/get">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>/
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/im/v1/chats/{chat_id}/moderation")]
    Task<FeishuApiResult<ChatGroupModeratorPageListResult>?> GetChatGroupModeratorPageListByIdAsync(
      [Path] string chat_id,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      [Query("page_size")] int? page_size = Consts.PageSize_10,
      [Query("page_token")] string? page_token = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定群的分享链接，他人点击分享链接后可加入群组。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat/link">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="shareLinkRequest">获取群分享链接群分享链接请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/link")]
    Task<FeishuApiResult<ShareLinkDataResult>?> GetChatGroupShareLinkByIdAsync(
     [Path] string chat_id,
     [Body] ShareLinkRequest shareLinkRequest,
     CancellationToken cancellationToken = default);

}
