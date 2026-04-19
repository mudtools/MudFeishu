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


    [Post("/open-apis/board/v1/whiteboards/{whiteboard_id}/nodes")]
    Task<FeishuNullDataApiResult?> CreateWhiteboardNodeAsync(
      [Path] string whiteboard_id,
      [Body] CreateWhiteboardNodeRequest createWhiteboardNodeRequest,
      CancellationToken cancellationToken = default);
}