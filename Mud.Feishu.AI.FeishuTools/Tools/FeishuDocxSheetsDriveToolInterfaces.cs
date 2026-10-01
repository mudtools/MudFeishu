// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

// <summary>
// R5/WP2 写类工具接口集（文档/表格/多维表格/云空间写面成环）。
// </summary>
// <remarks>
// <para>
// 复用既有 [FeishuTool] 源生成器（IsWrite 经 Schema 扩展 x-feishu.is_write 产出）；
// 执行链（FeishuToolBinding）对写工具强制授权门禁——
// EnforceToolAuthorization=true 且未注册 IToolExecutionAuthorizer 即拒绝（安全默认），
// 白名单单独键控（FeishuAgent:WriteAllowList，默认空=不启用任何写工具）。
// </para>
// <para>
// SDK 落点已在 T0-5 逐方法核实（见 R5 文档）。
// </para>
// </remarks>

// ─────────────────────────── Docx 写（2 个） ───────────────────────────

/// <summary>工具接口：docx.create_document（映射 <c>IFeishuTenantV1Docx.CreateDocumentAsync</c>）。</summary>
[FeishuTool("docx.create_document",
    Description = "在云空间中创建一个飞书文档（返回 document_id，可用 docx.get_raw_content 读回）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = "IFeishuTenantV1Docx.CreateDocumentAsync")]
public interface IFeishuDocxCreateDocumentTool
{
    /// <summary>创建文档。</summary>
    /// <returns>白名单投影后的 JSON 文本（document_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateDocumentAsync(
        [ToolParameter("folder_token", "父文件夹 token（可选；缺省创建到根目录）")] string? folder_token = null,
        [ToolParameter("title", "文档标题（可选，默认 '无标题文档'）")] string? title = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.append_blocks（映射 <c>IFeishuTenantV1DocxBlocks.CreateBlockAsync</c>）。</summary>
[FeishuTool("docx.append_blocks",
    Description = "在文档根块下创建子块（向文档追加内容，如段落、标题等）。document_id 可由 docx.create_document 创建后获得或来自已有文档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = "IFeishuTenantV1DocxBlocks.CreateBlockAsync")]
public interface IFeishuDocxAppendBlocksTool
{
    /// <summary>追加文档块。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AppendBlocksAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("block_type", "块类型（2=文本段落, 3=标题1, 4=标题2, ..., 11=标题9）", Required = true)] int block_type,
        [ToolParameter("text", "块文本内容", Required = true)] string text,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功创建一次；省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不追加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Sheets 写（2 个） ───────────────────────────

/// <summary>工具接口：sheets.update_range（映射 <c>IFeishuTenantV3SpreadsheetData.RangeWriteDataAsync</c>）。</summary>
[FeishuTool("sheets.update_range",
    Description = "向电子表格指定区域写入数据（覆盖写，天然幂等——同一区域重复写入结果一致）。spreadsheet_token 来自 sheets.list_sheets；range 形如 ShtXxx!A1:B2。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。",
    RequiredScopes = ["sheets:spreadsheet"],
    IsWrite = true,
    Source = "IFeishuTenantV3SpreadsheetData.RangeWriteDataAsync")]
public interface IFeishuSheetsUpdateRangeTool
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
    Source = "IFeishuTenantV3SpreadsheetData.AppendDataAsync")]
public interface IFeishuSheetsAppendRowsTool
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

// ─────────────────────────── Drive 写（3 个） ───────────────────────────

/// <summary>工具接口：drive.create_folder（映射 <c>IFeishuTenantV1DriveFolder.CreateFolderAsync</c>）。</summary>
[FeishuTool("drive.create_folder",
    Description = "在云空间中创建文件夹（返回 folder_token，可用 drive.list_folder_files 验证）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = "IFeishuTenantV1DriveFolder.CreateFolderAsync")]
public interface IFeishuDriveCreateFolderTool
{
    /// <summary>创建文件夹。</summary>
    /// <returns>白名单投影后的 JSON 文本（folder_token）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateFolderAsync(
        [ToolParameter("name", "文件夹名称", Required = true)] string name,
        [ToolParameter("folder_token", "父文件夹 token（可选；缺省创建到根目录）")] string? folder_token = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.move_file（映射 <c>IFeishuTenantV1DriveFiles.MoveFileByFileTokenAsync</c>）。</summary>
[FeishuTool("drive.move_file",
    Description = "将文件或文件夹移动到指定文件夹下（返回异步任务 ID——移动操作为异步执行，需后续轮询确认完成）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = "IFeishuTenantV1DriveFiles.MoveFileByFileTokenAsync")]
public interface IFeishuDriveMoveFileTool
{
    /// <summary>移动文件。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_id——异步任务）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> MoveFileAsync(
        [ToolParameter("file_token", "要移动的文件/文件夹 token", Required = true)] string file_token,
        [ToolParameter("type", "文件类型（如 doc/docx/sheet/bitable/file/folder/wiki 等）", Required = true)] string type,
        [ToolParameter("folder_token", "目标文件夹 token", Required = true)] string folder_token,
        [ToolParameter("dry_run", "仅预演不移动（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.upload_file（映射 <c>IFeishuTenantV1DriveFiles.UploadAllFileAsync</c>）。</summary>
[FeishuTool("drive.upload_file",
    Description = "将网络文件上传到云空间（file_url 为 http/https 绝对地址，由宿主负责下载落盘——与 im.send_image 同机制）。返回 file_token。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = "IFeishuTenantV1DriveFiles.UploadAllFileAsync")]
public interface IFeishuDriveUploadFileTool
{
    /// <summary>上传文件。</summary>
    /// <returns>白名单投影后的 JSON 文本（file_token）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UploadFileAsync(
        [ToolParameter("file_url", "文件的 http/https 绝对地址（不接受本地路径）", Required = true)] string file_url,
        [ToolParameter("file_name", "文件名（含扩展名，如 周报.pdf）", Required = true)] string file_name,
        [ToolParameter("folder_token", "目标文件夹 token（可选；缺省上传到根目录）")] string? folder_token = null,
        [ToolParameter("dry_run", "仅预演不上传（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
