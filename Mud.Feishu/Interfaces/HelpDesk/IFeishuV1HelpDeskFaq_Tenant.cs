// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu;

/// <summary>
/// 飞书服务台知识库FAQ API是开放平台基于飞书服务台知识库的常见问题功能开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台知识库FAQ进行操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk", InheritedFrom = nameof(FeishuV1HelpDeskFaq))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HelpDeskFaq : IFeishuV1HelpDeskFaq
{
    /// <summary>
    /// 查询知识库FAQ列表
    /// <para>用于获取服务台知识库详情。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/list">接口文档</see></para>
    /// </summary>
    /// <param name="category_id">
    /// <para>知识库分类ID</para>
    /// <para>示例值：6856395522433908739</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="status">
    /// <para>搜索条件: 知识库状态 1：在线 0：已删除，可恢复 2：已删除，不可恢复</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="search">
    /// <para>搜索条件: 关键词，匹配问题标题、问题关键词、用户名</para>
    /// <para>示例值：Order</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_token">
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>示例值：6856395634652479491</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">
    /// <para>分页大小，最大值为 100</para>
    /// <para>示例值：10</para>
    /// <para>默认值：20</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/faqs")]
    Task<FeishuApiResult<GetFaqListResult>?> GetFaqListAsync(
        [Query] string? category_id = null,
        [Query] string? status = null,
        [Query] string? search = null,
        [Query] string? page_token = null,
        [Query] int? page_size = Consts.PageSize_20,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 搜索知识库FAQ
    /// <para>用于搜索服务台知识库。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/search">接口文档</see></para>
    /// </summary>
    /// <param name="query">
    /// <para>搜索词。如果搜索内容不是英文，有 2 种编码策略：1. URL 编码；2. base64 编码并传入 base64=true 参数</para>
    /// <para>示例值：wifi</para>
    /// </param>
    /// <param name="base64">
    /// <para>是否转 base64。填 true 表示是。留空表示否。中文需要转 base64</para>
    /// <para>示例值：5bel5Y2V</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_token">
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>示例值：6936004780707807251</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">
    /// <para>分页大小，最大值为 100</para>
    /// <para>示例值：10</para>
    /// <para>默认值：20</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/faqs/search")]
    Task<FeishuApiResult<SearchFaqResult>?> SearchFaqAsync(
        [Query] string query,
        [Query] string? base64 = null,
        [Query] string? page_token = null,
        [Query] int? page_size = Consts.PageSize_20,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取知识库FAQ详情
    /// <para>用于获取服务台知识库FAQ详情。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/get">接口文档</see></para>
    /// </summary>
    /// <param name="id">
    /// <para>知识库FAQ ID</para>
    /// <para>示例值："6856395634652479491"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/faqs/{id}")]
    Task<FeishuApiResult<GetFaqResult>?> GetFaqAsync(
        [Path] string id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取知识库FAQ图片
    /// <para>用于获取服务台知识库FAQ图片。返回文件二进制流。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/faq_image">接口文档</see></para>
    /// </summary>
    /// <param name="id">
    /// <para>知识库FAQ ID</para>
    /// <para>示例值："12345"</para>
    /// </param>
    /// <param name="image_key">
    /// <para>图片 key</para>
    /// <para>示例值："img_b07ffac0-19c1-48a3-afca-599f8ea825fj"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// 成功时返回响应的二进制内容（取自 <c>HttpContent.ReadAsByteArrayAsync</c>，不会为 <see langword="null"/>；空响应体对应空数组）。
    /// </returns>
    /// <exception cref="ApiException">
    /// 服务端返回非 2xx 状态码时抛出（由 HTTP 执行器统一抛出，异常携带 <c>StatusCode</c> 与响应内容）。
    /// <para>
    /// 注意：飞书部分业务错误以 HTTP 200 + JSON 错误体（<c>{"code":...,"msg":...}</c>）返回，
    /// 此时本方法会把错误 JSON 当作文件内容返回。落盘前应按 <c>Content-Type</c> 自检，详见 <c>documents/ErrorHandling.md</c>。
    /// </para>
    /// </exception>
    [Get("/open-apis/helpdesk/v1/faqs/{id}/image/{image_key}")]
    Task<byte[]?> GetFaqImageAsync(
        [Path] string id,
        [Path] string image_key,
        CancellationToken cancellationToken = default);
}
