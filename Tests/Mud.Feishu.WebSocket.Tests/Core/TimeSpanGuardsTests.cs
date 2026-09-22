// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// <see cref="TimeSpanGuards"/> 钳制边界测试（R2 / WS2-04 / I16）。
/// </summary>
/// <remarks>
/// 修复前的问题：<c>CancellationTokenSource(TimeSpan)</c> 把时长折算为 <c>int</c> 毫秒，
/// 超过 <c>int.MaxValue - 1</c>（约 24.8 天）时**抛 <see cref="ArgumentOutOfRangeException"/>**。
/// 由于该异常发生在 fire-and-forget 的重连任务体内并被通用 catch 吞掉，
/// 表现为"该轮自动重连完全不执行"——生产上是长时断连且无自愈。
/// </remarks>
public class TimeSpanGuardsTests
{
    [Fact]
    public void ClampToCancellationTokenRange_ShouldReturnZero_WhenValueIsZero()
    {
        TimeSpanGuards.ClampToCancellationTokenRange(TimeSpan.Zero)
            .Should().Be(TimeSpan.Zero, "0 表示不启动计时器，必须原样保留（不得被抬升为其它值）");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void ClampToCancellationTokenRange_ShouldReturnZero_WhenValueIsNegative(int milliseconds)
    {
        TimeSpanGuards.ClampToCancellationTokenRange(TimeSpan.FromMilliseconds(milliseconds))
            .Should().Be(TimeSpan.Zero, "负值一律钳制为 0（调用方自行决定是否接受 0 语义）");
    }

    [Fact]
    public void ClampToCancellationTokenRange_ShouldKeepValue_WhenWithinRange()
    {
        var value = TimeSpan.FromDays(7);

        TimeSpanGuards.ClampToCancellationTokenRange(value)
            .Should().Be(value, "合法区间内必须原样返回，钳制不得改变既有语义");
    }

    [Fact]
    public void ClampToCancellationTokenRange_ShouldClampToIntMaxMilliseconds_WhenExceedsUpperBound()
    {
        // Arrange：30 天 > int.MaxValue-1 毫秒（约 24.855 天）
        var oversized = TimeSpan.FromDays(30);

        // Act
        var clamped = TimeSpanGuards.ClampToCancellationTokenRange(oversized);

        // Assert
        clamped.Should().BeLessThan(oversized);
        clamped.TotalMilliseconds.Should().Be(TimeSpanGuards.MaxCancellationTokenDelayMs,
            "超上界必须饱和到 int.MaxValue-1 毫秒（CancellationTokenSource 的计时器上界）");
    }

    [Fact]
    public void ClampToCancellationTokenRange_ShouldProduceValue_ThatCancellationTokenSourceAccepts()
    {
        // 这是本工具存在的**唯一理由**：钳制后的值必须能被 BCL 接受。
        // 用 TimeSpan.MaxValue 构造原始值，覆盖最极端的配置错误。
        var clamped = TimeSpanGuards.ClampToCancellationTokenRange(TimeSpan.MaxValue);

        var act = () =>
        {
            using var cts = new CancellationTokenSource(clamped);
            return cts.Token;
        };

        act.Should().NotThrow("钳制后的时长必须落在 CancellationTokenSource 的合法区间内");
    }

    [Fact]
    public void ClampToCancellationTokenRange_ShouldThrow_WhenUnclampedOversizedValueIsUsed()
    {
        // 反向确认"不钳制真的会炸"——避免钳制被误认为无用的防御代码而被删除。
        //
        // 注意取值：上界必须**同时**超出 .NET Framework（int.MaxValue-1 ≈ 24.8 天）与
        // .NET Core（uint.MaxValue-1 ≈ 49.7 天）两条实现路径的阈值，故取 60 天。
        // （实测：TimeSpan.FromDays(30) 在 .NET Core 上**不会**抛——上界比按 int 推断的值更高。）
        var oversized = TimeSpan.FromDays(60);

        var act = () => new CancellationTokenSource(oversized);

        act.Should().Throw<ArgumentOutOfRangeException>(
            "这正是 P1-3 的成因：未钳制的配置派生 TimeSpan 会让重连窗口构造直接抛异常");
    }

    [Fact]
    public void ClampToCancellationTokenRange_ShouldClampEvenWhenBclWouldStillAccept_ForCrossTfmConsistency()
    {
        // 30 天在 .NET Core 上"恰好合法"，但在 .NET Framework/netstandard2.0 宿主上越界。
        // 钳制工具刻意按**较小上界**（24.8 天）收敛，保证"钳制后一定可用"与运行宿主无关。
        var thirtyDays = TimeSpan.FromDays(30);

        var clamped = TimeSpanGuards.ClampToCancellationTokenRange(thirtyDays);

        clamped.Should().BeLessThan(thirtyDays, "跨 TFM 一致优先于保留 24.8~49.7 天区间的灵敏度");
        clamped.TotalMilliseconds.Should().Be(TimeSpanGuards.MaxCancellationTokenDelayMs);
    }

    [Fact]
    public void ClampToTaskDelayRange_ShouldClampToUintMaxMilliseconds_WhenExceedsUpperBound()
    {
        var clamped = TimeSpanGuards.ClampToTaskDelayRange(TimeSpan.MaxValue);

        clamped.TotalMilliseconds.Should().Be(TimeSpanGuards.MaxTaskDelayMs,
            "Task.Delay 的上界是 uint.MaxValue-1 毫秒（约 49.7 天），与 CTS 不同，必须分别钳制");

        // Task.Delay 对越界时长在**同步阶段**即抛 ArgumentOutOfRangeException
        var act = () => { _ = Task.Delay(clamped); };
        act.Should().NotThrow("钳制后的时长必须落在 Task.Delay 的合法区间内");
    }

    [Fact]
    public void ClampToTaskDelayRange_ShouldKeepValue_WhenWithinRange()
    {
        var value = TimeSpan.FromMinutes(30);

        TimeSpanGuards.ClampToTaskDelayRange(value).Should().Be(value);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void IsValidCancellationTokenDelay_ShouldReflectRange(int milliseconds, bool expected)
    {
        TimeSpanGuards.IsValidCancellationTokenDelay(TimeSpan.FromMilliseconds(milliseconds))
            .Should().Be(expected);
    }

    [Fact]
    public void IsValidCancellationTokenDelay_ShouldBeFalse_WhenBeyondUpperBound()
    {
        TimeSpanGuards.IsValidCancellationTokenDelay(TimeSpan.FromDays(30)).Should().BeFalse();
        TimeSpanGuards.IsValidCancellationTokenDelay(TimeSpan.FromMilliseconds(TimeSpanGuards.MaxCancellationTokenDelayMs))
            .Should().BeTrue("恰好等于上界属合法（边界为闭区间）");
    }
}
