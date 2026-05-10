// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 范围(range)：在工作表中进行读取数据、写入数据、筛选数据等各类操作时，需要通过 范围 range 参数指定操作数据的范围。
/// <para>在工作表中进行读取数据、写入数据、筛选数据等各类操作时，你需要通过 范围 range 参数指定操作数据的范围。range 参数的格式为 &lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;。其中
/// <list type="bullet">
/// <item>sheetId 为工作表的唯一标识，通过获取工作表 获取。</item>
/// <item>&lt;开始位置&gt;:&lt;结束位置&gt; 为工作表中单元格的范围，使用数字表示行索引，字母表示列索引。如 A2:B2 表示该工作表第 2 行的 A 列到 B 列。</item>
/// </list></para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV3SpreadsheetRange : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 增加行列
    /// <para>用于在电子表格工作表中增加空白行或列。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="createRangeRequest">增加行列请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dimension_range")]
    Task<FeishuApiResult<CreateRangeResult>?> CreateRangeAsync(
         [Path] string spreadsheet_token,
         [Body] CreateRangeRequest createRangeRequest,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 插入行列
    /// <para>用于在电子表格的指定位置插入空白行或列。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="insertRangeRequest">插入行列请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/insert_dimension_range")]
    Task<FeishuNullDataApiResult?> InsertRangeAsync(
        [Path] string spreadsheet_token,
        [Body] InsertRangeRequest insertRangeRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新行列
    /// <para>用于更新设置电子表格中行列的属性，包括是否隐藏行列和设置行高列宽。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="insertRangeRequest">更新行列请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dimension_range")]
    Task<FeishuNullDataApiResult?> UpdateRangeAsync(
      [Path] string spreadsheet_token,
      [Body] UpdateRangeRequest insertRangeRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 移动行列
    /// <para>用于移动行或列。行或列被移动到目标位置后，原本在目标位置的行列会对应右移或下移。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="moveRangeRequest">更新行列请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/move_dimension")]
    Task<FeishuNullDataApiResult?> MoveRangeAsync(
         [Path] string spreadsheet_token,
         [Path] string sheet_id,
         [Body] MoveRangeRequest moveRangeRequest,
         CancellationToken cancellationToken = default);



    /// <summary>
    /// 删除行列
    /// <para>用于删除电子表格中的指定行或列。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="deleteRangeRequest">删除行列请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dimension_range")]
    Task<FeishuNullDataApiResult?> DeleteRangeAsync(
         [Path] string spreadsheet_token,
         [Body] DeleteRangeRequest deleteRangeRequest,
         CancellationToken cancellationToken = default);
}
