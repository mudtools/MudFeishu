// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>视图 view 是多维表格数据的汇总和展现形式。视图有多种类型，包括表格视图、看板视图、画册视图、甘特视图和表单视图等，可参考飞书帮助中心文档视图类型。</para>
/// <para>一个数据表至少有一个视图，可能有多个视图。每个视图都有唯一标识 view_id，view_id 在一个多维表格中唯一，在全局不一定唯一。</para>
/// <para>可通过多维表格 URL 获取 view_id，也可通过列出视图接口获取 view_id。暂时无法获取到嵌入到文档中的多维表格的 view_id。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/bitable-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1BitableView : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 新增视图
    /// <para>在多维表格数据表中新增一个视图，可指定视图类型，包括表格视图、看板视图、画册视图、甘特视图和表单视图。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-view/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="createViewRequest">创建多维表格应用视图请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/views")]
    Task<FeishuApiResult<CreateViewResult>?> CreateViewAsync(
        [Path] string app_token,
        [Path] string table_id,
        [Body] CreateViewRequest createViewRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 新增视图
    /// <para>在多维表格数据表中新增一个视图，可指定视图类型，包括表格视图、看板视图、画册视图、甘特视图和表单视图。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-view/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="view_id">
    /// <para>多维表格中视图的唯一标识。</para>
    /// <para>示例值：vewTpR1urY</para>
    /// </param>
    /// <param name="updateViewRequest">更新多维表格应用视图请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/views/{view_id}")]
    Task<FeishuApiResult<UpdateViewResult>?> UpdateViewAsync(
       [Path] string app_token,
       [Path] string table_id,
       [Path] string view_id,
       [Body] UpdateViewRequest updateViewRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页列出视图
    /// <para>分页获取多维表格数据表中的所有视图。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-view/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/views")]
    Task<FeishuApiPageListTotalResult<AppViewDetailInfo>?> GetViewsPageListAsync(
       [Path] string app_token,
       [Path] string table_id,
       [Query("page_size")] int page_size = 20,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取视图
    /// <para>根据视图 ID 获取现有视图信息，包括视图名称、类型、属性等。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-view/get">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="view_id">
    /// <para>多维表格中视图的唯一标识。</para>
    /// <para>示例值：vewTpR1urY</para>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型与查询结果中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/views/{view_id}")]
    Task<FeishuApiResult<GetViewResult>?> GetViewAsync(
       [Path] string app_token,
       [Path] string table_id,
       [Path] string view_id,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除视图
    /// <para>通过 app_token、table_id 和 view_id，删除多维表格数据表中的指定视图。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-view/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="view_id">
    /// <para>多维表格中视图的唯一标识。</para>
    /// <para>示例值：vewTpR1urY</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/views/{view_id}")]
    Task<FeishuApiResult<GetViewResult>?> DeleteViewAsync(
          [Path] string app_token,
          [Path] string table_id,
          [Path] string view_id,
          CancellationToken cancellationToken = default);
}