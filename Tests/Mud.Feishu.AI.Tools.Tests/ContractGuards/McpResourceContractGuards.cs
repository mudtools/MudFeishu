// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>M-5：MCP resources 契约守卫</b>——把"资源面与工具面同源"、"清单与可读闭环"、
/// "未知 URI 可修"三件事钉死为可断言的事实。
/// </summary>
/// <remarks>
/// <para>
/// 守卫面（对应方案 §4 M-2/M-3/M-4）：
/// <list type="number">
/// <item><b>三源相等</b>：<c>resources/list</c> 的域集合 == <c>instructions</c> 摘要域集合 == 已启用工具域集合；</item>
/// <item><b>读取闭环</b>：对 <c>resources/list</c> 列出的<b>每一个</b> URI，<c>resources/read</c> 都必须成功且非空
///     （"列得出来读不到"是资源面最恶劣的形态：客户端按清单读却拿到错误）；</item>
/// <item><b>白名单纪律</b>：未启用域的资源既不在清单里，直接读也必须被拒；</item>
/// <item><b>未知 URI 可修</b>：回报 <c>-32602</c> 且给出合法 URI 形态（不是"没找到"）；</item>
/// <item><b>开关语义</b>：<c>IncludeResources=false</c> 时不声明能力且清单为空；
///     <c>InstructionsMode</c> 决定摘要/全文两种形态（routing 默认）；</item>
/// <item><b>错误契约摘要防漂移</b>：内联常量里的 category/subtype 闭集 == 代码闭集。</item>
/// </list>
/// </para>
/// </remarks>
public class McpResourceContractGuards
{
    private const string ErrorContractUri = "feishu-errors://contract";

    // ────────── ① 三源相等 ──────────

    /// <summary>
    /// <b>三源相等</b>：资源清单的域集合、instructions 摘要的域集合、已启用工具的域集合必须完全一致。
    /// </summary>
    /// <remarks>
    /// 三者中任意两者不等都会产生可观测的错误行为：资源多了 → 通过资源读到未启用域的知识
    /// （白名单泄漏）；instructions 多了 → 模型路由到不可用的域；除这两者外的第三源是白名单真相源。
    /// </remarks>
    [Fact]
    public async Task ResourceDomains_InstructionsDomains_EnabledDomains_ShouldBeEqual()
    {
        using var provider = BuildProvider(
            tools: ["bitable.list_tables", "wiki.get_node", "im.list_chat_members"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var enabledDomains = provider.GetRequiredService<FeishuToolRegistry>()
            .EnabledTools
            .Select(static tool => DomainOf(tool.Name))
            .ToHashSet(StringComparer.Ordinal);

        var initialized = await HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");
        var instructions = initialized["result"]!["instructions"]!.GetValue<string>();
        var instructionDomains = Regex.Matches(instructions, @"【(?<d>[a-z0-9_]+)】")
            .Select(static m => m.Groups["d"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var listed = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":2,"method":"resources/list","params":{}}""");
        var resourceDomains = listed["result"]!["resources"]!.AsArray()
            .Select(static node => node!["uri"]!.GetValue<string>())
            .Where(static uri => uri.StartsWith("feishu-guidance://", StringComparison.Ordinal))
            .Select(static uri => uri.Substring("feishu-guidance://".Length))
            .Select(static rest => rest.Contains('/') ? rest.Substring(0, rest.IndexOf('/')) : rest)
            .ToHashSet(StringComparer.Ordinal);

        resourceDomains.Should().BeEquivalentTo(enabledDomains, "资源域必须等于已启用工具的域集合");
        instructionDomains.Should().BeEquivalentTo(
            enabledDomains, "instructions 的域集合必须等于已启用工具的域集合");
    }

    // ────────── ② 读取闭环 ──────────

    /// <summary>
    /// <b>读取闭环</b>：清单里列出的每个 URI 都必须能读到非空正文。
    /// </summary>
    [Fact]
    public async Task EveryListedResource_ShouldBeReadable_AndNonEmpty()
    {
        using var provider = BuildProvider(
            tools: ["bitable.list_tables", "wiki.get_node", "im.list_chat_members", "wiki.list_nodes"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var listed = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":2,"method":"resources/list","params":{}}""");
        var resources = listed["result"]!["resources"]!.AsArray();
        resources.Should().NotBeEmpty("启用工具后资源清单不得为空（空清单说明资源面没接上）");

        var readableCount = 0;
        foreach (var node in resources)
        {
            var uri = node!["uri"]!.GetValue<string>();
            var read = await HandleAsync(
                server,
                "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"resources/read\",\"params\":{\"uri\":\"" + uri + "\"}}");

            read.ContainsKey("error").Should().BeFalse(
                $"清单列出的资源 '{uri}' 必须可读（列得出来读不到 = 资源面自相矛盾）");

            var text = read["result"]!["contents"]!.AsArray()[0]!["text"]!.GetValue<string>();
            text.Should().NotBeNullOrWhiteSpace($"资源 '{uri}' 的正文不得为空");

            // 体积估算必须与实际一致（±10% 容许 UTF-8 与字符计数的口径差）——否则客户端的
            // "按体积决定读不读"就失去依据。
            var advertised = node["sizeBytes"]!.GetValue<int>();
            var actual = System.Text.Encoding.UTF8.GetByteCount(text);
            advertised.Should().Be(actual, $"'{uri}' 的 sizeBytes 必须等于实际 UTF-8 字节数（客户端据此决策）");

            readableCount++;
        }

        readableCount.Should().Be(resources.Count);
    }

    /// <summary>目录里出现过的 URI 形态必须与 <c>UriPrefixes</c> 声明一致（模板 ↔ 条目一一对应）。</summary>
    [Fact]
    public async Task ListedUris_ShouldMatchDeclaredUriTemplates()
    {
        using var provider = BuildProvider(tools: ["bitable.list_tables", "wiki.get_node"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var listed = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":2,"method":"resources/list","params":{}}""");

        var prefixes = server.ResourcesUriPrefixes;
        prefixes.Should().NotBeEmpty();

        foreach (var node in listed["result"]!["resources"]!.AsArray())
        {
            var uri = node!["uri"]!.GetValue<string>();

            // 简化匹配：URI 去掉具体段后应等于某个声明的模板前缀。
            var scheme = uri.Substring(0, uri.IndexOf("://", StringComparison.Ordinal) + 3);
            prefixes.Should().Contain(
                prefix => prefix.StartsWith(scheme, StringComparison.Ordinal),
                $"资源 '{uri}' 的 URI 方案必须出现在声明的前缀清单里（否则客户端无从推断可写形态）");
        }
    }

    // ────────── ③ 白名单纪律与未知 URI ──────────

    /// <summary>未启用域的资源直接读必须被拒（资源面不得成为绕过白名单的读取通道）。</summary>
    [Fact]
    public async Task DisabledDomainResource_ShouldBeRejected()
    {
        using var provider = BuildProvider(tools: ["bitable.list_tables"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var read = await HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"feishu-guidance://payroll"}}""");

        read["error"]!["code"]!.GetValue<int>().Should().Be(-32602);
        read["error"]!["message"]!.GetValue<string>().Should().Contain(
            "feishu-guidance://", "未知/未启用资源的错误必须给出合法 URI 形态（可修，而不是只回没找到）");
    }

    /// <summary>缺 <c>params.uri</c> 时必须回参数错误（而不是当成"未知资源"）。</summary>
    [Fact]
    public async Task MissingUri_ShouldReturnInvalidParams()
    {
        using var provider = BuildProvider(tools: ["bitable.list_tables"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var read = await HandleAsync(server, """{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{}}""");

        read["error"]!["code"]!.GetValue<int>().Should().Be(-32602);
        read["error"]!["message"]!.GetValue<string>().Should().Contain("params.uri");
    }

    // ────────── ④ 开关语义（M-1/M-3/M-4） ──────────

    /// <summary>默认（<c>IncludeResources=true</c>）时 <c>initialize.capabilities</c> 必须声明 resources。</summary>
    [Fact]
    public async Task Capabilities_ShouldDeclareResources_WhenEnabled()
    {
        using var provider = BuildProvider(tools: ["bitable.list_tables"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var initialized = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");

        var capabilities = initialized["result"]!["capabilities"]!.AsObject();
        capabilities["tools"]!["listChanged"]!.GetValue<bool>().Should().BeFalse();
        capabilities["resources"]!["listChanged"]!.GetValue<bool>().Should().BeFalse(
            "resources 必须与 tools 同为装配期固定（listChanged=false 是契约而非遗漏）");
    }

    /// <summary><c>IncludeResources=false</c> 时不声明该能力，且 <c>resources/list</c> 为空。</summary>
    [Fact]
    public async Task Capabilities_ShouldOmitResources_WhenDisabled()
    {
        using var provider = BuildProvider(
            tools: ["bitable.list_tables"], configureMcp: options => options.IncludeResources = false);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var initialized = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");

        initialized["result"]!["capabilities"]!.AsObject().ContainsKey("resources").Should().BeFalse(
            "关闭后不得声明未提供的能力");

        var listed = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":2,"method":"resources/list","params":{}}""");
        listed["result"]!["resources"]!.AsArray().Should().BeEmpty();
    }

    /// <summary>
    /// <c>InstructionsMode=routing</c>（默认）：instructions 里每域只有<b>一行摘要</b>
    /// （≤256 字符），而全文经资源可得——这就是 BUG-4 的结构性解法。
    /// </summary>
    [Fact]
    public async Task Instructions_RoutingMode_ShouldBeConcise_AndFullTextAvailableViaResource()
    {
        using var provider = BuildProvider(tools: ["bitable.list_tables"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var initialized = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");
        var instructions = initialized["result"]!["instructions"]!.GetValue<string>();

        var bitableFull = FeishuToolGuidance.ByDomain["bitable"];
        instructions.Length.Should().BeLessThan(
            bitableFull.Length, "routing 形态必须显著短于 L1 全文（否则改造是名义性的）");
        instructions.Should().Contain(GuidanceRoutingSummary.Create("bitable", bitableFull));

        var read = await HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"feishu-guidance://bitable"}}""");
        read["result"]!["contents"]!.AsArray()[0]!["text"]!.GetValue<string>().Should().Be(
            bitableFull, "L1 全文必须经资源完整可得（渐进披露的另一半）");
    }

    /// <summary><c>InstructionsMode=full</c>：instructions 回到"每域 L1 全文"（显式回退开关）。</summary>
    [Fact]
    public async Task Instructions_FullMode_ShouldCarryWholeL1Asset()
    {
        using var provider = BuildProvider(
            tools: ["bitable.list_tables"], configureMcp: options => options.InstructionsMode = GuidanceModes.Full);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var initialized = await HandleAsync(
            server,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");
        var instructions = initialized["result"]!["instructions"]!.GetValue<string>();

        instructions.Should().Contain(FeishuToolGuidance.ByDomain["bitable"], "full 形态必须含 L1 全文");
    }

    // ────────── ⑤ 错误契约摘要防漂移 ──────────

    /// <summary><c>feishu-errors://contract</c> 的闭集清单必须与代码闭集逐一相等。</summary>
    [Fact]
    public void ErrorContractSummary_ClosedSets_ShouldMatchCodeClosure()
    {
        var categories = ExtractSectionLiterals(FeishuMcpResourceCatalog.ErrorContractSummary, "category 闭集");
        var subtypes = ExtractSectionLiterals(FeishuMcpResourceCatalog.ErrorContractSummary, "subtype 闭集");

        var codeCategories = Enum.GetValues<ToolErrorCategory>().Select(FeishuToolBinding.CategoryLiteral).ToArray();
        var codeSubtypes = typeof(ToolErrorSubtype)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => (string?)field.GetValue(null))
            .Where(static value => !string.IsNullOrEmpty(value))
            .Select(static value => value!)
            .ToArray();

        categories.Should().BeEquivalentTo(
            codeCategories, "MCP 内联的错误契约摘要必须与 ToolErrorCategory 闭集相等（消费侧不能拿到过期清单）");
        subtypes.Should().BeEquivalentTo(
            codeSubtypes, "MCP 内联的错误契约摘要必须与 ToolErrorSubtype 闭集相等");
    }

    /// <summary>错误契约资源必须可读（它是"模型自助纠偏"的入口，不在清单里就等于没提供）。</summary>
    [Fact]
    public async Task ErrorContractResource_ShouldBeListedAndReadable()
    {
        using var provider = BuildProvider(tools: ["bitable.list_tables"]);
        var server = provider.GetRequiredService<FeishuMcpToolServer>();

        var listed = await HandleAsync(
            server, """{"jsonrpc":"2.0","id":2,"method":"resources/list","params":{}}""");
        listed["result"]!["resources"]!.AsArray()
            .Select(static node => node!["uri"]!.GetValue<string>())
            .Should().Contain(ErrorContractUri);

        var read = await HandleAsync(
            server,
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"resources/read\",\"params\":{\"uri\":\"" + ErrorContractUri + "\"}}");
        read["result"]!["contents"]!.AsArray()[0]!["text"]!.GetValue<string>()
            .Should().Contain("category 闭集");
    }

    /// <summary>解析器自证：注释/空行被忽略，且只有带 <c>## </c> 的小节算定位锚点。</summary>
    [Fact]
    public void SectionLiteralExtractor_ShouldIgnoreNoise()
    {
        const string sample = """
            ## category 闭集
            - validation

            # 注释行
            - policy

            ## subtype 闭集
            - invalid_args
            """;

        ExtractSectionLiterals(sample, "category 闭集").Should().Equal(["validation", "policy"]);
        ExtractSectionLiterals(sample, "subtype 闭集").Should().Equal(["invalid_args"]);
        ExtractSectionLiterals("无关文本", "category 闭集").Should().BeEmpty();
    }

    // ────────── 夹具与实现 ──────────

    private static string[] ExtractSectionLiterals(string text, string sectionTitle)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var inSection = false;
        var values = new List<string>();

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inSection = line.Contains(sectionTitle, StringComparison.Ordinal);
                continue;
            }

            if (!inSection || !line.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }

            values.Add(line.Substring(2).Trim());
        }

        return [.. values];
    }

    private static string DomainOf(string toolName)
    {
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }

    private static ServiceProvider BuildProvider(
        string[] tools,
        Action<FeishuMcpServerOptions>? configureMcp = null)
    {
        var agentOptions = new FeishuAgentOptions
        {
            Instructions = "test",
            Tools = tools,

            // 写工具用于覆盖 affordance 资源；授权门禁不是本守卫的关注面（另有专门守卫覆盖）。
            WriteAllowList = ["bitable.delete_record"],
            EnforceToolAuthorization = false,
        };

        // 分域 mock 齐备（与 GuardProviderFactory 同口径，按本守卫实际启用的域取最小集）：
        // 客户端缺席 ⇒ 该域工具不注册 ⇒ 白名单 fail-fast，本守卫的"三源相等"就无从断言。
        var services = new ServiceCollection()
            .AddSingleton(Options.Create(agentOptions))
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFiles>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object);

        services.AddFeishuTools();
        services.AddFeishuMcpServer(options =>
        {
            options.AppKey = "cli_test";
            configureMcp?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 发送一条请求；若会话尚未初始化则<b>先自动握手</b>。
    /// </summary>
    /// <remarks>
    /// 初始化门禁本身（未握手时 <c>-32002</c>）由 <c>FeishuMcpToolServerTests</c> 的专门用例覆盖；
    /// 本守卫的关注面是<b>资源面</b>，故在夹具层自动完成握手，避免每条用例都重复一行与主题无关的调用
    /// （也让"新增用例忘记握手"不会退化成假红）。
    /// </remarks>
    private static async Task<JsonObject> HandleAsync(FeishuMcpToolServer server, string request)
    {
        if (!server.IsInitialized && !request.Contains("\"initialize\"", StringComparison.Ordinal))
        {
            await server.HandleMessageAsync(
                """{"jsonrpc":"2.0","id":99,"method":"initialize","params":{}}""");
        }

        var response = await server.HandleMessageAsync(request);
        response.Should().NotBeNull("请求（含 id）必须产生一条响应");
        return JsonNode.Parse(response!)!.AsObject();
    }
}
