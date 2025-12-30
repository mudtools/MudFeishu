// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatGroupNotice;

namespace Mud.Feishu;

/// <summary>
/// 群公告是群组中的公告文档，采用飞书云文档承载，每个群组只有一个群公告，每篇群公告都有唯一的 chat_id作为标识。
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV1ChatGroupAnnouncement
{
    /// <summary>
    /// 获取指定群组中的群公告基本信息。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement")]
    Task<FeishuApiResult<GetAnnouncementResult>?> GetNoticeInfoByIdAsync(
       [Path] string chat_id,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取群公告所有块的富文本内容并分页返回。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="revision_id">查询的群公告版本，-1 表示群公告最新版本。
    /// <para>群公告创建后，版本为 1。若查询的版本为群公告最新版本，则需要持有群公告的阅读权限；</para> 
    /// <para>若查询的版本为群公告的历史版本，则需要持有群公告的编辑权限。</para>
    /// <para>默认值：-1</para></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks")]
    Task<FeishuApiPageListResult<AnnouncementBlock>?> GetNoticeBlocksListByIdAsync(
      [Path] string chat_id,
      [Query("page_size")] int? page_size = 10,
      [Query("page_token")] string? page_token = null,
      [Query("revision_id")] int? revision_id = -1,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 在指定块的子块列表中，新创建一批子块，并放置到指定位置。如果操作成功，接口将返回新创建子块的富文本内容。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="block_id">父块的block_id，表示为其创建一批子块。如果需要对群公告树根节点创建子块，可将 chat_id 填入此处。
    /// <para>示例值："doxcnO6UW6wAw2qIcYf4hZpFIth"</para>
    /// </param>
    /// <param name="createBlockRequest">在群公告中创建块请求体</param>
    /// <param name="revision_id">要操作的群公告版本。-1 表示群公告最新版本。群公告创建后，版本为 1。</param>
    /// <param name="client_token">操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。
    /// <para>此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para></param>
    /// <param name="user_id_type">用户 ID 类型 示例值："open_id"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}/children")]
    Task<FeishuApiResult<CreateBlockResult>?> CreateNoticeBlockAsync(
       [Path] string chat_id,
       [Path] string block_id,
       [Body] CreateBlockRequest createBlockRequest,
       [Query("revision_id")] int revision_id = -1,
       [Query("client_token")] string? client_token = null,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新块的富文本内容。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="batchUpdateRequest">批量更新群公告块的内容请求体</param>
    /// <param name="revision_id">要操作的群公告版本。-1 表示群公告最新版本。群公告创建后，版本为 1。</param>
    /// <param name="client_token">操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。
    /// <para>此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para></param>
    /// <param name="user_id_type">用户 ID 类型 示例值："open_id"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/batch_update")]
    Task<FeishuApiResult<BatchUpdateResult>?> UpdateNoticeBlockAsync(
          [Path] string chat_id,
          [Body] BlocksBatchUpdateRequest batchUpdateRequest,
          [Query("revision_id")] int revision_id = -1,
          [Query("client_token")] string? client_token = null,
          [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取群公告块的富文本内容
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="block_id">Block 的唯一标识。 示例值："doxcnO6UW6wAw2qIcYf4hZabcef"</param>
    /// <param name="revision_id">要操作的群公告版本。-1 表示群公告最新版本。群公告创建后，版本为 1。</param>
    /// <param name="user_id_type">用户 ID 类型 示例值："open_id"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}")]
    Task<FeishuApiResult<GetBlockContentListResult>?> GetBlockContentByIdAsync(
        [Path] string chat_id,
        [Path] string block_id,
        [Query("revision_id")] int? revision_id = -1,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取群公告所有块的富文本内容并分页返回。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="block_id">Block 的唯一标识。 示例值："doxcnO6UW6wAw2qIcYf4hZabcef"</param>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="revision_id">查询的群公告版本，-1 表示群公告最新版本。
    /// <para>群公告创建后，版本为 1。若查询的版本为群公告最新版本，则需要持有群公告的阅读权限；</para> 
    /// <para>若查询的版本为群公告的历史版本，则需要持有群公告的编辑权限。</para>
    /// <para>默认值：-1</para></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}/children")]
    Task<FeishuApiPageListResult<AnnouncementBlock>?> GetBlockContentPageListByIdAsync(
        [Path] string chat_id,
        [Path] string block_id,
        [Query("page_size")] int? page_size = 10,
        [Query("page_token")] string? page_token = null,
        [Query("revision_id")] int? revision_id = -1,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定需要操作的块，删除其指定范围的子块。如果操作成功，接口将返回应用删除操作后的群公告版本号。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="block_id">Block 的唯一标识。 示例值："doxcnO6UW6wAw2qIcYf4hZabcef"</param>
    /// <param name="deleteRequest"></param>
    /// <param name="user_id_type">用户 ID 类型，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="client_token">操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}/children/batch_delete")]
    Task<FeishuApiResult<DeleteAnnouncementBlockResult>?> DeleteBlockByIdAsync(
       [Path] string chat_id,
       [Path] string block_id,
       [Body] DeleteAnnouncementBlockRequest deleteRequest,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       [Query("client_token")] string? client_token = null,
       CancellationToken cancellationToken = default);
}
