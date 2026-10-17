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
