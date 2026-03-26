// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 格数据保护用于设置电子表格保护范围指对工作表中的任意行或列进行保护，并可设置其他协作者是否有权限编辑该数据，有效保障数据信息安全。
/// <para>本接口提供飞书开放平台电子表格中数据保护能力相关方法。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2SpreadsheetProtected : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 增加保护范围
    /// <para>在电子表格工作表中设置多个保护范围，支持对行或列设置保护范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="user_id_type">定请求体中 users 字段对应的用户 ID 类型。</param>
    /// <param name="createProtectedRequest">创建保护范围的请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_dimension")]
    Task<FeishuApiResult<CreateProtectedResult>?> CreateProtectedAsync(
       [Path] string spreadsheet_token,
       [Body] CreateProtectedRequest createProtectedRequest,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改保护范围
    /// <para>修改电子表格工作表中指定的保护范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="updateProtectedRequest">创建保护范围的请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_update")]
    Task<FeishuApiResult<UpdateProtectedResult>?> UpdateProtectedAsync(
      [Path] string spreadsheet_token,
      [Body] UpdateProtectedRequest updateProtectedRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取保护范围
    /// <para>获取电子表格工作表中指定保护范围的信息，包括保护的行列索引、支持编辑的用户 ID、保护范围的备注等。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="protectIds">要获取的保护范围的 ID 列表，多个 ID 之间逗号分隔。
    /// <para>**示例值**："7379738014546812456,7379738014546812456"</para>
    /// </param>
    /// <param name="memberType">返回的用户 ID 的类型。默认为 `userId`，建议选择 `openId`。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_get")]
    Task<FeishuApiResult<GetProtectedResult>?> GetProtectedAsync(
        [Path] string spreadsheet_token,
        [Query("protectIds")] string protectIds,
        [Query("memberType")] string? memberType = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除保护范围
    /// <para>根据保护范围 ID 删除保护范围。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="protectIds">要删除的保护范围的 ID 列表，多个 ID 之间逗号分隔。
    /// <para>**示例值**："7379738014546812456,7379738014546812456"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_del")]
    Task<FeishuApiResult<DeleteProtectedResult>?> DeleteProtectedAsync(
        [Path] string spreadsheet_token,
        [Query("protectIds")] string protectIds,
        CancellationToken cancellationToken = default);
}
