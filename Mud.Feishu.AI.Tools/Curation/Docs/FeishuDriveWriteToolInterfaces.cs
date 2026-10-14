// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Drive 写（3 个） ───────────────────────────

/// <summary>工具接口：drive.create_folder（映射 <c>IFeishuTenantV1DriveFolder.CreateFolderAsync</c>）。</summary>
[AgentTool("drive.create_folder",
    Description = "在云空间中创建文件夹（返回 folder_token，可用 drive.list_folder_files 验证）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveFolder) + "." + nameof(IFeishuTenantV1DriveFolder.CreateFolderAsync))]
public interface IFeishuTenantDriveCreateFolderTool
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
[AgentTool("drive.move_file",
    Description = "将文件或文件夹移动到指定文件夹下（返回异步任务 ID——移动操作为异步执行，需后续轮询确认完成）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.MoveFileByFileTokenAsync))]
public interface IFeishuTenantDriveMoveFileTool
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
[AgentTool("drive.upload_file",
    Description = "将网络文件上传到云空间（file_url 为 http/https 绝对地址，由宿主负责下载落盘——与 im.send_image 同机制）。返回 file_token。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.UploadAllFileAsync))]
public interface IFeishuTenantDriveUploadFileTool
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

/// <summary>
/// 工具接口：drive.download_file（映射 <c>IFeishuTenantV1DriveFiles.DownloadFileAsync</c> + 宿主 stager 落盘）。
/// </summary>
// ─────────────────────────── R5 / F-10 现状（2026-10-07 更新）───────────────────────────
//
// 【已解决】B-2 的豁免机制此前**被收集却从未被使用**（`CollectBinarySafeTools()` 的返回值
//   在断言里没有参与任何判定）——即"看着存在但完全不生效"。现已接通并**加固为"挣得的豁免"**：
//   声明 `no-bytes-in-context` 只是**申请**，放行还需核验执行器源码里两道防线真的落地：
//     ① 注入**非可空**的 IFeishuAttachmentStager（字节交给宿主落盘边界；软缺席语义随之成立）；
//     ② 调用 DownloadedContentGuard.ShouldRejectForJsonErrorBody（平台错误体不得落盘）。
//   核验不通过的"豁免"会**按违规报出**，故"改一行描述就能关掉零容忍守卫"这条路已被堵死。
//   （守卫：Tests/…/ContractGuards/BinaryDownloadToolExposureContractTests.VerifyExemptions，
//     含合成源码的反向自证。）
//
// 【已排除】"改用返回临时 URL 的能力（BatchGetTmpDownloadUrlAsync）"**不可行**：
//   SDK 对该方法的注释原文——"本接口仅支持下载云文档而非云空间中的资源文件。
//   如要下载云空间中的资源文件，需调用[下载文件]接口"。即云空间资源文件**只能**走二进制接口。
//
// 【仍阻塞】`output_schema` 由 **Source 方法的返回类型**推导（CuratedToolScanner.ValidateReturnType
//   → TypeSchemaResolver.ResolveOutputSchema），而本工具的 Source 返回 `Task<byte[]?>` ⇒
//   生成的 output_schema 是 `{"type":"string","format":"binary"}`，会**告诉模型"本工具返回二进制"**，
//   与该工具实际返回的"本地路径/大小/Content-Type"元信息**矛盾**（对模型而言是假事实）。
//   `[AgentTool]` 目前**没有** output_schema 覆盖能力。
//
// ⇒ 落地 F-10 还差一个小能力（择一，属独立设计项）：
//   ⓐ 给 `[AgentTool]` 加 output_schema 覆盖（如 `OutputSchema = "…"` 或命名形态）；
//   ⓑ 或让"受控二进制出口"这条路径**不推导** output_schema（留空），由 Description 描述结果形状。
//   在选定之前不落 F-10 —— 避免交付一个"Schema 与实现互相矛盾"的工具。
