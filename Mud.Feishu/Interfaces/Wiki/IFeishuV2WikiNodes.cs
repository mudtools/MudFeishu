// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>知识空间中的节点，支持文档、表格等多种文件类型。</para>
/// <para>文件是各种类型的文件的统称，泛指云空间内所有的文件。每个文件都有唯一 token 作为标识。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/wiki-v2/wiki-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2WikiNodes : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建知识空间节点。
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="createSpaceNodeRequest">创建知识空间节点请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/wiki/v2/spaces/{space_id}/nodes")]
    Task<FeishuApiResult<SpaceNodeResult>?> CreateSpaceNodeAsync(
         [Path] string space_id,
         [Body] CreateSpaceNodeRequest createSpaceNodeRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取知识空间节点信息。
    /// </summary>
    /// <param name="token">
    /// <para>必填：是</para>
    /// <para>知识库节点或对应云文档的实际 token。</para>
    /// <para>- 知识库节点 token：如果 URL 链接中 token 前为 wiki，该 token 为知识库的节点 token。</para>
    /// <para>- 云文档实际 token：如果 URL 链接中 token 前为 docx、base、sheets 等非 wiki 类型，则说明该 token 是当前云文档的实际 token。</para>
    /// <para>**注意**：</para>
    /// <para>使用云文档 token 查询时，需要对 obj_type 参数传入文档对应的类型。</para>
    /// <para>示例值：wikcnKQ1k3p******8Vabcef</para>
    /// </param>
    /// <param name="obj_type">
    /// <para>必填：否</para>
    /// <para>文档类型。不传时默认以 wiki 类型查询。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：表格</item>
    /// <item>mindnote：思维导图</item>
    /// <item>bitable：多维表格</item>
    /// <item>file：文件</item>
    /// <item>slides：幻灯片</item>
    /// <item>wiki：知识库节点</item>
    /// </list>
    /// <para>默认值：wiki</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/wiki/v2/spaces/get_node")]
    Task<FeishuApiResult<SpaceNodeResult>?> GetNodeSpaceInfoAsync(
         [Query("token")] string token,
         [Query("obj_type")] string? obj_type = "wiki",
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>用于分页获取Wiki节点的子节点列表。</para>
    /// <para>此接口为分页接口。由于权限过滤，可能返回列表为空，但分页标记（has_more）为true，可以继续分页请求。</para>
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="parent_node_token">
    /// <para>必填：否</para>
    /// <para>父节点token</para>
    /// <para>示例值：wikcnKQ1k3p******8Vabce</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/wiki/v2/spaces/{space_id}/nodes")]
    Task<FeishuApiPageListResult<SpaceNodeInfo>?> GetSpaceNodesPageListAsync(
         [Path] string space_id,
         [Query("parent_node_token")] string? parent_node_token = null,
         [Query("page_size")] int page_size = Consts.PageSize_10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>移动知识空间节点</para>
    /// 用于在Wiki内移动节点，支持跨知识空间移动。如果有子节点，会携带子节点一起移动。
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="node_token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>需要迁移的节点token</para>
    /// <para>示例值：wikbcd6ydSUyOEzbdlt1BfpA5Yc</para>
    /// </param>
    /// <param name="moveSpaceNodeRequest">移动知识空间节点请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/wiki/v2/spaces/{space_id}/nodes/{node_token}/move")]
    Task<FeishuApiResult<SpaceNodeResult>?> MoveSpaceNodeAsync(
        [Path] string space_id,
        [Path] string node_token,
        [Body] MoveSpaceNodeRequest moveSpaceNodeRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>更新知识空间节点标题</para>
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="node_token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>需要迁移的节点token</para>
    /// <para>示例值：wikbcd6ydSUyOEzbdlt1BfpA5Yc</para>
    /// </param>
    /// <param name="updateTitleSpaceNodeRequest">更新知识空间节点标题请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/wiki/v2/spaces/{space_id}/nodes/{node_token}/update_title")]
    Task<FeishuNullDataApiResult?> UpdateTitleSpaceNodeAsync(
       [Path] string space_id,
       [Path] string node_token,
       [Body] UpdateTitleSpaceNodeRequest updateTitleSpaceNodeRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>用于在知识空间创建节点副本到指定位置。</para>
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="node_token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>需要迁移的节点token</para>
    /// <para>示例值：wikbcd6ydSUyOEzbdlt1BfpA5Yc</para>
    /// </param>
    /// <param name="copySpaceNodeRequest">移动知识空间节点请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/wiki/v2/spaces/{space_id}/nodes/{node_token}/copy")]
    Task<FeishuApiResult<SpaceNodeResult>?> CopySpaceNodeAsync(
         [Path] string space_id,
         [Path] string node_token,
         [Body] CopySpaceNodeRequest copySpaceNodeRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>移动云空间文档至知识空间，并挂载在指定位置。注意：该接口为异步接口。</para>
    /// </summary>
    /// <param name="space_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>知识空间 ID。</para>
    /// <para>示例值：6870403571079249922</para>
    /// </param>
    /// <param name="moveDocsToWikiSpaceNode">移动云空间文档至知识空间请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/wiki/v2/spaces/{space_id}/nodes/move_docs_to_wiki")]
    Task<FeishuApiResult<MoveDocsToWikiSpaceNodeResult>?> MoveDocsToWikiSpaceNodeAsync(
         [Path] string space_id,
         [Body] MoveDocsToWikiSpaceNodeRequest moveDocsToWikiSpaceNode,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>用于获取wiki异步任务的结果。</para>
    /// </summary>
    /// <param name="task_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>任务id</para>
    /// <para>示例值：7037044037068177428-075c9481e6a0007c1df689dfbe5b55a08b6b06f7</para>
    /// </param>
    /// <param name="task_type">
    /// <para>必填：是</para>
    /// <para>任务类型</para>
    /// <para>示例值：move</para>
    /// <list type="bullet">
    /// <item>move：[移动云空间文档至知识空间]任务</item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/wiki/v2/tasks/{task_id}")]
    Task<FeishuApiResult<GetTaskResult>?> GetTaskByIdAsync(
         [Path] string task_id,
         [Query("task_type")] string task_type = "move",
         CancellationToken cancellationToken = default);


}
