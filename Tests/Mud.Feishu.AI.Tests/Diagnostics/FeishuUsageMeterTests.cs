// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Tests.Diagnostics;

/// <summary>
/// 成本核算出口（E-2）：按 <c>(租户, 会话)</c> 与工具两个视角的聚合行为。
/// </summary>
public class FeishuUsageMeterTests
{
    [Fact]
    public void RecordModelUsage_ShouldAggregatePerSession()
    {
        var meter = new InMemoryFeishuUsageMeter();

        meter.RecordModelUsage("appA", "conv-1", 100, 20);
        meter.RecordModelUsage("appA", "conv-1", 50, 5);
        meter.RecordModelUsage("appA", "conv-2", 7, null);

        var sessions = meter.TakeSnapshot().Sessions;

        sessions.Should().HaveCount(2, "两个会话键必须分成两桶（账务按会话归因）");
        var first = sessions.Single(s => s.ConversationKey == "conv-1");
        first.InputTokens.Should().Be(150);
        first.OutputTokens.Should().Be(25);

        var second = sessions.Single(s => s.ConversationKey == "conv-2");
        second.InputTokens.Should().Be(7);
        second.OutputTokens.Should().Be(0, "未上报的输出 token 不得凭空补齐成别的数");
    }

    [Fact]
    public void RecordToolCall_ShouldSplitDeniedAndFailedPerSessionAndTool()
    {
        var meter = new InMemoryFeishuUsageMeter();

        meter.RecordToolCall("appA", "conv-1", "im.send_message", isWrite: true, denied: false, failed: false, durationMs: 30);
        meter.RecordToolCall("appA", "conv-1", "im.send_message", isWrite: true, denied: true, failed: false, durationMs: 2);
        meter.RecordToolCall("appA", "conv-1", "im.list_chats", isWrite: false, denied: false, failed: true, durationMs: 8);

        var snapshot = meter.TakeSnapshot();

        var session = snapshot.Sessions.Single();
        session.ToolCalls.Should().Be(3);
        session.WriteCalls.Should().Be(2);
        session.DeniedCalls.Should().Be(1);
        session.FailedCalls.Should().Be(1);

        var send = snapshot.Tools.Single(t => t.ToolName == "im.send_message");
        send.Calls.Should().Be(2);
        send.DeniedCalls.Should().Be(1);
        send.DurationMs.Should().Be(32);
    }

    [Fact]
    public void RecordToolCall_ShouldRejectEmptyKeys()
    {
        var meter = new InMemoryFeishuUsageMeter();

        var noApp = () => meter.RecordToolCall(string.Empty, null, "im.list_chats", false, false, false, 1);
        var noTool = () => meter.RecordToolCall("appA", null, string.Empty, false, false, false, 1);

        noApp.Should().Throw<ArgumentException>("没有租户键的用量无法归因，不得静默记到空桶");
        noTool.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Snapshot_ShouldBeIndependentOfLaterWrites()
    {
        var meter = new InMemoryFeishuUsageMeter();
        meter.RecordModelUsage("appA", null, 10, 1);

        var snapshot = meter.TakeSnapshot();
        meter.RecordModelUsage("appA", null, 10, 1);

        snapshot.Sessions.Single().InputTokens.Should().Be(10, "快照是值拷贝，不是活视图");
        meter.TakeSnapshot().Sessions.Single().InputTokens.Should().Be(20);
    }
}
