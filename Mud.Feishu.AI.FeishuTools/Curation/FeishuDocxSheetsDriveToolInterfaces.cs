// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

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
        [ToolParameter("block_type", "块类型（决定新增块的形态）。单块简写：与 text 配对使用；给了 blocks 时忽略。", Required = false, EnumType = typeof(global::Mud.Feishu.DataModels.Docx.BlockTypes))] string? block_type = null,
        [ToolParameter("text", "块文本内容（单块简写，与 block_type 配对；给了 blocks 时忽略）")] string? text = null,
            [ToolParameter("blocks", "要追加的块列表（JSON 数组字符串，可选）。每项形如 {\"block_type\":\"heading1\",\"text\":\"标题\"}；block_type 取值见单块简写说明（text/heading1~9/bullet/ordered/code/quote/todo/divider）。divider 忽略 text。⚠️ 表格/单元格/引用容器暂不支持（SDK 缺类型）。与 block_type/text 同时给出时**以 blocks 为准**；单次最多 50 个子块。")] string? blocks = null,
            [ToolParameter("parent_block_id", "父块 ID（可选，默认文档根块 document_id）")] string? parent_block_id = null,
            [ToolParameter("index", "插入到父块下的位置（可选，默认追加到末尾）")] int? index = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功创建一次；省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不追加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.update_blocks（映射 <c>IFeishuTenantV1DocxBlocks.BatchUpdateBlocksAsync</c>）。</summary>
/// <remarks>
/// <b>暴露范围（刻意收窄）</b>：只暴露 SDK <c>UpdateBlockRequest</c> 的 <c>update_text_elements</c> 面
/// （替换块的文本元素）。其余 11 个面（<c>update_text_style</c> / <c>update_table_property</c> /
/// <c>insert_table_row</c> / <c>merge_table_cells</c> …）属表格结构与富样式编辑，
/// 每个都需要独立的参数面；塞进同一个工具会让模型面对 12 个互斥可选参数而难以正确选择。
/// <b>这是有意的范围决策，不是遗漏</b>（登记于 §13.12）。
/// </remarks>
[FeishuTool("docx.update_blocks",
    Description = "更新文档中已有块的文本内容（改写段落/标题的文字）。block_id 来自 docx.append_blocks 或 docx.get_raw_content 的返回。⚠️ 只支持改文本，不支持改表格结构与富样式（见工具说明）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = "IFeishuTenantV1DocxBlocks.BatchUpdateBlocksAsync")]
public interface IFeishuDocxUpdateBlocksTool
{
    /// <summary>批量更新块文本。</summary>
    /// <returns>结构化文本（updated=成功条数；dry_run=true 时返回请求摘要且不调用下游）。</returns>
    Task<string> UpdateBlocksAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("blocks", "要更新的块列表（JSON 数组字符串）。每项形如 {\"block_id\":\"doxcnXxx\",\"text\":\"新文本\"}；单次最多 200 条。", Required = true)] string blocks,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功一次；省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.delete_blocks（映射 <c>IFeishuTenantV1DocxBlocks.BatchDeleteBlocksAsync</c>）。</summary>
[FeishuTool("docx.delete_blocks",
    Description = "删除某个父块下指定索引区间的子块（如删掉文档里过时的一批段落）。索引是父块下的子块序号（从 0 开始），可由 docx.get_raw_content 或 docx.append_blocks 的返回推算。⚠️ 删除不可撤销——务必先用 dry_run 确认区间。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = "IFeishuTenantV1DocxBlocks.BatchDeleteBlocksAsync")]
public interface IFeishuDocxDeleteBlocksTool
{
    /// <summary>删除子块区间 [start_index, end_index)。</summary>
    /// <returns>结构化文本（deleted=删除条数；dry_run=true 时返回请求摘要且不调用下游）。</returns>
    Task<string> DeleteBlocksAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("start_index", "起始索引（含），从 0 开始", Required = true)] int start_index,
        [ToolParameter("end_index", "结束索引（不含）", Required = true)] int end_index,
        [ToolParameter("parent_block_id", "父块 ID（可选，默认文档根块 document_id）")] string? parent_block_id = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功一次；省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path 与区间摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.import_markdown（映射 <c>IFeishuTenantV1DocxBlocks.ContentConvertAsync</c>）。</summary>
/// <remarks>
/// <b>为什么只做"转换"而不直接写入</b>：SDK 的 <c>ContentConvertAsync</c> 只做格式转换，
/// 返回可用的块结构；写入是另一跳。拆开的收益是模型可以先看转换结果（含图片 URL 映射）再决定是否写入，
/// 而"一步替换整篇"由 <c>docx.replace_document</c> 承担。
/// </remarks>
[FeishuTool("docx.import_markdown",
    Description = "把 Markdown 内容转换成文档块结构（不写入文档）——用于先预览转换结果，再用 docx.append_blocks 写入。支持文本、一到九级标题、有序/无序列表、代码块、引用、待办、图片、表格。只读转换，需 docx:document。",
    RequiredScopes = ["docx:document"],
    Source = "IFeishuTenantV1DocxBlocks.ContentConvertAsync")]
public interface IFeishuDocxImportMarkdownTool
{
    /// <summary>Markdown/HTML → 文档块。</summary>
    /// <returns>白名单投影后的 JSON 文本（blocks / first_level_block_ids / image_urls），超长截断并标记 truncated。</returns>
    Task<string> ImportMarkdownAsync(
        [ToolParameter("markdown", "Markdown 源文本", Required = true)] string markdown,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：docx.replace_document（<b>组合工具</b>：ContentConvert → CreateBlock → BatchDeleteBlock）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是组合而非单方法</b>：SDK <b>没有</b>"整篇替换"接口（已核实，`IFeishuV1DocxBlocks`
/// 仅 8 个方法，无 replace/overwrite）。故由 <c>ContentConvertAsync</c> →
/// <c>CreateBlockAsync</c> → <c>BatchDeleteBlocksAsync</c> 组合而成。
/// </para>
/// <para>
/// <b>⚠️ 与原文方案的关键差异（安全性设计）</b>：原文要求"先删后建 + 补偿路径"。
/// 本实现改为 <b>先建后删</b>，从而<b>从根本上消除"已删未建"的破坏窗口</b>：
/// <list type="number">
/// <item>转换失败 ⇒ 文档未被触碰；</item>
/// <item>追加失败 ⇒ 旧内容完整，新内容未入库（<b>无损失</b>）；</item>
/// <item>删除失败 ⇒ 新旧内容并存——这是<b>可见且可恢复</b>的状态，工具会如实上报
/// 精确的待删区间与新建块 ID，并明确告知"内容未丢失"。</item>
/// </list>
/// 对比"先删后建"：删除成功而创建失败会留下<b>被清空的文档</b>，且补偿（重放旧块）
/// 需要先完整快照旧文档——在 SDK 无事务的前提下，那才是真正的不可靠路径。
/// </para>
/// <para>
/// <b>为什么 <c>idempotency_key</c> 是必填</b>：写操作在"已追加未删除"处中断后重试，
/// 若没有稳定 client_token，会把新内容<b>再插一遍</b>并重复删除。必填让重试语义明确。
/// </para>
/// </remarks>
[FeishuTool("docx.replace_document",
    Description = "用 Markdown 内容整体替换文档正文（保留为新内容的旧块会被删除）。适用于'把这篇文章重建一遍'。执行顺序为先追加新块、再删除旧块，因此中途失败不会导致内容丢失。⚠️ 必须提供 idempotency_key（重试语义）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = "IFeishuTenantV1DocxBlocks.CreateBlockAsync")]
public interface IFeishuDocxReplaceDocumentTool
{
    /// <summary>整篇替换（Markdown → 新块，追加后删除旧块）。</summary>
    /// <returns>结构化文本（appended / deleted / 以及删除失败时的待删区间与修复指引）。</returns>
    Task<string> ReplaceDocumentAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("markdown", "新的正文内容（Markdown 格式）", Required = true)] string markdown,
        [ToolParameter("idempotency_key", "幂等键（**必填**）：相同 client_token 在 24 小时内至多成功一次，防止重试时重复插入并重复删除。请提供稳定值，不要用随机数。", Required = true)] string idempotency_key,
        [ToolParameter("parent_block_id", "父块 ID（可选，默认文档根块 document_id）")] string? parent_block_id = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的步骤与 method/path，不调用下游")] bool? dry_run = null,
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
