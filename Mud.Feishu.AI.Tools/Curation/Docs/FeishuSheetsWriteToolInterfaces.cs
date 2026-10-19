// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Sheets 写（17 个） ───────────────────────────

/// <summary>工具接口：sheets.update_range（映射 <c>IFeishuTenantV3SpreadsheetData.RangeWriteDataAsync</c>）。</summary>
[AgentTool("sheets.update_range",
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
[AgentTool("sheets.append_rows",
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

/// <summary>工具接口：sheets.cond_format_create（映射 <c>IFeishuTenantV2SpreadsheetConditionFormat.CreateConditionFormatsAsync</c>）。</summary>
[AgentTool("sheets.cond_format_create",
    Description = "为工作表批量创建条件格式规则（按内容/数值自动着色）。一条规则形如 {\"sheet_id\":\"ShtXxx\",\"condition_format\":{\"ranges\":[\"ShtXxx!A1:B2\"],\"rule_type\":\"containsText\",\"attrs\":[{\"operator\":\"contains\",\"text\":\"完成\"}],\"style\":{\"back_color\":\"#FDE2E2\"}}}；rule_type 可选 containsText / cellIs / timePeriod / duplicateValues / uniqueValues 等。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetConditionFormat) + "." + nameof(IFeishuTenantV2SpreadsheetConditionFormat.CreateConditionFormatsAsync))]
public interface IFeishuTenantSheetsCondFormatCreateTool
{
    /// <summary>批量创建条件格式规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（results: sheet_id/cf_id/res_code/res_msg）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateConditionFormatsAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("condition_formats", "条件格式数组 JSON 字符串（每项含 sheet_id 与 condition_format；cf_id 由服务端生成）", Required = true)] string condition_formats,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.cond_format_update（映射 <c>IFeishuTenantV2SpreadsheetConditionFormat.UpdateConditionFormatsAsync</c>）。</summary>
[AgentTool("sheets.cond_format_update",
    Description = "批量更新（覆盖）工作站的条件格式规则；每项 condition_format 必须携带 cf_id（来自 sheets.cond_format_get），更新按 cf_id 全量覆盖该规则。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetConditionFormat) + "." + nameof(IFeishuTenantV2SpreadsheetConditionFormat.UpdateConditionFormatsAsync))]
public interface IFeishuTenantSheetsCondFormatUpdateTool
{
    /// <summary>批量更新条件格式规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（results: sheet_id/cf_id/res_code/res_msg）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateConditionFormatsAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("condition_formats", "条件格式数组 JSON 字符串（每项含 sheet_id 与 condition_format，且 condition_format.cf_id 必填）", Required = true)] string condition_formats,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.cond_format_delete（映射 <c>IFeishuTenantV2SpreadsheetConditionFormat.DeleteConditionFormatsAsync</c>）。</summary>
[AgentTool("sheets.cond_format_delete",
    Description = "批量删除工作表的条件格式规则（不可恢复！请谨慎使用，建议先 dry_run 预演确认）；每项形如 {\"sheet_id\":\"ShtXxx\",\"cf_id\":\"...\"}，cf_id 来自 sheets.cond_format_get。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetConditionFormat) + "." + nameof(IFeishuTenantV2SpreadsheetConditionFormat.DeleteConditionFormatsAsync))]
public interface IFeishuTenantSheetsCondFormatDeleteTool
{
    /// <summary>批量删除条件格式规则（不可恢复）。</summary>
    /// <returns>白名单投影后的 JSON 文本（results: sheet_id/cf_id/res_code/res_msg）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteConditionFormatsAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_cf_ids", "待删除条件格式数组 JSON 字符串，每项形如 {\"sheet_id\":\"ShtXxx\",\"cf_id\":\"...\"}", Required = true)] string sheet_cf_ids,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.data_validation_create（映射 <c>IFeishuTenantV2SpreadsheetDataValidation.CreateDataValidationAsync</c>）。</summary>
[AgentTool("sheets.data_validation_create",
    Description = "为单元格区域创建「数据校验」（如下拉列表）。range 形如 ShtXxx!A1:B100；condition_values 为下拉选项值列表。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetDataValidation) + "." + nameof(IFeishuTenantV2SpreadsheetDataValidation.CreateDataValidationAsync))]
public interface IFeishuTenantSheetsDataValidationCreateTool
{
    /// <summary>创建数据校验规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（创建确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateDataValidationAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "生效区域（形如 ShtXxx!A1:B100）", Required = true)] string range,
        [ToolParameter("condition_values", "下拉选项值列表（字符串数组）", Required = true)] string[] condition_values,
        [ToolParameter("data_validation_type", "数据校验类型（可选，当前支持 \"list\" 下拉列表；默认 list）")] string? data_validation_type = null,
        [ToolParameter("multiple_values", "是否允许填写多个值（可选）")] bool? multiple_values = null,
        [ToolParameter("highlight_valid_data", "是否高亮有效数据（可选）")] bool? highlight_valid_data = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.data_validation_update（映射 <c>IFeishuTenantV2SpreadsheetDataValidation.UpdateDataValidationAsync</c>）。</summary>
[AgentTool("sheets.data_validation_update",
    Description = "更新（覆盖）指定工作表的数据校验规则——按 ranges 指定新的生效区域与选项值。sheet_id 来自 sheets.list_sheets。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetDataValidation) + "." + nameof(IFeishuTenantV2SpreadsheetDataValidation.UpdateDataValidationAsync))]
public interface IFeishuTenantSheetsDataValidationUpdateTool
{
    /// <summary>更新数据校验规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（sheet_id/ranges/condition_values）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateDataValidationAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("ranges", "更新的生效区域列表（字符串数组，形如 [\"ShtXxx!A1:B100\"]）", Required = true)] string[] ranges,
        [ToolParameter("condition_values", "下拉选项值列表（字符串数组）", Required = true)] string[] condition_values,
        [ToolParameter("data_validation_type", "数据校验类型（可选，当前支持 \"list\" 下拉列表；默认 list）")] string? data_validation_type = null,
        [ToolParameter("multiple_values", "是否允许填写多个值（可选）")] bool? multiple_values = null,
        [ToolParameter("highlight_valid_data", "是否高亮有效数据（可选）")] bool? highlight_valid_data = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.data_validation_delete（映射 <c>IFeishuTenantV2SpreadsheetDataValidation.DeleteDataValidationAsync</c>）。</summary>
[AgentTool("sheets.data_validation_delete",
    Description = "删除单元格区域上的数据校验规则（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。ranges 形如 [\"ShtXxx!A1:B100\"]。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetDataValidation) + "." + nameof(IFeishuTenantV2SpreadsheetDataValidation.DeleteDataValidationAsync))]
public interface IFeishuTenantSheetsDataValidationDeleteTool
{
    /// <summary>删除数据校验规则（不可恢复）。</summary>
    /// <returns>白名单投影后的 JSON 文本（删除结果：range/success/updated_cells）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteDataValidationAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("ranges", "要删除数据校验的区域列表（字符串数组，形如 [\"ShtXxx!A1:B100\"]）", Required = true)] string[] ranges,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.protected_create（映射 <c>IFeishuTenantV2SpreadsheetProtected.CreateProtectedAsync</c>）。</summary>
[AgentTool("sheets.protected_create",
    Description = "为工作表/行列区间创建保护范围（锁定后仅可编辑成员可改）；major_dimension：ROWS 按行区间 / COLUMNS 按列区间（区间下标从 0 开始、半开区间）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetProtected) + "." + nameof(IFeishuTenantV2SpreadsheetProtected.CreateProtectedAsync))]
public interface IFeishuTenantSheetsProtectedCreateTool
{
    /// <summary>创建保护范围。</summary>
    /// <returns>白名单投影后的 JSON 文本（protect_ids）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateProtectedAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("major_dimension", "保护维度：ROWS 按行 / COLUMNS 按列", Required = true)] string major_dimension,
        [ToolParameter("start_index", "起始下标（从 0 开始，含）", Required = true)] int start_index,
        [ToolParameter("end_index", "结束下标（不含，须大于 start_index）", Required = true)] int end_index,
        [ToolParameter("users", "可编辑成员 ID 列表（可选，字符串数组，ID 类型由 user_id_type 指定）")] string[]? users = null,
        [ToolParameter("lock_info", "锁定信息（可选，如 \"LOCK\" 表示锁定）")] string? lock_info = null,
        [ToolParameter("user_id_type", "成员 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.protected_update（映射 <c>IFeishuTenantV2SpreadsheetProtected.UpdateProtectedAsync</c>）。</summary>
[AgentTool("sheets.protected_update",
    Description = "更新保护范围的可编辑成员（新增/移除）或锁定信息；protect_id 来自 sheets.protected_get（或 protected_create 的结果）。add_editors / remove_editors 为成员 ID 列表（类型由 editor_member_type 指定）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetProtected) + "." + nameof(IFeishuTenantV2SpreadsheetProtected.UpdateProtectedAsync))]
public interface IFeishuTenantSheetsProtectedUpdateTool
{
    /// <summary>更新保护范围。</summary>
    /// <returns>白名单投影后的 JSON 文本（replies：sheet_id/protect_id/lock_info）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateProtectedAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("protect_id", "保护范围 ID（来自 sheets.protected_get）", Required = true)] string protect_id,
        [ToolParameter("add_editors", "新增的可编辑成员 ID 列表（可选，字符串数组）")] string[]? add_editors = null,
        [ToolParameter("remove_editors", "移除的可编辑成员 ID 列表（可选，字符串数组）")] string[]? remove_editors = null,
        [ToolParameter("editor_member_type", "成员 ID 类型（可选：openid / userid / unionid，默认 openid；与 add_editors/remove_editors 配套）")] string? editor_member_type = null,
        [ToolParameter("lock_info", "锁定信息（可选，如 \"LOCK\"）")] string? lock_info = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.protected_delete（映射 <c>IFeishuTenantV2SpreadsheetProtected.DeleteProtectedAsync</c>）。</summary>
[AgentTool("sheets.protected_delete",
    Description = "删除保护范围（保护撤销后该区域恢复全员可编辑；不可恢复！建议先 dry_run 预演确认）。protect_ids 为逗号分隔的保护范围 ID 列表。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2SpreadsheetProtected) + "." + nameof(IFeishuTenantV2SpreadsheetProtected.DeleteProtectedAsync))]
public interface IFeishuTenantSheetsProtectedDeleteTool
{
    /// <summary>删除保护范围（不可恢复）。</summary>
    /// <returns>白名单投影后的 JSON 文本（删除确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteProtectedAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("protect_ids", "要删除的保护范围 ID 列表（逗号分隔多个 protect_id）", Required = true)] string protect_ids,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.filter_create（映射 <c>IFeishuTenantV3SpreadsheetFilter.CreateFilterAsync</c>）。</summary>
[AgentTool("sheets.filter_create",
    Description = "为工作表创建筛选器（按列筛选并隐藏不匹配行；每个工作表仅一个筛选器）。filter_type 可选 multiValue 多值 / number 数字 / text 文本 / color 颜色；compare_type 如 less/equals/contains；expected 为筛选值列表。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetFilter) + "." + nameof(IFeishuTenantV3SpreadsheetFilter.CreateFilterAsync))]
public interface IFeishuTenantSheetsFilterCreateTool
{
    /// <summary>创建筛选器。</summary>
    /// <returns>白名单投影后的 JSON 文本（创建确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateFilterAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("range", "筛选生效区域（形如 ShtXxx!A1:F100）", Required = true)] string range,
        [ToolParameter("col", "筛选列（如 A 或 B）", Required = true)] string col,
        [ToolParameter("filter_type", "筛选类型：multiValue / number / text / color / clear", Required = true)] string filter_type,
        [ToolParameter("compare_type", "比较方式（可选，如 less / equals / contains，随 filter_type 组合）")] string? compare_type = null,
        [ToolParameter("expected", "筛选值列表（可选，字符串数组）")] string[]? expected = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.filter_update（映射 <c>IFeishuTenantV3SpreadsheetFilter.UpdateFilterAsync</c>）。</summary>
[AgentTool("sheets.filter_update",
    Description = "更新（覆盖）工作表筛选器的某列筛选条件。filter_type 传 clear 可清除该列筛选。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetFilter) + "." + nameof(IFeishuTenantV3SpreadsheetFilter.UpdateFilterAsync))]
public interface IFeishuTenantSheetsFilterUpdateTool
{
    /// <summary>更新筛选条件。</summary>
    /// <returns>白名单投影后的 JSON 文本（更新确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateFilterAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("col", "筛选列（如 A 或 B）", Required = true)] string col,
        [ToolParameter("filter_type", "筛选类型：multiValue / number / text / color / clear（clear 清除该列筛选）", Required = true)] string filter_type,
        [ToolParameter("compare_type", "比较方式（可选，如 less / equals / contains）")] string? compare_type = null,
        [ToolParameter("expected", "筛选值列表（可选，字符串数组）")] string[]? expected = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.filter_delete（映射 <c>IFeishuTenantV3SpreadsheetFilter.DeleteFilterAsync</c>）。</summary>
[AgentTool("sheets.filter_delete",
    Description = "删除工作表的筛选器（删除后被隐藏的行会恢复显示，筛选条件不可恢复！建议先 dry_run 预演确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetFilter) + "." + nameof(IFeishuTenantV3SpreadsheetFilter.DeleteFilterAsync))]
public interface IFeishuTenantSheetsFilterDeleteTool
{
    /// <summary>删除筛选器。</summary>
    /// <returns>白名单投影后的 JSON 文本（删除确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteFilterAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.float_image_create（映射 <c>IFeishuTenantV3SpreadsheetFloatImage.CreateFloatImageAsync</c>）。</summary>
[AgentTool("sheets.float_image_create",
    Description = "在工作表中创建浮动图片（图片素材的 token 经 drive.upload_file 上传后获得；range 指定锚定区域）。width/height/offset_x/offset_y 为像素数值字符串。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetFloatImage) + "." + nameof(IFeishuTenantV3SpreadsheetFloatImage.CreateFloatImageAsync))]
public interface IFeishuTenantSheetsFloatImageCreateTool
{
    /// <summary>创建浮动图片。</summary>
    /// <returns>白名单投影后的 JSON 文本（float_image：float_image_id/range/width/height/offset_x/offset_y）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateFloatImageAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("float_image_token", "浮动图片素材 token（经 drive.upload_file 上传图片后获得）", Required = true)] string float_image_token,
        [ToolParameter("range", "锚定区域（形如 ShtXxx!A1）", Required = true)] string range,
        [ToolParameter("float_image_id", "自定义浮动图片 ID（可选，10 位 0-9a-zA-Z；省略由服务端生成）")] string? float_image_id = null,
        [ToolParameter("width", "宽度（可选，像素数值字符串，如 \"320\"）")] string? width = null,
        [ToolParameter("height", "高度（可选，像素数值字符串，如 \"200\"）")] string? height = null,
        [ToolParameter("offset_x", "水平偏移（可选，像素数值字符串）")] string? offset_x = null,
        [ToolParameter("offset_y", "垂直偏移（可选，像素数值字符串）")] string? offset_y = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.float_image_update（映射 <c>IFeishuTenantV3SpreadsheetFloatImage.UpdateFloatImageAsync</c>）。</summary>
[AgentTool("sheets.float_image_update",
    Description = "更新工作表中的浮动图片（覆盖范围/尺寸/偏移）；未提供的字段保持不变，但至少提供一个。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetFloatImage) + "." + nameof(IFeishuTenantV3SpreadsheetFloatImage.UpdateFloatImageAsync))]
public interface IFeishuTenantSheetsFloatImageUpdateTool
{
    /// <summary>更新浮动图片。</summary>
    /// <returns>白名单投影后的 JSON 文本（float_image：float_image_id/range/width/height/offset_x/offset_y）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateFloatImageAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("float_image_id", "浮动图片 ID（来自 sheets.float_image_query）", Required = true)] string float_image_id,
        [ToolParameter("range", "新的锚定区域（可选，形如 ShtXxx!A1）")] string? range = null,
        [ToolParameter("width", "宽度（可选，像素数值字符串）")] string? width = null,
        [ToolParameter("height", "高度（可选，像素数值字符串）")] string? height = null,
        [ToolParameter("offset_x", "水平偏移（可选，像素数值字符串）")] string? offset_x = null,
        [ToolParameter("offset_y", "垂直偏移（可选，像素数值字符串）")] string? offset_y = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.float_image_delete（映射 <c>IFeishuTenantV3SpreadsheetFloatImage.DeleteFloatImageAsync</c>）。</summary>
[AgentTool("sheets.float_image_delete",
    Description = "删除工作表中的浮动图片（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。float_image_id 来自 sheets.float_image_query。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV3SpreadsheetFloatImage) + "." + nameof(IFeishuTenantV3SpreadsheetFloatImage.DeleteFloatImageAsync))]
public interface IFeishuTenantSheetsFloatImageDeleteTool
{
    /// <summary>删除浮动图片（不可恢复）。</summary>
    /// <returns>白名单投影后的 JSON 文本（删除确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteFloatImageAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("sheet_id", "工作表 ID（形如 ShtXxx，来自 sheets.list_sheets）", Required = true)] string sheet_id,
        [ToolParameter("float_image_id", "要删除的浮动图片 ID（来自 sheets.float_image_query）", Required = true)] string float_image_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
