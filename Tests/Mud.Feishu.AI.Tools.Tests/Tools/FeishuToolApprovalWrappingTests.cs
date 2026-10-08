// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// P4-1：写类工具必须被 MEAI <see cref="ApprovalRequiredAIFunction"/> 包裹，
/// 使「人工确认」由 MAF 审批管线在<b>工具调用之前</b>拦截（批准只能来自宿主，模型无法自批）。
/// </summary>
/// <remarks>
/// 详见 <c>FeishuToolAIFunction.ApplyApprovalGate</c> 的实测依据：包装后首轮模型回应是
/// <c>ToolApprovalRequestContent</c>、工具不执行；须由宿主回灌
/// <c>request.CreateResponse(approved: true, ...)</c> 才在下一轮真正执行。
/// </remarks>
public class FeishuToolApprovalWrappingTests
{
    private const string ReadTool = "bitable.query_records";
    private const string WriteTool = "bitable.add_record";

    private static FeishuToolDefinition Definition(string name, bool isWrite) => new(
        Name: name,
        Description: "desc",
        RequiredScopes: [],
        IsWrite: isWrite,
        Risk: isWrite ? FeishuToolRisk.Write : FeishuToolRisk.Read,
        Identity: FeishuToolIdentityNames.Tenant,
        Handler: (_, _, _) => Task.FromResult(FeishuToolResult.FromText("ok")));

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
        tools.Single(t => t.Name == ReadTool).Should().BeOfType<FeishuToolAIFunction>(
            "只读工具不进入审批流程（无副作用，无需人工确认）");
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
