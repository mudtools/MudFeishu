// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Tools.Tests.Tools.Execution;

/// <summary>
/// T4 / D4 单轮工具预算行为用例。
/// </summary>
public class ToolBudgetTests
{
    /// <summary>
    /// 越限时应返回策略错误（不抛异常、不重复计费）。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_OverBudget_ShouldReturnPolicyError_NotThrow()
    {
        // Arrange：预算 = 1，第一次调用消费后第二次越限
        var inner = new MockAIFunction("test.echo");
        var budget = new ToolCallBudget(1);
        using var scope = budget.Begin();
        var decorated = new ToolBudgetAIFunction(inner);

        // Act：第一次调用——消费预算
        var result1 = await decorated.InvokeAsync(new AIFunctionArguments());
        result1.Should().Be("ok");

        // 第二次调用——越限
        var result2 = await decorated.InvokeAsync(new AIFunctionArguments());

        // Assert：越限不抛异常，返回结构化错误
        result2.Should().BeOfType<ToolResult>();
        var errorResult = (ToolResult)result2!;
        errorResult.Text.Should().Contain("tool_error");
        errorResult.Text.Should().Contain("test.echo");
        errorResult.Text.Should().Contain("tool_budget_exceeded");
        errorResult.Text.Should().Contain("预算已用尽");
    }

    /// <summary>
    /// 预算随 Begin/Dispose 重置：新一轮从 0 开始。
    /// </summary>
    /// <remarks>
    /// N2-d：预算类型自身的语义用例（并发原子性 / 0=不限制 / 环境量栈式恢复）已随
    /// <c>ToolCallBudget</c> 迁入 <c>Tests/Mud.AI.Agent.Tests/Tools/ToolCallBudgetTests</c>；
    /// 本文件只留<b>装饰器</b>侧用例（越限 → 结构化错误、透传、不可重试）。
    /// </remarks>
    [Fact]
    public async Task Budget_ShouldReset_PerRun()
    {
        var inner = new MockAIFunction("test.echo");
        var decorated = new ToolBudgetAIFunction(inner);

        // 第一轮：预算 = 2
        using (var scope1 = new ToolCallBudget(2).Begin())
        {
            _ = await decorated.InvokeAsync(new AIFunctionArguments());
            _ = await decorated.InvokeAsync(new AIFunctionArguments());
            // 第三次越限
            var over = await decorated.InvokeAsync(new AIFunctionArguments());
            over.Should().BeOfType<ToolResult>();
        }

        // 第二轮：新预算 = 2，从 0 开始
        using (var scope2 = new ToolCallBudget(2).Begin())
        {
            _ = await decorated.InvokeAsync(new AIFunctionArguments());
            _ = await decorated.InvokeAsync(new AIFunctionArguments());
            // 第三次越限
            var over = await decorated.InvokeAsync(new AIFunctionArguments());
            over.Should().BeOfType<ToolResult>();
        }
    }

    /// <summary>
    /// max = 0 视为不限制——不拦截。
    /// </summary>
    [Fact]
    public async Task TryConsume_MaxZero_ShouldNotLimit()
    {
        var inner = new MockAIFunction("test.echo");
        var budget = new ToolCallBudget(0);
        using var scope = budget.Begin();
        var decorated = new ToolBudgetAIFunction(inner);

        // 100 次调用都不应越限
        for (var i = 0; i < 100; i++)
        {
            var result = await decorated.InvokeAsync(new AIFunctionArguments());
            result.Should().Be("ok", $"第 {i + 1} 次调用不应被 0 预算拦截");
        }
    }

    /// <summary>
    /// 预算未设置（Current = null）时装饰器透传不拦截。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NoBudget_ShouldPassthrough()
    {
        var inner = new MockAIFunction("test.echo");
        var decorated = new ToolBudgetAIFunction(inner);

        // 不 Begin 预算——Current 为 null
        ToolCallBudget.Current.Should().BeNull();

        // 任意次数都不拦截
        for (var i = 0; i < 50; i++)
        {
            var result = await decorated.InvokeAsync(new AIFunctionArguments());
            result.Should().Be("ok");
        }
    }

    /// <summary>
    /// 越限结果应标记 retryable=false（模型不应重试）。
    /// </summary>
    [Fact]
    public async Task OverBudget_ShouldBeNonRetryable()
    {
        var inner = new MockAIFunction("test.echo");
        var budget = new ToolCallBudget(1);
        using var scope = budget.Begin();
        var decorated = new ToolBudgetAIFunction(inner);

        _ = await decorated.InvokeAsync(new AIFunctionArguments());
        var over = await decorated.InvokeAsync(new AIFunctionArguments());

        var errorResult = over as ToolResult;
        errorResult.Should().NotBeNull();
        errorResult!.Text.Should().Contain("tool_budget_exceeded");
        // 文案中应明确「不要再次调用工具」——模型不可重试
        errorResult.Text.Should().Contain("不要再次调用工具");
    }

    /// <summary>
    /// 替身 AIFunction：返回固定字符串 "ok"。
    /// </summary>
    private sealed class MockAIFunction : AIFunction
    {
        private readonly string _name;

        public MockAIFunction(string name) => _name = name;

        public override string Name => _name;
        public override string Description => "test";
        public override JsonElement JsonSchema => default;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<object?>("ok");
    }
}
