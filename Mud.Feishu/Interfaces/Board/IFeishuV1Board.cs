// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Board;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 画板是全新的图形创作工具，使用门槛低、简洁高效且协作方便，能用画板轻松画出好看的流程图、规划图和方案图，并且可以和团队一起在画板上进行实时的图形化协作。
/// <para>通过画板 API，可以让画板接入内部业务系统，让画板成为业务流程的一部分。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/board-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1Board : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取画板主题
    /// <para>获取画板主题，不同主题下有不同的默认配色。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/board-v1/whiteboard/theme">接口文档</see></para>
    /// </summary>
    /// <param name="whiteboard_id">
    /// <para>画板标识，可通过云文档下的文档接口 [<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list">获取文档所有块</see>] 获取，`block_type` 为 43 的 block 即为画板，对应的 &lt;code&gt;block.token&lt;/code&gt; 就是画板的&lt;code&gt;whiteboard_id&lt;/code&gt;</para>
    /// <para>示例值：Ud8xwWH01hO5mwbakqHbHeqmcCI</para>
    /// </param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/board/v1/whiteboards/{whiteboard_id}/theme")]
    Task<FeishuApiResult<GetWhiteboardsThemeResult>?> GetWhiteboardThemeAsync(
         [Path] string whiteboard_id,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新画板主题
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/board-v1/whiteboard/update_theme">接口文档</see></para>
    /// </summary>
    /// <param name="whiteboard_id">
    /// <para>画板标识，可通过云文档下的文档接口 [<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list">获取文档所有块</see>] 获取，`block_type` 为 43 的 block 即为画板，对应的 &lt;code&gt;block.token&lt;/code&gt; 就是画板的&lt;code&gt;whiteboard_id&lt;/code&gt;</para>
    /// <para>示例值：Ud8xwWH01hO5mwbakqHbHeqmcCI</para>
    /// </param> 
    /// <param name="updateWhiteboardThemeRequest">更新画板主题请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/board/v1/whiteboards/{whiteboard_id}/update_theme")]
    Task<FeishuNullDataApiResult?> UpdateWhiteboardThemeAsync(
      [Path] string whiteboard_id,
      [Body] UpdateWhiteboardThemeRequest updateWhiteboardThemeRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取画板缩略图片
    /// <para>获取画板的缩略图片，响应数据为图片的二进制图片流。根据 Content-Type 值区图片格式：image/png、image/jpeg、image/gif、image/svg+xml。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/board-v1/whiteboard/download_as_image">接口文档</see></para>
    /// </summary>
    /// <param name="whiteboard_id">
    /// <para>画板标识，可通过云文档下的文档接口 [<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list">获取文档所有块</see>] 获取，`block_type` 为 43 的 block 即为画板，对应的 &lt;code&gt;block.token&lt;/code&gt; 就是画板的&lt;code&gt;whiteboard_id&lt;/code&gt;</para>
    /// <para>示例值：Ud8xwWH01hO5mwbakqHbHeqmcCI</para>
    /// </param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/board/v1/whiteboards/{whiteboard_id}/download_as_image")]
    Task<byte[]?> DownloadWhiteboardImageAsync(
      [Path] string whiteboard_id,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析画板语法
    /// <para>用户可以将PlantUml/Mermaid图表导入画板进行协同编辑</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/board-v1/whiteboard-node/create_plantuml">接口文档</see></para>
    /// </summary>
    /// <param name="whiteboard_id">
    /// <para>画板标识，可通过云文档下的文档接口 [<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list">获取文档所有块</see>] 获取，`block_type` 为 43 的 block 即为画板，对应的 &lt;code&gt;block.token&lt;/code&gt; 就是画板的&lt;code&gt;whiteboard_id&lt;/code&gt;</para>
    /// <para>示例值：Ud8xwWH01hO5mwbakqHbHeqmcCI</para>
    /// </param> 
    /// <param name="createPlantumlWhiteboardNodeRequest">解析画板语法请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes/plantuml")]
    Task<FeishuNullDataApiResult?> CreatePlantumlWhiteboardNodeAsync(
       [Path] string whiteboard_id,
       [Body] CreatePlantumlWhiteboardNodeRequest createPlantumlWhiteboardNodeRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建节点
    /// <para>创建画板节点，支持批量创建、创建含父子关系的节点等。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/board-v1/whiteboard-node/create">接口文档</see></para>
    /// </summary>
    /// <param name="whiteboard_id">
    /// <para>画板标识，可通过云文档下的文档接口 [<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list">获取文档所有块</see>] 获取，`block_type` 为 43 的 block 即为画板，对应的 &lt;code&gt;block.token&lt;/code&gt; 就是画板的&lt;code&gt;whiteboard_id&lt;/code&gt;</para>
    /// <para>示例值：Ud8xwWH01hO5mwbakqHbHeqmcCI</para>
    /// </param> 
    /// <param name="createWhiteboardNodeRequest">创建画板节点请求体</param>
    /// <param name="client_token">
    /// <para>操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b9</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes")]
    Task<FeishuApiResult<CreateWhiteboardNodeResult>?> CreateWhiteboardNodeAsync(
      [Path] string whiteboard_id,
      [Body] CreateWhiteboardNodeRequest createWhiteboardNodeRequest,
      [Query] string? client_token = null,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有节点
    /// <para>获取画板内所有的节点，节点以数组方式返回，可通过 parent_id（父节点）、children（子节点） 关系组装成画板内容。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/board-v1/whiteboard-node/list">接口文档</see></para>
    /// </summary>
    /// <param name="whiteboard_id">
    /// <para>画板标识，可通过云文档下的文档接口 [<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list">获取文档所有块</see>] 获取，`block_type` 为 43 的 block 即为画板，对应的 &lt;code&gt;block.token&lt;/code&gt; 就是画板的&lt;code&gt;whiteboard_id&lt;/code&gt;</para>
    /// <para>示例值：Ud8xwWH01hO5mwbakqHbHeqmcCI</para>
    /// </param> 
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes")]
    Task<FeishuApiResult<GetWhiteboardNodesResult>?> GetWhiteboardNodesAsync(
        [Path] string whiteboard_id,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}