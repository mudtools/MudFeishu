// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Okr;
using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// OKR 只读工具执行器（R6 / S2）：<c>okr.list_cycles</c> / <c>okr.list_objectives</c> /
/// <c>okr.get_objective</c> / <c>okr.list_key_results</c> / <c>okr.get_key_result</c> /
/// <c>okr.list_objective_progresses</c> / <c>okr.list_key_result_progresses</c> /
/// <c>okr.list_periods</c> / <c>okr.list_categories</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>富文本投影（本域的核心策展点）</b>：OKR v2 的 <c>content</c>/<c>notes</c> 是
/// <c>ContentBlockV2</c> 块树（paragraph → elements → text_run/docs_link/mention）。
/// 原样回填会把整棵块树（含样式）灌进模型上下文，既昂贵又无信息量。
/// 故按<b>纯文本</b>投影：text_run 取 <c>text</c>、mention 记为 <c>@user_id</c>、
/// docs_link 记为 <c>title(url)</c>、gallery 记为占位标记——模型读到的是"这句话是什么"，
/// 而不是"这句话的第 3 个 run 用了什么字号"。
/// </para>
/// <para>
/// <b>软缺席（执行链护栏）</b>：六个客户端全部<b>可空</b>，缺席时对应工具返回结构化错误而非
/// 让整个域从注册表消失（对齐 <c>MailTools</c> 的软依赖纪律）。
/// </para>
/// </remarks>
internal sealed class OkrTools(
    Mud.Feishu.IFeishuTenantV2OkrCycle? okrCycleClient,
    Mud.Feishu.IFeishuTenantV2OkrObjective? okrObjectiveClient,
    Mud.Feishu.IFeishuTenantV2OkrKeyResult? okrKeyResultClient,
    Mud.Feishu.IFeishuTenantV2OkrProgress? okrProgressClient,
    Mud.Feishu.IFeishuTenantV1OkrPeriod? okrPeriodClient,
    Mud.Feishu.IFeishuTenantV2OkrCategory? okrCategoryClient,
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>默认每页条数（官方上限 100；取 20 控量，模型可用 page_token 翻页）。</summary>
    private const int DefaultPageSize = 20;

    private readonly Mud.Feishu.IFeishuTenantV2OkrCycle? _okrCycleClient = okrCycleClient;
    private readonly Mud.Feishu.IFeishuTenantV2OkrObjective? _okrObjectiveClient = okrObjectiveClient;
    private readonly Mud.Feishu.IFeishuTenantV2OkrKeyResult? _okrKeyResultClient = okrKeyResultClient;
    private readonly Mud.Feishu.IFeishuTenantV2OkrProgress? _okrProgressClient = okrProgressClient;
    private readonly Mud.Feishu.IFeishuTenantV1OkrPeriod? _okrPeriodClient = okrPeriodClient;
    private readonly Mud.Feishu.IFeishuTenantV2OkrCategory? _okrCategoryClient = okrCategoryClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>okr.list_cycles：列出用户的 OKR 周期。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListCyclesTool))]
    public Task<FeishuToolResult> ListCyclesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListCycles, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrCycleClient
                ?? throw new ArgumentException("okr.list_cycles 需要 IFeishuTenantV2OkrCycle——宿主须启用 OKR 域客户端");
            var args = OkrListCyclesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .ListCyclesAsync(args.UserId, page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectCycles);
        });
    }

    /// <summary>okr.list_objectives：列出周期下的目标。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListObjectivesTool))]
    public Task<FeishuToolResult> ListObjectivesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListObjectives, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrCycleClient
                ?? throw new ArgumentException("okr.list_objectives 需要 IFeishuTenantV2OkrCycle——宿主须启用 OKR 域客户端");
            var args = OkrListObjectivesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .ListCycleObjectivesAsync(args.CycleId, page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectObjectives);
        });
    }

    /// <summary>okr.get_objective：获取目标详情。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrGetObjectiveTool))]
    public Task<FeishuToolResult> GetObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrGetObjective, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrObjectiveClient
                ?? throw new ArgumentException("okr.get_objective 需要 IFeishuTenantV2OkrObjective——宿主须启用 OKR 域客户端");
            var args = OkrGetObjectiveArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetObjectiveAsync(args.ObjectiveId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => ProjectObjective(data.Objective));
        });
    }

    /// <summary>okr.list_key_results：列出目标下的关键结果。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListKeyResultsTool))]
    public Task<FeishuToolResult> ListKeyResultsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListKeyResults, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrKeyResultClient
                ?? throw new ArgumentException("okr.list_key_results 需要 IFeishuTenantV2OkrKeyResult——宿主须启用 OKR 域客户端");
            var args = OkrListKeyResultsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .ListObjectiveKeyResultsAsync(args.ObjectiveId, page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectKeyResults);
        });
    }

    /// <summary>okr.get_key_result：获取关键结果详情。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrGetKeyResultTool))]
    public Task<FeishuToolResult> GetKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrGetKeyResult, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrKeyResultClient
                ?? throw new ArgumentException("okr.get_key_result 需要 IFeishuTenantV2OkrKeyResult——宿主须启用 OKR 域客户端");
            var args = OkrGetKeyResultArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetKeyResultAsync(args.KeyResultId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => ProjectKeyResult(data.KeyResult));
        });
    }

    /// <summary>okr.list_objective_progresses：列出目标下的进展记录。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListObjectiveProgressesTool))]
    public Task<FeishuToolResult> ListObjectiveProgressesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListObjectiveProgresses, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrProgressClient
                ?? throw new ArgumentException("okr.list_objective_progresses 需要 IFeishuTenantV2OkrProgress——宿主须启用 OKR 域客户端");
            var args = OkrListObjectiveProgressesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .ListObjectiveProgressesAsync(args.ObjectiveId, page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectProgresses);
        });
    }

    /// <summary>okr.list_key_result_progresses：列出关键结果下的进展记录。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListKeyResultProgressesTool))]
    public Task<FeishuToolResult> ListKeyResultProgressesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListKeyResultProgresses, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrProgressClient
                ?? throw new ArgumentException("okr.list_key_result_progresses 需要 IFeishuTenantV2OkrProgress——宿主须启用 OKR 域客户端");
            var args = OkrListKeyResultProgressesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .ListKeyResultProgressesAsync(args.KeyResultId, page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectProgresses);
        });
    }

    /// <summary>okr.list_periods：列出租户下的 OKR 周期定义。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListPeriodsTool))]
    public Task<FeishuToolResult> ListPeriodsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListPeriods, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrPeriodClient
                ?? throw new ArgumentException("okr.list_periods 需要 IFeishuTenantV1OkrPeriod——宿主须启用 OKR 域客户端");
            var args = OkrListPeriodsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .ListPeriodsAsync(page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPeriods);
        });
    }

    /// <summary>okr.list_categories：列出 OKR 分类。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantOkrListCategoriesTool))]
    public Task<FeishuToolResult> ListCategoriesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrListCategories, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrCategoryClient
                ?? throw new ArgumentException("okr.list_categories 需要 IFeishuTenantV2OkrCategory——宿主须启用 OKR 域客户端");
            var args = OkrListCategoriesArgs.Unpack(arguments);

            var ownerType = args.OwnerType ?? "user";
            if (ownerType is not ("user" or "department"))
            {
                throw new ArgumentException($"owner_type 只接受 user 或 department，实际: {ownerType}");
            }

            var outcome = FeishuApiResultReader.Read(await client
                .ListCategoriesAsync(page_size: args.PageSize ?? DefaultPageSize, page_token: args.PageToken, owner_type: ownerType, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectCategories);
        });
    }

    // ────────── 投影（纯文本化富文本 + 翻页契约） ──────────

    private static JsonObject ProjectCycles(ApiPageListResult<Cycle> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["cycle_id"] = item.Id,
                ["tenant_cycle_id"] = item.TenantCycleId,
                ["owner_id"] = item.Owner?.UserId,
                ["start_time"] = item.StartTime,
                ["end_time"] = item.EndTime,
                ["cycle_status"] = item.CycleStatus,
                ["score"] = item.Score,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectObjectives(ApiPageListResult<Objective> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(ProjectObjective(item));
        }

        return envelope;
    }

    private static JsonObject ProjectObjective(Objective? objective)
    {
        if (objective is null)
        {
            return new JsonObject();
        }

        return new JsonObject
        {
            ["objective_id"] = objective.Id,
            ["content"] = ToolResultText.Truncate(ToPlainText(objective.Content), PageSizes.MessagePreviewLength),
            ["notes"] = ToPlainText(objective.Notes),
            ["owner_id"] = objective.Owner?.UserId,
            ["cycle_id"] = objective.CycleId,
            ["position"] = objective.Position,
            ["score"] = objective.Score,
            ["weight"] = objective.Weight,
            ["deadline"] = objective.Deadline,
            ["category_id"] = objective.CategoryId,
        };
    }

    private static JsonObject ProjectKeyResults(ApiPageListResult<KeyResult> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(ProjectKeyResult(item));
        }

        return envelope;
    }

    private static JsonObject ProjectKeyResult(KeyResult? keyResult)
    {
        if (keyResult is null)
        {
            return new JsonObject();
        }

        return new JsonObject
        {
            ["key_result_id"] = keyResult.Id,
            ["objective_id"] = keyResult.ObjectiveId,
            ["content"] = ToolResultText.Truncate(ToPlainText(keyResult.Content), PageSizes.MessagePreviewLength),
            ["owner_id"] = keyResult.Owner?.UserId,
            ["position"] = keyResult.Position,
            ["score"] = keyResult.Score,
            ["weight"] = keyResult.Weight,
            ["deadline"] = keyResult.Deadline,
        };
    }

    private static JsonObject ProjectProgresses(ApiPageListResult<Progress> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["progress_id"] = item.Id,
                ["content"] = ToolResultText.Truncate(ToPlainText(item.Content), PageSizes.MessagePreviewLength),
                ["entity_type"] = item.EntityType,
                ["entity_id"] = item.EntityId,
                ["owner_id"] = item.Owner?.UserId,
                ["progress_percent"] = item.ProgressRate?.ProgressPercent,
                ["progress_status"] = item.ProgressRate?.ProgressStatus,
                ["create_time"] = item.CreateTime,
                ["update_time"] = item.UpdateTime,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectPeriods(ApiPageListResult<Period> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["period_id"] = item.Id,
                ["zh_name"] = item.ZhName,
                ["en_name"] = item.EnName,
                ["status"] = item.Status,
                ["period_start_time"] = item.PeriodStartTime,
                ["period_end_time"] = item.PeriodEndTime,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectCategories(ApiPageListResult<Category> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["category_id"] = item.Id,
                ["name_zh"] = item.Name?.Zh,
                ["name_en"] = item.Name?.En,
                ["enabled"] = item.Enabled,
                ["color"] = item.Color,
                ["category_type"] = item.CategoryType,
            });
        }

        return envelope;
    }

    /// <summary>翻页信封（items + has_more + 可选 page_token）。</summary>
    private static JsonObject PageHeader<T>(ApiPageListResult<T> data)
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

        return envelope;
    }

    /// <summary>
    /// 富文本块树 → 纯文本（OKR 域的投影核心）。
    /// </summary>
    /// <remarks>
    /// paragraph 逐 element 拼接：text_run 取原文、mention 记为 <c>@{user_id}</c>、
    /// docs_link 记为 <c>{title}({url})</c>；gallery（图片集合）与未知块以
    /// <c>[image]</c> / <c>[block:{type}]</c> 占位——保留"这里有一张图/一个块"的事实，
    /// 但不搬运二进制或样式树。
    /// </remarks>
    internal static string ToPlainText(ContentBlockV2? block)
    {
        if (block?.Blocks is not { Length: > 0 } blocks)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        foreach (var element in blocks)
        {
            if (element.Paragraph?.Elements is { Length: > 0 } paragraphElements)
            {
                foreach (var paragraphElement in paragraphElements)
                {
                    if (paragraphElement.TextRun?.Text is { Length: > 0 } text)
                    {
                        builder.Append(text);
                    }
                    else if (paragraphElement.Mention?.UserId is { Length: > 0 } mentionUserId)
                    {
                        builder.Append('@').Append(mentionUserId);
                    }
                    else if (paragraphElement.DocsLink is { } link)
                    {
                        builder.Append(link.Title ?? link.Url ?? string.Empty);
                        if (!string.IsNullOrEmpty(link.Url))
                        {
                            builder.Append('(').Append(link.Url).Append(')');
                        }
                    }
                }

                builder.Append('\n');
            }
            else if (element.Gallery is not null)
            {
                builder.Append("[image]\n");
            }
            else if (!string.IsNullOrEmpty(element.BlockElementType))
            {
                builder.Append("[block:").Append(element.BlockElementType).Append("]\n");
            }
        }

        return builder.ToString().TrimEnd('\n');
    }
}
