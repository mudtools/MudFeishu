// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书的筛选视图是解决在线表格协作中“互相干扰”问题的关键功能，同时也是一个强大的数据组织和分发工具，帮助团队在共享一份数据源的同时，拥有各自独立的、高效的观察视角。
/// <para>本接口提供飞书开放平台电子表格中筛选视图能力相关方法。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV3SpreadsheetFilterView : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建筛选视图
    /// <para>指定电子表格工作表的筛选范围，创建一个筛选视图。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="createFilterViewRequest">创建筛选视图请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter_views")]
    Task<FeishuApiResult<FilterViewResult>?> CreateFilterViewAsync(
        [Path] string spreadsheet_token,
        [Path] string sheet_id,
        [Body] CreateFilterViewRequest createFilterViewRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新筛选视图
    /// <para>更新筛选视图的名称或筛选范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="filter_view_id">筛选视图 ID。示例值："pH9hbVcCXA"</param>
    /// <param name="updateFilterViewRequest">更新筛选视图请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter_views/{filter_view_id}")]
    Task<FeishuApiResult<FilterViewResult>?> UpdateFilterViewAsync(
       [Path] string spreadsheet_token,
       [Path] string sheet_id,
       [Path] string filter_view_id,
       [Body] UpdateFilterViewRequest updateFilterViewRequest,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 查询筛选视图
    /// <para>查询电子表格指定工作表的所有筛选视图及其基本信息，包括视图 ID、视图名称和筛选范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter_views/query")]
    Task<FeishuApiResult<GetFilterViewsResult>?> GetFilterViewsAsync(
         [Path] string spreadsheet_token,
         [Path] string sheet_id,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取筛选视图
    /// <para>获取指定筛选视图的信息，包括 ID、名称和筛选范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="filter_view_id">筛选视图 ID。示例值："pH9hbVcCXA"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter_views/{filter_view_id}")]
    Task<FeishuApiResult<FilterViewResult>?> GetFilterViewByIdAsync(
        [Path] string spreadsheet_token,
        [Path] string sheet_id,
        [Path] string filter_view_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除筛选视图
    /// <para>删除指定筛选视图。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="filter_view_id">筛选视图 ID。示例值："pH9hbVcCXA"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter_views/{filter_view_id}")]
    Task<FeishuNullDataApiResult?> DeleteFilterViewByIdAsync(
      [Path] string spreadsheet_token,
      [Path] string sheet_id,
      [Path] string filter_view_id,
      CancellationToken cancellationToken = default);
}
