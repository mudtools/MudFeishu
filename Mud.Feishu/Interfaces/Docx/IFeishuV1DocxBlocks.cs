// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 块是文档中的最小构建单元，是内容的结构化组成元素，有着明确的含义。
/// <para>在一篇文档中，有多个不同类型的段落，这些段落被定义为块（Block）。</para>
/// <para>块有多种形态，可以是一段文字、一张电子表格、一张图片或一个多维表格等。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DocxBlocks : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 指定需要操作的块，为其创建一批子块，并插入到指定位置。如果操作成功，接口将返回新创建子块的富文本内容。
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="block_id">父块的block_id，表示为其创建一批子块。如果需要对文档树根节点创建子块，可将 document_id 填入此处。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="createBlockRequest">创建块请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}/children")]
    Task<FeishuApiResult<BlockOpResult>?> CreateBlockAsync(
         [Path] string document_id,
         [Path] string block_id,
         [Body] CreateBlockRequest createBlockRequest,
         [Query("document_revision_id")] int? document_revision_id = -1,
         [Query("client_token")] string? client_token = null,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);



    /// <summary>
    /// <para>在指定块的子块列表中，新创建一批有父子关系的子块，并放置到指定位置。</para>
    /// <para>如果操作成功，接口将返回新创建子块的富文本内容。</para>
    /// <para>调用该接口前，你可参考 <see href="https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview">文档概述-基本概念</see> 了解块的父子关系规则。</para>
    /// <para>当创建的子块中含有 GridColumn、TableCell、Callout 时其中至少需要包含一个子块 ，即内容为空时也需要填入一个空 Text Block 作为子块。</para>
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="block_id">父块的block_id，表示为其创建一批子块。如果需要对文档树根节点创建子块，可将 document_id 填入此处。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="createDescendantBlockRequest">创建嵌套块请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}/descendant")]
    Task<FeishuApiResult<CreateDescendantBlockResult>?> CreateDescendantBlockAsync(
        [Path] string document_id,
        [Path] string block_id,
        [Body] CreateDescendantBlockRequest createDescendantBlockRequest,
        [Query("document_revision_id")] int? document_revision_id = -1,
        [Query("client_token")] string? client_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>更新指定块的内容。如果操作成功，接口将返回更新后的块的富文本内容。</para>
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="block_id">父块的block_id，表示为其创建一批子块。如果需要对文档树根节点创建子块，可将 document_id 填入此处。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="updateBlockRequest">更新块的内容请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}")]
    Task<FeishuApiResult<BlockOpResult>?> UpdateBlockAsync(
       [Path] string document_id,
       [Path] string block_id,
       [Body] UpdateBlockRequest updateBlockRequest,
       [Query("document_revision_id")] int? document_revision_id = -1,
       [Query("client_token")] string? client_token = null,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>指定块的 block id 获取指定块的富文本内容数据。</para>
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="block_id">父块的block_id，表示为其创建一批子块。如果需要对文档树根节点创建子块，可将 document_id 填入此处。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}")]
    Task<FeishuApiResult<GetBlockInfoResult>?> GetBlockInfoAsync(
       [Path] string document_id,
       [Path] string block_id,
       [Query("document_revision_id")] int? document_revision_id = -1,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>批量更新块的富文本内容。</para>
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="updateBlockRequest">批量更新块的内容请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/docx/v1/documents/{document_id}/blocks/batch_update")]
    Task<FeishuApiResult<BatchUpdateBlocksResult>?> BatchUpdateBlocksAsync(
      [Path] string document_id,
      [Body] BatchUpdateBlocksRequest updateBlockRequest,
      [Query("document_revision_id")] int? document_revision_id = -1,
      [Query("client_token")] string? client_token = null,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>获取文档中指定块的所有子块的富文本内容并分页返回。文档版本号可选。</para>
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="block_id">父块的block_id，表示为其创建一批子块。如果需要对文档树根节点创建子块，可将 document_id 填入此处。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}/children")]
    Task<FeishuApiPageListResult<Block>?> GetChildrenBlocksPageListAsync(
         [Path] string document_id,
         [Path] string block_id,
         [Query("document_revision_id")] int? document_revision_id = -1,
         [Query("client_token")] string? client_token = null,
         [Query("page_size")] int page_size = 500,
         [Query("page_token")] string? page_token = null,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>指定需要操作的块，删除其指定范围的子块。如果操作成功，接口将返回应用删除操作后的文档版本号。</para>
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="batchDeleteBlocksRequest">删除块请求体</param>
    /// <param name="block_id">父 Block 的唯一标识</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}/children/batch_delete")]
    Task<FeishuApiResult<BatchDeleteBlocksResult>?> BatchDeleteBlocksAsync(
       [Path] string document_id,
       [Path] string block_id,
       [Body] BatchDeleteBlocksRequest batchDeleteBlocksRequest,
       [Query("document_revision_id")] int? document_revision_id = -1,
       [Query("client_token")] string? client_token = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>将 Markdown/HTML 格式的内容转换为文档块，以便于将 Markdown/HTML 格式的内容插入到文档中。</para>
    /// <para>目前支持转换为的块类型包含文本、一到九级标题、无序列表、有序列表、代码块、引用、待办事项、图片、表格、表格单元格。</para>
    /// </summary>
    /// <param name="convertContentRequest">内容转换请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/docx/v1/documents/blocks/convert")]
    Task<FeishuApiResult<ContentConvertResult>?> ContentConvertAsync(
       [Body] ConvertContentRequest convertContentRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

}
