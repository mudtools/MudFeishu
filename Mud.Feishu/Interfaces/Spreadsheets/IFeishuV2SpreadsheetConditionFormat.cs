// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 电子表格条件格式用于根据指定的条件更改单元格的外观格式。。
/// <para>目前，电子表格单个工作表中最多支持设置 20 个条件格式。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/conditionformat/condition-format-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2SpreadsheetConditionFormat : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 批量创建条件格式
    /// <para>在电子表格工作表的指定区域中，为满足指定条件的单元格和单元格中的数据设置样式。支持跨工作表创建多个条件格式。</para>
    /// </summary>
    /// <param name="spreadsheet_token">电子表格的 token。示例值："Iow7sNNEphp3WbtnbCscPqabcef"</param>
    /// <param name="createConditionFormatRequest">批量创建条件格式请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_create")]
    Task<FeishuApiResult<CreateConditionFormatResult>?> CreateConditionFormatAsync(
        [Path] string spreadsheet_token,
        [Body] CreateConditionFormatRequest createConditionFormatRequest,
        CancellationToken cancellationToken = default);
}
