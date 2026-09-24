// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）数据表 SDK 是一组服务端 OpenAPI 的封装，用于以编程方式管理妙搭应用下的数据表与数据记录。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkAppTable : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 获取数据表列表
    /// <para>获取应用下的数据表列表，包含数据表名称、描述，以及数据表列信息等字段。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/get_table_list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取，如 https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v 中的 app_4jcn5n11bpf5v 即为 app_id</param>
    /// <param name="page_size">分页大小，用于限制一次请求所返回的数据条目数。默认 10，最大 500。示例值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据表分页列表</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/tables")]
    Task<FeishuApiResult<GetTableListResult>?> GetTableListAsync(
        [Path] string app_id,
        [Query("page_size")] int page_size = 10,
        [Query("page_token")] string? page_token = null,
        [Query("env")] string? env = "online",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取数据表详细信息
    /// <para>获取应用下的数据表详情，包含数据表名称、描述，以及数据表列信息等字段。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/get_table_detail">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="table_name">妙搭数据表表名，必须属于 app_id 对应的妙搭应用。示例值：student_table</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据表详细信息</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/tables/{table_name}")]
    Task<FeishuApiResult<AppTable>?> GetTableDetailAsync(
        [Path] string app_id,
        [Path] string table_name,
        [Query("env")] string? env = "online",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询数据表数据记录
    /// <para>查询应用下的数据表数据记录，包括指定列、字段值及分页信息，适用于需要获取应用下某数据表数据的记录、展示等场景。</para>
    /// <para>查询参数采用查询对象模式（<see cref="GetTableRecordListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/get_table_record_list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="table_name">妙搭数据表表名，必须属于 app_id 对应的妙搭应用。示例值：student_table</param>
    /// <param name="query">分页、列筛选、过滤、排序、环境与用户 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据记录分页列表（items 为数组序列化后的 JSONString）</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/tables/{table_name}/records")]
    Task<FeishuApiResult<GetTableRecordListResult>?> GetTableRecordListAsync(
        [Path] string app_id,
        [Path] string table_name,
        [Query] GetTableRecordListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 向数据表中添加或更新记录
    /// <para>向应用下的数据表中添加或更新记录。</para>
    /// <para>可选请求头 <c>Prefer</c> 控制行为：为空表示 INSERT；<c>resolution=merge-duplicates</c> 表示 UPSERT；<c>missing=default</c> 代表未传入的字段按默认值处理；可组合，如 <c>resolution=merge-duplicates, missing=default</c>。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/post_table_records">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="table_name">妙搭数据表表名，必须属于 app_id 对应的妙搭应用。示例值：student_table</param>
    /// <param name="request">添加或更新记录请求体（records 为 JSON 数组字符串，单次最大 500 条）</param>
    /// <param name="columns">UPSERT 时使用，指定列，多列英文逗号拼接。示例值：name,age</param>
    /// <param name="on_conflict">UPSERT 时使用，指定使用哪一个或多个具有唯一约束的字段作为冲突判断依据，默认为表主键。示例值：user_id,product_id</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="user_identifier_type">此次调用使用的用户 ID 类型。示例值：miaoda_user_id、open_id、union_id。默认值：miaoda_user_id</param>
    /// <param name="prefer">可选请求头 Prefer，如 resolution=merge-duplicates（UPSERT）、missing=default，可逗号组合</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回按记录顺序创建或更新的记录 ID 列表</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/tables/{table_name}/records")]
    Task<FeishuApiResult<UpsertTableRecordsResult>?> PostTableRecordsAsync(
        [Path] string app_id,
        [Path] string table_name,
        [Body] PostTableRecordsRequest request,
        [Query("columns")] string? columns = null,
        [Query("on_conflict")] string? on_conflict = null,
        [Query("env")] string? env = "online",
        [Query("user_identifier_type")] string? user_identifier_type = "miaoda_user_id",
        [Header("Prefer")] string? prefer = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 按条件更新数据表中的记录
    /// <para>将数据表中符合 filter 条件的记录更新为 record 参数指定的内容。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/patch_table_records">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="table_name">妙搭数据表表名，必须属于 app_id 对应的妙搭应用。示例值：student_table</param>
    /// <param name="request">更新记录请求体（record 为 JSON 对象字符串）</param>
    /// <param name="filter">筛选条件，遵循 PostgREST 语法（horizontal filtering）。示例值：age=gt.10</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="user_identifier_type">此次调用使用的用户 ID 类型。示例值：miaoda_user_id、open_id、union_id。默认值：miaoda_user_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新的记录唯一 ID 列表</returns>
    [Patch("/open-apis/spark/v1/apps/{app_id}/tables/{table_name}/records")]
    Task<FeishuApiResult<UpsertTableRecordsResult>?> PatchTableRecordsAsync(
        [Path] string app_id,
        [Path] string table_name,
        [Body] PatchTableRecordsRequest request,
        [Query("filter")] string filter,
        [Query("env")] string? env = "online",
        [Query("user_identifier_type")] string? user_identifier_type = "miaoda_user_id",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量更新数据表中的记录
    /// <para>批量更新应用下的数据表中的记录，每条记录需包含主键如 _id，单次最多 500 条，且不同行要更新的字段需保持一致。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/batch_update_table_records">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="table_name">妙搭数据表表名，必须属于 app_id 对应的妙搭应用。示例值：student_table</param>
    /// <param name="request">批量更新记录请求体（records 为 JSON 数组字符串，单次最大 500 条）</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="user_identifier_type">此次调用使用的用户 ID 类型。示例值：miaoda_user_id、open_id、union_id。默认值：miaoda_user_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新的记录唯一 ID 列表</returns>
    [Patch("/open-apis/spark/v1/apps/{app_id}/tables/{table_name}/records_batch_update")]
    Task<FeishuApiResult<UpsertTableRecordsResult>?> BatchUpdateTableRecordsAsync(
        [Path] string app_id,
        [Path] string table_name,
        [Body] BatchUpdateTableRecordsRequest request,
        [Query("env")] string? env = "online",
        [Query("user_identifier_type")] string? user_identifier_type = "miaoda_user_id",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除数据表中的记录
    /// <para>删除指定应用下数据表中符合 filter 筛选条件的记录，删除后记录不可恢复。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-table/delete_table_records">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="table_name">妙搭数据表表名，必须属于 app_id 对应的妙搭应用。示例值：student_table</param>
    /// <param name="filter">筛选条件，遵循 PostgREST 语法（horizontal filtering），此处用法和查询数据记录一致。示例值：age=gt.10</param>
    /// <param name="env">访问的 database 环境，默认为 online（线上环境）。示例值：online、dev</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>删除成功时 data 为空对象</returns>
    [Delete("/open-apis/spark/v1/apps/{app_id}/tables/{table_name}/records")]
    Task<FeishuNullDataApiResult?> DeleteTableRecordsAsync(
        [Path] string app_id,
        [Path] string table_name,
        [Query("filter")] string filter,
        [Query("env")] string? env = "online",
        CancellationToken cancellationToken = default);
}
