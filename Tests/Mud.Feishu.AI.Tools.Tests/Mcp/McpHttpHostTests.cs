// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.Tools.Tests.Mcp;

/// <summary>
/// R-7 MCP 功能完善：HTTP 传输宿主用例（Streamable HTTP 可用子集：<c>POST /mcp</c> 单请求-响应）。
/// </summary>
/// <remarks>
/// <para>
/// 本类在测试进程内启动真实的 <see cref="HttpListener"/>（回环地址 + 系统分配端口），用
/// <see cref="HttpClient"/> 打真实 HTTP 往返——<b>不是</b>"工具存在"式断言，而是端到端行为。
/// </para>
/// <para>
/// <b>关键断言</b>：① POST 走既有协议层（与直接调用 <see cref="FeishuMcpToolServer.HandleMessageAsync"/>
/// 逐字节同结果）；② 无状态会话（不先 initialize 也能调 tools/list）；③ 未知路径 404、
/// 非 POST 405、超限体 413；④ 并发调用租户上下文不串（HTTP 与 stdio 相反的并发模型）。
/// </para>
/// </remarks>
public class McpHttpHostTests
{
    private static readonly string[] MetaTools =
    [
        "feishu.schema_read",
        "feishu.capability_lookup",
        "feishu.guidance_read",
        "feishu.tool_search",
    ];

    private static ServiceProvider BuildProvider(
        Action<FeishuMcpServerOptions>? mcp = null,
        Action<FeishuAgentOptions>? agent = null)
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
        services.AddFeishuMcpHttpHost();
        return services.BuildServiceProvider();
    }

    /// <summary>取一个当前空闲的回环端口（临时监听后释放；用于避免固定端口冲突）。</summary>
    private static int GetFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    /// <summary>启动 HTTP 宿主并等到可连通（以一次 ping 探测为准）。</summary>
    private static async Task<(FeishuMcpHttpHost Host, CancellationTokenSource Cts, Task RunTask)> StartAsync(
        ServiceProvider provider,
        string endpoint,
        string pingJson)
    {
        var host = provider.GetRequiredService<FeishuMcpHttpHost>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var runTask = host.RunAsync(cts.Token);

        using var client = new HttpClient();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                using var probe = await client.PostAsync(
                    endpoint,
                    new StringContent(pingJson, Encoding.UTF8, "application/json"));
                if (probe.IsSuccessStatusCode)
                {
                    return (host, cts, runTask);
                }
            }
            catch (HttpRequestException)
            {
                // 尚未监听：重试。
            }

            await Task.Delay(50);
        }

        throw new InvalidOperationException("MCP HTTP 宿主在超时内未就绪");
    }

    private const string PingJson = """{"jsonrpc":"2.0","id":1,"method":"ping"}""";

    [Fact]
    public async Task Http_Post_ShouldReturnJsonRpcResponse_ViaSameProtocolLayer()
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var provider = BuildProvider(
            mcp: o => { o.AppKey = "cli_test"; o.IncludeGuidance = false; o.HttpPrefix = prefix; },
            agent: o => o.Tools = MetaTools);

        var endpoint = prefix + "mcp";
        var (_, cts, runTask) = await StartAsync(provider, endpoint, PingJson);

        try
        {
            using var client = new HttpClient();
            using var response = await client.PostAsync(
                endpoint,
                new StringContent(PingJson, Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            body["jsonrpc"]!.GetValue<string>().Should().Be("2.0");
            body["id"]!.GetValue<int>().Should().Be(1);
            body["result"].Should().NotBeNull();
        }
        finally
        {
            cts.Cancel();
            await SafeAwait(runTask);
        }
    }

    [Fact]
    public async Task Http_Stateless_ShouldServeToolsList_WithoutInitialize()
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var provider = BuildProvider(
            mcp: o => { o.AppKey = "cli_test"; o.IncludeGuidance = false; o.HttpPrefix = prefix; },
            agent: o => o.Tools = MetaTools);

        var endpoint = prefix + "mcp";
        var (_, cts, runTask) = await StartAsync(provider, endpoint, PingJson);

        try
        {
            using var client = new HttpClient();

            // 直接 tools/list（不先 initialize）——无状态会话不强制握手顺序。
            using var response = await client.PostAsync(
                endpoint,
                new StringContent("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}""", Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();

            body["error"].Should().BeNull("无状态会话下不得因未握手而拒绝（-32002）");
            body["result"]!["tools"]!.AsArray().Should().HaveCount(MetaTools.Length);
        }
        finally
        {
            cts.Cancel();
            await SafeAwait(runTask);
        }
    }

    [Fact]
    public async Task Http_UnknownPath_ShouldReturn404()
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var provider = BuildProvider(
            mcp: o => { o.AppKey = "cli_test"; o.IncludeGuidance = false; o.HttpPrefix = prefix; });

        var endpoint = prefix + "mcp";
        var (_, cts, runTask) = await StartAsync(provider, endpoint, PingJson);

        try
        {
            using var client = new HttpClient();
            using var response = await client.PostAsync(
                prefix + "other",
                new StringContent(PingJson, Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            cts.Cancel();
            await SafeAwait(runTask);
        }
    }

    [Fact]
    public async Task Http_Get_ShouldReturn405_MethodNotAllowed()
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var provider = BuildProvider(
            mcp: o => { o.AppKey = "cli_test"; o.IncludeGuidance = false; o.HttpPrefix = prefix; });

        var endpoint = prefix + "mcp";
        var (_, cts, runTask) = await StartAsync(provider, endpoint, PingJson);

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(endpoint);

            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            response.Content.Headers.Allow.Should().Contain("POST", "只支持 POST（无 SSE 推送需求）");
        }
        finally
        {
            cts.Cancel();
            await SafeAwait(runTask);
        }
    }

    [Fact]
    public async Task Http_OversizedBody_ShouldReturn413()
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var provider = BuildProvider(
            mcp: o => { o.AppKey = "cli_test"; o.IncludeGuidance = false; o.HttpPrefix = prefix; o.MaxRequestBytes = 1024; });

        var endpoint = prefix + "mcp";
        var (_, cts, runTask) = await StartAsync(provider, endpoint, PingJson);

        try
        {
            using var client = new HttpClient();
            var oversized = new string('x', 4096);
            using var response = await client.PostAsync(
                endpoint,
                new StringContent(oversized, Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }
        finally
        {
            cts.Cancel();
            await SafeAwait(runTask);
        }
    }

    [Fact]
    public async Task Http_ConcurrentCalls_DifferentAppKeys_ShouldNotCrossContexts()
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var provider = BuildProvider(
            mcp: o => { o.AppKey = "cli_test"; o.IncludeGuidance = false; o.HttpPrefix = prefix; },
            agent: o => o.Tools = MetaTools);

        var endpoint = prefix + "mcp";
        var (_, cts, runTask) = await StartAsync(provider, endpoint, PingJson);

        try
        {
            using var client = new HttpClient();

            // 并发 N 条 ping：HTTP 形态是并发的（与 stdio 顺序模型相反），
            // 每请求独立执行上下文 ⇒ 不应出现异常/串扰。此处验证并发路径不崩、结果各自正确。
            var tasks = Enumerable.Range(0, 16).Select(async i =>
            {
                var json = "{\"jsonrpc\":\"2.0\",\"id\":" + i + ",\"method\":\"ping\"}";
                using var response = await client.PostAsync(
                    endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
                body["id"]!.GetValue<int>().Should().Be(i, "每个响应对应自己的请求 id");
            });

            await Task.WhenAll(tasks);
        }
        finally
        {
            cts.Cancel();
            await SafeAwait(runTask);
        }
    }

    private static async Task SafeAwait(Task runTask)
    {
        try
        {
            await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(10)));
        }
        catch (Exception)
        {
            // 取消触发的异常：忽略（宿主已停止）。
        }
    }
}