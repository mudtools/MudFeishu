// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书开放平台电子表格工作表中的单元格处理功能。
/// <para>在工作表单元格中进行读取数据、写入数据、筛选数据等各类操作时。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV3SpreadsheetCell : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 合并单元格
    /// <para>合并电子表格工作表中的单元格。。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="mergeCellsRequest">合并单元格请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/merge_cells")]
    Task<FeishuApiResult<CellsOpsResult>?> MergeCellsAsync(
       [Path] string spreadsheet_token,
       [Body] MergeCellsRequest mergeCellsRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 拆分单元格
    /// <para>拆分电子表格工作表中的单元格。。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="unMergeCellsRequest">拆分单元格请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/unmerge_cells")]
    Task<FeishuApiResult<CellsOpsResult>?> UnMergeCellsAsync(
      [Path] string spreadsheet_token,
      [Body] UnMergeCellsRequest unMergeCellsRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查找单元格
    /// <para>在指定范围内查找符合查找条件的单元格。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="getCellsRequest">查找单元格请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/find")]
    Task<FeishuApiResult<GetCellsResult>?> GetCellsAsync(
        [Path] string spreadsheet_token,
        [Path] string sheet_id,
        [Body] GetCellsRequest getCellsRequest,
        CancellationToken cancellationToken = default);
}
