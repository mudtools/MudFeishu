// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// AIFunction 桥接测试：Schema 常量注入、异步流上下文读取、缺失上下文结构化拒绝。
/// </summary>
public class FeishuToolAIFunctionTests
{
    [Fact]
    public void NameDescriptionAndSchema_ShouldComeFromDefinitionAndGeneratedConstant()
    {
        var definition = new FeishuToolDefinition(
            "bitable.query_records",
            "按条件查询多维表格记录",
            ["bitable:app:readonly"],
            false,
            (_, _, _) => Task.FromResult("ok"));

        var function = new FeishuToolAIFunction(definition, SchemaOf("bitable.query_records"));

        function.Name.Should().Be("bitable.query_records");
        function.Description.Should().Contain("多维表格");

        var schema = function.JsonSchema;
        schema.GetProperty("name").GetString().Should().Be("bitable.query_records");
        schema.GetProperty("x-feishu").GetProperty("required_scopes")
            .EnumerateArray().Select(e => e.GetString()).Should().Contain("bitable:app:readonly");
    }

    [Fact]
    public async Task Invoke_WithoutAmbientContext_ShouldRefuseStructured()
    {
        var definition = new FeishuToolDefinition(
            "test.tool", "d", [], false, (_, _, _) => throw new InvalidOperationException("不应执行到 Handler"));

        var function = new FeishuToolAIFunction(definition, "{\"name\":\"test.tool\"}", contextAccessor: null);

        var result = await function.InvokeAsync(new AIFunctionArguments());
        result.Should().BeOfType<string>()
            .Which.Should().StartWith("[tool_error] test.tool").And.Contain("appKey");
    }

    [Fact]
    public async Task Invoke_WithAmbientContext_ShouldReachHandler_AndReturnText()
    {
        var accessor = new FeishuToolContextAccessor();
        FeishuToolContext? seenContext = null;
        IReadOnlyDictionary<string, object?>? seenArguments = null;
        var definition = new FeishuToolDefinition(
            "test.tool",
            "d",
            [],
            false,
            (arguments, context, _) =>
            {
                seenContext = context;
                seenArguments = arguments;
                return Task.FromResult("handler-ok");
            });

        var function = new FeishuToolAIFunction(definition, "{\"name\":\"test.tool\"}", accessor);

        using (accessor.Begin(new FeishuToolContext("appKeyA", "conv", "chat1", "user1")))
        {
            var result = await function.InvokeAsync(new AIFunctionArguments { ["app_token"] = "bascnXxx" });
            result.Should().Be("handler-ok");
        }

        seenContext!.AppKey.Should().Be("appKeyA", "上下文经异步流注入（多租户隔离事实来源）");
        seenContext.ConversationKey.Should().Be("conv");
        seenArguments!["app_token"].Should().Be("bascnXxx");
    }

    [Fact]
    public void Invoke_WithInvalidSchemaConstant_ShouldFailFastOnSchemaAccess()
    {
        var definition = new FeishuToolDefinition("x", "d", [], false, (_, _, _) => Task.FromResult("ok"));

        var function = new FeishuToolAIFunction(definition, "not-json");
        var act = () => function.JsonSchema;

        act.Should().Throw<JsonException>("Schema 常量为编译期产物，损坏即 fail-fast");
    }

    private static string SchemaOf(string toolName) => FeishuToolSchemas.SchemaByToolName[toolName];
}
