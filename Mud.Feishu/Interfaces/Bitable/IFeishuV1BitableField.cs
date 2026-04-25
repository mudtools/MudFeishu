// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>字段 field即多维表格的“列”，多维表格提供丰富的字段类型。</para>
/// <para>每个字段都有唯一标识 field_id，field_id 在一个多维表格内唯一，在全局不一定唯一。field_id 需要通过列出字段接口获取。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-field/guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1BitableField : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 新增字段
    /// <para>在多维表格数据表中新增一个字段。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-field/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="addFieldRequest">新增多维表格字段操作请求体</param>
    /// <param name="client_token">
    /// <para>格式为标准的 uuidv4，操作的唯一标识，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作。</para>
    /// <para>示例值：fe599b60-450f-46ff-b2ef-9f6675625b97</para>
    /// <para>默认值：null</para> 
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/fields")]
    Task<FeishuApiResult<FieldOpsResult>?> AddFieldAsync(
      [Path] string app_token,
      [Path] string table_id,
      [Body] AddFieldRequest addFieldRequest,
      [Query("client_token")] string? client_token = null,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新字段
    /// <para>在多维表格数据表中更新一个字段。更新字段时为全量更新，property 等字段会被完全覆盖。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-field/update">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="field_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>数据表中一个字段的唯一标识。</para>
    /// <para>示例值：fldPTb0U2y</para>
    /// </param>
    /// <param name="updateFieldRequest">更新多维表格字段操作请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/fields/{field_id}")]
    Task<FeishuApiResult<FieldOpsResult>?> UpdateFieldAsync(
         [Path] string app_token,
         [Path] string table_id,
         [Path] string field_id,
         [Body] UpdateFieldRequest updateFieldRequest,
         CancellationToken cancellationToken = default);



    /// <summary>
    /// 列出字段
    /// <para>获取多维表格数据表中的的所有字段。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-field/list">接口文档</see></para>
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
    /// <para>示例值：vewOVMEXPF</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="text_field_as_array">
    /// <para>必填：否</para>
    /// <para>控制字段描述 `description` 数据的返回格式，默认为 false。true 表示 `description` 将以数组形式返回，如：</para>
    /// <para>```json</para>
    /// <para>{</para>
    /// <para>"description": [</para>
    /// <para>{</para>
    /// <para>"text": "字段的描述",</para>
    /// <para>"type": "text"</para>
    /// <para>}</para>
    /// <para>]</para>
    /// <para>}</para>
    /// <para>```</para>
    /// <para>示例值：true</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/fields")]
    Task<FeishuApiPageListTotalResult<AppTableFieldInfo>?> QueryRecordsPageListAsync(
      [Path] string app_token,
      [Path] string table_id,
      [Query("view_id")] string? view_id = null,
      [Query("text_field_as_array")] bool? text_field_as_array = null,
      [Query("page_size")] int page_size = Consts.PageSize_500,
      [Query("page_token")] string? page_token = null,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除字段
    /// <para>删除多维表格数据表中的一个字段。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-field/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="field_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>数据表中一个字段的唯一标识。</para>
    /// <para>示例值：fldPTb0U2y</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/fields/{field_id}")]
    Task<FeishuApiResult<DeleteFieldResult>?> DeleteFieldAsync(
     [Path] string app_token,
     [Path] string table_id,
     [Path] string field_id,
     CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建字段编组
    /// <para>用于为多维表格数据表的字段创建编组。创建字段编组后，字段将被组织到该编组中，便于多维表格的数据管理。</para>
    /// <para>适用于多维表格字段较多，需要分类管理字段的场景</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-field_group/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="table_id">
    /// <para>多维表格数据表的唯一标识。</para>
    /// <para>示例值：tbl1TkhyTWDkSoZ3</para>
    /// </param>
    /// <param name="createFieldGroupRequest">新增字段编组请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/field_groups")]
    Task<FeishuApiResult<CreateFieldGroupResult>?> CreateFieldGroupAsync(
     [Path] string app_token,
     [Path] string table_id,
     [Body] CreateFieldGroupRequest createFieldGroupRequest,
     CancellationToken cancellationToken = default);
}
