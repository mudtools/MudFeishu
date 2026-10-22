// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Observability;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 错误出口载荷契约：执行器把错误写进 <c>Text</c> 而不带 <c>Error</c> 载荷时，
/// 执行链出口必须把它升级为结构化失败结果——否则宿主按 <c>Error</c> 分流的观测面会把失败记成 Succeeded。
/// </summary>
/// <remarks>
/// 该契约的产地在 <c>FeishuToolBinding.ExecuteAsync</c>（单点），执行器侧 40+ 处调用点因此**零改动**；
/// 本测试既是行为锁，也是「不得把归一逻辑搬回执行器」的反向约束。
/// </remarks>
public class ToolErrorPayloadEgressTests
{
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public ToolErrorPayloadEgressTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new NoopScope());
    }

    /// <summary>租户作用域替身（本测试只关心出口载荷，作用域行为由 FeishuToolBindingTests 覆盖）。</summary>
    private sealed class NoopScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private FeishuToolBinding CreateBinding(
        IToolExecutionAuthorizer? authorizer = null,
        IAgentToolTelemetry? telemetry = null)
        => new(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            authorizer,
            toolTelemetry: telemetry);

    private static FeishuToolDefinition Definition(FeishuToolHandler handler)
        => new("test.tool", "测试工具", ["test:read"], false, FeishuToolRisk.Read, "tenant", handler);

    private static IReadOnlyDictionary<string, object?> Args() => new Dictionary<string, object?> { ["a"] = "1" };

    /// <summary>
    /// 执行器产出「分类 + 原因」的纯文本错误 ⇒ 出口后必须带 <c>Error</c> 载荷，且分类保持原分类。
    /// </summary>
    [Fact]
    public async Task Execute_ExecutorReturnsPlainErrorText_ShouldCarryStructuredPayload()
    {
        var binding = CreateBinding();
        var errorText = FeishuToolBinding.StructuredError(
            "test.tool", ToolErrorCategory.Validation, "缺少必填参数", subtype: ToolErrorSubtype.InvalidArgs);

        var result = await binding.ExecuteAsync(
            Definition((_, _, _) => Task.FromResult(ToolResult.FromText(errorText))),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText(errorText)));

        result.Error.Should().NotBeNull("失败结果不得以「无载荷 = 成功」形态离开执行链");
        result.Error!.Category.Should().Be(FeishuToolBinding.CategoryLiteral(ToolErrorCategory.Validation));
        result.Error.Subtype.Should().Be(ToolErrorSubtype.InvalidArgs);
        result.Text.Should().Contain("缺少必填参数", "载荷升级不得吞掉模型可见的原因");
    }

    /// <summary>正常结果不得被误升级（判据只认行首错误前缀）。</summary>
    [Fact]
    public async Task Execute_SuccessText_ShouldStayWithoutPayload()
    {
        var binding = CreateBinding();

        var result = await binding.ExecuteAsync(
            Definition((_, _, _) => Task.FromResult(ToolResult.FromText("正文 [tool_error] 出现在中间不升级"))),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("正文 [tool_error] 出现在中间不升级")));

        result.Error.Should().BeNull("错误前缀只认行首——正文出现同串不是失败");
    }

    /// <summary>授权拒绝路径的自有分类不得被出口归一覆盖。</summary>
    [Fact]
    public async Task Execute_AuthorizerDeny_ShouldKeepItsOwnCategory()
    {
        var denied = new Mock<IToolExecutionAuthorizer>();
        denied
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Deny("租户未开通该工具"));

        var binding = CreateBinding(denied.Object);

        var result = await binding.ExecuteAsync(
            Definition((_, _, _) => Task.FromResult(ToolResult.FromText("never-called"))),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("never-called")));

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be(
            FeishuToolBinding.CategoryLiteral(ToolErrorCategory.Authorization),
            "出口归一只补载荷，不改写既有分类");
    }

    /// <summary>
    /// 中立遥测端口：注册后每次调用上报一次（失败带分类字面量，成功带 null）。
    /// </summary>
    [Fact]
    public async Task Execute_WithTelemetry_ShouldReportInvocationOnce()
    {
        var telemetry = new Mock<IAgentToolTelemetry>();
        var binding = CreateBinding(telemetry: telemetry.Object);
        var errorText = FeishuToolBinding.StructuredError("test.tool", "坏参数");

        await binding.ExecuteAsync(
            Definition((_, _, _) => Task.FromResult(ToolResult.FromText(errorText))),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText(errorText)));

        telemetry.Verify(
            t => t.ToolInvoked(
                "test.tool",
                false,
                It.IsAny<long>(),
                It.IsAny<bool>(),
                FeishuToolBinding.CategoryLiteral(ToolErrorCategory.Api)),
            Times.Once,
            "失败样本不得缺席（未分类错误落 api，与中立 FromUnclassifiedError 同口径）");
    }
}
