// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 筛选指在电子表格工作表指定范围中，为指定列（col）设置筛选条件。
/// <para>本接口提供飞书开放平台电子表格中筛选能力相关方法列表。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV3SpreadsheetFilter : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建筛选
    /// <para>在电子表格工作表的指定范围内，设置筛选条件，创建筛选。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="createFilterRequest">创建筛选请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter")]
    Task<FeishuNullDataApiResult?> CreateFilterAsync(
        [Path] string spreadsheet_token,
        [Path] string sheet_id,
        [Body] CreateFilterRequest createFilterRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新筛选
    /// <para>在电子表格工作表筛选范围中，更新指定列的筛选条件。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="updateFilterRequest">更新筛选请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter")]
    Task<FeishuNullDataApiResult?> UpdateFilterAsync(
        [Path] string spreadsheet_token,
        [Path] string sheet_id,
        [Body] UpdateFilterRequest updateFilterRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取筛选
    /// <para>获取电子表格中工作表的详细筛选信息，包括筛选的应用范围、筛选条件、被筛选条件过滤掉的行。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter")]
    Task<FeishuApiResult<GetFilterResult>?> GetFilterAsync(
       [Path] string spreadsheet_token,
       [Path] string sheet_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除筛选
    /// <para>删除电子表格中指定工作表的所有筛选。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter")]
    Task<FeishuNullDataApiResult?> DeleteFilterAsync(
       [Path] string spreadsheet_token,
       [Path] string sheet_id,
       CancellationToken cancellationToken = default);
}
