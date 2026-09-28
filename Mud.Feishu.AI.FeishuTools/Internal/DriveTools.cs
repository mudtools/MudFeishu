// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Folder;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Drive 双工具执行器（<c>drive.list_folder_files</c> / <c>drive.get_file_metas</c>，
/// AI-FD-D12 P1D-1b 批次 A）：云空间导航入口——与 <c>search.doc_wiki</c> 互补（浏览 vs 搜索）。
/// </summary>
internal sealed class DriveTools(
    Mud.Feishu.IFeishuTenantV1DriveFolder folderClient,
    Mud.Feishu.IFeishuTenantV1DriveFiles filesClient,
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>drive.get_file_metas 缺省类型（官方 request_docs 需 doc_type；模型未提供时按 file 处理并在描述中声明）。</summary>
    private const string DefaultDocType = "file";

    private readonly Mud.Feishu.IFeishuTenantV1DriveFolder _folderClient = folderClient
        ?? throw new ArgumentNullException(nameof(folderClient));
    private readonly Mud.Feishu.IFeishuTenantV1DriveFiles _filesClient = filesClient
        ?? throw new ArgumentNullException(nameof(filesClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>drive.list_folder_files：列出文件夹内容（folder_token 缺省=根目录；白名单 token/name/type/url）。</summary>
    public async Task<FeishuToolResult> ListFolderFilesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var folderToken = ToolArgs.OptionalString(arguments, "folder_token");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var outcome = FeishuApiResultReader.Read(await _folderClient
                .GetFilesPageListAsync(folderToken, page_size: PageSizes.DriveFiles, page_token: pageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.DriveListFolderFiles, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = new JsonObject
            {
                ["items"] = new JsonArray(),
                ["has_more"] = data.HasMore,
            };
            if (!string.IsNullOrEmpty(data.PageToken))
            {
                envelope["page_token"] = data.PageToken;
            }

            foreach (var file in data.Files ?? [])
            {
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["token"] = file.Token,
                    ["name"] = file.Name,
                    ["type"] = file.Type,
                    ["url"] = file.Url,
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.DriveListFolderFiles, ex.Message));
        }
    }

    /// <summary>drive.get_file_metas：元信息批量查询（官方单请求上限 200；白名单 doc_token/doc_type/title/url/owner_id）。</summary>
    public async Task<FeishuToolResult> GetFileMetasAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = ToolArgs.OptionalStringArray(arguments, "tokens");
            var types = ToolArgs.OptionalStringArray(arguments, "types");

            if (tokens is null || tokens.Length == 0)
            {
                throw new ArgumentException("缺少必填参数 tokens");
            }

            if (tokens.Length > PageSizes.DriveMetas)
            {
                throw new ArgumentException(
                    $"tokens 最多 {PageSizes.DriveMetas.ToString(CultureInfo.InvariantCulture)} 个，实际 {tokens.Length.ToString(CultureInfo.InvariantCulture)} 个");
            }

            if (types is { Length: > 0 } && types.Length != tokens.Length)
            {
                throw new ArgumentException(
                    $"types 长度须与 tokens 一致（{tokens.Length.ToString(CultureInfo.InvariantCulture)}），实际 {types.Length.ToString(CultureInfo.InvariantCulture)}");
            }

            var requestDocs = new RequestDoc[tokens.Length];
            for (var i = 0; i < tokens.Length; i++)
            {
                requestDocs[i] = new RequestDoc
                {
                    DocToken = tokens[i],
                    DocType = types is { Length: > 0 } ? types[i] : DefaultDocType,
                };
            }

            var outcome = FeishuApiResultReader.Read(await _filesClient
                .BatchQueryMetasAsync(new MetasBatchQueryRequest { RequestDocs = requestDocs }, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.DriveGetFileMetas, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = new JsonObject { ["metas"] = new JsonArray() };
            foreach (var meta in data.Metas ?? [])
            {
                envelope["metas"]!.AsArray().AddNode(new JsonObject
                {
                    ["doc_token"] = meta.DocToken,
                    ["doc_type"] = meta.DocType,
                    ["title"] = meta.Title,
                    ["url"] = meta.Url,
                    ["owner_id"] = meta.OwnerId,
                });
            }

            if (data.FailedLists is { Length: > 0 })
            {
                var failed = new JsonArray();
                foreach (var failure in data.FailedLists)
                {
                    failed.AddNode(new JsonObject
                    {
                        ["token"] = failure.Token,
                        ["code"] = failure.Code,
                    });
                }

                envelope["failed_lists"] = failed;
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.DriveGetFileMetas, ex.Message));
        }
    }
}
