// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 电子表格浮动图片指悬浮在表格单元格上方的图片。图片大小可自行调整，不会随单元格大小而变化。
/// <para>单个电子表格最多支持放置 4,000 张不同 token 的图片，即表格内不重复的图片（包括浮动图片和单元格图片）总数不超过 4,000 张。将相同 token 的图片多次放置在表格的不同位置，数量上仅算一张图片。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/float-image-user-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV3SpreadsheetFloatImage : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建浮动图片
    /// <para>在电子表格工作表的指定位置创建一张浮动图片。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/create">接口文档</see></para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="createFloatImageRequest">创建浮动图片请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images")]
    Task<FeishuApiResult<FloatImageOpsResult>?> CreateFloatImageAsync(
       [Path] string spreadsheet_token,
       [Path] string sheet_id,
       [Body] CreateFloatImageRequest createFloatImageRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新浮动图片
    /// <para>更新已有的浮动图片位置和宽高。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/patch">接口文档</see></para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="updateFloatImageRequest">更新浮动图片请求体</param>
    /// <param name="float_image_id">工作表内浮动图片的唯一标识。示例值：ye06SS14ph</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}")]
    Task<FeishuApiResult<FloatImageOpsResult>?> UpdateFloatImageAsync(
          [Path] string spreadsheet_token,
          [Path] string sheet_id,
          [Path] string float_image_id,
          [Body] UpdateFloatImageRequest updateFloatImageRequest,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取浮动图片
    /// <para>获取电子表格工作表内指定浮动图片的参数信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/get">接口文档</see></para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="float_image_id">工作表内浮动图片的唯一标识。示例值：ye06SS14ph</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}")]
    Task<FeishuApiResult<FloatImageOpsResult>?> GetFloatImageAsync(
             [Path] string spreadsheet_token,
             [Path] string sheet_id,
             [Path] string float_image_id,
             CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询浮动图片
    /// <para>获取电子表格工作表内所有的浮动图片的参数信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/query">接口文档</see></para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/query")]
    Task<FeishuApiResult<GetFloatImagesResult>?> GetFloatImagesAsync(
           [Path] string spreadsheet_token,
           [Path] string sheet_id,
           CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除浮动图片
    /// <para>删除电子表格工作表内指定的浮动图片。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/delete">接口文档</see></para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="float_image_id">工作表内浮动图片的唯一标识。示例值：ye06SS14ph</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}")]
    Task<FeishuNullDataApiResult?> DeleteFloatImageAsync(
            [Path] string spreadsheet_token,
            [Path] string sheet_id,
            [Path] string float_image_id,
            CancellationToken cancellationToken = default);
}