// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;



/// <summary>
/// 数据校验用于限制电子表格单元格中的数据类型或用户输入单元格的值。
/// <para>目前，电子表格支持下拉列表相关接口，用于验证数据。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/datavalidation/datavalidation-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV3SpreadsheetDataValidation : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 增加保护范围
    /// <para>在电子表格工作表中设置多个保护范围，支持对行或列设置保护范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="createDataValidationRequest">创建数据验证请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation")]
    Task<FeishuApiResult?> CreateDataValidationAsync(
      [Path] string spreadsheet_token,
      [Body] CreateDataValidationRequest createDataValidationRequest,
      CancellationToken cancellationToken = default);



    /// <summary>
    /// 更新下拉列表设置
    /// <para>更新电子表格工作表中单个下拉列表的设置，支持更新下拉列表的选项和属性，包括是否支持多选、下拉选项的样式等。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheetId">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="updateDataValidationRequest">更新数据验证请求</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation/{sheetId}")]
    Task<FeishuApiResult<UpdateDataValidationResult>?> UpdateDataValidationAsync(
         [Path] string spreadsheet_token,
         [Path] string sheetId,
         [Body] UpdateDataValidationRequest updateDataValidationRequest,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 增加保护范围
    /// <para>在电子表格工作表中设置多个保护范围，支持对行或列设置保护范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="range">
    /// <para>查询范围。格式为 `&lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID</para>
    /// <para>- `&lt;开始位置&gt;:&lt;结束位置&gt;` 为工作表中单元格的范围，数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。</para>
    /// </param>
    /// <param name="dataValidationType">
    /// <para>数据验证类型。取固定值 "list"，表示下拉列表。</para>
    /// </param>
    [Get("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation")]
    Task<FeishuApiResult<GetDataValidationsResult>?> GetDataValidationsAsync(
      [Path] string spreadsheet_token,
      [Query("range")] string range,
      [Query("dataValidationType")] string dataValidationType,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除下拉列表设置
    /// <para>删除电子表格工作表指定范围中下拉列表的设置，但仍保留选项文本。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="deleteDataValidationRequest">删除数据验证请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation")]
    Task<FeishuApiResult<DeleteDataValidationResult>?> DeleteDataValidationAsync(
     [Path] string spreadsheet_token,
     [Body] DeleteDataValidationRequest deleteDataValidationRequest,
     CancellationToken cancellationToken = default);


}
