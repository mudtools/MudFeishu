// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书开放平台电子表格工作表中的数据处理相关功能。
/// <para>在工作表中进行读取数据、写入数据、写入图片等各类操作时。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV3SpreadsheetData : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 插入数据
    /// <para>在电子表格工作表的指定范围的起始位置上方增加若干行，并在该范围中填充数据。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="insertDataRequest">插入数据请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/values_prepend")]
    Task<FeishuApiResult<RangeDataOpsResult>?> InsertDataAsync(
     [Path] string spreadsheet_token,
     [Body] RangeDataOpsRequest insertDataRequest,
     CancellationToken cancellationToken = default);


    /// <summary>
    /// 插入数据
    /// <para>在电子表格工作表的指定范围中，在空白位置中追加数据。例如，若指定范围参数 range 为 6e5ed3!A1:B2，该接口将会依次寻找 A1、A2、A3...单元格，在找到的第一个空白位置中写入数据。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="appendDataRequest">追加数据请求体</param>
    /// <param name="insertDataOption">
    /// <para>指定追加数据的方式，默认值为 OVERWRITE，即若空行数量小于追加数据的行数，则会覆盖已有数据。可选值：</para>
    /// <para>- OVERWRITE：若空行的数量小于追加数据的行数，则会覆盖已有数据</para>
    /// <para>- INSERT_ROWS：插入足够数量的行后再进行数据追加</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/values_append")]
    Task<FeishuApiResult<RangeDataOpsResult>?> AppendDataAsync(
        [Path] string spreadsheet_token,
        [Body] RangeDataOpsRequest appendDataRequest,
        [Query("insertDataOption")] string? insertDataOption = "OVERWRITE",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 写入图片
    /// <para>向电子表格某个工作表的单个指定单元格写入图片，支持传入图片的二进制流，支持多种图片格式。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="imageDataOpsRequest">写入图片请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/values_image")]
    Task<FeishuApiResult<ImageDataOpsResult>?> ImageDataAsync(
         [Path] string spreadsheet_token,
         [Body] ImageDataOpsRequest imageDataOpsRequest,
         CancellationToken cancellationToken = default);


}
