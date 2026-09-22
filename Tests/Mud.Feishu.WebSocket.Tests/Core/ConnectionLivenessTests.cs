// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 连接存活探针的判定口径（R2 / F1 / WS2-18 用例 #29）。
/// </summary>
/// <remarks>
/// <see cref="ConnectionLiveness"/> 是 F1 "僵尸判定"的**唯一决策函数**：
/// 健康检查的 <c>Unhealthy</c>、周期性自愈重连、指标面的 <c>feishu.websocket.zombie</c>
/// 全部读它的 <see cref="ConnectionLiveness.IsZombie"/>。因此它的真值表必须被穷尽锁定——
/// 误判为僵尸会造成周期性无谓重连，漏判则退回"连接看似正常却收不到事件"的静默失活（P0-1）。
/// <para>
/// <b>为什么可以直接构造</b>：该类型是纯数据快照（三个输入根因，无 I/O），
/// 直接构造比"制造真实僵尸连接"更能穷尽组合；**真实触发路径**由
/// <c>WebSocketConnectionIntegrationTests.ReceiveLoop_ShouldRaiseDisconnected_WhenCancelledWhileDispatchingFrame</c>
/// （socket 保持 Open 而循环退出）覆盖。两者结合才是完整证据链。
/// </para>
/// </remarks>
public class ConnectionLivenessTests
{
    [Theory]
    // 存活 + 已连接 ⇒ 正常
    [InlineData(true, true, false)]
    // 循环已结束 + 已连接 ⇒ **僵尸态**（唯一确定性矛盾）
    [InlineData(false, true, true)]
    // 循环仍在 + 未连接 ⇒ 只是断开，交由"连接状态"路径处理
    [InlineData(true, false, false)]
    // 两者皆否 ⇒ 完全断开，不得再报僵尸（否则会重复告警/重复重连）
    [InlineData(false, false, false)]
    public void IsZombie_ShouldFollowTruthTable(bool receiveLoopAlive, bool isConnected, bool expectedZombie)
    {
        var liveness = new ConnectionLiveness(receiveLoopAlive, DateTime.UtcNow, isConnected);

        liveness.IsZombie.Should().Be(expectedZombie);
    }

    /// <summary>
    /// 单纯"长时间无帧"**不得**判为僵尸（否则空闲期会周期性误报并触发无谓重连）。
    /// </summary>
    [Fact]
    public void IsZombie_ShouldBeFalse_WhenConnectionIsMerelyIdle()
    {
        var liveness = new ConnectionLiveness(
            receiveLoopAlive: true,
            lastReceiveUtc: DateTime.UtcNow.AddHours(-6),
            isConnected: true);

        liveness.IsZombie.Should().BeFalse("飞书长连接空闲期本就没有事件帧，空闲 ≠ 僵尸");
        liveness.IdleMs.Should().BeGreaterThanOrEqualTo((long)TimeSpan.FromHours(6).TotalMilliseconds - 1000);
    }

    /// <summary>
    /// 无收帧样本时 <see cref="ConnectionLiveness.IdleMs"/> 必须是 <c>-1</c> 哨兵，而不是 <c>0</c>。
    /// </summary>
    /// <remarks>
    /// <c>0</c> 与"刚刚收到帧"无法区分，会让"静默时长"看板与告警规则把"从未收帧"读成"非常健康"。
    /// </remarks>
    [Fact]
    public void IdleMs_ShouldBeSentinelMinusOne_WhenNoFrameReceivedYet()
    {
        var liveness = new ConnectionLiveness(true, lastReceiveUtc: null, isConnected: true);

        liveness.IdleMs.Should().Be(-1);
        liveness.LastReceiveUtc.Should().BeNull();
    }

    /// <summary>
    /// 未来时间戳（时钟回退/进程内时钟跳变）不得产生负的静默时长。
    /// </summary>
    [Fact]
    public void IdleMs_ShouldClampToZero_WhenLastReceiveIsInTheFuture()
    {
        var liveness = new ConnectionLiveness(true, DateTime.UtcNow.AddMinutes(5), isConnected: true);

        liveness.IdleMs.Should().Be(0,
            "时钟回退会产生负静默时长，指标面应保持单调非负（否则看板出现负值尖峰）");
    }

    [Fact]
    public void Properties_ShouldRoundTripInputs()
    {
        var last = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);

        var liveness = new ConnectionLiveness(true, last, isConnected: true);

        liveness.ReceiveLoopAlive.Should().BeTrue();
        liveness.LastReceiveUtc.Should().Be(last);
        liveness.IsConnected.Should().BeTrue();
    }
}
