// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的数据知识能力，把它集成到自己的业务系统里。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/aily-v1/data-knowledge/ask"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1AilyDataKnowledge
{
    /// <summary>
    /// 执行数据知识问答
    /// <para>用于对某个 Aily 应用配置的数据知识执行问答，基于指定数据知识范围生成回答。</para>
    /// <para>此接口以 Server-sent Events（SSE）方式更新问答结果，响应体不是常规 JSON 包装，调用方需从 <see cref="HttpResponseMessage"/> 内容流中解析 <c>data:</c> 行并反序列化为 <see cref="KnowledgeAskEvent"/>。</para>
    /// <para>注意事项：</para>
    /// <para>- 使用 tenant_access_token 需在智能伙伴管理后台开启「支持使用应用身份调用 API 和 SDK」；</para>
    /// <para>- 使用 tenant_access_token 无法对直连模式引入的飞书云文档执行数据知识问答。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/ask">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_5862e4fea8__c</param>
    /// <param name="request">执行数据知识问答请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// 成功时返回承载 SSE 流的 <see cref="HttpResponseMessage"/>（可空签名，实际成功路径不会为 <see langword="null"/>）；
    /// 调用方负责读取 <c>Content</c> 流并解析事件后释放响应。
    /// </returns>
    /// <exception cref="ApiException">
    /// 服务端返回非 2xx 状态码时抛出（由 HTTP 执行器统一抛出，异常携带 <c>StatusCode</c> 与响应内容）。
    /// <para>
    /// 注意：飞书部分业务错误以 HTTP 200 + JSON 错误体返回（错误码 2700033 执行失败、2700034 问答被禁用、2700035 执行超时），
    /// 此时本方法会把响应原样返回，调用方需按 Content-Type 自检，详见 <c>documents/ErrorHandling.md</c>。
    /// </para>
    /// </exception>
    [Post("/open-apis/aily/v1/apps/{app_id}/knowledges/ask")]
    Task<HttpResponseMessage?> AskDataKnowledgeAsync(
        [Path] string app_id,
        [Body] AskDataKnowledgeRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 上传文件用于数据知识管理
    /// <para>用于上传文件到 Aily 数据知识管理系统，上传成功后可获取 file token 用于创建数据知识。</para>
    /// <para>注意事项：</para>
    /// <para>- 仅支持开发环境（tenant_type 仅支持 dev）；</para>
    /// <para>- 仅支持上传 docx、txt、pdf、pptx 类型的文件；</para>
    /// <para>- 开发者需要 Aily 创建平台的应用协作者角色（管理员、开发者、运维人员）。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/data-knowledge-management/upload_file">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_dsafdsaf__c</param>
    /// <param name="request">上传文件请求体（本地文件路径）</param>
    /// <param name="tenant_type">应用环境，枚举值：online（线上环境，默认值）、dev（开发环境）；目前只支持 dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>上传成功后返回文件 token 与解析出的文件 MIME 类型</returns>
    [Post("/open-apis/aily/v1/apps/{app_id}/data_assets/upload_file")]
    Task<FeishuApiResult<UploadDataAssetFileResult>?> UploadDataAssetFileAsync(
        [Path] string app_id,
        [FormContent] UploadDataAssetFileRequest request,
        [Query("tenant_type")] string? tenant_type = "dev",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建数据知识
    /// <para>用于在某个 Aily 应用中添加单个数据知识（Data Asset），支持文件导入、飞书云文档/知识空间/服务台直连等数据源。</para>
    /// <para>注意事项：仅支持开发环境；开发者需要 Aily 平台的应用协作者角色。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/data-knowledge-management/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_dfasdf__c</param>
    /// <param name="request">创建数据知识请求体</param>
    /// <param name="tenant_type">应用环境，枚举值：online（线上环境，默认值）、dev（开发环境）；目前只支持 dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>创建成功后返回数据知识信息</returns>
    [Post("/open-apis/aily/v1/apps/{app_id}/data_assets")]
    Task<FeishuApiResult<DataAssetOopsResult>?> CreateDataAssetAsync(
        [Path] string app_id,
        [Body] CreateDataAssetRequest request,
        [Query("tenant_type")] string? tenant_type = "dev",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取数据知识
    /// <para>用于获取某个 Aily 应用单个数据知识（Data Asset）的详细信息。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/data-knowledge-management/get">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_feafdsaf__c</param>
    /// <param name="data_asset_id">数据知识 ID，示例值：data_asset_dafefadsaf1</param>
    /// <param name="with_data_asset_item">结果是否包含数据与知识项</param>
    /// <param name="with_connect_status">结果是否包含数据知识连接状态</param>
    /// <param name="tenant_type">应用环境，默认为线上环境，dev 代表开发环境</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据知识详细信息</returns>
    [Get("/open-apis/aily/v1/apps/{app_id}/data_assets/{data_asset_id}")]
    Task<FeishuApiResult<DataAssetOopsResult>?> GetDataAssetAsync(
        [Path] string app_id,
        [Path] string data_asset_id,
        [Query("with_data_asset_item")] bool? with_data_asset_item = null,
        [Query("with_connect_status")] bool? with_connect_status = null,
        [Query("tenant_type")] string? tenant_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除数据知识
    /// <para>用于删除某个 Aily 应用的单个数据知识（Data Asset）。</para>
    /// <para>注意事项：仅支持开发环境。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/data-knowledge-management/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_dfadsaf__c</param>
    /// <param name="data_asset_id">数据知识 ID，示例值：data_asset_dfadsafe</param>
    /// <param name="tenant_type">应用环境，枚举值：online（线上环境，默认值）、dev（开发环境）；目前只支持 dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被删除的数据知识信息</returns>
    [Delete("/open-apis/aily/v1/apps/{app_id}/data_assets/{data_asset_id}")]
    Task<FeishuApiResult<DataAssetOopsResult>?> DeleteDataAssetAsync(
        [Path] string app_id,
        [Path] string data_asset_id,
        [Query("tenant_type")] string? tenant_type = "dev",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取数据知识列表
    /// <para>用于获取某个 Aily 应用的数据知识（Data Asset）列表，支持分页与多条件过滤。</para>
    /// <para>查询参数采用查询对象模式（<see cref="DataAssetListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/data-knowledge-management/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_5862e4fea8__c</param>
    /// <param name="query">分页与过滤查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据知识分页列表</returns>
    [Get("/open-apis/aily/v1/apps/{app_id}/data_assets")]
    Task<FeishuApiResult<DataAssetPageListResult>?> GetDataAssetPageListAsync(
        [Path] string app_id,
        [Query] DataAssetListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取数据知识分类列表
    /// <para>用于获取某个 Aily 应用的数据知识分类（Data Asset Tag）列表，支持分页与模糊匹配。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/data-knowledge/data-knowledge-management/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">Aily 应用 ID（spring_xxx__c），示例值：spring_5862e4fea8__c</param>
    /// <param name="page_size">分页大小，默认 20，最大 100。示例值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="keyword">模糊匹配分类名称，示例值：电影</param>
    /// <param name="data_asset_tag_ids">按数据知识分类 ID 过滤，示例值：spring_5862e4fea8__c__asset_tag_aadg2b5ql4gbs</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据知识分类分页列表</returns>
    [Get("/open-apis/aily/v1/apps/{app_id}/data_asset_tags")]
    Task<FeishuApiResult<DataAssetTagPageListResult>?> GetDataAssetTagPageListAsync(
        [Path] string app_id,
        [Query("page_size")] int page_size = Consts.PageSize_20,
        [Query("page_token")] string? page_token = null,
        [Query] string? keyword = null,
        [Query] string[]? data_asset_tag_ids = null,
        CancellationToken cancellationToken = default);
}
