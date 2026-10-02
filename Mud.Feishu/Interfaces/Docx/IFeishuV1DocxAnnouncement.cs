// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 群公告是群聊中的公告消息，新版群公告以文档（docx）形式存储，其内容由多个块（Block）组成。
/// <para>通过群公告接口可以获取群公告基本信息、读取群公告中的块、批量更新块的内容，以及在群公告中创建或删除块。</para>
/// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement/get">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DocxAnnouncement : IFeishuAppContextSwitcher
{
    /// <summary>
    /// <para>获取群公告基本信息，包含群公告类型、群公告版本号、群公告所有者与最新修改者等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement/get">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement")]
    Task<FeishuApiResult<GetChatAnnouncementResult>?> GetChatAnnouncementAsync(
        [Path] string chat_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>批量更新群公告中块的富文本内容。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="updateBlockRequest">批量更新块的内容请求体</param>
    /// <param name="revision_id">
    /// <para>必填：否</para>
    /// <para>要操作的群公告版本，-1 表示群公告最新版本。群公告创建后，版本为 1。你需确保你已拥有群公告的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/batch_update")]
    Task<FeishuApiResult<BatchUpdateChatAnnouncementBlocksResult>?> BatchUpdateChatAnnouncementBlocksAsync(
        [Path] string chat_id,
        [Body] BatchUpdateBlocksRequest updateBlockRequest,
        [Query("revision_id")] int? revision_id = -1,
        [Query("client_token")] string? client_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>指定块的 block_id 获取群公告指定块的富文本内容数据。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block/get">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="block_id">块的唯一标识。</param>
    /// <param name="revision_id">
    /// <para>必填：否</para>
    /// <para>查询的群公告版本，-1 表示群公告最新版本。群公告创建后，版本为 1。若查询的版本为群公告最新版本，则需要持有群公告的阅读权限；若查询的版本为群公告的历史版本，则需要持有群公告的更新权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}")]
    Task<FeishuApiResult<GetBlockInfoResult>?> GetChatAnnouncementBlockInfoAsync(
        [Path] string chat_id,
        [Path] string block_id,
        [Query("revision_id")] int? revision_id = -1,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>获取群公告所有块的富文本内容并分页返回。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block/list">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="revision_id">
    /// <para>必填：否</para>
    /// <para>查询的群公告版本，-1 表示群公告最新版本。群公告创建后，版本为 1。若查询的版本为群公告最新版本，则需要持有群公告的阅读权限；若查询的版本为群公告的历史版本，则需要持有群公告的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks")]
    Task<FeishuApiPageListResult<Block>?> GetChatAnnouncementBlocksPageListAsync(
        [Path] string chat_id,
        [Query("revision_id")] int? revision_id = -1,
        [Query("page_size")] int page_size = 500,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>指定需要操作的块，删除其指定范围的子块。如果操作成功，接口将返回应用删除操作后群公告的版本号。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block-children/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="block_id">父 Block 的唯一标识。</param>
    /// <param name="batchDeleteBlocksRequest">删除块请求体</param>
    /// <param name="revision_id">
    /// <para>必填：否</para>
    /// <para>要操作的群公告版本，-1 表示群公告最新版本。群公告创建后，版本为 1。你需确保你已拥有群公告的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}/children/batch_delete")]
    Task<FeishuApiResult<DeleteChatAnnouncementBlockChildrenResult>?> BatchDeleteChatAnnouncementBlockChildrenAsync(
        [Path] string chat_id,
        [Path] string block_id,
        [Body] BatchDeleteBlocksRequest batchDeleteBlocksRequest,
        [Query("revision_id")] int? revision_id = -1,
        [Query("client_token")] string? client_token = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>指定需要操作的块，为其创建一批子块，并插入到指定位置。如果操作成功，接口将返回新创建子块的富文本内容。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block-children/create">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="block_id">父块的 block_id，表示为其创建一批子块。如果需要对群公告根节点创建子块，可将群公告的根块 ID 填入此处。</param>
    /// <param name="createBlockRequest">创建块请求体</param>
    /// <param name="revision_id">
    /// <para>必填：否</para>
    /// <para>要操作的群公告版本，-1 表示群公告最新版本。群公告创建后，版本为 1。你需确保你已拥有群公告的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}/children")]
    Task<FeishuApiResult<CreateChatAnnouncementBlockChildrenResult>?> CreateChatAnnouncementBlockChildrenAsync(
        [Path] string chat_id,
        [Path] string block_id,
        [Body] CreateBlockRequest createBlockRequest,
        [Query("revision_id")] int? revision_id = -1,
        [Query("client_token")] string? client_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>获取群公告中指定块的所有子块的富文本内容并分页返回。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block-children/get">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。获取方式：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create">创建群</see>接口返回的 chat_id。</param>
    /// <param name="block_id">父块的 block_id，表示获取其子块列表。</param>
    /// <param name="revision_id">
    /// <para>必填：否</para>
    /// <para>查询的群公告版本，-1 表示群公告最新版本。群公告创建后，版本为 1。若查询的版本为群公告最新版本，则需要持有群公告的阅读权限；若查询的版本为群公告的历史版本，则需要持有群公告的更新权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/chats/{chat_id}/announcement/blocks/{block_id}/children")]
    Task<FeishuApiPageListResult<Block>?> GetChatAnnouncementBlockChildrenPageListAsync(
        [Path] string chat_id,
        [Path] string block_id,
        [Query("revision_id")] int? revision_id = -1,
        [Query("page_size")] int page_size = 500,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
