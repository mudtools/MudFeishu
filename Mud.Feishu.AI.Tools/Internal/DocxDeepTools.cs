// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.Docx;
// System.Diagnostics.FileVersionInfo 与本域同名（global using System.Diagnostics）。
using FileVersionInfo = Mud.Feishu.DataModels.Drive.FileVersions.FileVersionInfo;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Docx 深化工具执行器（6 个）：
/// <c>docx.get_document_info</c>/<c>docx.get_block</c>/<c>docx.list_block_children</c>/<c>docx.history_list</c> +
/// <c>docx.create_descendant_blocks</c> + <c>docx.get_chat_announcement</c>/<c>docx.set_chat_announcement</c>。
/// </summary>
/// <remarks>
/// 执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担；本类只保留参数校验与投影语义。
/// 写操作默认空名单不启用，启用前须经宿主授权（<c>IToolExecutionAuthorizer</c>）。
/// </remarks>
internal sealed class DocxDeepTools(
    Mud.Feishu.IFeishuTenantV1Docx docxClient,
    Mud.Feishu.IFeishuTenantV1DocxBlocks blocksClient,
    Mud.Feishu.IFeishuTenantV1DocxAnnouncement announcementClient,
    Mud.Feishu.IFeishuTenantV1DriveFilesVersions versionsClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1Docx _docxClient = docxClient
        ?? throw new ArgumentNullException(nameof(docxClient));
    private readonly Mud.Feishu.IFeishuTenantV1DocxBlocks _blocksClient = blocksClient
        ?? throw new ArgumentNullException(nameof(blocksClient));
    private readonly Mud.Feishu.IFeishuTenantV1DocxAnnouncement _announcementClient = announcementClient
        ?? throw new ArgumentNullException(nameof(announcementClient));
    private readonly Mud.Feishu.IFeishuTenantV1DriveFilesVersions _versionsClient = versionsClient
        ?? throw new ArgumentNullException(nameof(versionsClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ─────────────────────────── 只读面（4 个） ───────────────────────────

    /// <summary>docx.get_document_info：获取文档基本信息。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxGetDocumentInfoTool))]
    public Task<ToolResult> GetDocumentInfoAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxGetDocumentInfo, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxGetDocumentInfoArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _docxClient
                .GetDocumentInfoAsync(args.DocumentId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["document_id"] = data.Document?.DocumentId,
                ["revision_id"] = data.Document?.RevisionId,
                ["title"] = data.Document?.Title,
            });
        });
    }

    /// <summary>docx.get_block：获取单块详情。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxGetBlockTool))]
    public Task<ToolResult> GetBlockInfoAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxGetBlock, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxGetBlockArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .GetBlockInfoAsync(args.DocumentId, args.BlockId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => ProjectBlock(data.Block));
        });
    }

    /// <summary>docx.list_block_children：列出子块（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxListBlockChildrenTool))]
    public Task<ToolResult> ListBlockChildrenAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxListBlockChildren, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxListBlockChildrenArgs.Unpack(arguments);
            var blockId = args.BlockId ?? args.DocumentId; // 缺省=文档根块

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<Block>>(
                    async (token, ct) => FeishuApiResultReader.Read(await _blocksClient
                        .GetChildrenBlocksPageListAsync(
                            args.DocumentId,
                            blockId,
                            page_size: PageSizes.DocxBlocks,
                            page_token: token,
                            cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectBlockList(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .GetChildrenBlocksPageListAsync(
                    args.DocumentId,
                    blockId,
                    page_size: PageSizes.DocxBlocks,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectBlockList);
        });
    }

    /// <summary>docx.history_list：列出文档/电子表格的历史版本（平台端点归 Drive，分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxHistoryListTool))]
    public Task<ToolResult> ListHistoryAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxHistoryList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxHistoryListArgs.Unpack(arguments);
            var objType = (args.ObjType ?? "docx").Trim().ToLowerInvariant();
            if (objType is not ("docx" or "sheet"))
            {
                throw new ArgumentException($"obj_type 仅支持 docx / sheet，实际: {args.ObjType}");
            }

            // P-0 补齐：统一分页出口——聚合路径与单页路径共用同一次调用定义与同一份投影。
            async Task<ToolApiOutcome<ApiPageListResult<FileVersionInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _versionsClient
                    .GetFileVersionPageListByFileTokenAsync(
                        args.DocumentId,
                        obj_type: objType,
                        page_size: PageSizes.FileVersions,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectHistory, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    // ─────────────────────────── 写面（2 个） ───────────────────────────

    /// <summary>docx.create_descendant_blocks：创建结构化子树。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxCreateDescendantBlocksTool))]
    public Task<ToolResult> CreateDescendantBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxCreateDescendantBlocks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxCreateDescendantBlocksArgs.Unpack(arguments);

            // 解析 descendants（JSON 数组→Block[]）和 children_id（JSON 数组→string[]）
            var descendants = ParseBlocksJson(args.Descendants, "descendants");
            var childrenId = ParseStringArrayJson(args.ChildrenId, "children_id");

            // 引用完整性校验：children_id 中的每个临时 ID 必须在 descendants 中有对应块
            ValidateChildrenReferences(descendants, childrenId);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    $"/open-apis/docx/v1/documents/{args.DocumentId}/blocks/{args.BlockId}/descendant",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_id", args.DocumentId.Length), ("block_id", args.BlockId.Length),
                    ("descendants", descendants.Count), ("children_id", childrenId.Count)));
            }

            var request = new CreateDescendantBlockRequest
            {
                Descendants = [.. descendants],
                ChildrenId = [.. childrenId],
            };
            if (args.Index is not null)
            {
                request.Index = args.Index;
            }

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .CreateDescendantBlockAsync(
                    args.DocumentId,
                    args.BlockId,
                    request,
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["document_revision_id"] = data.DocumentRevisionId,
                ["block_id_relations"] = new JsonArray(
                    [.. (data.BlockIdRelations ?? []).Select(static r => (JsonNode?)new JsonObject
                    {
                        ["temporary_block_id"] = r.TemporaryBlockId,
                        ["block_id"] = r.BlockId,
                    })]),
            });
        });
    }

    // ─────────────────────────── 群公告面（2 个） ───────────────────────────

    /// <summary>docx.get_chat_announcement：读取群公告块内容（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxGetChatAnnouncementTool))]
    public Task<ToolResult> GetChatAnnouncementBlocksPageListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxGetChatAnnouncement, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxGetChatAnnouncementArgs.Unpack(arguments);

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<Block>>(
                    async (token, ct) => FeishuApiResultReader.Read(await _announcementClient
                        .GetChatAnnouncementBlocksPageListAsync(
                            args.ChatId,
                            page_size: PageSizes.DocxBlocks,
                            page_token: token,
                            cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectBlockList(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await _announcementClient
                .GetChatAnnouncementBlocksPageListAsync(
                    args.ChatId,
                    page_size: PageSizes.DocxBlocks,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectBlockList);
        });
    }

    /// <summary>docx.set_chat_announcement：批量更新群公告块文本。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxSetChatAnnouncementTool))]
    public Task<ToolResult> BatchUpdateChatAnnouncementBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxSetChatAnnouncement, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxSetChatAnnouncementArgs.Unpack(arguments);
            var requests = ParseUpdateRequests(args.Blocks, MaxUpdatesPerRequest);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH",
                    $"/open-apis/docx/v1/chats/{args.ChatId}/announcement/blocks/batch_update",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("chat_id", args.ChatId.Length), ("blocks", requests.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _announcementClient
                .BatchUpdateChatAnnouncementBlocksAsync(
                    args.ChatId,
                    new BatchUpdateBlocksRequest { Requests = requests },
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, _ => new JsonObject { ["updated"] = requests.Length });
        });
    }

    // ─────────────────────────── 投影辅助 ───────────────────────────

    /// <summary>投影 Block 列表（分页信封）：items（block_id/block_type/text 预览）+ 翻页契约。</summary>
    private static JsonObject ProjectBlockList(ApiPageListResult<Block> data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var block in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(ProjectBlock(block));
        }

        return envelope;
    }

    /// <summary>history_list 投影：items（version/name/creator_id/create_time/update_time/status）+ 翻页契约。</summary>
    private static JsonObject ProjectHistory(ApiPageListResult<FileVersionInfo> data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["version"] = item.VersionSuffix,
                ["name"] = item.Name,
                ["obj_type"] = item.ObjType,
                ["creator_id"] = item.CreatorId,
                ["create_time"] = item.CreateTime,
                ["update_time"] = item.UpdateTime,
                ["status"] = item.Status,
            });
        }

        return envelope;
    }

    /// <summary>投影单个 Block：block_id/block_type/text/children。</summary>
    private static JsonObject ProjectBlock(Block? block)
    {
        var obj = new JsonObject
        {
            ["block_id"] = block?.BlockId,
            ["block_type"] = block is not null ? BlockTypes.GetName(block.BlockType) : null,
            ["text"] = block is not null ? ToolResultText.Truncate(ExtractText(block) ?? string.Empty, PageSizes.MessagePreviewLength) : null,
        };

        if (block?.Children is { Length: > 0 })
        {
            obj["children"] = new JsonArray([.. block.Children.Select(static id => (JsonNode?)id)]);
        }

        return obj;
    }

    /// <summary>取块文本：首个非空文本类字段（text_run 拼接）。</summary>
    private static string? ExtractText(Block block)
    {
        var textBlock = FirstNonNull(
            block.Text, block.Page, block.Heading1, block.Heading2, block.Heading3, block.Heading4,
            block.Heading5, block.Heading6, block.Heading7, block.Heading8, block.Heading9,
            block.Bullet, block.Ordered, block.Code, block.Quote, block.Equation, block.Todo);
        if (textBlock is null)
        {
            return null;
        }

        var text = new StringBuilder();
        foreach (var element in textBlock.Elements ?? [])
        {
            var content = element?.TextRun?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                text.Append(content);
            }
        }

        return text.Length == 0 ? null : text.ToString();
    }

    private static BlockText? FirstNonNull(params BlockText?[] candidates)
        => candidates.FirstOrDefault(static c => c is not null);

    // ─────────────────────────── 块构造辅助 ───────────────────────────

    /// <summary>单次批量更新的块数上限（平台约束）。</summary>
    private const int MaxUpdatesPerRequest = 200;

    /// <summary>支持的块型名（小写，与平台/模型写法一致）。</summary>
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

    private static readonly string DividerBlockTypeName =
        BlockTypes.GetName(BlockTypes.Divider).ToLowerInvariant();

    /// <summary>按块型名称构造块（复用 DocxWriteTools 的同一套逻辑）。</summary>
    private static Block BuildBlockByName(string rawTypeName, string? text)
    {
        var typeName = rawTypeName.Trim().ToLowerInvariant();
        var matched = Array.Find(SupportedBlockTypeNames, candidate =>
            string.Equals(candidate, typeName, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
        {
            throw new ArgumentException(
                $"block_type '{rawTypeName}' 不支持。可用值：{string.Join(" / ", SupportedBlockTypeNames)}");
        }

        var block = new Block { BlockType = ResolveBlockType(matched) };

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

    // ─────────────────────────── JSON 解析辅助 ───────────────────────────

    /// <summary>解析 descendants JSON（块数组），复用 DocxWriteTools 的块解析逻辑。</summary>
    private static List<Block> ParseBlocksJson(string json, string paramName)
    {
        List<Block> blocks;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    $"{paramName} 必须是 JSON 数组，如 [{{\"block_type\":\"text\",\"text\":\"内容\",\"block_id\":\"temp-1\"}}]");
            }

            blocks = [.. document.RootElement.EnumerateArray().Select(ParseBlockItem)];
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"{paramName} 不是合法 JSON 数组：{ex.Message}", ex);
        }

        if (blocks.Count == 0)
        {
            throw new ArgumentException($"{paramName} 至少要有一个块");
        }

        return blocks;
    }

    private static Block ParseBlockItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                $"descendants 每一项都必须是对象（如 {{\"block_type\":\"text\",\"text\":\"…\",\"block_id\":\"temp-1\"}}），收到 {item.ValueKind}");
        }

        var typeName = item.TryGetProperty("block_type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String
                ? typeElement.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("descendants 每项都必须有字符串字段 block_type");
        }

        var text = item.TryGetProperty("text", out var textElement)
            && textElement.ValueKind == JsonValueKind.String
                ? textElement.GetString()
                : null;

        var block = BuildBlockByName(typeName!, text);

        // 可选的临时 block_id（供 children_id 引用）
        if (item.TryGetProperty("block_id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            block.BlockId = idElement.GetString();
        }

        return block;
    }

    /// <summary>解析字符串数组 JSON。</summary>
    private static List<string> ParseStringArrayJson(string json, string paramName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    $"{paramName} 必须是 JSON 数组，如 [\"temp-1\",\"temp-2\"]");
            }

            var result = new List<string>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    throw new ArgumentException($"{paramName} 每项都必须是字符串");
                }

                result.Add(item.GetString() ?? string.Empty);
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"{paramName} 不是合法 JSON 数组：{ex.Message}", ex);
        }
    }

    /// <summary>校验 children_id 引用完整性：每个 ID 必须在 descendants 中有对应块。</summary>
    private static void ValidateChildrenReferences(List<Block> descendants, List<string> childrenId)
    {
        if (childrenId.Count == 0)
        {
            throw new ArgumentException("children_id 至少要有一个元素");
        }

        var knownIds = new HashSet<string>(
            descendants
                .Where(static b => b.BlockId is not null)
                .Select(static b => b.BlockId!),
            StringComparer.Ordinal);

        foreach (var id in childrenId)
        {
            if (!knownIds.Contains(id))
            {
                throw new ArgumentException(
                    $"children_id 中的 '{id}' 在 descendants 中没有对应块（children_id 引用完整性校验失败）。"
                    + "请确保 children_id 中的每个临时 ID 都在 descendants 的 block_id 字段中出现过。");
            }
        }
    }

    /// <summary>解析 blocks（JSON 数组）为批量更新请求（复用 DocxWriteTools 的逻辑）。</summary>
    private static BatchUpdateBlockRequest[] ParseUpdateRequests(string json, int maxItems)
    {
        List<BatchUpdateBlockRequest> requests;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    "blocks 必须是 JSON 数组，如 [{\"block_id\":\"doxcnXxx\",\"text\":\"新文本\"}]");
            }

            requests = [.. document.RootElement.EnumerateArray().Select(ParseUpdateItem)];
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"blocks 不是合法 JSON 数组：{ex.Message}", ex);
        }

        if (requests.Count == 0)
        {
            throw new ArgumentException("blocks 至少要有一个待更新块");
        }

        if (requests.Count > maxItems)
        {
            throw new ArgumentException($"单次最多更新 {maxItems} 个块，收到 {requests.Count} 个");
        }

        return [.. requests];
    }

    private static BatchUpdateBlockRequest ParseUpdateItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException($"blocks 每一项都必须是对象，收到 {item.ValueKind}");
        }

        var blockId = item.TryGetProperty("block_id", out var idElement) && idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(blockId))
        {
            throw new ArgumentException("blocks 每项都必须有字符串字段 block_id");
        }

        if (!item.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"blocks 项 '{blockId}' 缺少字符串字段 text");
        }

        return new BatchUpdateBlockRequest
        {
            BlockId = blockId!,
            UpdateTextElements = new UpdateTextElementsRequest
            {
                Elements =
                [
                    new TextElement
                    {
                        TextRun = new TextElementTextRun { Content = textElement.GetString() ?? string.Empty },
                    },
                ],
            },
        };
    }
}
