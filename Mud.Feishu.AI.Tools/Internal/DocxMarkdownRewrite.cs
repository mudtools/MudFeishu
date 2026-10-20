// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 「Markdown → 整篇文档替换」的唯一实现：转换 → 追加新块 → 删除旧块。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须先建后删</b>：先删会打开一个破坏窗口——删除成功而创建失败即文档被清空，
/// 且补偿需要预先完整快照（SDK 无事务）。先建后删的三种失败点都不丢内容：
/// 转换失败文档未动；追加失败旧内容完整；删除失败则新旧并存（可见、可恢复）。
/// </para>
/// <para>
/// <b>为什么抽成共享实现</b>：<c>docx.replace_document</c> 与 <c>markdown.patch</c>/
/// <c>markdown.overwrite</c> 是同一套三步语义。复制一份会让"先建后删"的锁序在两条路径上漂移，
/// 而这类漂移只会在真实故障时才暴露。
/// </para>
/// </remarks>
internal static class DocxMarkdownRewrite
{
    /// <summary>子块单页条数（平台上限）。</summary>
    private const int ChildrenPageSize = 500;

    /// <summary>翻页上限（1 万块）；超出即拒绝整篇替换，避免异常循环。</summary>
    private const int MaxChildrenPages = 20;

    /// <summary>
    /// 用 Markdown 重建文档正文，返回与 <c>docx.replace_document</c> 既有口径一致的结果信封。
    /// </summary>
    /// <param name="blocksClient">Docx 块客户端。</param>
    /// <param name="documentId">文档 ID。</param>
    /// <param name="parentBlockId">父块 ID（文档根块 = documentId）。</param>
    /// <param name="markdown">新正文（调用方已校验非空）。</param>
    /// <param name="idempotencyKey">幂等键（调用方已校验非空）。</param>
    /// <param name="toolName">工具名（失败信封里的 <c>tool</c> 字段）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>
    /// 信封字段：<c>appended</c> / <c>new_block_ids</c> / <c>deleted</c>；
    /// 失败时为 <c>partial_failure=true</c> + <c>step</c> + <c>message</c>（内容未丢失）。
    /// </returns>
    internal static async Task<JsonObject> ReplaceAsync(
        Mud.Feishu.IFeishuTenantV1DocxBlocks blocksClient,
        string documentId,
        string parentBlockId,
        string markdown,
        string idempotencyKey,
        string toolName,
        CancellationToken cancellationToken)
    {
        // ① 转换（只读；失败 ⇒ 文档完全未被触碰）。
        var convertOutcome = FeishuApiResultReader.Read(await blocksClient
            .ContentConvertAsync(
                new ConvertContentRequest { ContentType = "markdown", Content = markdown },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false));

        if (!convertOutcome.Ok || convertOutcome.Data is null)
        {
            return Untouched(toolName, "Markdown 转换", convertOutcome.ErrorText);
        }

        var newBlocks = convertOutcome.Data.Blocks ?? [];
        if (newBlocks.Length == 0)
        {
            // 拒绝执行：若放行，下一步会删光旧内容却无新内容可写。
            throw new ArgumentException(
                "markdown 未产生任何块，已拒绝执行替换（否则会清空文档而无新内容）。请检查输入是否为有效 Markdown。");
        }

        // ② 读取现有子块数（旧内容区间上界）。
        var countOutcome = await CountChildrenAsync(blocksClient, documentId, parentBlockId, cancellationToken).ConfigureAwait(false);
        if (!countOutcome.Ok)
        {
            return Untouched(toolName, "读取现有子块", countOutcome.ErrorText);
        }

        var oldCount = countOutcome.Count;

        // ③ 先追加新块（失败 ⇒ 旧内容完整，无损失）。
        var appendOutcome = FeishuApiResultReader.Read(await blocksClient
            .CreateBlockAsync(
                documentId,
                parentBlockId,
                new CreateBlockRequest { Childrens = newBlocks },
                client_token: idempotencyKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false));

        if (!appendOutcome.Ok || appendOutcome.Data is null)
        {
            return Untouched(toolName, "追加新块", appendOutcome.ErrorText);
        }

        var newBlockIds = new JsonArray(
            [.. (appendOutcome.Data.Childrens ?? []).Select(static child => (JsonNode?)child?.BlockId)]);

        var envelope = new JsonObject
        {
            ["appended"] = newBlocks.Length,
            ["new_block_ids"] = newBlockIds,
        };

        // 空文档没有"旧块"可删，跳过删除。
        if (oldCount == 0)
        {
            envelope["deleted"] = 0;
            envelope["note"] = "原文档无子块，仅追加";
            return envelope;
        }

        // ④ 再删除旧块 [0, oldCount)。失败时新旧并存——如实上报，不做二次破坏性操作。
        var deleteOutcome = FeishuApiResultReader.Read(await blocksClient
            .BatchDeleteBlocksAsync(
                documentId,
                parentBlockId,
                new BatchDeleteBlocksRequest { StartIndex = 0, EndIndex = oldCount },
                client_token: idempotencyKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false));

        if (!deleteOutcome.Ok)
        {
            envelope["deleted"] = 0;
            envelope["partial_failure"] = true;
            envelope["message"] =
                $"新内容已写入（{newBlocks.Length} 块），但旧内容删除失败：{deleteOutcome.ErrorText}。"
                + "文档现为**新旧内容并存**，内容未丢失。修复：调用 docx.delete_blocks，"
                + $"document_id={documentId}，start_index=0，end_index={oldCount}（建议先 dry_run 确认区间）。";
            return envelope;
        }

        envelope["deleted"] = oldCount;
        return envelope;
    }

    /// <summary>统计某父块下的子块总数（翻页累加；上限 1 万块）。</summary>
    private static async Task<(bool Ok, int Count, string? ErrorText)> CountChildrenAsync(
        Mud.Feishu.IFeishuTenantV1DocxBlocks blocksClient,
        string documentId,
        string parentBlockId,
        CancellationToken cancellationToken)
    {
        var total = 0;
        string? pageToken = null;

        for (var page = 0; page < MaxChildrenPages; page++)
        {
            var outcome = FeishuApiResultReader.Read(await blocksClient
                .GetChildrenBlocksPageListAsync(
                    documentId,
                    parentBlockId,
                    page_size: ChildrenPageSize,
                    page_token: pageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            if (!outcome.Ok || outcome.Data is null)
            {
                // 返回元组而非 ToolApiOutcome<int>：后者约束 T : class，int 不满足。
                return (false, 0, outcome.ErrorText ?? "读取子块列表返回空数据");
            }

            total += outcome.Data.Items?.Count ?? 0;

            if (!outcome.Data.HasMore || string.IsNullOrEmpty(outcome.Data.PageToken))
            {
                return (true, total, null);
            }

            pageToken = outcome.Data.PageToken;
        }

        throw new InvalidOperationException(
            $"文档子块超过 {MaxChildrenPages * ChildrenPageSize} 块，超出本工具安全范围；"
            + "请改用 docx.delete_blocks 分段处理，避免对超大文档执行整体替换");
    }

    /// <summary>构造"文档未被修改"的中断结果（模型据此可安全重试）。</summary>
    private static JsonObject Untouched(string toolName, string step, string? errorText)
        => new()
        {
            ["partial_failure"] = true,
            ["step"] = step,
            ["tool"] = toolName,
            ["message"] =
                $"{step}失败：{errorText}。文档**未被修改**（旧内容完整、新内容未写入），可安全重试。",
        };
}
