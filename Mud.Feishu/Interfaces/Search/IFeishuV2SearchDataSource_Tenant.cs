// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Search;

namespace Mud.Feishu;

/// <summary>
/// 飞书搜索连接器提供了一组RESTful API，帮助企业快速实现飞书之外的各类信息的搜索能力，在飞书内即可实现一站式的信息检索和获取，有效提升工作和协同效率。
/// <para>开发者使用数据源API建立数据源，再通过数据API将数据推送到该数据源，就完成了飞书搜索能力的构建。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/search-connector-overview--"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Search")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV2SearchDataSource : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建数据源。
    /// <para>创建搜索数据源。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建数据源请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/search/v2/data_sources")]
    Task<FeishuApiResult<CreateDataSourceResult>?> CreateDataSourceAsync(
       [Body] CreateDataSourceRequest request,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除数据源。
    /// <para>删除一个已存在的数据源。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/delete">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/search/v2/data_sources/{data_source_id}")]
    Task<FeishuNullDataApiResult?> DeleteDataSourceAsync(
      [Path] string data_source_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新数据源。
    /// <para>更新一个已存在的数据源。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/patch">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="request">更新数据源请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/search/v2/data_sources/{data_source_id}")]
    Task<FeishuNullDataApiResult?> UpdateDataSourceAsync(
      [Path] string data_source_id,
      [Body] UpdateDataSourceRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取数据源。
    /// <para>获取已经创建的数据源。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/get">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/search/v2/data_sources/{data_source_id}")]
    Task<FeishuApiResult<GetDataSourceResult>?> GetDataSourceAsync(
      [Path] string data_source_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取数据源。
    /// <para>批量获取创建的数据源信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/list">接口文档</see></para>
    /// </summary>
    /// <param name="view">
    /// <para>回包数据格式，0-全量数据；1-摘要数据。</para>
    /// <para>**注**：摘要数据仅包含"id"，"name"，"state"。</para>
    /// <para>**示例值**：0</para>
    /// <para>**可选值有**：</para>
    /// <para>0:全量数据,1:摘要数据</para>
    /// <list type="bullet">
    /// <item>0：全量数据</item>
    /// <item>1：摘要数据</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/search/v2/data_sources")]
    Task<FeishuApiPageListResult<AppDataSourceInfo>?> GetDataSourcePageListAsync(
        [Query] int? view = null,
        [Query] int? page_size = 20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 为指定数据项创建索引。
    /// <para>索引一条数据记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source-item/create">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="request">创建数据项索引请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/search/v2/data_sources/{data_source_id}/items")]
    Task<FeishuNullDataApiResult?> CreateDataItemIndexAsync(
        [Path] string data_source_id,
        [Body] CreateDataItemIndexRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量为数据项创建索引。
    /// <para>为提高索引数据记录的速度，特提供批量索引数据记录的接口。</para>
    /// <para>注意：一个batch中所有数据项的datasourceID需要一致，tenantID也需要一致</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source-item/batch_create">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="request">创建数据项索引请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/search/v2/data_sources/{data_source_id}/items/batch_create")]
    Task<FeishuApiResult<BatchCreateDataItemIndexResult>?> BatchCreateDataItemIndexAsync(
       [Path] string data_source_id,
       [Body] BatchCreateDataItemIndexRequest request,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除数据项。
    /// <para>删除数据项。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/delete">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="item_id">
    /// <para>数据记录的ID</para>
    /// <para>**示例值**："01010111"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/search/v2/data_sources/{data_source_id}/items/{item_id}")]
    Task<FeishuNullDataApiResult?> DeleteDataItemIndexAsync(
      [Path] string data_source_id,
      [Path] string item_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询指定数据项。
    /// <para>获取单个数据记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source-item/get?appId=cli_a98ea7d1a0ba100b">接口文档</see></para>
    /// </summary>
    /// <param name="data_source_id">
    /// <para>数据源的唯一标识</para>
    /// <para>**示例值**："6953903108179099667"</para>
    /// </param>
    /// <param name="item_id">
    /// <para>数据记录的ID</para>
    /// <para>**示例值**："01010111"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/search/v2/data_sources/{data_source_id}/items/{item_id}")]
    Task<FeishuApiResult<DataItemIndex>?> GetDataItemIndexAsync(
        [Path] string data_source_id,
        [Path] string item_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建数据范式。
    /// <para>创建一个数据范式。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建数据范式请求体</param>
    /// <param name="validate_only">
    /// <para>是否只用来校验合法性</para>
    /// <para>**示例值**：true</para>
    /// <para>**默认值**：`false`</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/search/v2/schemas")]
    Task<FeishuApiResult<CreateSchemaResult>?> CreateSchemaAsync(
      [Body] CreateSchemaRequest request,
      [Query] bool? validate_only = null,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除数据范式。
    /// <para>删除已存在的数据范式。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/delete">接口文档</see></para>
    /// </summary>
    /// <param name="schema_id">
    /// <para>用户自定义数据范式的唯一标识</para>
    /// <para>**示例值**："custom_schema_id"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`40` 字符</para>
    /// <para>- 正则校验：`^[a-zA-Z][a-zA-Z0-9-_].*$`</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/search/v2/schemas/{schema_id}")]
    Task<FeishuNullDataApiResult?> DeleteSchemaAsync(
       [Path] string schema_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改数据范式。
    /// <para>修改数据范式。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/patch">接口文档</see></para>
    /// </summary>
    /// <param name="schema_id">
    /// <para>用户自定义数据范式的唯一标识</para>
    /// <para>**示例值**："custom_schema_id"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`40` 字符</para>
    /// <para>- 正则校验：`^[a-zA-Z][a-zA-Z0-9-_].*$`</para>
    /// </param>
    /// <param name="request">修改数据范式请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/search/v2/schemas/{schema_id}")]
    Task<FeishuApiResult<UpdateSchemaResult>?> UpdateSchemaAsync(
        [Path] string schema_id,
        [Body] UpdateSchemaRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取数据范式。
    /// <para>获取单个数据范式。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/get">接口文档</see></para>
    /// </summary>
    /// <param name="schema_id">
    /// <para>用户自定义数据范式的唯一标识</para>
    /// <para>**示例值**："custom_schema_id"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`40` 字符</para>
    /// <para>- 正则校验：`^[a-zA-Z][a-zA-Z0-9-_].*$`</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/search/v2/schemas/{schema_id}")]
    Task<FeishuApiResult<GetSchemaResult>?> GetSchemaAsync(
       [Path] string schema_id,
       CancellationToken cancellationToken = default);
}
