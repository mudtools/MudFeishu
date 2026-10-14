// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Tools;

/// <summary>
/// 单轮工具调用预算（<c>ToolCallBudget</c>）的中立行为用例。
/// </summary>
/// <remarks>
/// N2-d：随类型自 <c>Mud.Feishu.AI</c> 迁入 <c>Mud.AI.Agent</c>（Q1「测试随类型走」）——
/// 并发原子性用例原样搬迁；装饰器侧（越限 → 结构化错误）用例留在工具面测试工程。
/// </remarks>
public class ToolCallBudgetTests
{
    /// <summary>
    /// 计数原子：并发 TryConsume 不会超过 max。
    /// </summary>
    [Fact]
    public void TryConsume_Concurrent_ShouldNotExceedMax()
    {
        const int max = 100;
        const int threadCount = 10;
        const int callsPerThread = 20;
        var budget = new ToolCallBudget(max);

        var allowed = 0;
        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                var localAllowed = 0;
                for (var i = 0; i < callsPerThread; i++)
                {
                    if (budget.TryConsume())
                        localAllowed++;
                }

                Interlocked.Add(ref allowed, localAllowed);
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // 原子性：允许数 = max（不超过）
        allowed.Should().Be(max,
            "TryConsume 经 Interlocked.Increment 保证原子性——并发调用不允许超过 max");
        budget.Used.Should().Be(threadCount * callsPerThread,
            "Used 是实际递增次数（包括越限的递增）");
    }

    /// <summary>
    /// max = 0 视为不限制——任意次数都放行（与产品配置 0 语义一致）。
    /// </summary>
    [Fact]
    public void TryConsume_MaxZero_ShouldNotLimit()
    {
        var budget = new ToolCallBudget(0);

        for (var i = 0; i < 100; i++)
        {
            budget.TryConsume().Should().BeTrue($"第 {i + 1} 次调用不应被 0 预算拦截");
        }
    }

    /// <summary>
    /// Begin / Dispose 的环境量语义：内层作用域释放后恢复外层预算（AsyncLocal 栈式语义）。
    /// </summary>
    [Fact]
    public void Begin_ShouldRestorePreviousAmbient_OnDispose()
    {
        ToolCallBudget.Current.Should().BeNull("测试起点：无环境量预算");

        var outer = new ToolCallBudget(5);
        using var outerScope = outer.Begin();
        ToolCallBudget.Current.Should().BeSameAs(outer);

        var inner = new ToolCallBudget(1);
        using var innerScope = inner.Begin();
        ToolCallBudget.Current.Should().BeSameAs(inner, "内层 Begin 后环境量指向内层预算");

        innerScope.Dispose();
        ToolCallBudget.Current.Should().BeSameAs(outer, "内层释放后必须恢复外层预算（栈式语义）");

        outerScope.Dispose();
        ToolCallBudget.Current.Should().BeNull("外层释放后回到无预算状态");
    }
}
