// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Observability;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Tests.Diagnostics;

/// <summary>
/// <see cref="FeishuAgentTelemetry"/> 运行期端口（<c>StartRun</c> / <c>RunCompleted</c>）的 Span 黄金：
/// 经端口埋点与既有 <see cref="FeishuAgentDiagnostics"/> 直调必须产出**同一命名面**（R2-13 的机械判据）。
/// 另断言 <c>RunCompleted</c> 的第二条出口——<c>feishu.agent.llm.tokens</c> 成本计数（E-2 支柱②）：
/// 它与 Span 相互独立，宿主没接链路追踪时也拿得到用量。
/// </summary>
public class FeishuAgentTelemetryTests
{
    private static readonly FeishuAgentTelemetry Telemetry = new();

    [Fact]
    public void StartRun_ShouldEmitSameSpan_AsDirectDiagnosticsCall()
    {
        var agentName = Unique("run");
        var viaPort = Capture(agentName, () => Telemetry.StartRun(agentName, streaming: false));
        var direct = Capture(agentName, () => FeishuAgentDiagnostics.StartRunActivity(
            agentName, FeishuAgentDiagnostics.OperationRun));

        viaPort.OperationName.Should().Be(direct.OperationName);
        viaPort.Tags.Should().BeEquivalentTo(direct.Tags);
    }

    [Fact]
    public void StartRun_ShouldSelectStreamingOperation_WhenStreaming()
    {
        var agentName = Unique("stream");
        var activity = Capture(agentName, () => Telemetry.StartRun(agentName, streaming: true));

        activity.OperationName.Should().Be("feishu.agent.run_streaming");
        activity.GetTagItem(FeishuAgentDiagnostics.TagOperation).Should().Be(
            FeishuAgentDiagnostics.OperationRunStreaming);
    }

    [Fact]
    public void RunCompleted_ShouldMarkErrorWithoutTokenTags_WhenFailed()
    {
        var agentName = Unique("fail");
        var activity = Capture(agentName, () => Telemetry.StartRun(agentName, streaming: false));

        Telemetry.RunCompleted(activity, failed: true, inputTokens: null, outputTokens: null, totalTokens: null);

        activity.Status.Should().Be(ActivityStatusCode.Error, "失败必须在链路里可见");
        activity.Tags.Should().NotContain(t => t.Key == FeishuAgentDiagnostics.TagInputTokens);
    }

    [Fact]
    public void RunCompleted_ShouldWriteOnlyReportedTokenTags()
    {
        var agentName = Unique("usage");
        var activity = Capture(agentName, () => Telemetry.StartRun(agentName, streaming: false));

        Telemetry.RunCompleted(activity, failed: false, inputTokens: 10, outputTokens: null, totalTokens: 15);

        activity.Status.Should().NotBe(ActivityStatusCode.Error);
        activity.GetTagItem(FeishuAgentDiagnostics.TagInputTokens).Should().Be(10L);
        activity.GetTagItem(FeishuAgentDiagnostics.TagTotalTokens).Should().Be(15L);
        activity.Tags.Should().NotContain(t => t.Key == FeishuAgentDiagnostics.TagOutputTokens,
            "未上报的计数不得填伪值（否则宿主仪表盘把缺失当成 0）");
    }

    // ─────────────── E-2 成本核算出口（feishu.agent.llm.tokens，GAP-11 支柱②） ───────────────

    /// <summary>三项 usage 各自成一条方向计数，且只带 direction 一个维度（高基数纪律）。</summary>
    [Fact]
    public void RunCompleted_ShouldCountTokensPerDirection_WhenUsageReported()
    {
        var measurements = CaptureTokenCounts(
            () => Telemetry.RunCompleted(null, failed: false, inputTokens: 10, outputTokens: 20, totalTokens: 30));

        measurements.Select(static m => $"{m.Value}:{m.Direction}").Should().BeEquivalentTo(
            [$"10:{FeishuMetrics.TokenDirections.Input}", $"20:{FeishuMetrics.TokenDirections.Output}",
             $"30:{FeishuMetrics.TokenDirections.Total}"],
            "三项独立累加，方向取值受控于 TokenDirections 闭集");
        measurements.Select(static m => m.TagCount).Should().OnlyContain(static n => n == 1,
            "按会话/租户的下钻走 Span 属性，指标维度一旦带上 agent/app_key 就随配置应用数线性膨胀");
    }

    /// <summary>
    /// 没有 Activity 监听器时成本读数仍然产生——成本挂在指标出口上，不挂在 Span 上
    /// （此前 RunCompleted 首行按 scope 类型早退，宿主没接链路追踪就等于没成本口径）。
    /// </summary>
    [Fact]
    public void RunCompleted_ShouldCountTokens_WhenScopeIsNotAnActivity()
    {
        var measurements = CaptureTokenCounts(
            () => Telemetry.RunCompleted(null, failed: true, inputTokens: 7, outputTokens: null, totalTokens: null));

        measurements.Select(static m => $"{m.Value}:{m.Direction}").Should().Equal(
            [$"7:{FeishuMetrics.TokenDirections.Input}"]);
    }

    /// <summary>缺项与零/负值不计数：供应商未回传 usage 时宁缺不造。</summary>
    [Theory]
    [InlineData(null, 5L, null, "output")]
    [InlineData(0L, 0L, 0L, "")]
    [InlineData(-1L, 2L, -3L, "output")]
    public void RunCompleted_ShouldCountOnlyPositiveDirections(long? input, long? output, long? total, string expected)
    {
        var measurements = CaptureTokenCounts(
            () => Telemetry.RunCompleted(null, failed: false, input, output, total));

        var directions = expected.Length == 0
            ? Array.Empty<string>()
            : expected.Split(',');

        measurements.Select(static m => m.Direction).Should().Equal(directions);
    }

    private readonly record struct TokenMeasurement(long Value, string Direction, int TagCount);

    /// <summary>
    /// 收本用例发出的 <c>feishu.agent.llm.tokens</c> 计数。
    /// 指标是进程级通道、xUnit 并行用例会打同一 instrument，而 <c>direction</c> 之外没有可过滤维度——
    /// 故按发射线程收口（Counter.Add 同步回调到调用线程），只采信本线程的测量。
    /// </summary>
    private static List<TokenMeasurement> CaptureTokenCounts(Action emit)
    {
        var threadId = Environment.CurrentManagedThreadId;
        var seen = new List<TokenMeasurement>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Name == "feishu.agent.llm.tokens")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            if (Environment.CurrentManagedThreadId != threadId)
            {
                return;
            }

            var direction = string.Empty;
            var tagCount = 0;
            foreach (var tag in tags)
            {
                tagCount++;
                if (tag.Key == FeishuMetrics.Tags.Direction)
                {
                    direction = tag.Value as string ?? string.Empty;
                }
            }

            seen.Add(new TokenMeasurement(value, direction, tagCount));
        });
        listener.Start();
        // 兜底：instrument 是 static readonly，可能早于监听器创建（Published 不保证回放）。
        listener.EnableMeasurementEvents(FeishuMetrics.AgentLlmTokens);

        emit();
        return seen;
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
    /// <summary>在监听期内开启并结束一次作用域，返回被该 Agent 名标的的 Span（邻居用例的 Span 不予采信）。</summary>
    private static Activity Capture(string agentName, Func<IDisposable?> start)
    {
        var stopped = new ConcurrentQueue<Activity>();
        using (var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeishuActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => stopped.Enqueue(activity),
        })
        {
            ActivitySource.AddActivityListener(listener);
            start()?.Dispose();
        }

        return stopped.Single(activity =>
            activity.OperationName.StartsWith("feishu.agent.", StringComparison.Ordinal)
            && (string?)activity.GetTagItem(FeishuAgentDiagnostics.TagAgentName) == agentName);
    }
}
