// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Mcp;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tools.Tests.Mcp;

/// <summary>
/// MCP server（R7 / C6b · Batch-8）协议层行为用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类的重点是"复用执行链"这件事必须可观测</b>：不是断言"工具被调用了"，而是断言
/// ① 审计出口收到了记录（说明确实走了 <c>FeishuToolBinding</c>）；
/// ② 授权器看到的上下文里 appKey/userId 与配置一致（说明 <c>Begin</c> 真的生效、且是每次调用重建）；
/// ③ 写工具在授权器返回"需人工确认"时 <b>零调用下游</b>（fail-closed——MCP 侧没有 MAF 审批管线，
/// 若把"没有管线"当成"无需审批"就会静默放行写操作）。
/// </para>
/// <para>
/// 工具面用真实的 <c>AddFeishuTools()</c> 装配（域客户端缺席时该域软缺席），白名单经
/// <c>FeishuAgentOptions.Tools</c> / <c>WriteAllowList</c> 给出——正是 MCP 与进程内 Agent 共用的那份配置。
/// </para>
/// </remarks>
public class FeishuMcpToolServerTests
{
    private const string SchemaReadTool = "feishu.schema_read";
    private const string SchemaReadMcpName = "feishu_schema_read";
    private const string SendMessageTool = "im.send_message";
    private const string SendMessageMcpName = "im_send_message";

    private static ServiceProvider BuildProvider(
        Action<FeishuAgentOptions>? agent = null,
        Action<FeishuMcpServerOptions>? mcp = null,
        Mock<IToolExecutionAuthorizer>? authorizer = null,
        IToolExecutionAuditSink? auditSink = null,
        Mock<Mud.Feishu.IFeishuTenantV1Message>? messageClient = null)
    {
        var agentOptions = new FeishuAgentOptions { Instructions = "test" };
        agent?.Invoke(agentOptions);

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(agentOptions))

            // 作用域工厂的核心依赖（与生成的 HTTP 客户端同构，与工具域无关）。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())));

        if (messageClient is not null)
        {
            services.AddSingleton(messageClient.Object);
        }

        if (authorizer is not null)
        {
            services.AddSingleton(authorizer.Object);
        }

        if (auditSink is not null)
        {
            services.AddSingleton(auditSink);
        }

        services.AddFeishuTools();
        services.AddFeishuMcpServer(mcp);
        return services.BuildServiceProvider();
    }

    /// <summary>MCP 配置（appKey 必填；其它走默认值）。</summary>
    private static Action<FeishuMcpServerOptions> McpConfigure(
        string appKey = "cli_test",
        string? userId = null,
        bool includeGuidance = true)
        => options =>
        {
            options.AppKey = appKey;
            options.UserId = userId;
            options.IncludeGuidance = includeGuidance;
        };

    private static Mock<IToolExecutionAuthorizer> Authorizer(AuthorizationResult decision)
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
            .ReturnsAsync(decision);
        return authorizer;
    }

    private static async Task<JsonObject> HandleAsync(FeishuMcpToolServer server, string request)
    {
        var response = await server.HandleMessageAsync(request);
        response.Should().NotBeNull("请求（含 id）必须产生一条响应");
        return JsonNode.Parse(response!)!.AsObject();
    }

    private static Task<JsonObject> InitializeAsync(FeishuMcpToolServer server)
        => HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"t","version":"1"}}}""");

    private static string ToolsCallRequest(string name, string argumentsJson)
        => "{\"jsonrpc\":\"2.0\",\"id\":9,\"method\":\"tools/call\",\"params\":{\"name\":\"" + name
           + "\",\"arguments\":" + argumentsJson + "}}";

    private static string ContentText(JsonObject response)
        => response["result"]!["content"]!.AsArray()[0]!["text"]!.GetValue<string>();

    // ─────────────────────────── 工具面（白名单同源） ───────────────────────────

    [Fact]
    public async Task Tools_ShouldExposeOnlyWhitelistedTools()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        server.Tools.Select(t => t.ContractName).Should().BeEquivalentTo(
            [SchemaReadTool],
            "MCP 暴露的工具集必须等于注册表白名单（不新增第二条启用通道）");

        await InitializeAsync(server);
        var list = await HandleAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");
        var names = list["result"]!["tools"]!.AsArray()
            .Select(t => t!["name"]!.GetValue<string>())
            .ToArray();

        names.Should().Equal([SchemaReadMcpName], "MCP 名是契约名的点号映射（严格客户端只接受 [a-zA-Z0-9_-]）");
    }

    [Fact]
    public async Task ToolsList_ShouldCarryContractTitle_AnnotationsAndInputSchema()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var list = await HandleAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");
        var tool = list["result"]!["tools"]!.AsArray()[0]!.AsObject();

        tool["title"]!.GetValue<string>().Should().Be(SchemaReadTool, "title 保留契约名（与《工具权限对照表》一致）");
        tool["description"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();

        var annotations = tool["annotations"]!.AsObject();
        annotations["readOnlyHint"]!.GetValue<bool>().Should().BeTrue("只读工具必须如实标注");
        annotations["destructiveHint"]!.GetValue<bool>().Should().BeFalse();

        tool["inputSchema"]!["properties"]!.AsObject().ContainsKey("method").Should().BeTrue(
            "入参 Schema 必须来自编译期常量（与进程内 Agent 同一份）");
    }

    [Fact]
    public async Task WriteTool_ShouldAppearOnlyWhenWhitelisted_AndBeMarkedNotReadOnly()
    {
        using var provider = BuildProvider(
            agent: o => o.WriteAllowList = [SendMessageTool],
            mcp: McpConfigure(),
            messageClient: new Mock<Mud.Feishu.IFeishuTenantV1Message>());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var list = await HandleAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");
        var tool = list["result"]!["tools"]!.AsArray()
            .Single(t => t!["name"]!.GetValue<string>() == SendMessageMcpName);

        tool["annotations"]!["readOnlyHint"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public void WriteTool_ShouldBeAbsent_WhenWhitelistEmpty()
    {
        using var provider = BuildProvider(mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        server.Tools.Should().BeEmpty(
            "默认配置（Tools/WriteAllowList 皆空）下没有任何工具被启用——MCP 不得自行放宽");
    }

    // ─────────────────────────── 会话握手与生命周期 ───────────────────────────

    [Theory]
    [InlineData("2024-11-05")]
    [InlineData("2025-03-26")]
    [InlineData("2025-06-18")]
    public async Task Initialize_ShouldEchoSupportedProtocolVersion(string requested)
    {
        using var provider = BuildProvider(mcp: McpConfigure(includeGuidance: false));
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var response = await HandleAsync(
            server,
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
            + requested + "\"}}");

        response["result"]!["protocolVersion"]!.GetValue<string>().Should().Be(requested);
        response["result"]!["capabilities"]!["tools"]!["listChanged"]!.GetValue<bool>().Should().BeFalse(
            "白名单在装配期固定，进程生命周期内不变 ⇒ 不发 listChanged 通知");
        response["result"]!["serverInfo"]!["name"]!.GetValue<string>().Should().Be("mud-feishu");
    }

    [Fact]
    public async Task Initialize_ShouldFallBack_ForUnsupportedProtocolVersion()
    {
        using var provider = BuildProvider(mcp: McpConfigure(includeGuidance: false));
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var response = await HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"9999-01-01"}}""");

        response["result"]!["protocolVersion"]!.GetValue<string>().Should().Be(
            "2025-06-18",
            "闭集外的版本回落本服务默认版本（而不是回显一个自己并不遵守的版本）");
    }

    [Fact]
    public async Task Initialize_ShouldIncludeEnabledDomainGuidance()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        var response = await InitializeAsync(server);

        response["result"]!["instructions"]!.GetValue<string>().Should()
            .NotBeNullOrWhiteSpace("启用域的路由/避坑资产必须随握手下发（外部 Agent 与进程内 Agent 同一份资产）");
    }

    [Fact]
    public async Task Initialize_ShouldOmitInstructions_WhenGuidanceDisabled()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure(includeGuidance: false));

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        var response = await InitializeAsync(server);

        response["result"]!.AsObject().ContainsKey("instructions").Should().BeFalse();
        server.Instructions.Should().BeEmpty();
    }

    [Fact]
    public async Task Requests_BeforeInitialize_ShouldFailClosed()
    {
        using var provider = BuildProvider(mcp: McpConfigure());
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var response = await HandleAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        response["error"]!["code"]!.GetValue<int>().Should().Be(-32002);
        server.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task Notification_ShouldNotProduceResponse_ButCompletesHandshake()
    {
        using var provider = BuildProvider(mcp: McpConfigure());
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        (await server.HandleMessageAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}"""))
            .Should().BeNull("通知按协议不产生响应");

        server.IsInitialized.Should().BeTrue();

        (await server.HandleMessageAsync("""{"jsonrpc":"2.0","method":"notifications/unknown-thing"}"""))
            .Should().BeNull("未知通知静默（协议允许扩展通知）");
    }

    [Fact]
    public async Task ParseError_And_UnknownMethod_And_Ping()
    {
        using var provider = BuildProvider(mcp: McpConfigure());
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var parseError = await HandleAsync(server, "{ 这不是 JSON ");
        parseError["error"]!["code"]!.GetValue<int>().Should().Be(-32700);
        parseError["id"].Should().BeNull();

        await InitializeAsync(server);

        var unknown = await HandleAsync(server, """{"jsonrpc":"2.0","id":3,"method":"does/not/exist"}""");
        unknown["error"]!["code"]!.GetValue<int>().Should().Be(-32601);

        var ping = await HandleAsync(server, """{"jsonrpc":"2.0","id":4,"method":"ping"}""");
        ping["result"]!.AsObject().Count.Should().Be(0, "ping 的空结果约定");

        // 空行/空白不产生响应（stdio 尾部换行不得被当成请求）。
        (await server.HandleMessageAsync("   ")).Should().BeNull();
    }

    // ─────────────────────────── tools/call：执行链复用 ───────────────────────────

    [Fact]
    public async Task ToolsCall_ShouldGoThroughExecutionChain_AuditedAsAllowed()
    {
        var audit = new Mock<IToolExecutionAuditSink>();
        ToolExecutionAuditRecord? captured = null;
        audit.Setup(s => s.WriteAsync(It.IsAny<ToolExecutionAuditRecord>(), It.IsAny<CancellationToken>()))
            .Callback<ToolExecutionAuditRecord, CancellationToken>((record, _) => captured = record)
            .Returns(Task.CompletedTask);

        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure(),
            auditSink: audit.Object);

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(
            server,
            ToolsCallRequest(SchemaReadMcpName, """{"module":"bitable","limit":1}"""));

        response["result"]!["isError"]!.GetValue<bool>().Should().BeFalse();
        ContentText(response).Should().NotBeNullOrWhiteSpace();

        audit.Verify(
            s => s.WriteAsync(It.IsAny<ToolExecutionAuditRecord>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "MCP 调用必须留下执行链的审计记录（结构上无法旁路才可信）");

        captured!.ToolName.Should().Be(SchemaReadTool, "审计使用契约名");
        captured.Decision.Should().Be("allowed");
        captured.AppKey.Should().Be("cli_test", "审计记录里的 appKey 来自 MCP 进程配置");
    }

    [Fact]
    public async Task ToolsCall_ShouldRebuildTenantContext_PerCall()
    {
        FeishuToolContext? seen = null;
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
                CancellationToken _____) => seen = context)
            .ReturnsAsync(AuthorizationResult.Allowed);

        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure(userId: "ou_1"),
            authorizer: authorizer);

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        await HandleAsync(server, ToolsCallRequest(SchemaReadMcpName, """{"module":"bitable"}"""));

        seen.Should().NotBeNull("授权器必须被执行链调用（否则说明根本没走执行链）");
        seen!.AppKey.Should().Be("cli_test");
        seen.UserId.Should().Be("ou_1", "user 身份工具依赖上下文里的用户 ID（令牌缓存查找键）");
    }

    [Fact]
    public async Task ToolsCall_ShouldAcceptContractNameAlias()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(
            server,
            ToolsCallRequest(SchemaReadTool, """{"module":"bitable","limit":1}"""));

        response["result"]!["isError"]!.GetValue<bool>().Should().BeFalse(
            "客户端把 title（契约名）当 name 回传时必须照常工作——否则集成方会踩到'名字对不上'的坑");
    }

    [Fact]
    public async Task ToolsCall_UnknownOrDisabledTool_ShouldReturnInvalidParams_WithoutEnumeration()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(server, ToolsCallRequest("docx_get_raw_content", """{}"""));

        response["error"]!["code"]!.GetValue<int>().Should().Be(-32602);
        var message = response["error"]!["message"]!.GetValue<string>();
        message.Should().Contain("未启用").And.Contain("WriteAllowList",
            "错误必须指向可修配置（否则宿主只看到'工具不存在'）");
        message.Should().NotContain(SchemaReadTool,
            "不得回显已启用清单——那等于提供一条枚举通道");
    }

    [Fact]
    public async Task ToolsCall_ShouldRejectNonObjectArguments()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":9,"method":"tools/call","params":{"name":"feishu_schema_read","arguments":"oops"}}""");

        response["error"]!["code"]!.GetValue<int>().Should().Be(-32602);
    }

    // ─────────────────────────── tools/call：写工具 fail-closed ───────────────────────────

    [Fact]
    public async Task WriteTool_WhenAuthorizerRequiresConfirmation_ShouldFailClosed_WithoutDownstreamCall()
    {
        var messageClient = new Mock<Mud.Feishu.IFeishuTenantV1Message>();

        using var provider = BuildProvider(
            agent: o => o.WriteAllowList = [SendMessageTool],
            mcp: McpConfigure(),
            authorizer: Authorizer(AuthorizationResult.Confirm("请管理员在审批面板确认")),
            messageClient: messageClient);

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(
            server,
            ToolsCallRequest(SendMessageMcpName, """{"receive_id":"oc_1","text":"你好"}"""));

        response["result"]!["isError"]!.GetValue<bool>().Should().BeTrue();
        ContentText(response).Should().Contain("needs_user_confirmation",
            "待人工确认必须以结构化子类回填（MCP 侧不因'没有 MAF 审批卡片'而放行）");

        messageClient.Invocations.Should().BeEmpty(
            "未获批的写工具必须零调用下游——这是 MCP 路径下 HITL 的唯一有效门禁");
    }

    [Fact]
    public async Task WriteTool_WithoutAuthorizer_ShouldBeDeniedByDefault()
    {
        var messageClient = new Mock<Mud.Feishu.IFeishuTenantV1Message>();

        using var provider = BuildProvider(
            agent: o => o.WriteAllowList = [SendMessageTool],
            mcp: McpConfigure(),
            messageClient: messageClient);

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(
            server,
            ToolsCallRequest(SendMessageMcpName, """{"receive_id":"oc_1","text":"你好"}"""));

        response["result"]!["isError"]!.GetValue<bool>().Should().BeTrue();
        ContentText(response).Should().Contain("authorization_denied");
        messageClient.Invocations.Should().BeEmpty("安全默认：写工具未注册授权器时默认拒绝");
    }

    [Fact]
    public async Task WriteTool_WhenAllowed_ShouldReachDownstream_WithMappedRequest()
    {
        var messageClient = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        messageClient
            .Setup(c => c.SendMessageAsync(
                It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_1" },
            });

        using var provider = BuildProvider(
            agent: o => o.WriteAllowList = [SendMessageTool],
            mcp: McpConfigure(),
            authorizer: Authorizer(AuthorizationResult.Allowed),
            messageClient: messageClient);

        var server = provider.GetRequiredService<FeishuMcpToolServer>();
        await InitializeAsync(server);

        var response = await HandleAsync(
            server,
            ToolsCallRequest(SendMessageMcpName, """{"receive_id":"oc_1","text":"你好"}"""));

        response["result"]!["isError"]!.GetValue<bool>().Should().BeFalse(
            "获批后调用应真正执行：MCP 只是传输层，不改变执行语义");
        ContentText(response).Should().Contain("om_1");

        messageClient.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReceiveId == "oc_1" && r.MsgType == "text"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─────────────────────────── 配置与宿主装配 ───────────────────────────

    [Fact]
    public void Options_ShouldFailFast_WhenAppKeyMissing()
    {
        using var provider = BuildProvider(mcp: _ => { });

        var act = () => provider.GetRequiredService<FeishuMcpToolServer>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FeishuMcp:AppKey*", "缺租户标识必须启动期 fail-fast，而不是每次调用都失败");
    }

    [Fact]
    public async Task StdioHost_ShouldProcessRequestsInOrder_AndStopAtEof()
    {
        using var provider = BuildProvider(
            agent: o => o.Tools = [SchemaReadTool],
            mcp: McpConfigure());

        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var input = new StringReader(string.Join(
            "\n",
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18"}}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
            string.Empty));
        var output = new StringWriter();

        var host = new FeishuMcpStdioHost(server, input, output, logger: null);
        var handled = await host.RunAsync();

        handled.Should().Be(2, "通知不产生响应，故只有两条请求计入");
        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("\"id\":1");
        lines[1].Should().Contain("\"tools\"", "逐行顺序处理：第二条响应必须在第一条之后");
    }
}
