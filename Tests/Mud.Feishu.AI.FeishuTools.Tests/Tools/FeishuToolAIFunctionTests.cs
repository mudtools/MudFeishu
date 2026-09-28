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
            FeishuToolRisk.Read,
            "tenant",
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText("ok")));

        var function = new FeishuToolAIFunction(definition, SchemaOf("bitable.query_records"));

        function.Name.Should().Be("bitable.query_records");
        function.Description.Should().Contain("多维表格");

        // JsonSchema 是「参数本身」的 Schema（MEAI AIFunctionDeclaration 契约）。
        var schema = function.JsonSchema;
        schema.GetProperty("type").GetString().Should().Be("object");
        schema.GetProperty("properties").TryGetProperty("app_token", out _).Should().BeTrue();
    }

    /// <summary>
    /// MEAI 契约回归守卫：<see cref="AIFunction.JsonSchema"/> 必须是<b>纯参数 JSON Schema</b>。
    /// </summary>
    /// <remarks>
    /// 原实现把编译期<b>描述符信封</b>（<c>name</c>/<c>description</c>/<c>parameters</c>/<c>x-feishu</c>）
    /// 整个当成 <c>JsonSchema</c> 返回——信封顶层没有 JSON Schema 的 <c>type</c> 关键字，
    /// 消费方按 Schema 解释时等价于"任意 JSON 可接受"，全部参数约束静默丢失。
    /// 注：<c>AIFunctionFactory</c> 的对应物是"从方法参数推导"，故正确形状 = <c>parameters</c> 段本身。
    /// </remarks>
    [Fact]
    public void JsonSchema_ShouldBePureParameterSchema_NotTheDescriptorEnvelope()
    {
        var definition = new FeishuToolDefinition(
            "bitable.query_records", "d", [], false, FeishuToolRisk.Read, "tenant",
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText("ok")));

        var schema = new FeishuToolAIFunction(definition, SchemaOf("bitable.query_records")).JsonSchema;

        schema.GetProperty("type").GetString().Should().Be("object",
            "参数 Schema 必须有类型约束（信封顶层没有 type，这正是原缺陷）");
        schema.TryGetProperty("name", out _).Should().BeFalse("信封字段不得进入 Schema 关键字空间");
        schema.TryGetProperty("x-feishu", out _).Should().BeFalse("厂商扩展不得进入 Schema 关键字空间");
        schema.TryGetProperty("parameters", out _).Should().BeFalse("信封的 parameters 段本身才是 JsonSchema");
    }

    /// <summary>编译期常量仍须保留完整信封（注册表元数据与审计从信封读取）。</summary>
    [Fact]
    public void GeneratedConstant_ShouldStillCarryEnvelopeMetadata()
    {
        using var document = JsonDocument.Parse(SchemaOf("bitable.query_records"));
        var root = document.RootElement;

        root.GetProperty("name").GetString().Should().Be("bitable.query_records");
        root.GetProperty("x-feishu").GetProperty("required_scopes")
            .EnumerateArray().Select(e => e.GetString()).Should().Contain("bitable:app:readonly");
        root.GetProperty("x-feishu").GetProperty("risk").GetString().Should().Be("read");
    }

    [Fact]
    public async Task Invoke_WithoutAmbientContext_ShouldRefuseStructured()
    {
        var definition = new FeishuToolDefinition(
            "test.tool", "d", [], false, FeishuToolRisk.Read, "tenant",
            (_, _, _) => throw new InvalidOperationException("不应执行到 Handler"));

        var function = new FeishuToolAIFunction(definition, "{\"name\":\"test.tool\"}", contextAccessor: null);

        var result = await function.InvokeAsync(new AIFunctionArguments());
        result.Should().BeOfType<FeishuToolResult>()
            .Which.ToString().Should().StartWith("[tool_error] test.tool").And.Contain("appKey");
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
            FeishuToolRisk.Read,
            "tenant",
            (arguments, context, _) =>
            {
                seenContext = context;
                seenArguments = arguments;
                return Task.FromResult(FeishuToolResult.FromText("handler-ok"));
            });

        var function = new FeishuToolAIFunction(definition, "{\"name\":\"test.tool\"}", accessor);

        using (accessor.Begin(new FeishuToolContext("appKeyA", "conv", "chat1", "user1")))
        {
            var result = await function.InvokeAsync(new AIFunctionArguments { ["app_token"] = "bascnXxx" });
            result.Should().BeOfType<FeishuToolResult>().Which.ToString().Should().Be("handler-ok");
        }

        seenContext!.AppKey.Should().Be("appKeyA", "上下文经异步流注入（多租户隔离事实来源）");
        seenContext.ConversationKey.Should().Be("conv");
        seenArguments!["app_token"].Should().Be("bascnXxx");
    }

    [Fact]
    public void Invoke_WithInvalidSchemaConstant_ShouldFailFastOnConstruction()
    {
        var definition = new FeishuToolDefinition("x", "d", [], false, FeishuToolRisk.Read, "tenant", (_, _, _) => Task.FromResult(FeishuToolResult.FromText("ok")));

        var act = () => new FeishuToolAIFunction(definition, "not-json");

        act.Should().Throw<JsonException>("Schema 常量为编译期产物，损坏即 fail-fast（构造期解析）");
    }

    private static string SchemaOf(string toolName) => FeishuToolSchemas.SchemaByToolName[toolName];
}
