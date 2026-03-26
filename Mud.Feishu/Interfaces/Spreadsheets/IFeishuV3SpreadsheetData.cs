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


    /// <summary>
    /// 读取单个范围
    /// <para>读取电子表格中单个指定范围的数据。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="range">
    /// <para>查询范围。格式为 `&lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID</para>
    /// <para>- `&lt;开始位置&gt;:&lt;结束位置&gt;` 为工作表中单元格的范围，数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。</para>
    /// <para>**注意**：若使用 `&lt;sheetId&gt;!&lt;开始单元格&gt;:&lt;结束列&gt;` 和 `&lt;sheetId&gt;!&lt;开始列&gt;:&lt;结束列&gt;` 的写法时，仅支持获取 100 列数据。</para>
    /// <para>**示例值**："Q7PlXT!A1:B2"</para>
    /// </param>
    /// <param name="valueRenderOption">
    /// <para>指定单元格数据的格式。可选值如下所示。当参数缺省时，默认不进行公式计算，返回公式本身，且单元格为数值格式。</para>
    /// <para>- ToString：返回纯文本的值（数值类型除外）</para>
    /// <para>- Formula：单元格中含有公式时，返回公式本身</para>
    /// <para>- FormattedValue：计算并格式化单元格</para>
    /// <para>- UnformattedValue：计算但不对单元格进行格式化</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="dateTimeRenderOption">
    /// <para>指定数据类型为日期、时间、或时间日期的单元格数据的格式。</para>
    /// <para>- 若不传值，默认返回浮点数值，整数部分为自 1899 年 12 月 30 日以来的天数；小数部分为该时间占 24 小时的份额。例如：若时间为 1900 年 1 月 1 日中午 12 点，则默认返回 2.5。其中，2 表示 1900 年 1 月 1 日为 1899 年12 月 30 日之后的 2 天；0.5 表示 12 点占 24 小时的二分之一，即 12/24=0.5。</para>
    /// <para>- 可选值为 FormattedString，此时接口将计算并对日期、时间、或时间日期类型的数据格式化并返回格式化后的字符串，但不会对数字进行格式化。</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">当单元格中包含@用户等涉及用户信息的元素时，该参数可指定返回的用户 ID 类型。默认为 lark_id，建议选择 open_id 或 union_id。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/values/{range}")]
    Task<FeishuApiResult<GetRangeDataResult>?> GetRangeDataAsync(
        [Path] string spreadsheet_token,
        [Path] string range,
        [Query("valueRenderOption")] string? valueRenderOption = null,
        [Query("dateTimeRenderOption")] string? dateTimeRenderOption = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 读取多个范围
    /// <para>读取电子表格中多个指定范围的数据。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="ranges">
    /// <para>多个查询范围，范围之间使用逗号分隔，如 `Q7PlXT!A2:B6,0b6377!B1:C8`。range 的格式为 `&lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID</para>
    /// <para>- `&lt;开始位置&gt;:&lt;结束位置&gt;` 为工作表中单元格的范围，数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。</para>
    /// <para>**注意**：若使用 `&lt;sheetId&gt;!&lt;开始单元格&gt;:&lt;结束列&gt;` 和 `&lt;sheetId&gt;!&lt;开始列&gt;:&lt;结束列&gt;` 的写法时，仅支持获取 100 列数据。</para>
    /// <para>**示例值**："Q7PlXT!A1:B2"</para>
    /// </param>
    /// <param name="valueRenderOption">
    /// <para>指定单元格数据的格式。可选值如下所示。当参数缺省时，默认不进行公式计算，返回公式本身，且单元格为数值格式。</para>
    /// <para>- ToString：返回纯文本的值（数值类型除外）</para>
    /// <para>- Formula：单元格中含有公式时，返回公式本身</para>
    /// <para>- FormattedValue：计算并格式化单元格</para>
    /// <para>- UnformattedValue：计算但不对单元格进行格式化</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="dateTimeRenderOption">
    /// <para>指定数据类型为日期、时间、或时间日期的单元格数据的格式。</para>
    /// <para>- 若不传值，默认返回浮点数值，整数部分为自 1899 年 12 月 30 日以来的天数；小数部分为该时间占 24 小时的份额。例如：若时间为 1900 年 1 月 1 日中午 12 点，则默认返回 2.5。其中，2 表示 1900 年 1 月 1 日为 1899 年12 月 30 日之后的 2 天；0.5 表示 12 点占 24 小时的二分之一，即 12/24=0.5。</para>
    /// <para>- 可选值为 FormattedString，此时接口将计算并对日期、时间、或时间日期类型的数据格式化并返回格式化后的字符串，但不会对数字进行格式化。</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="user_id_type">当单元格中包含@用户等涉及用户信息的元素时，该参数可指定返回的用户 ID 类型。默认为 lark_id，建议选择 open_id 或 union_id。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v2/spreadsheets/{spreadsheetToken}/values_batch_get")]
    Task<FeishuApiResult<GetRangesDataResult>?> GetRangesDataAsync(
       [Path] string spreadsheet_token,
       [Query("ranges")] string ranges,
       [Query("valueRenderOption")] string? valueRenderOption = null,
       [Query("dateTimeRenderOption")] string? dateTimeRenderOption = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
