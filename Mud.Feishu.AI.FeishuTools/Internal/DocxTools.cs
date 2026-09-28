// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Docx 工具执行器（<c>docx.get_raw_content</c> / <c>docx.get_document_blocks</c>）：
/// 正文纯文本不做字段投影仅截断；分块读取按白名单 block_id/block_type/text 投影（§3.3.3 +
/// AI-FD-D12 P1D-1b）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class DocxTools(Mud.Feishu.IFeishuTenantV1Docx docxClient, IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1Docx _docxClient = docxClient
        ?? throw new ArgumentNullException(nameof(docxClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>docx.get_raw_content：读取文档纯文本正文。</summary>
    public Task<FeishuToolResult> GetRawContentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxGetRawContent, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var documentId = ToolArgs.RequireString(arguments, "document_id");
            var lang = ToolArgs.OptionalString(arguments, "lang");

            var outcome = FeishuApiResultReader.Read(await _docxClient
                .GetDocumentRawContentAsync(
                    documentId,
                    ParseLang(lang),
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromPlainText(outcome, static data => data.Content);
        });
    }

    /// <summary>docx.get_document_blocks：分块读取文档（白名单 block_id/block_type/text；text 取首个非空文本块字段）。</summary>
    public Task<FeishuToolResult> GetDocumentBlocksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DocxGetDocumentBlocks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var documentId = ToolArgs.RequireString(arguments, "document_id");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var outcome = FeishuApiResultReader.Read(await _docxClient
                .GetDocumentBlocksPageListAsync(documentId, page_size: PageSizes.DocxBlocks, page_token: pageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectBlocks);
        });
    }

    /// <summary>get_document_blocks 投影：items（block_id/block_type/text 预览）+ 翻页契约。</summary>
    private static JsonObject ProjectBlocks(ApiPageListResult<Block> data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var block in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["block_id"] = block.BlockId,
                ["block_type"] = block.BlockType,
                ["text"] = ToolResultText.Truncate(ExtractText(block) ?? string.Empty, PageSizes.MessagePreviewLength),
            });
        }

        return envelope;
    }

    /// <summary>取块文本：首个非空文本类字段（页面/正文/标题/列表/代码/引用/公式/待办）的 text_run 拼接。</summary>
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
            // netstandard2.0 的 BCL 缺少流转注解，取局部值显式判空。
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

    private static int? ParseLang(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return 0; // 默认中文（§3.3.3：lang 默认 0）。
        }

        if (!int.TryParse(lang, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException($"lang 需为整数（0=中文），实际: {lang}");
        }

        return value;
    }
}
