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
    /// 创建筛选
    /// <para>在电子表格工作表的指定范围内，设置筛选条件，创建筛选。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="sheet_id">工作表的 ID。示例值："2jm6f6"</param>
    /// <param name="createFilterViewRequest">创建筛选视图请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter_views")]
    Task<FeishuApiResult<FilterViewsResult>?> CreateFilterViewAsync(
        [Path] string spreadsheet_token,
        [Path] string sheet_id,
        [Body] CreateFilterViewRequest createFilterViewRequest,
        CancellationToken cancellationToken = default);
}
