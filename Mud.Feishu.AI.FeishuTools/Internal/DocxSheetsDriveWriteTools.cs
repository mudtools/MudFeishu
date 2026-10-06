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
            var blocks = BuildBlocks(args);
            var parentBlockId = args.ParentBlockId ?? args.DocumentId;

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/docx/v1/documents/{args.DocumentId}/blocks/{parentBlockId}/children",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_id", args.DocumentId.Length), ("blocks", blocks.Count)));
            }

            var request = new CreateBlockRequest { Childrens = [.. blocks] };
            if (args.Index is not null)
            {
                request.Index = args.Index;
            }

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .CreateBlockAsync(
                    args.DocumentId,
                    parentBlockId,  // 缺省父块 = 文档根块（block_id == document_id）
                    request,
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                // 逐个回填新增块 ID（模型下一跳要用它定位），单块时长度 1。
                ["block_ids"] = new JsonArray(
                    [.. (data.Childrens ?? []).Select(static child => (JsonNode?)child?.BlockId)]),
            });
        });
    }

    /// <summary>单次追加的子块上限（平台约束）。</summary>
    private const int MaxChildrenPerRequest = 50;

    /// <summary>
    /// 支持的块型名（**小写**，即平台/飞书文档里的写法：<c>text</c>/<c>heading1</c>/<c>todo</c>…）。
    /// </summary>
    /// <remarks>
    /// <b>为什么是小写而不是 <c>BlockTypes</c> 的 PascalCase 常量名</b>：模型（与平台文档） naturally
    /// 写的是小写；<see cref="BlockTypes"/> 的常量名是 C# 标识符（PascalCase）。二者靠
    /// <b>大小写不敏感</b>匹配，既接受 <c>text</c> 也接受 <c>Text</c>，而错误清单统一给
    /// <b>规范小写名</b>（与平台一致，模型照抄即可）。
    /// </remarks>
    private static readonly string[] SupportedBlockTypeNames =
    [
        BlockTypes.GetName(BlockTypes.Text).ToLowerInvariant(),
        .. Enumerable.Range(1, 9)
            .Select(i => BlockTypes.GetName(BlockTypes.Heading1 + i - 1).ToLowerInvariant()),
        BlockTypes.GetName(BlockTypes.Bullet).ToLowerInvariant(),
        BlockTypes.GetName(BlockTypes.Ordered).ToLowerInvariant(),
        BlockTypes.GetName(BlockTypes.Code).ToLowerInvariant(),
        BlockTypes.GetName(BlockTypes.Quote).ToLowerInvariant(),
        BlockTypes.GetName(BlockTypes.Todo).ToLowerInvariant(),
        BlockTypes.GetName(BlockTypes.Divider).ToLowerInvariant(),
    ];

    /// <summary>
    /// 构造要追加的块列表：<c>blocks</c>（多块 JSON 数组）优先，否则 <c>block_type</c>+<c>text</c> 单块简写。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么两条路径并存</b>：单块是主流（多数调用只加一段），让模型填两个标量比手写 JSON
    /// 数组更不易错；多块必须走数组。<b>blocks 优先</b>，规则写在参数描述里。
    /// </para>
    /// <para>
    /// <b>为什么仍按名称枚举块型</b>：单块路径的 <c>block_type</c> 带 F-2 的
    /// <c>EnumType=BlockTypes</c> 闭集（模型在 Schema 里直接看到合法名）；多块路径在 JSON 内部
    /// 拿不到 Schema 枚举，故<b>运行时按同一份 <c>BlockTypes</c> 校验</b>并把合法值清单写进错误文案
    /// （F-8 suggestions 的来源）。两条路径共用同一真相源，不会出现两套块型口径。
    /// </para>
    /// </remarks>
    private static List<Block> BuildBlocks(DocxAppendBlocksArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Blocks))
        {
            return ParseBlocksJson(args.Blocks!);
        }

        if (args.BlockType is null || args.Text is null)
        {
            throw new ArgumentException(
                "blocks 与（block_type + text）至少提供一组：单块用 block_type+text，多块用 blocks（JSON 数组）");
        }

        return [BuildBlockByName(BlockTypes.GetName(args.BlockType.Value), args.Text)];
    }

    private static List<Block> ParseBlocksJson(string json)
    {
        List<Block> blocks;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    "blocks 必须是 JSON 数组，如 [{\"block_type\":\"heading1\",\"text\":\"标题\"}]");
            }

            blocks = [.. document.RootElement.EnumerateArray().Select(ParseBlockItem)];
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"blocks 不是合法 JSON 数组：{ex.Message}。示例：[{{\"block_type\":\"heading1\",\"text\":\"标题\"}}]", ex);
        }

        if (blocks.Count == 0)
        {
            throw new ArgumentException("blocks 至少要有一个块");
        }

        if (blocks.Count > MaxChildrenPerRequest)
        {
            throw new ArgumentException($"单次最多追加 {MaxChildrenPerRequest} 个子块，收到 {blocks.Count} 个");
        }

        return blocks;
    }

    private static Block ParseBlockItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                $"blocks 每一项都必须是对象（如 {{\"block_type\":\"text\",\"text\":\"…\"}}），收到 {item.ValueKind}");
        }

        var typeName = item.TryGetProperty("block_type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String
                ? typeElement.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("blocks 每项都必须有字符串字段 block_type");
        }

        var text = item.TryGetProperty("text", out var textElement)
            && textElement.ValueKind == JsonValueKind.String
                ? textElement.GetString()
                : null;

        return BuildBlockByName(typeName!, text);
    }

    /// <summary>按块型<b>名称</b>构造块；非法值附合法值清单（divider 无需文本）。</summary>
    private static Block BuildBlockByName(string rawTypeName, string? text)
    {
        // 归一化到规范小写名（平台/模型写法），再做**大小写不敏感**匹配。
        var typeName = rawTypeName.Trim().ToLowerInvariant();
        var matched = Array.Find(SupportedBlockTypeNames, candidate =>
            string.Equals(candidate, typeName, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
        {
            throw new ArgumentException(
                $"block_type '{rawTypeName}' 不支持。可用值：{string.Join(" / ", SupportedBlockTypeNames)}"
                + "（表格/单元格/引用容器暂不支持：SDK 侧尚无对应类型，见 C-7）");
        }

        var block = new Block { BlockType = ResolveBlockType(matched) };

        // divider 没有内容属性，其余文本型块都挂 BlockText。
        if (matched == DividerBlockTypeName)
        {
            block.Divider = new { };
        }
        else
        {
            SetTextElement(block, matched, new BlockText
            {
                Elements = [new TextElement { TextRun = new TextElementTextRun { Content = text ?? string.Empty } }],
            });
        }

        return block;
    }

    /// <summary>按块型名把内容挂到对应的元素属性（text/heading1..9/bullet/ordered/code/quote/todo）。</summary>
    private static void SetTextElement(Block block, string typeName, BlockText content)
    {
        switch (typeName)
        {
            case "text": block.Text = content; break;
            case "heading1": block.Heading1 = content; break;
            case "heading2": block.Heading2 = content; break;
            case "heading3": block.Heading3 = content; break;
            case "heading4": block.Heading4 = content; break;
            case "heading5": block.Heading5 = content; break;
            case "heading6": block.Heading6 = content; break;
            case "heading7": block.Heading7 = content; break;
            case "heading8": block.Heading8 = content; break;
            case "heading9": block.Heading9 = content; break;
            case "bullet": block.Bullet = content; break;
            case "ordered": block.Ordered = content; break;
            case "code": block.Code = content; break;
            case "quote": block.Quote = content; break;
            case "todo": block.Todo = content; break;
        }
    }

    /// <summary>规范小写的 divider 块型名（避免每次调用重复构造）。</summary>
    private static readonly string DividerBlockTypeName =
        BlockTypes.GetName(BlockTypes.Divider).ToLowerInvariant();

    private static int ResolveBlockType(string normalizedName)
    {
        foreach (var value in BlockTypes.All)
        {
            if (string.Equals(BlockTypes.GetName(value), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        throw new ArgumentException(
            $"block_type '{normalizedName}' 不支持。可用值：{string.Join(" / ", SupportedBlockTypeNames)}");
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
