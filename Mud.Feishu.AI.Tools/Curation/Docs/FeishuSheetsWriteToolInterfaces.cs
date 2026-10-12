// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Sheets 写（2 个） ───────────────────────────

/// <summary>工具接口：sheets.update_range（映射 <c>IFeishuTenantV3SpreadsheetData.RangeWriteDataAsync</c>）。</summary>
[FeishuTool("sheets.update_range",
    Description = "向电子表格指定区域写入数据（覆盖写，天然幂等——同一区域重复写入结果一致）。spreadsheet_token 来自 sheets.list_sheets；range 形如 ShtXxx!A1:B2。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetData) + "." + nameof(IFeishuTenantV3SpreadsheetData.RangeWriteDataAsync))]
public interface IFeishuTenantSheetsUpdateRangeTool
{
    /// <summary>更新区域数据。</summary>
    /// <returns>白名单投影后的 JSON 文本（revision）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateRangeAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "写入区域（形如 ShtXxx!A1:B2，须与 values 维度匹配）", Required = true)] string range,
        [ToolParameter("values", "写入数据（二维数组的 JSON 字符串，如 [[\"A1\",\"B1\"],[\"A2\",\"B2\"]]）", Required = true)] string values,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.append_rows（映射 <c>IFeishuTenantV3SpreadsheetData.AppendDataAsync</c>）。</summary>
[FeishuTool("sheets.append_rows",
    Description = "向电子表格追加行数据（在指定区域末尾追加，不清空既有行）。spreadsheet_token 来自 sheets.list_sheets。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetData) + "." + nameof(IFeishuTenantV3SpreadsheetData.AppendDataAsync))]
public interface IFeishuTenantSheetsAppendRowsTool
{
    /// <summary>追加行数据。</summary>
    /// <returns>白名单投影后的 JSON 文本（revision + appended_range）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AppendRowsAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "追加目标区域（形如 ShtXxx!A1:Z1，指定列范围）", Required = true)] string range,
        [ToolParameter("values", "追加数据（二维数组的 JSON 字符串，如 [[\"data1\",\"data2\"]]）", Required = true)] string values,
        [ToolParameter("dry_run", "仅预演不追加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
