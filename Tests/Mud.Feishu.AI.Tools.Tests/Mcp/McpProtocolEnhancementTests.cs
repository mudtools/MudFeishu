// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.Tools.Tests.Mcp;

/// <summary>
/// R-7 MCP 功能完善：协议层增强用例（<c>tools/list</c> 分页 / 客户端能力感知 / stdio 有界读）。
/// </summary>
/// <remarks>
/// 本类只测<b>纯协议层</b>（<see cref="FeishuMcpToolServer"/> 与 <see cref="FeishuMcpStdioHost"/>），
/// 不启 HTTP 监听（HTTP 传输的用例在 <c>McpHttpHostTests</c>）。
/// 工具面用真实的 <c>AddFeishuTools()</c> 装配、白名单经 <c>FeishuAgentOptions.Tools</c> 给出
/// ——与 MCP / 进程内 Agent 共用同一份配置。
/// </remarks>
public class McpProtocolEnhancementTests
{
    /// <summary>四个只读元工具（含 <c>feishu</c> 域 ⇒ 有 guidance / scopes 资源）。</summary>
    private static readonly string[] MetaTools =
    [
        "feishu.schema_read",
        "feishu.capability_lookup",
        "feishu.guidance_read",
        "feishu.tool_search",
    ];

    private static ServiceProvider BuildProvider(
        Action<FeishuAgentOptions>? agent = null,
        Action<FeishuMcpServerOptions>? mcp = null)
    {
        var agentOptions = new FeishuAgentOptions { Instructions = "test" };
        agent?.Invoke(agentOptions);

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(agentOptions))
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())));

        services.AddFeishuTools();
        services.AddFeishuMcpServer(mcp);
        return services.BuildServiceProvider();
    }

    private static Action<FeishuMcpServerOptions> McpConfigure(
        bool pagination = false,
        int pageSize = 50,
        bool includeGuidance = false)
        => options =>
        {
            options.AppKey = "cli_test";
            options.IncludeGuidance = includeGuidance;
            options.EnableToolsPagination = pagination;
            options.ToolsPageSize = pageSize;
        };

    private static async Task<JsonObject> HandshakeAsync(FeishuMcpToolServer server, string? capabilitiesJson = null)
    {
        var capabilities = capabilitiesJson is null ? string.Empty : ",\"capabilities\":" + capabilitiesJson;
        var response = await server.HandleMessageAsync(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"clientInfo\":{\"name\":\"t\",\"version\":\"1\"}"
            + capabilities + "}}");
        response.Should().NotBeNull();
        return JsonNode.Parse(response!)!.AsObject();
    }

    private static async Task<JsonArray> ToolsListPageAsync(FeishuMcpToolServer server, string? cursor = null)
    {
        var request = cursor is null
            ? """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""
            : "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{\"cursor\":\"" + cursor + "\"}}";

        var response = await server.HandleMessageAsync(request);
        response.Should().NotBeNull();
        return JsonNode.Parse(response!)!.AsObject()["result"]!["tools"]!.AsArray();
    }

    private static async Task<string?> ToolsListNextCursorAsync(FeishuMcpToolServer server, string? cursor = null)
    {
        var request = cursor is null
            ? """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""
            : "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{\"cursor\":\"" + cursor + "\"}}";

        var response = await server.HandleMessageAsync(request);
        var result = JsonNode.Parse(response!)!.AsObject()["result"]!.AsObject();
        return result.ContainsKey("nextCursor") ? result["nextCursor"]!.GetValue<string>() : null;
    }

    // ─────────────────────────── tools/list 分页 ───────────────────────────

    [Fact]
    public async Task ToolsList_Default_ShouldReturnAllTools_WithoutNextCursor()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = MetaTools,
            mcp: McpConfigure(pagination: false));

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await HandshakeAsync(server);

        var tools = await ToolsListPageAsync(server);
        tools.Should().HaveCount(MetaTools.Length, "分页默认关闭时必须全量返回（不改既有行为）");
        (await ToolsListNextCursorAsync(server)).Should().BeNull("全量返回没有下一页");
    }

    [Fact]
    public async Task ToolsList_WithPagination_ShouldCoverAllTools_AcrossPages()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = MetaTools,
            mcp: McpConfigure(pagination: true, pageSize: 2));

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await HandshakeAsync(server);

        var seen = new List<string>();
        string? cursor = null;
        var pages = 0;

        while (true)
        {
            var page = await ToolsListPageAsync(server, cursor);
            page.Should().HaveCountLessThanOrEqualTo(2, "每页不超过 ToolsPageSize");
            seen.AddRange(page.Select(t => t!["name"]!.GetValue<string>()));

            cursor = await ToolsListNextCursorAsync(server, cursor);
            pages++;
            if (cursor is null)
            {
                break;
            }

            pages.Should().BeLessThan(10, "游标必须单调前进，否则会死循环");
        }

        pages.Should().Be(2, "4 个工具 / 每页 2 个 = 2 页");
        seen.Should().BeEquivalentTo(
            MetaTools.Select(n => n.Replace('.', '_')),
            "多页并集必须覆盖白名单全集（分页不得丢工具）");
        seen.Should().OnlyHaveUniqueItems("跨页不得重复工具");
    }

    // ─────────────────────────── 客户端能力感知 ───────────────────────────

    [Fact]
    public async Task Initialize_WhenClientOmitsCapabilities_ShouldStillDeclareResources()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = MetaTools,
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        var response = await HandshakeAsync(server); // 无 capabilities（不完整握手）

        response["result"]!["capabilities"]!.AsObject().ContainsKey("resources").Should().BeTrue(
            "capabilities 整体缺席时保持既有行为——不得因客户端没声明而收回能力");
    }

    [Fact]
    public async Task Initialize_WhenClientDeclaresResources_ShouldDeclareResources()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = MetaTools,
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        var response = await HandshakeAsync(server, """{"resources":{}}""");

        response["result"]!["capabilities"]!.AsObject().ContainsKey("resources").Should().BeTrue();
    }

    [Fact]
    public async Task Initialize_WhenClientLacksResourcesCapability_ShouldNotDeclareResources()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = MetaTools,
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        // 客户端显式声明了 capabilities、但其中不含 resources ⇒ 服务端收回 resources 能力声明
        // （避免客户端 UI 出现永远为空的资源面板）。
        var response = await HandshakeAsync(server, """{"tools":{}}""");

        response["result"]!["capabilities"]!.AsObject().ContainsKey("resources").Should().BeFalse(
            "客户端不支持 resources 时不得声明该能力");
        response["result"]!["capabilities"]!["tools"].Should().NotBeNull("tools 能力恒在");
    }

    [Fact]
    public async Task ToolSurface_ShouldNotDependOnClientCapabilities()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = MetaTools,
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await HandshakeAsync(server, """{"tools":{}}"""); // 客户端未声明 resources

        var tools = await ToolsListPageAsync(server);
        tools.Should().HaveCount(MetaTools.Length, "工具面只由白名单决定，与客户端能力无关（同源铁律）");
    }

    // ─────────────────────────── stdio 有界读 ───────────────────────────

    [Fact]
    public async Task Stdio_ShouldRejectOversizedLine_WithMinus32600_AndContinue()
    {
        using var provider = BuildProvider(mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        var options = Options.Create(new FeishuMcpServerOptions { AppKey = "cli_test", MaxRequestBytes = 1024 });

        // ping（合法，且在未初始化时也被允许）+ 一条 2000 字符的超长行 + 又一条 ping（验证超限后循环继续）。
        var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\"}\n"
            + new string('x', 2000) + "\n"
            + "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"ping\"}\n");
        var output = new StringWriter();

        var host = new FeishuMcpStdioHost(server, input, output, logger: null, options: options);
        var handled = await host.RunAsync();

        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(3, "两条 ping 各回一条 + 超长行回一条错误");
        lines[1].Should().Contain("-32600", "超长行必须被拒（而不是读进内存或静默丢弃）");
        lines[1].Should().Contain("字符上限");
        handled.Should().Be(3);
    }

    [Fact]
    public async Task Stdio_ShouldProcessNormalLines_WithinBound()
    {
        using var provider = BuildProvider(mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        var options = Options.Create(new FeishuMcpServerOptions { AppKey = "cli_test", MaxRequestBytes = 4096 });

        var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\"}\n"
            + "\n" // 空行：忽略
            + "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"ping\"}\n");
        var output = new StringWriter();

        var host = new FeishuMcpStdioHost(server, input, output, logger: null, options: options);
        var handled = await host.RunAsync();

        handled.Should().Be(2, "空行不计入；两条 ping 各回一条");
        output.ToString().Should().NotContain("-32600");
    }
}