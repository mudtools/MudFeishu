// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 妙记生产面执行器（<c>minutes.clip</c> / <c>minutes.upload</c> /
/// <c>minutes.subscribe</c> / <c>minutes.unsubscribe</c>）：四者均为<b>用户身份</b>写操作。
/// </summary>
/// <remarks>
/// <para>
/// <b>客户端可空（软缺席）</b>：宿主未启用妙记 API 或未放行 user 身份时，本执行器仍须能构造，
/// 由执行期给出可执行错误——硬依赖会让整域工具静默从注册表消失（<c>MinutesReadTools</c> 踩过的坑）。
/// </para>
/// <para>
/// <b>时间段的本地校验</b>：剪辑入参用「起始|结束」文本 DSL，故必须在下发前把每段解析成带时区的
/// RFC3339——宽松解析会把无时区值按本地时区折算，产生<b>静默偏移</b>（剪错时间段且平台不会报错）。
/// </para>
/// </remarks>
internal sealed class MinutesUserWriteTools(
    Mud.Feishu.IFeishuUserV1MinutesMinute? minutesClient = null)
{
    /// <summary>单次剪辑的时间段数上限（平台约束 1~50）。</summary>
    private const int MaxTimeRanges = 50;

    /// <summary>单段时间长度下限（毫秒，平台约束：每段须大于 1000ms）。</summary>
    private const double MinRangeLengthMs = 1000d;

    /// <summary>可订阅的事件类型闭集（平台当前仅此一种）。</summary>
    private static readonly string[] SubscriptionEventTypes = ["minutes.minute.generated_v1"];

    private readonly Mud.Feishu.IFeishuUserV1MinutesMinute? _minutesClient = minutesClient;

    /// <summary>minutes.clip：按时间段剪辑妙记（异步生成，成功只代表已提交）。</summary>
    [ToolHandler(typeof(IFeishuUserMinutesClipTool))]
    public Task<ToolResult> ClipMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesClip);
        return executor.RunAsync(async () =>
        {
            var args = MinutesClipArgs.Unpack(arguments);

            // 校验先于预演：dry_run 也必须暴露 DSL 错误，否则"预演通过、真跑失败"毫无价值。
            var timeRanges = ParseTimeRanges(args.TimeRanges);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/minutes/v1/minutes/{args.MinuteToken}/clip",
                    null,
                    ("minute_token", args.MinuteToken.Length),
                    ("time_ranges", timeRanges.Length),
                    ("title", args.Title?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .ClipMinuteAsync(
                    args.MinuteToken,
                    new ClipMinuteRequest { TimeRanges = timeRanges, Title = args.Title },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => MinuteUrlPayload(data, timeRanges.Length));
        });
    }

    /// <summary>minutes.upload：把云盘音视频文件导入生成妙记（只接受 file_token，不搬运字节）。</summary>
    [ToolHandler(typeof(IFeishuUserMinutesUploadTool))]
    public Task<ToolResult> UploadMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesUpload);
        return executor.RunAsync(async () =>
        {
            var args = MinutesUploadArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/minutes/v1/minutes/upload",
                    null,
                    ("file_token", args.FileToken.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .UploadMinuteAsync(new UploadMinuteRequest { FileToken = args.FileToken }, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => MinuteUrlPayload(data, 0));
        });
    }

    /// <summary>minutes.subscribe：订阅妙记生成事件（改的是"后续事件流向"，故属写面）。</summary>
    [ToolHandler(typeof(IFeishuUserMinutesSubscribeTool))]
    public Task<ToolResult> SubscribeMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesSubscribe);
        return executor.RunAsync(async () =>
        {
            var args = MinutesSubscribeArgs.Unpack(arguments);
            return await SubscriptionAsync(
                executor,
                "/open-apis/minutes/v1/minutes/subscription",
                "subscribed",
                args.EventType,
                args.DryRun,
                cancellationToken,
                static (client, request, ct) => client.SubscribeMinuteAsync(request, ct)).ConfigureAwait(false);
        });
    }

    /// <summary>minutes.unsubscribe：取消订阅（与 minutes.subscribe 成对，用户须能收回事件授权）。</summary>
    [ToolHandler(typeof(IFeishuUserMinutesUnsubscribeTool))]
    public Task<ToolResult> UnsubscribeMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesUnsubscribe);
        return executor.RunAsync(async () =>
        {
            var args = MinutesUnsubscribeArgs.Unpack(arguments);
            return await SubscriptionAsync(
                executor,
                "/open-apis/minutes/v1/minutes/unsubscription",
                "unsubscribed",
                args.EventType,
                args.DryRun,
                cancellationToken,
                static (client, request, ct) => client.UnsubscribeMinuteAsync(request, ct)).ConfigureAwait(false);
        });
    }

    /// <summary>订阅/退订阅的共同路径：两端除工具名、path 与回执键外完全同形，分叉只会让两侧口径漂移。</summary>
    private async Task<ToolResult> SubscriptionAsync(
        ToolExecutor executor,
        string path,
        string receiptKey,
        string? rawEventType,
        bool? dryRun,
        CancellationToken cancellationToken,
        Func<Mud.Feishu.IFeishuUserV1MinutesMinute, MinuteSubscriptionRequest, CancellationToken, Task<FeishuNullDataApiResult?>> invoke)
    {
        var eventType = ResolveEventType(rawEventType);

        if (ToolDryRun.IsRequested(dryRun))
        {
            return ToolResult.FromText(ToolDryRun.Describe(
                executor.ToolName, "POST", path, null, ("event_type", eventType.Length)));
        }

        var failure = RequireNullDataSuccess(
            executor.ToolName,
            await invoke(Require(executor.ToolName), new MinuteSubscriptionRequest { EventType = eventType }, cancellationToken)
                .ConfigureAwait(false));
        return failure ?? ToolResult.FromText(new JsonObject
        {
            [receiptKey] = true,
            ["event_type"] = eventType,
        }.ToJsonString());
    }

    // ────────── 参数解析与投影 ──────────

    /// <summary>
    /// 解析「起始|结束」文本 DSL 为平台请求体（1~50 段、每段 &gt; 1000ms、两端必须带时区）。
    /// </summary>
    private static MinutesTimeRange[] ParseTimeRanges(string[]? rawRanges)
    {
        var ranges = (rawRanges ?? [])
            .Where(static raw => !string.IsNullOrWhiteSpace(raw))
            .ToArray();

        if (ranges.Length == 0)
        {
            throw new ArgumentException("time_ranges 不能为空——每段格式「起始|结束」（ISO 8601 带时区）");
        }

        if (ranges.Length > MaxTimeRanges)
        {
            throw new ArgumentException($"time_ranges 一次最多 {MaxTimeRanges} 段，实际: {ranges.Length}");
        }

        var parsed = new MinutesTimeRange[ranges.Length];
        for (var i = 0; i < ranges.Length; i++)
        {
            // netstandard2.0 既无 StringSplitOptions.TrimEntries，也无 Split(char, StringSplitOptions)
            // 重载 ⇒ 先按 '|' 全切，再 Trim 去空（与 RemoveEmptyEntries 等价）。
            var parts = ranges[i]
                .Split('|')
                .Select(static part => part.Trim())
                .Where(static part => part.Length > 0)
                .ToArray();
            if (parts.Length != 2)
            {
                throw new ArgumentException(
                    $"time_ranges[{i}] 需为「起始|结束」两段，实际 {parts.Length} 段: {ranges[i]}");
            }

            var start = ParseRfc3339(parts[0], $"time_ranges[{i}] 起始");
            var end = ParseRfc3339(parts[1], $"time_ranges[{i}] 结束");
            var lengthMs = (end - start).TotalMilliseconds;
            if (lengthMs <= MinRangeLengthMs)
            {
                throw new ArgumentException(
                    $"time_ranges[{i}] 结束须晚于起始且区间大于 {MinRangeLengthMs.ToString(CultureInfo.InvariantCulture)} 毫秒，实际长度: {lengthMs.ToString(CultureInfo.InvariantCulture)} 毫秒");
            }

            parsed[i] = new MinutesTimeRange { StartTime = parts[0], EndTime = parts[1] };
        }

        return parsed;
    }

    /// <summary>RFC3339 严格解析（必须带时区；无时区值按本地时区折算会静默偏移，故直接拒绝）。</summary>
    private static DateTimeOffset ParseRfc3339(string value, string parameterName)
    {
        var trimmed = value.Trim();
        var hasOffset = trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || trimmed.IndexOf('+') > 0
            || trimmed.LastIndexOf('-') > 10;

        if (!hasOffset
            || !DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException(
                $"{parameterName} 需为带时区的 RFC3339 格式（如 2026-10-01T14:00:00+08:00），实际: {trimmed}");
        }

        return parsed;
    }

    /// <summary>事件类型闭集校验（缺省取唯一可选值）。</summary>
    private static string ResolveEventType(string? raw)
    {
        var value = string.IsNullOrWhiteSpace(raw) ? SubscriptionEventTypes[0] : raw.Trim();
        if (!SubscriptionEventTypes.Contains(value, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"event_type 仅支持 {string.Join("/", SubscriptionEventTypes)}，实际: {raw}");
        }

        return value;
    }

    /// <summary>剪辑/导入的共同回执：链接 + （剪辑时）请求段数。</summary>
    private static JsonObject MinuteUrlPayload(MinuteUrlResult? data, int requestedRangeCount)
    {
        var payload = new JsonObject { ["minute_url"] = data?.MinuteUrl };
        if (requestedRangeCount > 0)
        {
            payload["requested_range_count"] = requestedRangeCount;
        }

        return payload;
    }

    private Mud.Feishu.IFeishuUserV1MinutesMinute Require(string toolName)
        => _minutesClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuUserV1MinutesMinute——宿主须启用妙记（Minutes）API 并放行 user 身份");

    /// <summary>解包 <c>FeishuNullDataApiResult</c>（T 不可推断，不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>）。</summary>
    /// <returns>成功返回 <see langword="null"/>；失败返回结构化错误结果。</returns>
    private static ToolResult? RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        return result.Code == 0
            ? null
            : ToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName, result.Code,
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}"));
    }
}
