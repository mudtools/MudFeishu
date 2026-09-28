// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Mud.Feishu.Abstractions.Authentication;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMR2-P1-5：令牌存储「待清库」门测试（D10 约束的承载点）。
/// </summary>
/// <remarks>
/// 门为进程内静态状态，故本类所有用例在 <c>try/finally</c> 中调用
/// <see cref="TokenStorePurgeGate.ResetForTest"/>，避免相互污染。
/// 超时路径通过注入"当前时刻"（<c>Query(appKey, nowTimestamp)</c>）确定性覆盖，
/// 不依赖真实等待（与 <c>FeishuAppContextRetirement.Sweep(DateTimeOffset now)</c> 同一模式）。
/// </remarks>
/// <remarks>
/// <b>隔离边界说明</b>：上述 <c>try/finally</c> 只能隔离**本类内部**（xUnit 串行执行同一类的用例），
/// **不足以**隔离跨测试类：本程序集还有若干用例经 FeishuAppManager 的凭据轮换热更新间接打门，
/// 且清库是 <c>Task.Run</c> fire-and-forget、其撤门会跨越测试边界落地。
/// 前者由本程序集的 <c>AssemblyInfo</c> 禁用并行处理，后者由 <see cref="TokenStorePurgeGate"/>
/// 的世代隔离（<c>ResetForTest</c> 递增世代、跨世代撤门被忽略）处理——两者缺一都会回到随机红。
/// </remarks>
public class TokenStorePurgeGateTests
{
    private const string AppKey = "app1";

    [Fact]
    public void Query_ShouldReturnNotPending_WhenNeverMarked()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public void Query_ShouldReturnPending_AfterMark_AndNotPending_AfterRelease()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Mark(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.Pending);

            TokenStorePurgeGate.Release(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public void Query_ShouldStayPending_UntilAllLeasesReleased()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            // 两段清库（Phase-P 预清库 + 提交后二次清库）各自打门一次
            TokenStorePurgeGate.Mark(AppKey);
            TokenStorePurgeGate.Mark(AppKey);

            TokenStorePurgeGate.Release(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.Pending,
                "仍有未撤除的租约，门必须继续有效（覆盖提交窗口）");

            TokenStorePurgeGate.Release(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public void Release_ShouldBeNoOp_WhenNoLeaseExists()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            var act = () => TokenStorePurgeGate.Release(AppKey);
            act.Should().NotThrow();
            TokenStorePurgeGate.ActiveLeaseCount.Should().Be(0);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public void IsPending_ShouldReturnFalse_AfterSafetyTimeout()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Mark(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.Pending);

            // Act：把"当前时刻"推进到安全超时之后（fail-open）
            var future = Stopwatch.GetTimestamp()
                + ((long)Stopwatch.Frequency * (TokenStorePurgeGate.SafetyTimeoutSeconds + 1));

            var state = TokenStorePurgeGate.Query(AppKey, future);

            // Assert：上报一次 TimedOut 且门被摘除，恢复路径重新启用（绝不永久禁用 store 恢复）
            state.Should().Be(TokenStorePurgeGate.PurgeGateState.TimedOut);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
            TokenStorePurgeGate.ActiveLeaseCount.Should().Be(0);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public void Query_ShouldReturnNotPending_ForNullOrEmptyAppKey()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Query(null).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
            TokenStorePurgeGate.Query(string.Empty).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);

            TokenStorePurgeGate.Mark(null);
            TokenStorePurgeGate.Mark(string.Empty);
            TokenStorePurgeGate.ActiveLeaseCount.Should().Be(0, "空 appKey 不建立租约");
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    /// <summary>
    /// 世代隔离：跨越 <see cref="TokenStorePurgeGate.ResetForTest"/> 才落地的异步撤门
    /// 必须被忽略，否则它会减掉**下一个用例**刚打上的门。
    /// </summary>
    /// <remarks>
    /// 对应真实故障面：清库在 <c>Task.Run</c> 中 fire-and-forget 执行，
    /// <c>finally { Release(...) }</c> 可能晚于下一个用例的 <c>ResetForTest</c> + <c>Mark</c>。
    /// 该缺陷只靠「串行执行」**无法**根治（同一个类的相邻用例之间也会发生）。
    /// </remarks>
    [Fact]
    public void Release_FromPreviousEpoch_ShouldBeIgnored()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Mark(AppKey);

            // Act：模拟用例结束——重置（世代递增）后，上一个用例遗留的异步撤门才到达。
            TokenStorePurgeGate.ResetForTest();
            TokenStorePurgeGate.Release(AppKey);

            // Assert：遗留撤门不得留下任何影响（也不得把计数减成负数）。
            TokenStorePurgeGate.ActiveLeaseCount.Should().Be(0);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    /// <summary>
    /// 世代隔离的边界：同一世代内的撤门必须照常生效（不得因隔离而被吞掉）。
    /// </summary>
    [Fact]
    public void Release_WithinSameEpoch_ShouldStillDecrement()
    {
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Mark(AppKey);
            TokenStorePurgeGate.Mark(AppKey);

            TokenStorePurgeGate.Release(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.Pending);

            TokenStorePurgeGate.Release(AppKey);
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public void Gate_ShouldTrackLeasesPerAppKey()
    {
        const string otherKey = "app2";
        TokenStorePurgeGate.ResetForTest();
        try
        {
            TokenStorePurgeGate.Mark(AppKey);

            TokenStorePurgeGate.Query(otherKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending,
                "门按 appKey 隔离——其他应用的恢复路径不受影响");

            TokenStorePurgeGate.ActiveLeaseCount.Should().Be(1);
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }
    }
}
