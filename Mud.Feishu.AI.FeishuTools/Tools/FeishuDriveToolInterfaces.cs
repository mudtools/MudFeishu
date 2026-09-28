// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

// <summary>
// Drive 域只读工具接口（AI-FD-D12 P1D-1b 批次 A：云空间导航入口，与 search.doc_wiki 互补——浏览 vs 搜索）。
// </summary>

/// <summary>工具接口：drive.list_folder_files（映射 <c>IFeishuTenantV1DriveFolder.GetFilesPageListAsync</c>）。</summary>
[FeishuTool("drive.list_folder_files",
    Description = "列出云空间文件夹内的文件与子文件夹（token/name/type/url）；folder_token 缺省列根目录。与 search.doc_wiki 互补（浏览 vs 搜索）。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = "IFeishuTenantV1DriveFolder.GetFilesPageListAsync")]
public interface IFeishuDriveFolderFilesTool
{
    /// <summary>列出文件夹内容（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetFilesPageListAsync(
        [ToolParameter("folder_token", "文件夹 token（可选；缺省列出云空间根目录）")] string? folder_token = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.get_file_metas（映射 <c>IFeishuTenantV1DriveFiles.BatchQueryMetasAsync</c>）。</summary>
[FeishuTool("drive.get_file_metas",
    Description = "按 token 批量查询文件元信息（类型/标题/链接/所有者，最多 200 个）；支撑后续读文档链（docx.get_raw_content 等）。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = "IFeishuTenantV1DriveFiles.BatchQueryMetasAsync")]
public interface IFeishuDriveFileMetasTool
{
    /// <summary>元信息批量查询。</summary>
    /// <returns>白名单投影后的 JSON 文本（metas/failed_lists），超长截断并标记 truncated。</returns>
    Task<string> BatchQueryMetasAsync(
        [ToolParameter("tokens", "文件/文档 token 数组（最多 200 个）", Required = true)] string[] tokens,
        [ToolParameter("types", "与 tokens 等长的类型数组（可选：doc/docx/sheet/bitable/mindnote/file/wiki/folder/synced_block；缺省按 file 处理）")] string[]? types = null,
        CancellationToken cancellationToken = default);
}
