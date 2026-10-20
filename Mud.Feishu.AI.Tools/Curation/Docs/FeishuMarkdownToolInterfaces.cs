// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// markdown 域：Agent ↔ 飞书的内容交换格式生命周期（.md）。
//
// 为什么不是「上传一个 .md 二进制文件就完了」：云空间里的资源文件**没有原位更新接口**，
// 覆盖只能删旧建新（token 变化，所有引用一起失效）。故「可直接改写的 Markdown 载体」落成
// 新版文档（docx）：写入走 md→块转换，读取走 docs/v1/content 的 markdown 出口。
// 云空间里现成的 .md 资源文件仍可读（drive.download_file 的文本形态），但只读。

/// <summary>工具接口：markdown.create（组合：上传 .md → 创建导入任务 → 有界轮询）。</summary>
/// <remarks>
/// <para>
/// <b>两步为何不可省</b>：平台没有「直接创建 docx 并写入正文」的单接口；官方链路就是
/// 上传源文件 → 导入任务转 docx。源 .md 是**临时物**，导入终态后由执行器清理（失败时保留 token 供排查）。
/// </para>
/// <para>
/// <b>file_extension 必须与源文件后缀逐字一致</b>（平台 1069910）：执行器统一把源文件命名为 <c>*.md</c>
/// 并传 <c>md</c>，不接受调用方改后缀。
/// </para>
/// </remarks>
[AgentTool("markdown.create",
    Description = "把 Markdown 内容创建为飞书在线文档（异步导入为 docx），返回 document_token/url。导入任务终态后自动清理源 .md 临时文件。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.CreateImportTaskAsync))]
public interface IFeishuTenantMarkdownCreateTool
{
    /// <summary>创建 Markdown 文档。</summary>
    /// <returns>结构化 JSON（document_token/url/title/source_file_token/source_file_cleaned）；<c>dry_run=true</c> 时只返回步骤摘要。</returns>
    Task<string> CreateAsync(
        [ToolParameter("title", "新文档标题（同时作为源 .md 文件名，不含扩展名）", Required = true)] string title,
        [ToolParameter("markdown", "Markdown 正文（非空；UTF-8 后不超过 20 MB）", Required = true)] string markdown,
        [ToolParameter("folder_token", "目标文件夹 token（可选；缺省为云空间根目录）")] string? folder_token = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的步骤与 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：markdown.fetch（docx 走 docs/v1/content；.md 资源文件走下载 + UTF-8 解码）。</summary>
/// <remarks>
/// <b>窗口化而非截断</b>：正文可能远超上下文预算，按 <c>offset</c>/<c>limit</c> 切片并给出
/// <c>next_offset</c>，模型可自行续读全量；「截断 + 标记」会让后半段永远不可达。
/// </remarks>
[AgentTool("markdown.fetch",
    Description = "读取 Markdown 正文（token_type=docx 读新版文档；token_type=file 读云空间 .md 资源文件）。按 offset/limit 分窗口返回，has_more=true 时用 next_offset 续读。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.GetFileContentByFileTokenAsync))]
public interface IFeishuTenantMarkdownFetchTool
{
    /// <summary>读取 Markdown 正文（窗口）。</summary>
    /// <returns>结构化 JSON（markdown/total_length/offset/limit/has_more/next_offset）。</returns>
    Task<string> FetchAsync(
        [ToolParameter("token", "文档或文件 token（来自 markdown.create / drive.list_folder_files）", Required = true)] string token,
        [ToolParameter("token_type", "token 类型：docx=新版文档（默认）；file=云空间 .md 资源文件")] string? token_type = null,
        [ToolParameter("offset", "起始字符偏移（可选，默认 0）")] int? offset = null,
        [ToolParameter("limit", "窗口长度（可选，默认 10000，上限 50000）")] int? limit = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：markdown.patch（读 → 精确文本替换 → 整篇重建；组合 Source = Docx 块接口）。</summary>
/// <remarks>
/// <para>
/// <b>为什么是「精确文本」而不是行号</b>：docx 的 Markdown 出口不保证行号稳定（块结构决定换行），
/// 而行号漂移会让补丁打到错误位置。精确文本命中 0 次即报错、多次即要求 <c>replace_all</c> 或更长上下文，
/// 把歧义留在本地而不是让平台猜。
/// </para>
/// <para>
/// <b>idempotency_key 必填</b>：整篇重建是「先追加后删除」两步写，重试无稳定 client_token 会重复插入。
/// </para>
/// </remarks>
[AgentTool("markdown.patch",
    Description = "按精确文本替换文档正文的一段（find 必须唯一命中；多处命中须显式 replace_all=true）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document + drive:drive:readonly。",
    RequiredScopes = ["docx:document", "drive:drive:readonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateBlockAsync))]
public interface IFeishuTenantMarkdownPatchTool
{
    /// <summary>按文本补丁修改文档。</summary>
    /// <returns>结构化 JSON（changed/replacements/before_length/after_length/appended/deleted）；未命中时 changed=false。</returns>
    Task<string> PatchAsync(
        [ToolParameter("document_token", "新版文档 token（形如 doxcnXxx）", Required = true)] string document_token,
        [ToolParameter("find", "要替换的原文（非空；须与正文逐字一致，含空白）", Required = true)] string find,
        [ToolParameter("replace_with", "替换文本（缺省或空串 = 删除该段）")] string? replace_with = null,
        [ToolParameter("replace_all", "替换全部命中（可选，默认 false）；命中多处时不设此项会被拒绝")] bool? replace_all = null,
        [ToolParameter("idempotency_key", "幂等键（必填）：相同 client_token 在 24 小时内至多成功一次，防止重试重复插入。请用稳定值")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回命中数与长度变化，不调用下游写接口")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：markdown.overwrite（用 Markdown 整体重建正文；组合 Source = Docx 块接口）。</summary>
/// <remarks>
/// 执行顺序为「先追加新块、再删除旧块」：中途失败不会导致内容丢失（详见 <c>docx.replace_document</c>）。
/// </remarks>
[AgentTool("markdown.overwrite",
    Description = "用一段 Markdown 整体重建文档正文（先追加新块再删旧块，中途失败不丢内容）。与 docx.replace_document 同语义，但面向 markdown 生命周期。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateBlockAsync))]
public interface IFeishuTenantMarkdownOverwriteTool
{
    /// <summary>整体重建文档正文。</summary>
    /// <returns>结构化 JSON（appended/new_block_ids/deleted；删除失败时 partial_failure=true 与修复指引）。</returns>
    Task<string> OverwriteAsync(
        [ToolParameter("document_token", "新版文档 token（形如 doxcnXxx）", Required = true)] string document_token,
        [ToolParameter("markdown", "新的正文内容（Markdown 格式，非空）", Required = true)] string markdown,
        [ToolParameter("idempotency_key", "幂等键（必填）：相同 client_token 在 24 小时内至多成功一次，防止重试重复插入。请用稳定值")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的步骤与 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：markdown.diff（两份 Markdown 的行级差异）。</summary>
/// <remarks>
/// 右值二选一：<c>right_token</c>（另一份文档/文件）或 <c>right_text</c>（模型手头的草稿）。
/// 输出是有界 unified diff（按工具预算截断并标记 <c>truncated</c>），不是整篇正文。
/// </remarks>
[AgentTool("markdown.diff",
    Description = "比较两份 Markdown 的行级差异（左侧 token；右侧二选一：token 或直接文本），返回 unified diff 与增删行数。用于写入前预审、或核对远端是否被别人改过。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.GetFileContentByFileTokenAsync))]
public interface IFeishuTenantMarkdownDiffTool
{
    /// <summary>比较两份 Markdown。</summary>
    /// <returns>结构化 JSON（changed/added_lines/removed_lines/diff/truncated/diff_total_length）。</returns>
    Task<string> DiffAsync(
        [ToolParameter("left_token", "左侧文档/文件 token", Required = true)] string left_token,
        [ToolParameter("right_token", "右侧文档/文件 token（与 right_text 二选一）")] string? right_token = null,
        [ToolParameter("right_text", "右侧 Markdown 文本（与 right_token 二选一）")] string? right_text = null,
        [ToolParameter("left_token_type", "左侧 token 类型：docx（默认）/ file")] string? left_token_type = null,
        [ToolParameter("right_token_type", "右侧 token 类型：docx（默认）/ file")] string? right_token_type = null,
        CancellationToken cancellationToken = default);
}
