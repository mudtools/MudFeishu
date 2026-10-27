// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// OKR 写入工具执行器（R6 / S2）：<c>okr.create_objective</c> / <c>okr.update_objective</c> /
/// <c>okr.delete_objective</c> / <c>okr.create_key_result</c> / <c>okr.update_key_result</c> /
/// <c>okr.delete_key_result</c> / <c>okr.update_indicator</c>。
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
    IFeishuTenantV2OkrCycle? okrCycleClient,
    IFeishuTenantV2OkrObjective? okrObjectiveClient,
    IFeishuTenantV2OkrKeyResult? okrKeyResultClient,
    IOptions<FeishuAgentOptions> options,
    IFeishuTenantV2OkrIndicator? okrIndicatorClient = null)
{
    private readonly IFeishuTenantV2OkrCycle? _okrCycleClient = okrCycleClient;
    private readonly IFeishuTenantV2OkrObjective? _okrObjectiveClient = okrObjectiveClient;
    private readonly IFeishuTenantV2OkrKeyResult? _okrKeyResultClient = okrKeyResultClient;
    private readonly IFeishuTenantV2OkrIndicator? _okrIndicatorClient = okrIndicatorClient;
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>okr.create_objective：在周期下创建目标（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantOkrCreateObjectiveTool))]
    public Task<ToolResult> CreateObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantOkrUpdateObjectiveTool))]
    public Task<ToolResult> UpdateObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantOkrDeleteObjectiveTool))]
    public Task<ToolResult> DeleteObjectiveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrDeleteObjective);
        return executor.RunAsync(async () =>
        {
            var client = _okrObjectiveClient
                ?? throw new ArgumentException("okr.delete_objective 需要 IFeishuTenantV2OkrObjective——宿主须启用 OKR 域客户端");
            var args = OkrDeleteObjectiveArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantOkrCreateKeyResultTool))]
    public Task<ToolResult> CreateKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantOkrUpdateKeyResultTool))]
    public Task<ToolResult> UpdateKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return ToolResult.FromText(ToolDryRun.Describe(
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
    [ToolHandler(typeof(IFeishuTenantOkrDeleteKeyResultTool))]
    public Task<ToolResult> DeleteKeyResultAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrDeleteKeyResult);
        return executor.RunAsync(async () =>
        {
            var client = _okrKeyResultClient
                ?? throw new ArgumentException("okr.delete_key_result 需要 IFeishuTenantV2OkrKeyResult——宿主须启用 OKR 域客户端");
            var args = OkrDeleteKeyResultArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
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

    /// <summary>okr.update_indicator：更新指标取值与计算方式（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantOkrUpdateIndicatorTool))]
    public Task<ToolResult> UpdateIndicatorAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.OkrUpdateIndicator, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _okrIndicatorClient
                ?? throw new ArgumentException("okr.update_indicator 需要 IFeishuTenantV2OkrIndicator——宿主须启用 OKR 域客户端");
            var args = OkrUpdateIndicatorArgs.Unpack(arguments);

            // 校验全部先于预演：dry_run 也必须走同一套本地拒绝，否则预演通过、实调失败。
            var request = BuildIndicatorRequest(
                args,
                ParseIndicatorValue(args.CurrentValue, "current_value"),
                ParseIndicatorValue(args.StartValue, "start_value"),
                ParseIndicatorValue(args.TargetValue, "target_value"));

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/okr/v2/indicators/{indicator_id}",
                    "⚠️ 指标 PATCH 不支持幂等键；且「改哪个字段受实体类型限制」须先读指标确认",
                    ("indicator_id", args.IndicatorId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .UpdateIndicatorAsync(args.IndicatorId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => OkrTools.ProjectIndicator(data.Indicator));
        });
    }

    /// <summary>指标数值取值范围上限（官方：-99999999999 ~ 99999999999）。</summary>
    private const double IndicatorValueLimit = 99_999_999_999d;

    private static double? ParseIndicatorValue(string? raw, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || double.IsNaN(value) || double.IsInfinity(value)
            || Math.Abs(value) > IndicatorValueLimit)
        {
            throw new ArgumentException($"{parameterName} 需为 -99999999999 ~ 99999999999 之间的数值，实际: {raw}");
        }

        return value;
    }

    /// <summary>
    /// 组装指标 PATCH 请求体，并只做<b>无需读即可判定</b>的本地拒绝（闭集、成对、空请求、
    /// indicator_status 与非手动状态计算方式的冲突）。实体归属约束交平台。
    /// </summary>
    private static PatchIndicatorRequest BuildIndicatorRequest(
        OkrUpdateIndicatorArgs args, double? currentValue, double? startValue, double? targetValue)
    {
        var currentCalcType = RequireIndicatorValue(args.CurrentValueCalculateType, [0, 1, 2], "current_value_calculate_type");
        var statusCalcType = RequireIndicatorValue(args.StatusCalculateType, [0, 1, 2], "status_calculate_type");
        var indicatorStatus = RequireIndicatorValue(args.IndicatorStatus, [-1, 0, 1, 2], "indicator_status");
        var unit = BuildIndicatorUnit(args.UnitType, args.UnitValue);

        if (currentValue is null && startValue is null && targetValue is null
            && currentCalcType is null && statusCalcType is null && indicatorStatus is null && unit is null)
        {
            throw new ArgumentException(
                "至少提供一个要更新的字段（current_value / start_value / target_value / "
                + "current_value_calculate_type / status_calculate_type / indicator_status / unit_type+unit_value）");
        }

        // 平台只在「手动更新」下接受 indicator_status；把这条判据放在本地，是为了让模型
        // 不必等一次平台报错（错误体不带字段名）就知道该改哪个参数。
        if (indicatorStatus is not null && statusCalcType is not null && statusCalcType != 0)
        {
            throw new ArgumentException(
                $"indicator_status 只在 status_calculate_type=0（手动更新）时可修改，"
                + $"本次同时传入 status_calculate_type={statusCalcType.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return new PatchIndicatorRequest
        {
            CurrentValue = currentValue,
            StartValue = startValue,
            TargetValue = targetValue,
            CurrentValueCalculateType = currentCalcType,
            StatusCalculateType = statusCalcType,
            IndicatorStatus = indicatorStatus,
            Unit = unit,
        };
    }

    /// <summary>单位面：unit_type 与 unit_value 必须成对；公共单位走闭集，自定义单位限 5 字符。</summary>
    private static IndicatorUnit? BuildIndicatorUnit(int? unitType, string? unitValue)
    {
        if (unitType is null && string.IsNullOrWhiteSpace(unitValue))
        {
            return null;
        }

        if (unitType is null || string.IsNullOrWhiteSpace(unitValue))
        {
            throw new ArgumentException("unit_type 与 unit_value 必须成对提供（0=公共单位 / 1=自定义单位）");
        }

        RequireIndicatorValue(unitType, [0, 1], "unit_type");
        var value = unitValue!.Trim();
        if (unitType == 0 && value is not ("PERCENT" or "NONE" or "YUAN" or "DOLLAR"))
        {
            throw new ArgumentException($"unit_type=0（公共单位）的 unit_value 仅支持 PERCENT/NONE/YUAN/DOLLAR，实际: {unitValue}");
        }

        if (unitType == 1 && value.Length > 5)
        {
            throw new ArgumentException($"unit_type=1（自定义单位）的 unit_value 不超过 5 个字符，实际长度: {value.Length}");
        }

        return new IndicatorUnit { UnitType = unitType, UnitValue = value };
    }

    /// <summary>整数闭集校验（null = 未提供，直接放行）。</summary>
    private static int? RequireIndicatorValue(int? raw, int[] allowed, string parameterName)
    {
        if (raw is null)
        {
            return null;
        }

        if (Array.IndexOf(allowed, raw.Value) < 0)
        {
            throw new ArgumentException(
                $"{parameterName} 仅支持 {string.Join(" / ", allowed.Select(static x => x.ToString(CultureInfo.InvariantCulture)))}，"
                + $"实际: {raw.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return raw;
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
