// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Sheets（8 个） ───────────────────────────

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

/// <summary>工具接口：sheets.cond_format_get（映射 <c>IFeishuTenantV2SpreadsheetConditionFormat.GetConditionFormatsAsync</c>）。</summary>
[AgentTool("sheets.cond_format_get",
    Description = "查询电子表格指定工作表的条件格式规则（cf_id/生效范围/规则类型）。cf_id 供 sheets.cond_format_update / sheets.cond_format_delete 使用。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV2SpreadsheetConditionFormat) + "." + nameof(IFeishuTenantV2SpreadsheetConditionFormat.GetConditionFormatsAsync))]
public interface IFeishuTenantSheetsCondFormatGetTool
{
    /// <summary>查询条件格式规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: sheet_id/cf_id/rule_type/ranges）。</returns>
    Task<string> GetConditionFormatsAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_ids", "工作表 ID 列表（逗号分隔多个，如 ShtXxx,ShtYyy；sheet_id 来自 sheets.list_sheets）", Required = true)] string sheet_ids,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.data_validation_get（映射 <c>IFeishuTenantV2SpreadsheetDataValidation.GetDataValidationsAsync</c>）。</summary>
[AgentTool("sheets.data_validation_get",
    Description = "查询电子表格指定区域的「数据校验规则」（下拉选项/选项值等）。range 形如 ShtXxx!A1:B100。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV2SpreadsheetDataValidation) + "." + nameof(IFeishuTenantV2SpreadsheetDataValidation.GetDataValidationsAsync))]
public interface IFeishuTenantSheetsDataValidationGetTool
{
    /// <summary>查询数据校验规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（sheet_id + items: ranges/data_validation_type/condition_values）。</returns>
    Task<string> GetDataValidationsAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "查询区域（形如 ShtXxx!A1:B100）", Required = true)] string range,
        [ToolParameter("data_validation_type", "数据校验类型（当前支持 \"list\"——下拉列表）", Required = true)] string data_validation_type,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.protected_get（映射 <c>IFeishuTenantV2SpreadsheetProtected.GetProtectedAsync</c>）。</summary>
[AgentTool("sheets.protected_get",
    Description = "查询电子表格的保护范围（受保护的工作表/区域、锁定状态）。protect_ids 为逗号分隔的保护范围 ID 列表（protect_id 来自 sheets.protected_create 的结果）。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV2SpreadsheetProtected) + "." + nameof(IFeishuTenantV2SpreadsheetProtected.GetProtectedAsync))]
public interface IFeishuTenantSheetsProtectedGetTool
{
    /// <summary>查询保护范围。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: sheet_id/protect_id/dimension/lock_info）。</returns>
    Task<string> GetProtectedAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("protect_ids", "保护范围 ID 列表（逗号分隔多个 protect_id）", Required = true)] string protect_ids,
        [ToolParameter("member_type", "成员 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? member_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.filter_get（映射 <c>IFeishuTenantV3SpreadsheetFilter.GetFilterAsync</c>）。</summary>
[AgentTool("sheets.filter_get",
    Description = "查询指定工作表的筛选器（生效区域/筛选列/条件/被隐藏的行号）。sheet_id 来自 sheets.list_sheets。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3SpreadsheetFilter) + "." + nameof(IFeishuTenantV3SpreadsheetFilter.GetFilterAsync))]
public interface IFeishuTenantSheetsFilterGetTool
{
    /// <summary>查询工作表筛选器。</summary>
    /// <returns>白名单投影后的 JSON 文本（range/filtered_out_rows/filter_infos）。</returns>
    Task<string> GetFilterAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.float_image_get（映射 <c>IFeishuTenantV3SpreadsheetFloatImage.GetFloatImageAsync</c>）。</summary>
[AgentTool("sheets.float_image_get",
    Description = "按 float_image_id 获取工作表中的单个浮动图片（覆盖范围/尺寸/偏移）。float_image_id 来自 sheets.float_image_query。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3SpreadsheetFloatImage) + "." + nameof(IFeishuTenantV3SpreadsheetFloatImage.GetFloatImageAsync))]
public interface IFeishuTenantSheetsFloatImageGetTool
{
    /// <summary>获取单个浮动图片。</summary>
    /// <returns>白名单投影后的 JSON 文本（float_image：float_image_id/float_image_token/range/width/height/offset_x/offset_y）。</returns>
    Task<string> GetFloatImageAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("float_image_id", "浮动图片 ID（来自 sheets.float_image_query）", Required = true)] string float_image_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.float_image_query（映射 <c>IFeishuTenantV3SpreadsheetFloatImage.GetFloatImagesAsync</c>）。</summary>
[AgentTool("sheets.float_image_query",
    Description = "查询工作表中的全部浮动图片（float_image_id/覆盖范围/尺寸/偏移）。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3SpreadsheetFloatImage) + "." + nameof(IFeishuTenantV3SpreadsheetFloatImage.GetFloatImagesAsync))]
public interface IFeishuTenantSheetsFloatImageQueryTool
{
    /// <summary>查询浮动图片列表。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：float_image_id/float_image_token/range/width/height/offset_x/offset_y）。</returns>
    Task<string> GetFloatImagesAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        CancellationToken cancellationToken = default);
}
