// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>记录 record是多维表格的数据表中的每一行数据都是一条记录（record）。</para>
/// <para>每条记录都有唯一标识 record_id，record_id 在一个多维表格中唯一，在全局不一定唯一。record_id 需要通过查询记录接口获取。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/bitable-v1/app-table-record/bitable-record-data-structure-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV1BitableRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 新增记录
    /// <para>在多维表格数据表中新增一条记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-record/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="addRecordRequest">新增记录请求体</param>
    /// <param name="client_token">
    /// <para>格式为标准的 uuidv4，操作的唯一标识，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para> /// </param>
    /// <param name="ignore_consistency_check">
    /// <para>是否忽略一致性读写检查，默认为 false，即在进行读写操作时，系统将确保读取到的数据和写入的数据是一致的。可选值：</para>
    /// <para>- true：忽略读写一致性检查，提高性能，但可能会导致某些节点的数据不同步，出现暂时不一致</para>
    /// <para>- false：开启读写一致性检查，确保数据在读写过程中一致</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records")]
    Task<FeishuApiResult<RecordOpsResult>?> AddRecordAsync(
         [Path] string app_token,
         [Path] string table_id,
         [Body] RecordOpsRequest addRecordRequest,
         [Query("client_token")] string? client_token = null,
         [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新记录
    /// <para>更新多维表格数据表中的一条记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-record/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="record_id">
    /// <para>数据表中一条记录的唯一标识。</para>
    /// <para>示例值：recqwIwhc6</para>
    /// </param>
    /// <param name="updateRecordRequest">更新记录请求体</param>
    /// <param name="client_token">
    /// <para>格式为标准的 uuidv4，操作的唯一标识，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para> 
    /// </param>
    /// <param name="ignore_consistency_check">
    /// <para>是否忽略一致性读写检查，默认为 false，即在进行读写操作时，系统将确保读取到的数据和写入的数据是一致的。可选值：</para>
    /// <para>- true：忽略读写一致性检查，提高性能，但可能会导致某些节点的数据不同步，出现暂时不一致</para>
    /// <para>- false：开启读写一致性检查，确保数据在读写过程中一致</para>
    /// <para>示例值：true</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/{record_id}")]
    Task<FeishuApiResult<RecordOpsResult>?> UpdateRecordAsync(
       [Path] string app_token,
       [Path] string table_id,
       [Path] string record_id,
       [Body] RecordOpsRequest updateRecordRequest,
       [Query("client_token")] string? client_token = null,
       [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询记录
    /// <para>用于查询数据表中的现有记录，单次最多查询 500 行记录，支持分页获取。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/app-table-record/search">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="queryRecordsRequest">查询记录请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/search")]
    Task<FeishuApiPageListTotalResult<AppTableRecord>?> QueryRecordsPageListAsync(
        [Path] string app_token,
        [Path] string table_id,
        [Body] QueryRecordsRequest queryRecordsRequest,
        [Query("page_size")] int page_size = 20,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除记录
    /// <para>删除多维表格数据表中的一条记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-record/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="record_id">
    /// <para>数据表中一条记录的唯一标识。</para>
    /// <para>示例值：recqwIwhc6</para>
    /// </param>
    /// <param name="ignore_consistency_check">
    /// <para>是否忽略一致性读写检查，默认为 false，即在进行读写操作时，系统将确保读取到的数据和写入的数据是一致的。可选值：</para>
    /// <para>- true：忽略读写一致性检查，提高性能，但可能会导致某些节点的数据不同步，出现暂时不一致</para>
    /// <para>- false：开启读写一致性检查，确保数据在读写过程中一致</para>
    /// <para>示例值：true</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/{record_id}")]
    Task<FeishuApiResult<DeleteRecordResult>?> DeleteRecordAsync(
        [Path] string app_token,
        [Path] string table_id,
        [Path] string record_id,
        [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 新增多条记录
    /// <para>在多维表格数据表中新增多条记录，单次调用最多新增 1,000 条记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-record/batch_create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="addRecordsRequest">新增多条记录请求体</param>
    /// <param name="client_token">
    /// <para>格式为标准的 uuidv4，操作的唯一标识，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para> /// </param>
    /// <param name="ignore_consistency_check">
    /// <para>是否忽略一致性读写检查，默认为 false，即在进行读写操作时，系统将确保读取到的数据和写入的数据是一致的。可选值：</para>
    /// <para>- true：忽略读写一致性检查，提高性能，但可能会导致某些节点的数据不同步，出现暂时不一致</para>
    /// <para>- false：开启读写一致性检查，确保数据在读写过程中一致</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_create")]
    Task<FeishuApiResult<RecordsOpsResult>?> AddRecordsAsync(
         [Path] string app_token,
         [Path] string table_id,
         [Body] AddRecordsRequest addRecordsRequest,
         [Query("client_token")] string? client_token = null,
         [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新多条记录
    /// <para>更新数据表中的多条记录，单次调用最多更新 1,000 条记录。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-record/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="updateRecordsRequest">更新记录请求体</param>
    /// <param name="ignore_consistency_check">
    /// <para>是否忽略一致性读写检查，默认为 false，即在进行读写操作时，系统将确保读取到的数据和写入的数据是一致的。可选值：</para>
    /// <para>- true：忽略读写一致性检查，提高性能，但可能会导致某些节点的数据不同步，出现暂时不一致</para>
    /// <para>- false：开启读写一致性检查，确保数据在读写过程中一致</para>
    /// <para>示例值：true</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_update")]
    Task<FeishuApiResult<RecordsOpsResult>?> UpdateRecordsAsync(
       [Path] string app_token,
       [Path] string table_id,
       [Body] UpdateRecordsRequest updateRecordsRequest,
       [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}