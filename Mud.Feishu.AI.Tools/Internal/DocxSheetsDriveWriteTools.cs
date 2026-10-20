// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Docx;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Folder;
using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.AI.Tools.Internal;

// ─────────────────────────── Docx 写执行器 ───────────────────────────

/// <summary>
/// Docx 写工具执行器（<c>docx.create_document</c> / <c>docx.append_blocks</c>，WP2/R5）。
/// </summary>
internal sealed class DocxWriteTools(
    Mud.Feishu.IFeishuTenantV1Docx docxClient,
    Mud.Feishu.IFeishuTenantV1DocxBlocks blocksClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1Docx _docxClient = docxClient
        ?? throw new ArgumentNullException(nameof(docxClient));
    private readonly Mud.Feishu.IFeishuTenantV1DocxBlocks _blocksClient = blocksClient
        ?? throw new ArgumentNullException(nameof(blocksClient));

    /// <summary>
    /// 结果截断预算（与读侧执行器同源）。
    /// </summary>
    /// <remarks>
    /// <b>为什么本类必须有它</b>（S-16）：单参 <c>ToolExecutor(...)</c> 把预算记作 <b>0</b>，
    /// 而 <c>FromApi</c> 在预算为 0 时会把结果截到 <b>1 个字符</b>。本类原先因此改用
    /// <c>FromApiUntruncated</c> 绕过 —— 但 <c>docx.import_markdown</c> 的结果体积由输入
    /// markdown 决定（可含数百个块 + 图片映射），<b>不截断就等于把上下文交给调用方</b>，
    /// 与它自己声明的"超长截断并标记 truncated"契约相矛盾。给出真实预算后，
    /// 正常载荷不受影响（<c>TruncateJson</c> 在预算内原样返回），超长载荷才被约束。
    /// </remarks>
    private readonly int _maxResultLength =
        ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>docx.create_document：创建文档（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxCreateDocumentTool))]
    public Task<ToolResult> CreateDocumentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxCreateDocument, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxCreateDocumentArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/docx/v1/documents",
                    ToolDryRun.IdempotencyNote(null),
                    ("folder_token", args.FolderToken?.Length ?? 0), ("title", args.Title?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _docxClient
                .CreateDocumentAsync(
                    new CreateDocumentRequest { FolderToken = args.FolderToken, Title = args.Title },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["document_id"] = data.Document?.DocumentId,
            });
        });
    }

    /// <summary>docx.append_blocks：在文档根块下创建子块（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（client_token）：相同 client_token 在 24 小时内至多成功创建一次。</remarks>
    [ToolHandler(typeof(IFeishuTenantDocxAppendBlocksTool))]
    public Task<ToolResult> AppendBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxAppendBlocks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxAppendBlocksArgs.Unpack(arguments);
            var blocks = BuildBlocks(args);
            var parentBlockId = args.ParentBlockId ?? args.DocumentId;

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
            return executor.FromApi(outcome, data => new JsonObject
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

    // ────────── R5 / F-5（续）：update / delete / import_markdown ──────────

    /// <summary>单次批量更新的块数上限（平台约束）。</summary>
    private const int MaxUpdatesPerRequest = 200;

    /// <summary>im/docx 的根块 ID 约定：文档根块的 block_id == document_id。</summary>
    private static string ResolveParentBlockId(string? modelSupplied, string documentId)
        => string.IsNullOrWhiteSpace(modelSupplied) ? documentId : modelSupplied!;

    /// <summary>docx.update_blocks：批量替换块的文本元素（<c>update_text_elements</c> 面）。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxUpdateBlocksTool))]
    public Task<ToolResult> UpdateBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxUpdateBlocks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxUpdateBlocksArgs.Unpack(arguments);
            var requests = ParseUpdateRequests(args.Blocks, MaxUpdatesPerRequest);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/docx/v1/documents/{args.DocumentId}/blocks/batch_update",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_id", args.DocumentId.Length), ("blocks", requests.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .BatchUpdateBlocksAsync(
                    args.DocumentId,
                    new BatchUpdateBlocksRequest { Requests = requests },
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, _ => new JsonObject { ["updated"] = requests.Length });
        });
    }

    /// <summary>docx.delete_blocks：删除父块下 [start_index, end_index) 的子块。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxDeleteBlocksTool))]
    public Task<ToolResult> DeleteBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxDeleteBlocks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxDeleteBlocksArgs.Unpack(arguments);
            var parentBlockId = ResolveParentBlockId(args.ParentBlockId, args.DocumentId);
            var count = ValidateRange(args.StartIndex, args.EndIndex);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE",
                    $"/open-apis/docx/v1/documents/{args.DocumentId}/blocks/{parentBlockId}/children/batch_delete",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_id", args.DocumentId.Length), ($"range [{args.StartIndex},{args.EndIndex})", count)));
            }

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .BatchDeleteBlocksAsync(
                    args.DocumentId,
                    parentBlockId,
                    new BatchDeleteBlocksRequest { StartIndex = args.StartIndex, EndIndex = args.EndIndex },
                    client_token: args.IdempotencyKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, _ => new JsonObject { ["deleted"] = count });
        });
    }

    /// <summary>docx.import_markdown：Markdown → 文档块（只转换、不写入）。</summary>
    [ToolHandler(typeof(IFeishuTenantDocxImportMarkdownTool))]
    public Task<ToolResult> ImportMarkdownAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        // 本类已持有 _maxResultLength（见字段注释）：用**有预算**的截断出口。
        // 转换结果体积由输入 markdown 决定，不设上限等于把上下文交给调用方。
        var executor = new ToolExecutor(FeishuToolNames.DocxImportMarkdown, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxImportMarkdownArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.Markdown))
            {
                throw new ArgumentException("markdown 不能为空");
            }

            var outcome = FeishuApiResultReader.Read(await _blocksClient
                .ContentConvertAsync(
                    new ConvertContentRequest { ContentType = "markdown", Content = args.Markdown! },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectContentConvert);
        });
    }

    /// <summary>
    /// docx.replace_document：整篇替换（<b>先追加新块、再删除旧块</b>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么顺序是"先建后删"而不是原文方案的"先删后建"</b>：先删会打开一个
    /// <b>破坏窗口</b>——删除成功而创建失败 ⇒ 文档被清空，且补偿需要预先完整快照
    /// （SDK 无事务，快照只能自己读出来再回放，可靠性远低于"不删就不会丢"）。
    /// 先建后删的三种失败点都<b>不丢内容</b>：转换失败文档未动；追加失败旧内容完整；
    /// 删除失败则新旧并存（可见、可恢复，并如实上报待删区间）。
    /// </para>
    /// <para>
    /// <b>删除失败为何不回滚新块</b>：回滚（删掉刚追加的新块）会把状态从"新旧并存"
    /// 变成"旧内容完整"——看似更干净，但若回滚本身也失败，就会留下<b>部分旧 + 部分新</b>
    /// 的更难诊断的状态。既然"新旧并存"无损且给出精确修复指引，就<b>不做二次破坏性操作</b>。
    /// </para>
    /// </remarks>
    [ToolHandler(typeof(IFeishuTenantDocxReplaceDocumentTool))]
    public Task<ToolResult> ReplaceDocumentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxReplaceDocument, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DocxReplaceDocumentArgs.Unpack(arguments);
            var parentBlockId = ResolveParentBlockId(args.ParentBlockId, args.DocumentId);

            // 幂等键必填：缺省时"追加成功但删除失败"的重试会重复插入并重复删除。
            if (string.IsNullOrWhiteSpace(args.IdempotencyKey))
            {
                throw new ArgumentException(
                    "replace_document 必须提供 idempotency_key：该工具是两步写（追加新块 + 删除旧块），"
                    + "重试若无稳定 client_token 会重复插入并重复删除。请传入一个稳定值（如业务单号），不要用随机数。");
            }

            if (string.IsNullOrWhiteSpace(args.Markdown))
            {
                throw new ArgumentException("markdown 不能为空");
            }

            var baseRoute = $"/open-apis/docx/v1/documents/{args.DocumentId}/blocks";

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST+DELETE", $"{baseRoute}/convert → {baseRoute}/{parentBlockId}/children → {baseRoute}/{parentBlockId}/children/batch_delete",
                    "步骤：① 转换 Markdown ② 追加新块 ③ 删除旧块 [0, 原块数)。"
                    + "原块数在**执行时**读取（dry_run 不发起任何调用）。"
                    + ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_id", args.DocumentId.Length), ("markdown", args.Markdown!.Length)));
            }

            // 三步语义（转换 → 先追加 → 后删除旧块）由 DocxMarkdownRewrite 单点实现：
            // docx.replace_document 与 markdown.patch / markdown.overwrite 必须共享同一锁序。
            var envelope = await DocxMarkdownRewrite.ReplaceAsync(
                _blocksClient,
                args.DocumentId,
                parentBlockId,
                args.Markdown!,
                args.IdempotencyKey,
                executor.ToolName,
                cancellationToken).ConfigureAwait(false);
            return FromEnvelope(envelope);
        });
    }

    /// <summary>
    /// 按预算截断后返回手工信封（<c>replace_document</c> 的 3 条返回路径都走它）。
    /// </summary>
    /// <remarks>
    /// 该工具的成功路径需要**手工拼装**信封（追加数 / 新块 ID / 部分失败指引），
    /// 无法复用 <c>FromApi</c> 的投影形参；但**不截断**会让 <c>new_block_ids</c>
    /// 这类数组随文档规模膨胀。故手工路径也必须过同一道预算。
    /// </remarks>
    private ToolResult FromEnvelope(JsonObject envelope)
        => ToolResultPipeline.OkJson(envelope, _maxResultLength);

    /// <summary>校验删除区间；返回待删条数。<b>非法区间必须提前拒绝</b>（否则平台会按意外区间删除）。</summary>
    private static int ValidateRange(int startIndex, int endIndex)
    {
        if (startIndex < 0)
        {
            throw new ArgumentException($"start_index 不能为负，收到 {startIndex}");
        }

        if (endIndex <= startIndex)
        {
            throw new ArgumentException(
                $"end_index 必须大于 start_index（区间为 [start, end) 半开），收到 [{startIndex}, {endIndex})");
        }

        return endIndex - startIndex;
    }

    /// <summary>
    /// 解析 <c>blocks</c>（JSON 数组）为批量更新请求，逐项校验。
    /// </summary>
    /// <remarks>
    /// 每项形如 <c>{"block_id":"…","text":"…"}</c>；映射到 SDK 的
    /// <c>UpdateBlockRequest.UpdateTextElements.Elements</c>（单个文本元素 = 整块替换文本）。
    /// </remarks>
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
                $"blocks 不是合法 JSON 数组：{ex.Message}。示例：[{{\"block_id\":\"doxcnXxx\",\"text\":\"新文本\"}}]", ex);
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

        // 批量接口用的是 BatchUpdateBlockRequest（= UpdateBlockRequest + block_id）。
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

    /// <summary>投影 <c>ContentConvertResult</c>：块的 <c>block_type</c> 用 <see cref="BlockTypes"/> 转成可读名。</summary>
    private static JsonObject ProjectContentConvert(ContentConvertResult data)
    {
        var envelope = new JsonObject
        {
            ["first_level_block_ids"] = new JsonArray(
                [.. (data.FirstLevelBlockIds ?? []).Select(static id => (JsonNode?)id)]),
            ["blocks"] = new JsonArray([.. (data.Blocks ?? []).Select(static block => (JsonNode?)ProjectConvertedBlock(block))]),
        };

        // 图片占位块 → 真实 URL 的映射：模型据此把图片写进文档。
        var images = new JsonObject();
        foreach (var entry in data.BlockIdToImageUrls ?? [])
        {
            images[entry.BlockId] = entry.ImageUrl;
        }

        envelope["image_urls"] = images;
        return envelope;
    }

    /// <summary>投影单个转换块：只回填模型写入所需的最小字段（block_type 名 + 文本摘要）。</summary>
    private static JsonNode? ProjectConvertedBlock(Block block)
    {
        if (block is null)
        {
            return null;
        }

        var text = block.Text?.Elements is null
            ? string.Empty
            : string.Concat(block.Text.Elements.Select(static e => e?.TextRun?.Content));

        return new JsonObject
        {
            ["block_type"] = BlockTypes.GetName(block.BlockType),

            // ⚠️ 无文本块（分隔线 / 图片 / 表格）的 Text 为 null，必须回 null 而不是把 null
            //    交给 Truncate —— 后者会抛 NullReferenceException，让**任何含分隔线或图片的
            //    Markdown 都无法转换**。这类块在转换结果里很常见，属于必现路径而非边角。
            ["text"] = string.IsNullOrEmpty(text)
                ? null
                : ToolResultText.Truncate(text, PageSizes.MessagePreviewLength),
        };
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
    [ToolHandler(typeof(IFeishuTenantSheetsUpdateRangeTool))]
    public Task<ToolResult> UpdateRangeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsUpdateRange);
        return executor.RunAsync(async () =>
        {
            var args = SheetsUpdateRangeArgs.Unpack(arguments);

            // 解析 values JSON 为二维数组
            var values = ParseValuesArray(args.Values);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantSheetsAppendRowsTool))]
    public Task<ToolResult> AppendRowsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsAppendRows);
        return executor.RunAsync(async () =>
        {
            var args = SheetsAppendRowsArgs.Unpack(arguments);

            var values = ParseValuesArray(args.Values);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantDriveCreateFolderTool))]
    public Task<ToolResult> CreateFolderAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveCreateFolder);
        return executor.RunAsync(async () =>
        {
            var args = DriveCreateFolderArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantDriveMoveFileTool))]
    public Task<ToolResult> MoveFileAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveMoveFile);
        return executor.RunAsync(async () =>
        {
            var args = DriveMoveFileArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantDriveUploadFileTool))]
    public Task<ToolResult> UploadFileAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveUploadFile);
        return executor.RunAsync(async () =>
        {
            var args = DriveUploadFileArgs.Unpack(arguments);
            var fileUrl = AttachmentTools.RequireHttpUrl(args.FileUrl, "file_url");

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/files/upload_all",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_url", fileUrl.Length), ("file_name", args.FileName.Length), ("folder_token", args.FolderToken?.Length ?? 0)));
            }

            if (_stager is null)
            {
                return ToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, "宿主未注册 IFeishuAttachmentStager，drive.upload_file 不可用"));
            }

            var staged = await _stager.StageAsync(new AttachmentSource(Url: fileUrl, Content: null, FileName: args.FileName), cancellationToken);
            if (staged is null)
            {
                return ToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, "来源不被宿主允许（安全域拒绝）"));
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

    // R5 / F-10 本轮未落地：B-2 的零容忍守卫规定 Source 不得指向返回二进制的 SDK 方法，
    // 而受控下载出口必须调用 DownloadFileAsync（返回二进制数组）。
    // stager 已作为软依赖挂在本类上，无需扩展契约 —— 缺的只是与 B-2 和解的表达方式。
    // 详见 Curation/Docs/FeishuDriveWriteToolInterfaces.cs 末尾的回滚说明与三条候选出路
    // （D-1 拆分后，原 FeishuDocxSheetsDriveToolInterfaces.cs 的尾部说明随 Drive 写面归位）。
}
