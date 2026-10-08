// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// OKR 写入工具执行器（R6 / S2）：<c>okr.create_objective</c> / <c>okr.update_objective</c> /
/// <c>okr.delete_objective</c> / <c>okr.create_key_result</c> / <c>okr.update_key_result</c> /
/// <c>okr.delete_key_result</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>富文本入参策展（与只读侧对称）</b>：飞书 v2 的 <c>content</c> 是块树。让模型直接产出块树
/// 是本域最容易被误用之处（模型会写出半成品 JSON），故工具层只收<b>纯文本</b>，
/// 由 <see cref="OkrContent.ToRichText"/> 包装为单段单 run 的合法块树。
/// </para>
/// <para>
/// <b>dry-run 纪律（与全部既有写工具一致）</b>：<c>dry_run=true</c> 时<b>不调用下游</b>，
/// 只回字段名 + 长度摘要与 method/path 模板字面量；<b>不回原文</b>（防把用户正文回灌进模型上下文）。
/// </para>
/// <para>
/// <b>score / weight 的取值校验</b>：以字符串入参（引擎参数解包映射表不覆盖 <c>double</c>，
/// 见 Curation 的 remarks），在执行器内解析并强制 [0,1] 区间——把"模型给了 5 分"这类
/// 平台会静默接受或语焉不详报错的输入，变成一次可读的本地拒绝。
/// </para>
/// </remarks>
internal sealed class OkrWriteTools(
    Mud.Feishu.IFeishuTenantV2OkrCycle? okrCycleClient,
    Mud.Feishu.IFeishuTenantV2OkrObjective? okrObjectiveClient,
    Mud.Feishu.IFeishuTenantV2OkrKeyResult? okrKeyResultClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2OkrCycle? _okrCycleClient = okrCycleClient;
    private readonly Mud.Feishu.IFeishuTenantV2OkrObjective? _okrObjectiveClient = okrObjectiveClient;
    private readonly Mud.Feishu.IFeishuTenantV2OkrKeyResult? _okrKeyResultClient = okrKeyResultClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>okr.create_objective：在周期下创建目标（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrCreateObjectiveTool))]
    public Task<FeishuToolResult> CreateObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrCreateObjective, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrCycleClient
                ?? throw new ArgumentException("okr.create_objective 需要 IFeishuTenantV2OkrCycle——宿主须启用 OKR 域客户端");
            var args = OkrCreateObjectiveArgs.Unpack(arguments);
            var score = ParseRatio(args.Score, "score");
            var weight = ParseRatio(args.Weight, "weight");

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/okr/v2/cycles/{cycle_id}/objectives"));
            }

            var request = new CreateCycleObjectiveRequest
            {
                Content = OkrContent.ToRichText(args.Content),
                Notes = args.Notes is null ? null : OkrContent.ToRichText(args.Notes),
                Deadline = args.Deadline,
                Score = score,
                Weight = weight,
                CategoryId = args.CategoryId,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .CreateCycleObjectiveAsync(args.CycleId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["objective_id"] = data.ObjectiveId,
            });
        });
    }

    /// <summary>okr.update_objective：修改目标（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrUpdateObjectiveTool))]
    public Task<FeishuToolResult> UpdateObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrUpdateObjective, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrObjectiveClient
                ?? throw new ArgumentException("okr.update_objective 需要 IFeishuTenantV2OkrObjective——宿主须启用 OKR 域客户端");
            var args = OkrUpdateObjectiveArgs.Unpack(arguments);
            var score = ParseRatio(args.Score, "score");

            if (args.Content is null && args.Notes is null && args.Deadline is null && score is null && args.CategoryId is null)
            {
                throw new ArgumentException("至少需要提供 content / notes / deadline / score / category_id 中的一个字段");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/okr/v2/objectives/{objective_id}"));
            }

            var request = new PatchObjectiveRequest();
            if (args.Content is not null)
            {
                request.Content = OkrContent.ToRichText(args.Content);
            }

            if (args.Notes is not null)
            {
                request.Notes = OkrContent.ToRichText(args.Notes);
            }

            request.Deadline = args.Deadline;
            request.Score = score;
            request.CategoryId = args.CategoryId;

            var outcome = FeishuApiResultReader.Read(await client
                .UpdateObjectiveAsync(args.ObjectiveId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["objective_id"] = data.Objective?.Id ?? args.ObjectiveId,
            });
        });
    }

    /// <summary>okr.delete_objective：删除目标及其关键结果（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrDeleteObjectiveTool))]
    public Task<FeishuToolResult> DeleteObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrDeleteObjective);
        return executor.RunAsync(async () =>
        {
            var client = _okrObjectiveClient
                ?? throw new ArgumentException("okr.delete_objective 需要 IFeishuTenantV2OkrObjective——宿主须启用 OKR 域客户端");
            var args = OkrDeleteObjectiveArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/okr/v2/objectives/{objective_id}"));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .DeleteObjectiveAsync(args.ObjectiveId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["deleted"] = true,
                ["objective_id"] = data.ObjectiveId ?? args.ObjectiveId,
            });
        });
    }

    /// <summary>okr.create_key_result：在目标下创建关键结果（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrCreateKeyResultTool))]
    public Task<FeishuToolResult> CreateKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrCreateKeyResult, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrKeyResultClient
                ?? throw new ArgumentException("okr.create_key_result 需要 IFeishuTenantV2OkrKeyResult——宿主须启用 OKR 域客户端");
            var args = OkrCreateKeyResultArgs.Unpack(arguments);
            var score = ParseRatio(args.Score, "score");

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/okr/v2/objectives/{objective_id}/key_results"));
            }

            var request = new CreateKeyResultRequest
            {
                Content = OkrContent.ToRichText(args.Content),
                Deadline = args.Deadline,
                Score = score,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .CreateObjectiveKeyResultAsync(args.ObjectiveId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["key_result_id"] = data.KeyResultId,
            });
        });
    }

    /// <summary>okr.update_key_result：修改关键结果（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrUpdateKeyResultTool))]
    public Task<FeishuToolResult> UpdateKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrUpdateKeyResult, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrKeyResultClient
                ?? throw new ArgumentException("okr.update_key_result 需要 IFeishuTenantV2OkrKeyResult——宿主须启用 OKR 域客户端");
            var args = OkrUpdateKeyResultArgs.Unpack(arguments);
            var score = ParseRatio(args.Score, "score");

            if (args.Content is null && args.Deadline is null && score is null)
            {
                throw new ArgumentException("至少需要提供 content / deadline / score 中的一个字段");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/okr/v2/key_results/{key_result_id}"));
            }

            var request = new PatchKeyResultRequest { Deadline = args.Deadline, Score = score };
            if (args.Content is not null)
            {
                request.Content = OkrContent.ToRichText(args.Content);
            }

            var outcome = FeishuApiResultReader.Read(await client
                .UpdateKeyResultAsync(args.KeyResultId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["key_result_id"] = data.KeyResult?.Id ?? args.KeyResultId,
            });
        });
    }

    /// <summary>okr.delete_key_result：删除关键结果（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrDeleteKeyResultTool))]
    public Task<FeishuToolResult> DeleteKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrDeleteKeyResult);
        return executor.RunAsync(async () =>
        {
            var client = _okrKeyResultClient
                ?? throw new ArgumentException("okr.delete_key_result 需要 IFeishuTenantV2OkrKeyResult——宿主须启用 OKR 域客户端");
            var args = OkrDeleteKeyResultArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/okr/v2/key_results/{key_result_id}"));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .DeleteKeyResultAsync(args.KeyResultId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["deleted"] = true,
                ["key_result_id"] = data.KeyResultId ?? args.KeyResultId,
            });
        });
    }

    /// <summary>解析 [0,1] 区间的比例参数（缺失返回 null；非法值抛可读错误）。</summary>
    private static double? ParseRatio(string? raw, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || value < 0d || value > 1d)
        {
            throw new ArgumentException($"{parameterName} 需为 0~1 之间的数值（保留小数，如 \"0.5\"），实际: {raw}");
        }

        return value;
    }
}

/// <summary>OKR v2 富文本块树的最小构造器（纯文本 → 单段单 run 的合法块树）。</summary>
internal static class OkrContent
{
    /// <summary>块元素类型：段落。</summary>
    private const string ParagraphBlockElement = "paragraph";

    /// <summary>段落元素类型：文本 run。</summary>
    private const string TextRunParagraphElement = "text_run";

    /// <summary>把纯文本包装为飞书 OKR v2 富文本块树。</summary>
    public static ContentBlockV2 ToRichText(string text)
        => new()
        {
            Blocks =
            [
                new ContentBlockElementV2
                {
                    BlockElementType = ParagraphBlockElement,
                    Paragraph = new ContentParagraphV2
                    {
                        Elements =
                        [
                            new ContentParagraphElementV2
                            {
                                ParagraphElementType = TextRunParagraphElement,
                                TextRun = new ContentTextRunV2 { Text = text },
                            },
                        ],
                    },
                },
            ],
        };
}
