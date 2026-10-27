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
/// MCP Streamable HTTP 传输适配层用例：会话生命周期、租户绑定的 fail-closed 判据、
/// 以及「传输层只做拒绝、不改执行链」这条边界。
/// </summary>
/// <remarks>
/// <para>
/// 每条判据都成对给出绿/红两极：只断言「坏请求被拒」会漏掉「把好请求一并拒了」的过度收紧
/// ——那会让端点看起来健康、真实客户端一个请求都进不来。
/// </para>
/// <para>
/// 零网络：适配器消费的是请求/响应快照，不起监听、不引 Web 框架依赖。
/// </para>
/// </remarks>
public class FeishuMcpHttpHandlerTests
{
    private const string SchemaReadTool = "feishu.schema_read";
    private const string SchemaReadMcpName = "feishu_schema_read";
    private const string TenantA = "cli_tenant_a";
    private const string TenantB = "cli_tenant_b";
    private const string PerRequestHint = nameof(FeishuMcpHttpOptions.TenantBoundPerRequest);
    private const string ToolsListBody = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}";
    private const string NotificationBody = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}";

    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ServiceProvider BuildProvider(
        Action<FeishuMcpServerOptions>? mcp = null,
        Mock<IToolExecutionAuthorizer>? authorizer = null)
    {
        var agentOptions = new FeishuAgentOptions { Instructions = "test", Tools = [SchemaReadTool] };

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(agentOptions))
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())));

        if (authorizer is not null)
        {
            services.AddSingleton(authorizer.Object);
        }

        services.AddFeishuTools();
        services.AddFeishuMcpServer(mcp);
        return services.BuildServiceProvider();
    }

    /// <summary>按配置构造适配器，并把时钟接进会话目录（空闲回收判据可推进，不用等真实时间）。</summary>
    private FeishuMcpHttpHandler BuildHandler(
        IServiceProvider provider,
        Action<FeishuMcpHttpOptions>? http = null)
    {
        var options = provider.GetRequiredService<IOptions<FeishuMcpServerOptions>>().Value;
        http?.Invoke(options.Http);
        options.Validate();

        return new FeishuMcpHttpHandler(
            provider.GetRequiredService<FeishuMcpToolServer>(),
            options,
            sessions: null,
            utcNow: () => _now,
            logger: null);
    }

    private static McpHttpRequest Post(
        string? body,
        string? appKey = TenantA,
        string? sessionId = null,
        string? protocolVersion = null,
        string? origin = null,
        string? contentType = "application/json",
        string? accept = null,
        string? userId = null)
        => new("POST")
        {
            Body = body,
            AppKey = appKey,
            SessionId = sessionId,
            ProtocolVersion = protocolVersion,
            Origin = origin,
            ContentType = contentType,
            Accept = accept,
            UserId = userId,
        };

    private static McpHttpRequest Delete(string? appKey = TenantA, string? sessionId = null)
        => new("DELETE") { AppKey = appKey, SessionId = sessionId };

    private static string InitializeBody(string protocolVersion = "2025-06-18")
        => "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
        + protocolVersion + "\",\"clientInfo\":{\"name\":\"t\",\"version\":\"1\"},\"capabilities\":{}}}";

    private static string ToolsCallBody(string name)
        => "{\"jsonrpc\":\"2.0\",\"id\":9,\"method\":\"tools/call\",\"params\":{\"name\":\""
        + name + "\",\"arguments\":{\"module\":\"bitable\"}}}";

    private static JsonObject Envelope(McpHttpResponse response)
    {
        response.Body.Should().NotBeNullOrWhiteSpace("200 响应必须带 JSON-RPC 信封");
        return JsonNode.Parse(response.Body!)!.AsObject();
    }

    private static string Reason(McpHttpResponse response)
        => JsonNode.Parse(response.Body!)!["error"]!.GetValue<string>();

    private async Task<string> InitializeAsync(
        FeishuMcpHttpHandler handler,
        string? appKey = TenantA,
        string? userId = null,
        string protocolVersion = "2025-06-18")
    {
        var response = await handler.HandleAsync(Post(InitializeBody(protocolVersion), appKey, userId: userId));

        response.StatusCode.Should().Be(200, "握手是后续所有请求的前提");
        return response.Headers[McpHttpHeaders.SessionId];
    }

    private static Mock<IToolExecutionAuthorizer> Authorizer(
        Action<FeishuToolContext> observe,
        AuthorizationResult decision)
    {
        var authorizer = new Mock<IToolExecutionAuthorizer>();
        authorizer
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                IReadOnlyList<string> __,
                bool ___,
                IReadOnlyDictionary<string, object?> ____,
                FeishuToolContext context,
                CancellationToken _____) => observe(context))
            .ReturnsAsync(decision);
        return authorizer;
    }

    // ─────────────────────────── 装配与握手 ───────────────────────────

    /// <summary>装配入口必须真的把 HTTP 适配器接进容器——「配置写好了但没人解析」等于端点不存在。</summary>
    [Fact]
    public void AddFeishuMcpServer_ShouldRegisterHttpHandler()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);

        provider.GetRequiredService<FeishuMcpHttpHandler>().Should().NotBeNull();
    }

    [Fact]
    public async Task Initialize_ShouldIssueSessionId_AndEchoNegotiatedVersion()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(InitializeBody()));

        response.StatusCode.Should().Be(200);
        var sessionId = response.Headers[McpHttpHeaders.SessionId];
        sessionId.Should().NotBeNullOrWhiteSpace("会话标识只在这里下发：后续每条请求都靠它定位绑定的租户");
        sessionId.Should().HaveLength(32, "标识由 GUID 生成，必须不可猜");
        Envelope(response)["result"]!["protocolVersion"]!.GetValue<string>().Should().Be("2025-06-18");
        handler.SessionCount.Should().Be(1);
    }

    [Theory]
    [InlineData("2024-11-05", "2024-11-05")]
    [InlineData("2025-03-26", "2025-03-26")]
    [InlineData("2099-01-01", "2025-06-18")]
    public async Task Initialize_ShouldBindSessionToNegotiatedVersion(string requested, string negotiated)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler, protocolVersion: requested);

        Envelope(await handler.HandleAsync(Post(
            ToolsListBody,
            sessionId: sessionId,
            protocolVersion: negotiated)))["result"]!
            .AsObject().ContainsKey("tools").Should().BeTrue(
                "后续请求按会话握手结果校验版本；闭集外的请求版本已回落到默认版本，不会卡在头校验上");
    }

    [Fact]
    public async Task ToolsList_ShouldReachTheServedToolSurface()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler);

        var response = await handler.HandleAsync(Post(ToolsListBody, sessionId: sessionId));

        response.StatusCode.Should().Be(200);
        response.ContentType.Should().Be(McpHttpResponse.JsonContentType);
        Envelope(response)["result"]!["tools"]!.AsArray()
            .Select(static t => t!["name"]!.GetValue<string>())
            .Should().Equal([SchemaReadMcpName], "工具面与 stdio 共用同一份白名单（不留第二条启用通道）");
    }

    /// <summary>会话绑定的租户与用户必须落到执行链上下文里——否则 HTTP 通道只是换了个入口的「默认应用兜底」。</summary>
    [Fact]
    public async Task ToolsCall_ShouldCarrySessionTenantAndUserIntoExecutionChain()
    {
        FeishuToolContext? seen = null;
        var authorizer = Authorizer(context => seen = context, AuthorizationResult.Allowed);

        using var provider = BuildProvider(
            mcp: o =>
            {
                o.AppKey = string.Empty;
                o.Http.TenantBoundPerRequest = true;
            },
            authorizer: authorizer);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler, appKey: TenantB, userId: "ou_http");

        var response = await handler.HandleAsync(
            Post(ToolsCallBody(SchemaReadMcpName), appKey: TenantB, sessionId: sessionId));

        response.StatusCode.Should().Be(200);
        seen.Should().NotBeNull("工具调用必须走既有执行链（授权/净化/审计都在其内）");
        seen!.AppKey.Should().Be(TenantB, "租户身份取会话，而不是进程配置（进程配置此处刻意留空）");
        seen.UserId.Should().Be("ou_http");
    }

    /// <summary>交错使用两条会话时，上下文必须各归各的租户（AsyncLocal 泄漏 = 跨租户串号）。</summary>
    [Fact]
    public async Task InterleavedSessions_ShouldNotLeakTenantAcrossCalls()
    {
        var seen = new List<string?>();
        var authorizer = Authorizer(context => seen.Add(context.AppKey), AuthorizationResult.Allowed);

        using var provider = BuildProvider(
            mcp: o =>
            {
                o.AppKey = string.Empty;
                o.Http.TenantBoundPerRequest = true;
            },
            authorizer: authorizer);
        var handler = BuildHandler(provider);

        var sessionA = await InitializeAsync(handler, appKey: TenantA);
        var sessionB = await InitializeAsync(handler, appKey: TenantB);

        foreach (var (appKey, sessionId) in new[]
        {
            (TenantA, sessionA), (TenantB, sessionB), (TenantA, sessionA),
        })
        {
            (await handler.HandleAsync(Post(ToolsCallBody(SchemaReadMcpName), appKey: appKey, sessionId: sessionId)))
                .StatusCode.Should().Be(200);
        }

        seen.Should().Equal([TenantA, TenantB, TenantA], "交错调用不得共享租户上下文");
    }

    // ─────────────────────────── 租户身份判据 ───────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_ShouldReject401_WhenIdentityMissing(string? appKey)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(InitializeBody(), appKey: appKey));

        response.StatusCode.Should().Be(401, "HTTP 通道禁止默认应用兜底：没认证出身份就没有可服务的租户");
        Reason(response).Should().Contain(nameof(McpHttpRequest.AppKey));
        handler.SessionCount.Should().Be(0, "拒绝发生在建会话之前，不留下任何驻留状态");
    }

    [Fact]
    public async Task Post_ShouldReject403_WhenTenantNotInAllowList()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.AllowedAppKeys.Add("cli_other"));

        (await handler.HandleAsync(Post(InitializeBody()))).StatusCode.Should().Be(403);
        handler.SessionCount.Should().Be(0);
    }

    [Fact]
    public async Task Post_ShouldReject403_WhenProcessIsBoundToOneTenant()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(InitializeBody(), appKey: TenantB));

        response.StatusCode.Should().Be(403,
            "未显式声明逐请求绑租户时，换一个 appKey 进来等于用 A 租户装配的端点读 B 租户的数据");
        Reason(response).Should().Contain(PerRequestHint, "拒绝文案要指向可改的配置项");
    }

    [Fact]
    public async Task Post_ShouldAcceptAnotherTenant_WhenBoundPerRequestIsEnabled()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.TenantBoundPerRequest = true);

        (await handler.HandleAsync(Post(InitializeBody(), appKey: TenantB)))
            .StatusCode.Should().Be(200, "显式打开逐请求绑租户后，身份以认证层为准");
    }

    // ─────────────────────────── 会话归属判据 ───────────────────────────

    [Fact]
    public async Task Post_ShouldReject400_WhenSessionIdMissing()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        await InitializeAsync(handler);

        var response = await handler.HandleAsync(Post(ToolsListBody));

        response.StatusCode.Should().Be(400, "除握手外的每条请求都必须自带会话标识");
        Reason(response).Should().Contain(McpHttpHeaders.SessionId);
    }

    [Fact]
    public async Task Post_ShouldReject404_WhenSessionIsUnknown()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(ToolsListBody, sessionId: "no-such-session"));

        response.StatusCode.Should().Be(404);
        Reason(response).Should().Contain("initialize", "重连指引：404 的下一步是重新握手");
    }

    [Fact]
    public async Task Post_ShouldReject403_WhenSessionBelongsToAnotherTenant()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.TenantBoundPerRequest = true);
        var sessionA = await InitializeAsync(handler, appKey: TenantA);

        var response = await handler.HandleAsync(Post(ToolsListBody, appKey: TenantB, sessionId: sessionA));

        response.StatusCode.Should().Be(403, "会话标识泄漏不得变成切租户的凭据——标识与身份不符就整体拒绝");
        handler.SessionCount.Should().Be(1, "拒用不等于删除：A 租户自己的会话仍然可用");
    }

    [Fact]
    public async Task Delete_ShouldReject403_WhenSessionBelongsToAnotherTenant()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.TenantBoundPerRequest = true);
        var sessionA = await InitializeAsync(handler, appKey: TenantA);

        (await handler.HandleAsync(Delete(TenantB, sessionA))).StatusCode.Should().Be(403);
        handler.SessionCount.Should().Be(1, "跨租户终止通道必须不存在");
    }

    // ─────────────────────────── 传输头判据 ───────────────────────────

    [Fact]
    public async Task Post_ShouldReject403_WhenOriginNotAllowed()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(InitializeBody(), origin: "https://evil.example"));

        response.StatusCode.Should().Be(403, "空清单 = 拒绝一切带 Origin 的请求（fail-closed）");
        Reason(response).Should().NotContain("evil", "不点名允许清单，避免把探测变成可观测的反馈");
    }

    [Fact]
    public async Task Post_ShouldAcceptAllowlistedOrigin_AndStillWorkWithoutOrigin()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.AllowedOrigins.Add("https://agent.example"));

        (await handler.HandleAsync(Post(InitializeBody(), origin: "https://agent.example")))
            .StatusCode.Should().Be(200);
        (await handler.HandleAsync(Post(InitializeBody())))
            .StatusCode.Should().Be(200, "服务端 Agent/CLI 不发 Origin，默认形态对它们零影响");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text/plain")]
    [InlineData("application/x-www-form-urlencoded")]
    public async Task Post_ShouldReject415_WhenContentTypeIsNotJson(string? contentType)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        (await handler.HandleAsync(Post(InitializeBody(), contentType: contentType)))
            .StatusCode.Should().Be(415);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    public async Task Post_ShouldReject406_WhenAcceptRefusesJson(string accept)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        (await handler.HandleAsync(Post(InitializeBody(), accept: accept)))
            .StatusCode.Should().Be(406);
    }

    /// <summary>
    /// 只声明 <c>text/event-stream</c> 的客户端必须得到 406：本实现不产出 SSE，
    /// 放行它会返回一个客户端明确拒绝过的媒体类型（"看起来成功"的往返比明确拒绝更难排障）。
    /// </summary>
    [Fact]
    public async Task Post_ShouldReject406_WhenAcceptIsEventStreamOnly()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(InitializeBody(), accept: "text/event-stream"));

        response.StatusCode.Should().Be(406);
        Reason(response).Should().Contain("application/json", "拒绝文案必须指出可修的 Accept 形态");
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json, text/event-stream")]
    [InlineData("*/*")]
    [InlineData(null)]
    public async Task Post_ShouldAcceptJsonCapableAccept(string? accept)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        (await handler.HandleAsync(Post(InitializeBody(), accept: accept)))
            .StatusCode.Should().Be(200);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("OPTIONS")]
    public async Task NonPostOrDelete_ShouldReject405AndAdvertiseAllow(string httpMethod)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(new McpHttpRequest(httpMethod) { AppKey = TenantA });

        response.StatusCode.Should().Be(405, "GET 在 Streamable HTTP 里只用于服务端推送流，本实现没有推送能力");
        response.Headers[McpHttpHeaders.Allow].Should().Be(McpHttpHeaders.AllowedMethods);
    }

    // ─────────────────────────── 正文形态判据 ───────────────────────────

    [Fact]
    public async Task Post_ShouldReject400_WhenBodyIsEmpty()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        (await handler.HandleAsync(Post("  "))).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Post_ShouldReject400_WhenBodyIsNotJson()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        (await handler.HandleAsync(Post("{ not json"))).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Post_ShouldReject400_OnBatchRequest()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(
            Post("""[{"jsonrpc":"2.0","id":1,"method":"initialize"}]"""));

        response.StatusCode.Should().Be(400,
            "本实现只处理单条消息：批量语义要逐条回执，伪装成支持会让客户端拿不到它期望的那条");
    }

    [Fact]
    public async Task Post_ShouldNotThrow_WhenMethodFieldHasWrongType()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler);

        var response = await handler.HandleAsync(
            Post("""{"jsonrpc":"2.0","id":3,"method":123}""", sessionId: sessionId));

        response.StatusCode.Should().Be(200, "字段类型不符属协议层错误，不是传输层拒绝");
        Envelope(response)["error"]!["code"]!.GetValue<int>().Should().Be(-32600);
    }

    [Fact]
    public async Task Post_Notification_ShouldReturn202WithoutBody()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler);

        var response = await handler.HandleAsync(Post(NotificationBody, sessionId: sessionId));

        response.StatusCode.Should().Be(202, "通知没有响应体：回一条 200 空信封等于伪造一次往返");
        response.Body.Should().BeNull();
        response.ContentType.Should().BeNull();
    }

    /// <summary>
    /// 以通知形态到达的 <c>initialize</c>：202 无正文，但会话标识必须随响应头下发——
    /// 否则客户端拿不到续约凭据，握手"成功"却不可用（下一条请求只能撞 400）。
    /// </summary>
    [Fact]
    public async Task Initialize_AsNotification_ShouldReturn202WithSessionId()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        var response = await handler.HandleAsync(Post(
            """{"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"2025-06-18"}}"""));

        response.StatusCode.Should().Be(202, "通知没有响应体：不得以 200 + 空信封伪造一次往返");
        response.Body.Should().BeNull();
        response.Headers.Should().ContainKey(McpHttpHeaders.SessionId, "会话标识是握手唯一的凭据载体");

        var sessionId = response.Headers[McpHttpHeaders.SessionId];
        (await handler.HandleAsync(Post(ToolsListBody, sessionId: sessionId)))
            .StatusCode.Should().Be(200, "下发的会话标识必须真的可用");
    }

    [Fact]
    public async Task Post_ProtocolVersionHeaderUnsupported_ShouldReject400()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler);

        (await handler.HandleAsync(
            Post(ToolsListBody, sessionId: sessionId, protocolVersion: "2099-01-01")))
            .StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Post_ProtocolVersionHeaderMismatchWithSession_ShouldReject400()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler, protocolVersion: "2025-03-26");

        (await handler.HandleAsync(
            Post(ToolsListBody, sessionId: sessionId, protocolVersion: "2025-06-18")))
            .StatusCode.Should().Be(400,
                "闭集内的另一个版本也不行：会话按握手结果解释报文，换版本就是换语法");
    }

    [Fact]
    public async Task Post_WithoutProtocolVersionHeader_ShouldUseSessionVersion()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler, protocolVersion: "2025-03-26");

        (await handler.HandleAsync(Post(ToolsListBody, sessionId: sessionId)))
            .StatusCode.Should().Be(200, "早期修订的客户端不带该头，不因缺头拒服务");
    }

    // ─────────────────────────── 会话上界与空闲回收 ───────────────────────────

    [Fact]
    public async Task Initialize_ShouldReject503_WhenSessionCapReached()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.MaxSessions = 2);

        (await handler.HandleAsync(Post(InitializeBody()))).StatusCode.Should().Be(200);
        (await handler.HandleAsync(Post(InitializeBody()))).StatusCode.Should().Be(200);

        (await handler.HandleAsync(Post(InitializeBody())))
            .StatusCode.Should().Be(503, "握手是唯一的会话增长点，上界必须落在这里而不是无限吃内存");
        handler.SessionCount.Should().Be(2);
    }

    [Fact]
    public async Task Session_ShouldBeRecycledAfterIdleTimeout()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.SessionIdleTimeoutSeconds = 60);
        var sessionId = await InitializeAsync(handler);

        _now = _now.AddSeconds(61);

        (await handler.HandleAsync(Post(ToolsListBody, sessionId: sessionId)))
            .StatusCode.Should().Be(404, "空闲过限的会话视同不存在：请重新握手，而不是带着旧状态继续");
        handler.SessionCount.Should().Be(0);
    }

    [Fact]
    public async Task Session_ShouldStayAliveWhileAccessed()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http => http.SessionIdleTimeoutSeconds = 60);
        var sessionId = await InitializeAsync(handler);

        for (var i = 0; i < 3; i++)
        {
            _now = _now.AddSeconds(50);

            (await handler.HandleAsync(Post(ToolsListBody, sessionId: sessionId)))
                .StatusCode.Should().Be(200, "每次访问刷新时刻：长会话只要在用就不该被回收");
        }

        handler.SessionCount.Should().Be(1);
    }

    [Fact]
    public async Task Session_ShouldBeFreedForNewHandshake_AfterIdleExpiry()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider, http =>
        {
            http.MaxSessions = 1;
            http.SessionIdleTimeoutSeconds = 60;
        });
        await InitializeAsync(handler);

        _now = _now.AddSeconds(61);

        (await handler.HandleAsync(Post(InitializeBody())))
            .StatusCode.Should().Be(200, "上界判断在清扫之后：过期会话腾出的名额必须能被复用");
        handler.SessionCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_ShouldReturn204AndDropSession()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler);

        var response = await handler.HandleAsync(Delete(TenantA, sessionId));

        response.StatusCode.Should().Be(204);
        response.Body.Should().BeNull();
        handler.SessionCount.Should().Be(0);
        (await handler.HandleAsync(Post(ToolsListBody, sessionId: sessionId)))
            .StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-such-session")]
    public async Task Delete_ShouldReject_WithoutAUsableSession(string? sessionId)
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var expected = sessionId is null ? 400 : 404;

        (await handler.HandleAsync(Delete(TenantA, sessionId))).StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Delete_ShouldReject401_WithoutIdentity()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);
        var sessionId = await InitializeAsync(handler);

        (await handler.HandleAsync(Delete(appKey: null, sessionId)))
            .StatusCode.Should().Be(401, "终止会话同样需要认证出的租户身份");
        handler.SessionCount.Should().Be(1, "被拒的终止请求不得真的终止别人的会话");
    }

    // ─────────────────────────── 配置校验（装配期 fail-fast） ───────────────────────────

    [Theory]
    [InlineData(0, 900)]
    [InlineData(-1, 900)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public void Validate_ShouldThrow_WhenNumericBoundsBroken(int maxSessions, int idleSeconds)
    {
        var options = new FeishuMcpHttpOptions
        {
            MaxSessions = maxSessions,
            SessionIdleTimeoutSeconds = idleSeconds,
        };

        options.Invoking(o => o.Validate()).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldThrow_WhenAllowListHasBlankEntry(string entry)
    {
        var origins = new FeishuMcpHttpOptions();
        origins.AllowedOrigins.Add(entry);
        var appKeys = new FeishuMcpHttpOptions();
        appKeys.AllowedAppKeys.Add(entry);

        origins.Invoking(o => o.Validate()).Should().Throw<InvalidOperationException>(
            "空白项会让逐字比较退化成「空字符串也算命中」");
        appKeys.Invoking(o => o.Validate()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_ShouldAllowBlankAppKey_OnlyWhenBoundPerRequest()
    {
        var singleTenant = new FeishuMcpServerOptions { AppKey = string.Empty };
        var perRequest = new FeishuMcpServerOptions { AppKey = string.Empty };
        perRequest.Http.TenantBoundPerRequest = true;

        singleTenant.Invoking(o => o.Validate()).Should().Throw<InvalidOperationException>(
            "appKey 必填这条 fail-closed 规则不得被 HTTP 隐式放宽");
        perRequest.Invoking(o => o.Validate()).Should().NotThrow(
            "逐请求绑租户是唯一的显式豁免形态");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenRequestIsNull()
    {
        using var provider = BuildProvider(mcp: o => o.AppKey = TenantA);
        var handler = BuildHandler(provider);

        await ((Func<Task>)(() => handler.HandleAsync(null!)))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}
