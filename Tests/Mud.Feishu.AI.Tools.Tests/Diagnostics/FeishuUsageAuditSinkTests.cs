// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Diagnostics;
using Mud.Feishu.AI.Tools.Hosting;

namespace Mud.Feishu.AI.Tools.Tests.Diagnostics;

/// <summary>
/// 审计 → 成本核算桥（E-2）：磁盘上的一行审计必须原样转发，聚合侧只做归因。
/// </summary>
public class FeishuUsageAuditSinkTests
{
    private static ToolExecutionAuditRecord Record(string decision, string? reason = null, bool isWrite = false)
        => new(
            "im.send_message",
            "appA",
            ["im:message:send"],
            decision,
            reason,
            isWrite,
            FeishuToolRiskNames.HighRiskWrite,
            "digest",
            42,
            "conv-1");

    [Fact]
    public async Task WriteAsync_ShouldAggregateAndForward()
    {
        var meter = new InMemoryFeishuUsageMeter();
        var inner = new RecordingSink();
        var sink = new FeishuUsageAuditSink(meter, inner);

        await sink.WriteAsync(Record(FeishuToolDiagnostics.DecisionAllowed, isWrite: true));

        var snapshot = meter.TakeSnapshot();
        snapshot.Tools.Single().Calls.Should().Be(1);
        snapshot.Sessions.Single().WriteCalls.Should().Be(1);
        snapshot.Sessions.Single().FailedCalls.Should().Be(0);
        inner.Records.Should().ContainSingle("内层落库链不得因叠加聚合而丢事件");
    }

    [Fact]
    public async Task WriteAsync_Denied_ShouldCountAsDeniedNotFailed()
    {
        var meter = new InMemoryFeishuUsageMeter();

        await new FeishuUsageAuditSink(meter).WriteAsync(
            Record(FeishuToolDiagnostics.DecisionDenied, reason: "租户未开通该工具"));

        var session = meter.TakeSnapshot().Sessions.Single();
        session.DeniedCalls.Should().Be(1);
        session.FailedCalls.Should().Be(0, "拒绝是授权决策而非执行失败——混在一起会让成功率口径失真");
    }

    [Fact]
    public async Task WriteAsync_AllowedWithReason_ShouldCountAsFailed()
    {
        var meter = new InMemoryFeishuUsageMeter();

        await new FeishuUsageAuditSink(meter).WriteAsync(
            Record(FeishuToolDiagnostics.DecisionAllowed, reason: "api_error: 上游 400"));

        meter.TakeSnapshot().Sessions.Single().FailedCalls.Should().Be(1);
    }

    private sealed class RecordingSink : IToolExecutionAuditSink
    {
        public List<ToolExecutionAuditRecord> Records { get; } = [];

        public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
