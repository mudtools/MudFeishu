// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.Tools.Tests.Mcp;

/// <summary>
/// <c>initialize</c> 的运行时自描述（E-2）：客户端握手期即可探测规模与协议口径，无需先 <c>tools/list</c>。
/// </summary>
/// <remarks>
/// 这些键是外部探测脚本的输入契约，故除行为断言外还锁死<b>键集合</b>——增删改键都属于破坏性变更。
/// </remarks>
public class McpInitializeCapabilitiesTests
{
    private static ServiceProvider BuildProvider(bool includeGuidance = true)
    {
        var services = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions
            {
                Instructions = "test",

                // 元工具不依赖任何飞书客户端，故白名单只放它就能得到非空工具面（与规模无关的装配）。
                Tools = ["feishu.schema_read"],
            }))
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())));

        services.AddFeishuTools();
        services.AddFeishuMcpServer(options =>
        {
            options.AppKey = "cli_test";
            options.IncludeGuidance = includeGuidance;
        });

        return services.BuildServiceProvider();
    }

    private static async Task<JsonObject> SendAsync(FeishuMcpToolServer server, string request)
    {
        var response = await server.HandleMessageAsync(request);
        response.Should().NotBeNull();
        return JsonNode.Parse(response!)!.AsObject();
    }

    private static Task<JsonObject> InitializeAsync(FeishuMcpToolServer server)
        => SendAsync(
            server,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"t","version":"1"}}}""");

    private static JsonObject Experimental(JsonObject initializeResponse)
        => initializeResponse["result"]!["capabilities"]!["experimental"]!["feishu"]!.AsObject();

    [Fact]
    public async Task Initialize_ShouldExposeExperimentalSelfDescription()
    {
        var server = BuildProvider().GetRequiredService<FeishuMcpToolServer>();

        var node = Experimental(await InitializeAsync(server));

        node["protocolRevision"]!.GetValue<string>().Should().Be(McpProtocol.DefaultVersion);
        node["toolCount"]!.GetValue<int>().Should().BeGreaterThan(0, "真实装配下工具数不可能为 0（为 0 说明装配失败被吞）");
        node["domainCount"]!.GetValue<int>().Should().BeGreaterThan(0);
        node["guidanceIncluded"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Initialize_ToolCount_ShouldMatchToolsList()
    {
        var server = BuildProvider().GetRequiredService<FeishuMcpToolServer>();

        var reported = Experimental(await InitializeAsync(server))["toolCount"]!.GetValue<int>();

        var listed = await SendAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");
        var actualCount = listed["result"]!["tools"]!.AsArray().Count;

        reported.Should().Be(actualCount, "自描述与 tools/list 必须同源同值——两处不一致会让探测脚本误判工具面");
    }

    [Fact]
    public async Task Initialize_ExperimentalKeys_ShouldBeStable()
    {
        var server = BuildProvider().GetRequiredService<FeishuMcpToolServer>();

        var node = Experimental(await InitializeAsync(server));

        node.Select(pair => pair.Key).Should().BeEquivalentTo(
            [
                "protocolRevision",
                "toolCount",
                "domainCount",
                "writeToolCount",
                "highRiskWriteToolCount",
                "resourceCount",
                "guidanceIncluded",
            ],
            "键集合是外部探测脚本的契约：增删改键必须同批更新文档与探测脚本（此处锁死）");
    }

    [Fact]
    public async Task Initialize_WithoutGuidance_ShouldReportFalse()
    {
        var server = BuildProvider(includeGuidance: false).GetRequiredService<FeishuMcpToolServer>();

        Experimental(await InitializeAsync(server))["guidanceIncluded"]!.GetValue<bool>().Should().BeFalse();
    }
}
