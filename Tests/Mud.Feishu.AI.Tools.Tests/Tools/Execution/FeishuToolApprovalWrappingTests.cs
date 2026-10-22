// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.Tools.Tests.Tools.Execution;

/// <summary>
/// P4-1：写类工具必须被 MEAI <see cref="ApprovalRequiredAIFunction"/> 包裹，
/// 使「人工确认」由 MAF 审批管线在<b>工具调用之前</b>拦截（批准只能来自宿主，模型无法自批）。
/// </summary>
/// <remarks>
/// <para>
/// 详见 <c>FeishuToolAIFunction.ApplyApprovalGate</c> 的实测依据：包装后首轮模型回应是
/// <c>ToolApprovalRequestContent</c>、工具不执行；须由宿主回灌
/// <c>request.CreateResponse(approved: true, ...)</c> 才在下一轮真正执行。
/// </para>
/// <para>
/// <b>T4 / D4 后的读取口径</b>：工具面返回的每个函数都套了预算装饰器
/// （<c>ToolBudgetAIFunction</c>，单轮调用上限），而审批标记必须在<b>最外层</b>才被 MAF 看见——
/// 故"某工具是否进入审批"的判据是<b>不剥壳</b>的顶层类型，而"工具桥形态"的判据需先
/// <see cref="UnwrapBudget"/> 剥掉预算装饰器（本类用后者表达"只读工具没被审批标记包裹"）。
/// </para>
/// </remarks>
public class FeishuToolApprovalWrappingTests
{
    private const string ReadTool = "bitable.query_records";
    private const string WriteTool = "bitable.record_batch_create";

    /// <summary>剥掉最内层的预算装饰器（T4 / D4：它位于审批标记之内、工具桥之外）。</summary>
    private static AIFunction UnwrapBudget(AIFunction function)
        => function is ToolBudgetAIFunction budget ? budget.Inner : function;

    private static FeishuToolDefinition Definition(string name, bool isWrite) => new(
        Name: name,
        Description: "desc",
        RequiredScopes: [],
        IsWrite: isWrite,
        Risk: isWrite ? FeishuToolRisk.Write : FeishuToolRisk.Read,
        Identity: FeishuToolIdentityNames.Tenant,
        Handler: (_, _, _) => Task.FromResult(ToolResult.FromText("ok")));

    private static IReadOnlyList<AIFunction> GetTools()
    {
        var registry = new FeishuToolRegistry()
            .Register(Definition(ReadTool, isWrite: false))
            .Register(Definition(WriteTool, isWrite: true));
        registry.MapTool(ReadTool);
        registry.MapTool(WriteTool);

        var services = new ServiceCollection();
        services.AddSingleton(registry);
        using var provider = services.BuildServiceProvider();

        return new FeishuToolsToolSource().GetTools(provider);
    }

    [Fact]
    public void GetTools_ShouldWrapWriteTool_WithApprovalRequiredAIFunction()
    {
        var tools = GetTools();

        tools.Should().HaveCount(2);
        UnwrapBudget(tools.Single(t => t.Name == ReadTool)).Should().BeOfType<FeishuToolAIFunction>(
            "只读工具不进入审批流程（无副作用，无需人工确认）——剥掉预算装饰器后应直接是工具桥");
        tools.Single(t => t.Name == ReadTool).Should().NotBeOfType<ApprovalRequiredAIFunction>(
            "只读工具的顶层不得是审批标记（MAF 只看顶层类型）");
        tools.Single(t => t.Name == WriteTool).Should().BeOfType<ApprovalRequiredAIFunction>(
            "写工具必须包装——否则 MAF 的 FunctionInvokingChatClient 不会把它转成审批请求，"
            + "似曾相识的『模型自批复』面又会回来");
    }

    [Fact]
    public void WrappedWriteTool_ShouldKeepNameDescriptionAndSchema()
    {
        // 包装层不得破坏工具契约：模型仍须看到同一套 name/description/参数 Schema。
        var tools = GetTools();
        var wrapped = tools.Single(t => t.Name == WriteTool);
        var unwrapped = new FeishuToolAIFunction(
            Definition(WriteTool, isWrite: true), FeishuToolSchemas.SchemaByToolName[WriteTool]);

        wrapped.Name.Should().Be(unwrapped.Name);
        wrapped.Description.Should().Be(unwrapped.Description);
        wrapped.JsonSchema.GetRawText().Should().Be(unwrapped.JsonSchema.GetRawText(),
            "ApprovalRequiredAIFunction 必须原样委派 Schema——否则模型拿到的参数约束会被改变");
    }

    [Fact]
    public void GetTools_ShouldReturnEmpty_WhenRegistryNotRegistered()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        new FeishuToolsToolSource().GetTools(provider).Should().BeEmpty("未注册工具注册表时无可用工具");
    }
}
