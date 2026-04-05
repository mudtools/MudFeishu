// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书开放平台电子表格分为表格（spreadsheet）、工作表（sheet）和范围（range）。
/// <para>表格是承载数据的容器，提供数据处理、展示、分析的功能。一个表格包含一个或多个工作表。每个表格都有一个 spreadsheetToken 作为唯一标识。</para>
/// <para>工作表（sheet）是表格中的单独页面。每个工作表都有自己的行和列，形成一个网格，用于组织和存储数据。每一个工作表都有唯一的 sheetId 作为标识。</para>
/// <para>在工作表中进行读取数据、写入数据、筛选数据等各类操作时，需要通过 范围 range 参数指定操作数据的范围。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV3Spreadsheets : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建电子表格
    /// <para>在云空间指定目录下创建电子表格。可自定义表格标题。不支持带内容创建表格。</para>
    /// </summary>
    /// <param name="createSpreadsheetRequest">创建电子表格请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets")]
    Task<FeishuApiResult<CreateSpreadsheetResult>?> CreateSpreadsheetAsync(
       [Body] CreateSpreadsheetRequest createSpreadsheetRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改电子表格属性
    /// <para>用于修改电子表格的属性。目前支持修改电子表格标题。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="patchSpreadsheetRequest">修改电子表格属性请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}")]
    Task<FeishuNullDataApiResult?> PatchSpreadsheetAsync(
      [Path] string spreadsheet_token,
      [Body] PatchSpreadsheetRequest patchSpreadsheetRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据电子表格 token 获取电子表格的基础信息，包括电子表格的所有者、URL 链接等。
    /// </summary>
    /// <param name="spreadsheet_token">文件夹的 token。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}")]
    Task<FeishuApiResult<GetSpreadsheetResult>?> GetSpreadsheetByTokenAsync(
        [Path] string spreadsheet_token,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 操作工作表。
    /// <para>根据电子表格的 token 对工作表进行操作，包括增加工作表、复制工作表、删除工作表。</para>
    /// </summary>
    /// <param name="batchUpdateSheetRequest">操作工作表请求体</param>
    /// <param name="spreadsheet_token">文件夹的 token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/sheets_batch_update")]
    Task<FeishuApiResult<BatchUpdateSheetResult>?> BatchUpdateSheetAsync(
        [Path] string spreadsheet_token,
        [Body] BatchUpdateSheetRequest batchUpdateSheetRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新工作表属性
    /// <para>更新电子表格中的工作表。支持更新工作表的标题、位置，和隐藏、冻结、保护等属性。</para>
    /// </summary>
    /// <param name="batchUpdateSheetPropertiesRequest">更新工作表属性请求体</param>
    /// <param name="spreadsheet_token">文件夹的 token。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/sheets_batch_update")]
    Task<FeishuApiResult<BatchUpdateSheetPropertiesResult>?> BatchUpdateSheetPropertiesAsync(
       [Path] string spreadsheet_token,
       [Body] BatchUpdateSheetPropertiesRequest batchUpdateSheetPropertiesRequest,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>获取电子表格的所有工作表。</para>
    /// <para>根据电子表格 token 获取电子表格的基础信息，包括电子表格的所有者、URL 链接等。</para>
    /// </summary>
    /// <param name="spreadsheet_token">文件夹的 token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/query")]
    Task<FeishuApiResult<GetSpreadsheetSheetsResult>?> GetSpreadsheetSheetsByTokenAsync(
        [Path] string spreadsheet_token,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询电子表格中的工作表。
    /// <para>根据工作表 ID 查询工作表属性信息，包括工作表的标题、索引位置、是否被隐藏等。</para>
    /// </summary>
    /// <param name="spreadsheet_token">文件夹的 token。</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}")]
    Task<FeishuApiResult<GetSpreadsheetSheetResult>?> GetSpreadsheetSheetBySheetIdAsync(
         [Path] string spreadsheet_token,
         [Path] string sheet_id,
         CancellationToken cancellationToken = default);
}
