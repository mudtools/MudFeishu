// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Docx;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Folder;
using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.AI.FeishuTools.Internal;

// ─────────────────────────── Docx 写执行器 ───────────────────────────

/// <summary>
/// Docx 写工具执行器（<c>docx.create_document</c> / <c>docx.append_blocks</c>，WP2/R5）。
/// </summary>
internal sealed class DocxWriteTools(Mud.Feishu.IFeishuTenantV1Docx docxClient, Mud.Feishu.IFeishuTenantV1DocxBlocks blocksClient)
{
    private readonly Mud.Feishu.IFeishuTenantV1Docx _docxClient = docxClient
        ?? throw new ArgumentNullException(nameof(docxClient));
    private readonly Mud.Feishu.IFeishuTenantV1DocxBlocks _blocksClient = blocksClient
        ?? throw new ArgumentNullException(nameof(blocksClient));

    /// <summary>docx.create_document：创建文档（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuDocxCreateDocumentTool))]
    public Task<FeishuToolResult> CreateDocumentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxCreateDocument);
        return executor.RunAsync(async () =>
        {
            var args = DocxCreateDocumentArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/docx/v1/documents",
                    ToolDryRun.IdempotencyNote(null),
                    ("folder_token", args.FolderToken?.Length ?? 0), ("title", args.Title?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _docxClient
                .CreateDocumentAsync(
                    new CreateDocumentRequest { FolderToken = args.FolderToken, Title = args.Title },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["document_id"] = data.Document?.DocumentId,
            });
        });
    }

    /// <summary>docx.append_blocks：在文档根块下创建子块（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（client_token）：相同 client_token 在 24 小时内至多成功创建一次。</remarks>
    [FeishuToolHandler(typeof(IFeishuDocxAppendBlocksTool))]
    public Task<FeishuToolResult> AppendBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxAppendBlocks);
        return executor.RunAsync(async () =>
        {
            var args = DocxAppendBlocksArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/docx/v1/documents/{args.DocumentId}/blocks/{args.DocumentId}/children",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_id", args.DocumentId.Length), ("block_type", args.BlockType.ToString().Length), ("text", args.Text.Length)));
            }

            // 构造 Block：根据 block_type 设置 Text 元素
            var block = new Block
            {
                BlockType = args.BlockType,
                Text = new BlockText
                {
                    Elements = [new TextElement { TextRun = new TextElementTextRun { Content = args.Text } }],
                },
            };

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .CreateBlockAsync(
                    args.DocumentId,
                    args.DocumentId,  // 根块的 block_id = document_id
                    new CreateBlockRequest { Childrens = [block] },
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["block_id"] = data.Childrens?.Length > 0 ? data.Childrens[0]?.BlockId : null,
            });
        });
    }
}

// ─────────────────────────── Sheets 写执行器 ───────────────────────────

/// <summary>
/// Sheets 写工具执行器（<c>sheets.update_range</c> / <c>sheets.append_rows</c>，WP2/R5）。
/// </summary>
internal sealed class SheetsWriteTools(Mud.Feishu.IFeishuTenantV3SpreadsheetData dataClient)
{
    private readonly Mud.Feishu.IFeishuTenantV3SpreadsheetData _dataClient = dataClient
        ?? throw new ArgumentNullException(nameof(dataClient));

    /// <summary>sheets.update_range：向指定区域写入数据（覆盖写，天然幂等）。</summary>
    [FeishuToolHandler(typeof(IFeishuSheetsUpdateRangeTool))]
    public Task<FeishuToolResult> UpdateRangeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsUpdateRange);
        return executor.RunAsync(async () =>
        {
            var args = SheetsUpdateRangeArgs.Unpack(arguments);

            // 解析 values JSON 为二维数组
            var values = ParseValuesArray(args.Values);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", $"/open-apis/sheets/v2/spreadsheets/{args.SpreadsheetToken}/values",
                    ToolDryRun.IdempotencyNote("覆盖写天然幂等"),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("range", args.Range.Length), ("values", args.Values.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _dataClient
                .RangeWriteDataAsync(
                    args.SpreadsheetToken,
                    new RangeDataOpsRequest { ValueRange = new RangeValues { Range = args.Range, Values = values } },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["revision"] = data.Revision,
            });
        });
    }

    /// <summary>sheets.append_rows：追加行数据。</summary>
    [FeishuToolHandler(typeof(IFeishuSheetsAppendRowsTool))]
    public Task<FeishuToolResult> AppendRowsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsAppendRows);
        return executor.RunAsync(async () =>
        {
            var args = SheetsAppendRowsArgs.Unpack(arguments);

            var values = ParseValuesArray(args.Values);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/sheets/v2/spreadsheets/{args.SpreadsheetToken}/values_append",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("range", args.Range.Length), ("values", args.Values.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _dataClient
                .AppendDataAsync(
                    args.SpreadsheetToken,
                    new RangeDataOpsRequest { ValueRange = new RangeValues { Range = args.Range, Values = values } },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["revision"] = data.Revision,
                ["spreadsheet_token"] = data.SpreadsheetToken,
            });
        });
    }

    private static object[][] ParseValuesArray(string valuesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(valuesJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("values 必须是 JSON 二维数组，如 [[\"A1\",\"B1\"]]");
            }

            // 转换为 object[][] 以匹配 RangeValues.Values 的类型
            var result = new List<object[]>();
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                var cells = new List<object>();
                foreach (var cell in row.EnumerateArray())
                {
                    cells.Add(cell.ValueKind switch
                    {
                        JsonValueKind.String => cell.GetString() ?? string.Empty,
                        JsonValueKind.Number => cell.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => string.Empty,
                        _ => cell.GetRawText(),
                    });
                }

                result.Add([.. cells]);
            }

            return [.. result];
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"values 不是合法 JSON：{ex.Message}", ex);
        }
    }
}

// ─────────────────────────── Drive 写执行器 ───────────────────────────

/// <summary>
/// Drive 写工具执行器（<c>drive.create_folder</c> / <c>drive.move_file</c> / <c>drive.upload_file</c>，WP2/R5）。
/// </summary>
internal sealed class DriveWriteTools(
    Mud.Feishu.IFeishuTenantV1DriveFolder folderClient,
    Mud.Feishu.IFeishuTenantV1DriveFiles filesClient,
    IFeishuAttachmentStager? stager,
    ILogger<DriveWriteTools>? logger = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1DriveFolder _folderClient = folderClient
        ?? throw new ArgumentNullException(nameof(folderClient));
    private readonly Mud.Feishu.IFeishuTenantV1DriveFiles _filesClient = filesClient
        ?? throw new ArgumentNullException(nameof(filesClient));
    private readonly IFeishuAttachmentStager? _stager = stager;

    /// <summary>日志（可空；R2-06 起用于"临时附件清理失败"留痕——磁盘残留是用户数据驻留面）。</summary>
    private readonly ILogger? _logger = logger;

    /// <summary>drive.create_folder：创建文件夹。</summary>
    [FeishuToolHandler(typeof(IFeishuDriveCreateFolderTool))]
    public Task<FeishuToolResult> CreateFolderAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveCreateFolder);
        return executor.RunAsync(async () =>
        {
            var args = DriveCreateFolderArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/files/create_folder",
                    ToolDryRun.IdempotencyNote(null),
                    ("name", args.Name.Length), ("folder_token", args.FolderToken?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _folderClient
                .CreateFolderAsync(
                    new CreateFolderRequest { Name = args.Name, FolderToken = args.FolderToken ?? string.Empty },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["folder_token"] = data.Token,
            });
        });
    }

    /// <summary>drive.move_file：移动文件（返回异步任务 ID）。</summary>
    [FeishuToolHandler(typeof(IFeishuDriveMoveFileTool))]
    public Task<FeishuToolResult> MoveFileAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveMoveFile);
        return executor.RunAsync(async () =>
        {
            var args = DriveMoveFileArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/files/move",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_token", args.FileToken.Length), ("type", args.Type.Length), ("folder_token", args.FolderToken.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _filesClient
                .MoveFileByFileTokenAsync(
                    new MoveFileRequest { Type = args.Type, FolderToken = args.FolderToken },
                    args.FileToken,
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["task_id"] = data.TaskId,
                ["async_task"] = true,
            });
        });
    }

    /// <summary>drive.upload_file：上传文件（需宿主落盘器）。</summary>
    [FeishuToolHandler(typeof(IFeishuDriveUploadFileTool))]
    public Task<FeishuToolResult> UploadFileAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveUploadFile);
        return executor.RunAsync(async () =>
        {
            var args = DriveUploadFileArgs.Unpack(arguments);
            var fileUrl = AttachmentTools.RequireHttpUrl(args.FileUrl, "file_url");

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/files/upload_all",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_url", fileUrl.Length), ("file_name", args.FileName.Length), ("folder_token", args.FolderToken?.Length ?? 0)));
            }

            if (_stager is null)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, "宿主未注册 IFeishuAttachmentStager，drive.upload_file 不可用"));
            }

            var staged = await _stager.StageAsync(new AttachmentSource(Url: fileUrl, Content: null, FileName: args.FileName), cancellationToken);
            if (staged is null)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, "来源不被宿主允许（安全域拒绝）"));
            }

            var stagedValue = staged.Value;
            try
            {
                var outcome = FeishuApiResultReader.Read(await _filesClient
                    .UploadAllFileAsync(
                        new UploadAllFileRequest
                        {
                            FileName = args.FileName,
                            FilePath = stagedValue.LocalPath,
                            ParentType = "explorer",
                            ParentNode = args.FolderToken ?? string.Empty,
                            Size = (int)stagedValue.Size,
                        },
                        cancellationToken)
                    .ConfigureAwait(false));
                return executor.FromApiUntruncated(outcome, data => new JsonObject
                {
                    ["file_token"] = data.FileToken,
                });
            }
            finally
            {
                try
                {
                    await stagedValue.Cleanup().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // 清理失败不覆盖业务结果，但必须留痕（R2-06）：宿主此前没有任何可观测来源。
                    _logger?.LogWarning(ex, "临时附件清理失败，文件可能残留（path: {Path}）", stagedValue.LocalPath);
                }
            }
        });
    }
}
