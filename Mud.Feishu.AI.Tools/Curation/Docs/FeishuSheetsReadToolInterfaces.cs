// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Sheets（2 个） ───────────────────────────

/// <summary>工具接口：sheets.list_sheets（映射 <c>IFeishuTenantV3Spreadsheets.GetSpreadsheetSheetsByTokenAsync</c>）。</summary>
[AgentTool("sheets.list_sheets",
    Description = "列出电子表格的全部工作表（sheet_id/title/index）；sheet_id 供 sheets.get_range_values 构造 range。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3Spreadsheets) + "." + nameof(IFeishuTenantV3Spreadsheets.GetSpreadsheetSheetsByTokenAsync))]
public interface IFeishuTenantSheetsListTool
{
    /// <summary>列出工作表。</summary>
    /// <returns>白名单投影后的 JSON 文本（items）。</returns>
    Task<string> GetSpreadsheetSheetsByTokenAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.get_range_values（映射 <c>IFeishuTenantV3SpreadsheetData.GetRangeDataAsync</c>）。</summary>
[AgentTool("sheets.get_range_values",
    Description = "读取工作表单元格区域数据；range 形如 ShtXxx!A1:C100（sheet_id 来自 sheets.list_sheets），建议先小范围取数。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3SpreadsheetData) + "." + nameof(IFeishuTenantV3SpreadsheetData.GetRangeDataAsync))]
public interface IFeishuTenantSheetsRangeTool
{
    /// <summary>读取单元格区域数据。</summary>
    /// <returns>区域值网格 JSON 文本（range/values），超长截断并标记 truncated。</returns>
    Task<string> GetRangeDataAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "单元格区域（形如 ShtXxx!A1:C100）", Required = true)] string range,
        [ToolParameter("value_render_option", "取值格式（可选：ToString / FormattedValue / UnformattedValue）")] string? value_render_option = null,
        CancellationToken cancellationToken = default);
}
