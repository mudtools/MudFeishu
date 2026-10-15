// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using Mud.Feishu.Abstractions.Observability;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Tests.Diagnostics;

/// <summary>
/// <see cref="FeishuAgentTelemetry"/> 运行期端口（<c>StartRun</c> / <c>RunCompleted</c>）的 Span 黄金：
/// 经端口埋点与既有 <see cref="FeishuAgentDiagnostics"/> 直调必须产出**同一命名面**（R2-13 的机械判据）。
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
