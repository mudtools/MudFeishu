// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 写类工具接口集（文档/表格/多维表格/云空间写面成环）。
// </summary>
// <remarks>
// <para>
// 复用既有 [AgentTool] 源生成器（IsWrite 经 Schema 扩展 x-feishu.is_write 产出）；
// 执行链（FeishuToolBinding）对写工具强制授权门禁——
// EnforceToolAuthorization=true 且未注册 IToolExecutionAuthorizer 即拒绝（安全默认），
// 白名单单独键控（FeishuAgent:WriteAllowList，默认空=不启用任何写工具）。
// </para>
// </remarks>

// ─────────────────────────── Docx 写（2 个） ───────────────────────────

/// <summary>工具接口：docx.create_document（映射 <c>IFeishuTenantV1Docx.CreateDocumentAsync</c>）。</summary>
[AgentTool("docx.create_document",
    Description = "在云空间中创建一个飞书文档（返回 document_id，可用 docx.get_raw_content 读回）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Docx) + "." + nameof(IFeishuTenantV1Docx.CreateDocumentAsync))]
public interface IFeishuTenantDocxCreateDocumentTool
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
[AgentTool("docx.append_blocks",
    Description = "在文档根块下创建子块（向文档追加内容，如段落、标题等）。document_id 可由 docx.create_document 创建后获得或来自已有文档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateBlockAsync))]
public interface IFeishuTenantDocxAppendBlocksTool
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
/// <b>暴露范围</b>：只暴露 SDK <c>UpdateBlockRequest</c> 的 <c>update_text_elements</c> 面
/// （替换块的文本元素）。其余 11 个面（<c>update_text_style</c> / <c>update_table_property</c> /
/// <c>insert_table_row</c> / <c>merge_table_cells</c> …）属表格结构与富样式编辑，
/// 每个都需要独立的参数面；塞进同一个工具会让模型面对 12 个互斥可选参数而难以正确选择。
/// 故本工具不提供那些面（需改表格结构/富样式时另立工具）。
/// </remarks>
[AgentTool("docx.update_blocks",
    Description = "更新文档中已有块的文本内容（改写段落/标题的文字）。block_id 来自 docx.append_blocks 或 docx.get_raw_content 的返回。⚠️ 只支持改文本，不支持改表格结构与富样式（见工具说明）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.BatchUpdateBlocksAsync))]
public interface IFeishuTenantDocxUpdateBlocksTool
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
[AgentTool("docx.delete_blocks",
    Description = "删除某个父块下指定索引区间的子块（如删掉文档里过时的一批段落）。索引是父块下的子块序号（从 0 开始），可由 docx.get_raw_content 或 docx.append_blocks 的返回推算。⚠️ 删除不可撤销——务必先用 dry_run 确认区间。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.BatchDeleteBlocksAsync))]
public interface IFeishuTenantDocxDeleteBlocksTool
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
[AgentTool("docx.import_markdown",
    Description = "把 Markdown 内容转换成文档块结构（不写入文档）——用于先预览转换结果，再用 docx.append_blocks 写入。支持文本、一到九级标题、有序/无序列表、代码块、引用、待办、图片、表格。只读转换，需 docx:document。",
    RequiredScopes = ["docx:document"],
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.ContentConvertAsync))]
public interface IFeishuTenantDocxImportMarkdownTool
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
/// <b>⚠️ 安全性设计：执行顺序是<b>先建后删</b></b>（先追加新块、再删除旧块），从而消除"已删未建"的破坏窗口：
/// <list type="number">
/// <item>转换失败 ⇒ 文档未被触碰；</item>
/// <item>追加失败 ⇒ 旧内容完整，新内容未入库（<b>无损失</b>）；</item>
/// <item>删除失败 ⇒ 新旧内容并存——这是<b>可见且可恢复</b>的状态，工具会如实上报
/// 精确的待删区间与新建块 ID，并明确告知"内容未丢失"。</item>
/// </list>
/// 不要改成"先删后建"：删除成功而创建失败会留下<b>被清空的文档</b>，且补偿（重放旧块）
/// 需要先完整快照旧文档——在 SDK 无事务的前提下，那是真正的不可靠路径。
/// </para>
/// <para>
/// <b>为什么 <c>idempotency_key</c> 是必填</b>：写操作在"已追加未删除"处中断后重试，
/// 若没有稳定 client_token，会把新内容<b>再插一遍</b>并重复删除。必填让重试语义明确。
/// </para>
/// </remarks>
[AgentTool("docx.replace_document",
    Description = "用 Markdown 内容整体替换文档正文（保留为新内容的旧块会被删除）。适用于'把这篇文章重建一遍'。执行顺序为先追加新块、再删除旧块，因此中途失败不会导致内容丢失。⚠️ 必须提供 idempotency_key（重试语义）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateBlockAsync))]
public interface IFeishuTenantDocxReplaceDocumentTool
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

/// <summary>
/// 工具接口：docx.media_insert（<b>组合工具</b>：Drive 媒体上传 → Docx 图片块创建）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是组合</b>：图片要出现在文档里，平台要求先把二进制上传到「云文档素材」拿
/// <c>file_token</c>（<c>/drive/v1/medias/upload_all</c>，<c>parent_type=docx_image</c>），
/// 再以该 token 创建 <c>image</c> 块——两步分属 Drive 与 Docx 两个 SDK 模块，Docx 无单步插入接口。
/// </para>
/// <para>
/// <b>输入形态与落盘</b>：只接受 http/https 绝对地址；由宿主的
/// <see cref="IFeishuAttachmentStager"/> 落盘后交 SDK 的 <c>[FilePath]</c> 参数消费
/// （域名白名单/大小上限/MIME 校验由宿主实现，工具层不假装能做安全域）。
/// 临时文件在 <c>finally</c> 中清理。
/// </para>
/// <para>
/// <b>⚠️ 部分失败语义</b>：两步之间没有事务——媒体上传成功而块创建失败时，会留下一枚
/// <b>未被引用的素材</b>（不显示在文档里，但计入云文档素材配额）。工具会如实上报
/// <c>partial_failure=true</c> 与 <c>image_token</c>，重试会再次上传（素材不具幂等键）；
/// 块创建本身可用 <c>idempotency_key</c> 去重。
/// </para>
/// </remarks>
[AgentTool("docx.media_insert",
    Description = "把图片从 URL 上传到云文档素材并插入为文档中的图片块（两步：Drive 媒体上传 → Docx 图片块）。仅接受 http/https 绝对地址；宿主须注册 IFeishuAttachmentStager 与 Drive API。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document 与 drive:drive。",
    RequiredScopes = ["docx:document", "drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateBlockAsync))]
public interface IFeishuTenantDocxMediaInsertTool
{
    /// <summary>上传图片并插入图片块。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id / image_token）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> MediaInsertAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("image_url", "图片地址（http/https 绝对地址；经宿主落盘器校验域名/大小/MIME）", Required = true)] string image_url,
        [ToolParameter("file_name", "素材文件名（可选；缺省从 URL 路径推导，无路径时用 image.png）")] string? file_name = null,
        [ToolParameter("parent_block_id", "父块 ID（可选，默认文档根块 document_id）")] string? parent_block_id = null,
        [ToolParameter("width", "图片显示宽度（像素，可选；平台按比例约束）")] int? width = null,
        [ToolParameter("height", "图片显示高度（像素，可选；平台按比例约束）")] int? height = null,
        [ToolParameter("align", "对齐方式（可选：left / center / right，默认 center）")] string? align = null,
        [ToolParameter("caption", "图片描述（可选，显示在图片下方）")] string? caption = null,
        [ToolParameter("index", "插入到父块下的位置（可选，默认追加到末尾）")] int? index = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：仅作用于图片块创建（client_token，24 小时内至多成功一次）；媒体上传本身不支持幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 steps 与 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
