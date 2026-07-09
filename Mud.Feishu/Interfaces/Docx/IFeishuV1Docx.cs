// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书开放平台云文档分为文档和块。
/// <para>文档是用户在云文档中创建的一篇在线文档。每篇文档都有唯一的 document_id 作为标识。</para>
/// <para>块是文档中的最小构建单元，是内容的结构化组成元素，有着明确的含义。在一篇文档中，有多个不同类型的段落，这些段落被定义为块（Block）。块有多种形态，可以是一段文字、一张电子表格、一张图片或一个多维表格等。每个块都有唯一的 block_id 作为标识。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1Docx : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建文档类型为 docx 的文档。可选择传入文档标题和文件夹。
    /// </summary>
    /// <param name="createDocumentRequest">创建文档请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/docx/v1/documents")]
    Task<FeishuApiResult<DocumentInfoResult>?> CreateDocumentAsync(
         [Body] CreateDocumentRequest createDocumentRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文档基本信息。
    /// </summary>
    /// <param name="document_id">文档的唯一标识。
    /// <para>**注意**：</para>
    /// <para>对于知识库（wiki）中的文档，其 URL 地址中的 token 并不是该文档的 `document_id`。使用时请注意区分。</para>
    /// <para>示例值：doxcnePuYufKa49ISjhD8Iabcef</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/documents/{document_id}")]
    Task<FeishuApiResult<DocumentInfoResult>?> GetDocumentInfoAsync(
        [Path] string document_id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文档的纯文本内容。
    /// </summary>
    /// <param name="document_id">文档的唯一标识。
    /// <para>**注意**：</para>
    /// <para>对于知识库（wiki）中的文档，其 URL 地址中的 token 并不是该文档的 `document_id`。使用时请注意区分。</para>
    /// <para>示例值：doxcnePuYufKa49ISjhD8Iabcef</para>
    /// </param>
    /// <param name="lang">
    /// <para>必填：否</para>
    /// <para>指定返回的 MentionUser 即 @用户 的语言</para>
    /// <para>示例值：0</para>
    /// <list type="bullet">
    /// <item>0：该用户的默认名称。如：@张敏</item>
    /// <item>1：该用户的英文名称。如：@Min Zhang</item>
    /// <item>2：暂不支持该枚举，使用时返回该用户的默认名称</item>
    /// </list>
    /// <para>默认值：0</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/docx/v1/documents/{document_id}/raw_content")]
    Task<FeishuApiResult<DocumentRawContentResult>?> GetDocumentRawContentAsync(
       [Path] string document_id,
       [Query("lang")] int? lang = 0,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取文档所有块的富文本内容并分页返回。
    /// </summary>
    /// <param name="document_id">文档的唯一标识。</param>
    /// <param name="document_revision_id">
    /// <para>必填：否</para>
    /// <para>查询的文档版本，-1表示文档最新版本。若此时查询的版本为文档最新版本，则需要持有文档的阅读权限；若此时查询的版本为文档的历史版本，则需要持有文档的编辑权限。</para>
    /// <para>示例值：-1</para>
    /// <para>默认值：-1</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/docx/v1/documents/{document_id}/blocks")]
    Task<FeishuApiPageListResult<Block>?> GetDocumentBlocksPageListAsync(
        [Path] string document_id,
        [Query("document_revision_id")] int? document_revision_id = -1,
        [Query("page_size")] int page_size = 500,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
