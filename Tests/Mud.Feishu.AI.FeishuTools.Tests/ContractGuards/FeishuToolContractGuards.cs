// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.FeishuTools.Tests.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 工具名契约表守卫（Phase 1 §7 + Phase 2 §3.3）：Schema 注册表恰为契约名全集
/// （当前为 **24 个只读 + 7 个写类 = 31**），防增删/改名漂移；读写白名单分离语义锁定。
/// </summary>
/// <remarks>
/// 数量口径以 <c>FeishuToolNames.All</c>（生成器派生）为准——本类中的任何数字只是说明，
/// <b>不构成断言依据</b>（原注释里的"十个/十九个"曾长期与实际漂移，见 AT-B21）。
/// </remarks>
public class FeishuToolContractGuards
{
    /// <summary>
    /// 生成器产出的 Schema 键集必须与<b>编译期派生的</b>工具名契约表（<c>FeishuToolNames</c>）完全一致。
    /// </summary>
    /// <remarks>
    /// 契约表本身由源生成器从 <c>[FeishuTool]</c> 特性派生（D2 单一真相源），故本用例锁定的是
    /// "生成器两条产物路径（Schema 常量 / 名字表）不漂移"，而非手写清单——手写清单已被删除。
    /// </remarks>
    [Fact]
    public void SchemaRegistry_ShouldContainExactlyTheContractTools()
    {
        SchemaByToolName.Keys.Should().BeEquivalentTo(FeishuToolNames.All,
            "Schema 注册表键集必须与 [FeishuTool] 派生的工具名契约表一致（增删/改名必须同批更新守卫与 golden）");
    }

    [Fact]
    public void ReadonlyTools_ShouldBeReadOnly_WithScopes()
    {
        // 免 scope 的元工具白名单（AT-F12）：它们不映射任何飞书 API，由
        // MetadataOnlyTools_ShouldNotDeclarePlatformScopes 反向锁定"scope 必须为空"。
        var scopeExemptTools = new[] { FeishuToolNames.FeishuCapabilityLookup };

        foreach (var name in FeishuToolNames.ReadonlyAll)
        {
            using var document = JsonDocument.Parse(SchemaByToolName[name]);
            var root = document.RootElement;

            root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean().Should()
                .BeFalse($"{name} 为只读工具");

            if (scopeExemptTools.Contains(name, StringComparer.Ordinal))
            {
                continue;
            }

            var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            scopes.Should().NotBeEmpty($"{name} 必须声明 required_scopes（已决策⑥：scope 随 Schema 供授权钩子与审计消费）");
        }
    }

    /// <summary>
    /// 不映射飞书 API 的元工具，其 scope 必须为空（AT-F12 收窄，R3 评审）。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="ReadonlyTools_ShouldBeReadOnly_WithScopes"/> 的"必须非空"形成互补约束：
    /// 元工具（<c>feishu.capability_lookup</c>）只读编译期目录常量、一个飞书请求都不发，
    /// 给它挂占位 scope 会让宿主审计误以为需要开权限。
    /// </remarks>
    [Fact]
    public void MetadataOnlyTools_ShouldNotDeclarePlatformScopes()
    {
        using var document = JsonDocument.Parse(SchemaByToolName[FeishuToolNames.FeishuCapabilityLookup]);
        var extension = document.RootElement.GetProperty("x-feishu");

        extension.TryGetProperty("source", out _).Should().BeFalse(
            "能力出处元工具不映射任何飞书 API（无 x-feishu.source）——这是它免 scope 的前提");

        extension.GetProperty("required_scopes").EnumerateArray().Should().BeEmpty(
            "不映射飞书 API 的工具不得声明平台 scope（会造成审计误导）");
    }

    /// <summary>
    /// 能力出处元工具的 <c>Description</c> 必须自述用途与边界（§8.2 #20，C-9 收窄后的落点）。
    /// </summary>
    /// <remarks>
    /// 发现性（"遇到未覆盖能力该去哪问"）此前被设计为"在错误文案里提示"，但那条分支
    /// <b>不可达</b>（未注册工具不在模型的 tools 列表里，模型无从调用）。故唯一可达的提示位
    /// 就是该工具自己的描述——它是模型"遇到不知道的域时会去看的东西"。
    /// </remarks>
    [Fact]
    public void CapabilityLookup_Description_ShouldStatePurposeAndBoundary()
    {
        using var document = JsonDocument.Parse(SchemaByToolName[FeishuToolNames.FeishuCapabilityLookup]);
        var description = document.RootElement.GetProperty("description").GetString()!;

        description.Should().Contain("不在当前工具集内",
            "必须说明它的使用时机（否则模型不知道什么时候该用它）");
        description.Should().Contain("只返回元数据",
            "必须说明它的边界（否则模型会把它当成通用 api 工具，踩到决策①/⑤的红线）");
    }

    [Fact]
    public void WriteTools_ShouldBeFlaggedAsWrite_WithScopes()
    {
        foreach (var name in FeishuToolNames.WriteAll)
        {
            using var document = JsonDocument.Parse(SchemaByToolName[name]);
            var root = document.RootElement;

            root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean().Should()
                .BeTrue($"{name} 为 Phase 2 写类工具——执行链据此强制授权门禁（安全默认）");

            var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            scopes.Should().NotBeEmpty($"{name} 必须声明 required_scopes（已决策⑥）");
        }
    }

    /// <summary>
    /// scope 契约的单一真相源（WP2 / R-B 根因落地）：生成器发射的类型化契约表
    /// （<see cref="FeishuToolContracts"/>）与 Schema 常量出自<b>同一 pass</b>，
    /// 本守卫锁定二者的 scope 视图不漂移；与《工具权限对照表》的逐行交叉验证
    /// 由 <c>PermissionMappingDocContractGuards.PermissionDoc_RequiredScopesColumn_ShouldMatchToolContracts</c> 承担。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>手抄期望表已删除（原 AT-B21 的 24 行字典）</b>：scope 期望值不再存在于守卫代码中——
    /// 契约侧真相 = 生成器（<c>[FeishuTool]</c> + SDK 符号），文档侧真相 = 对照表（人读），
    /// 权威清单 = <c>scope-authority.json</c>（<see cref="ScopeAuthorityContractGuards"/> 锁三方关系）。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolScopes_ContractTable_ShouldBeTheSingleScopeSource()
    {
        var contractScopes = FeishuToolContracts.ByToolName.ToDictionary(
            static kv => kv.Key,
            static kv => string.Join(",", kv.Value.RequiredScopes),
            StringComparer.Ordinal);

        foreach (var (toolName, schemaJson) in SchemaByToolName)
        {
            using var document = JsonDocument.Parse(schemaJson);
            var scopes = document.RootElement.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(static e => e.GetString()!).ToArray();

            contractScopes.Should().ContainKey(toolName, "契约表与 Schema 注册表必须同键集");
            contractScopes[toolName].Should().Be(string.Join(",", scopes),
                $"工具 {toolName} 的契约表 scope 必须与 Schema 常量一致（同一 pass 产物，漂移 = 生成器缺陷）");
        }

        contractScopes.Keys.Should().HaveCount(FeishuToolNames.All.Length, "契约表须覆盖全部契约工具");
    }

    /// <summary>
    /// 注册表 <c>Risk</c>/<c>Identity</c> 必须与 Schema 的 <c>x-feishu.risk</c>/<c>identity</c> 一致。
    /// </summary>
    /// <remarks>
    /// <b>防 D2 双源（§8.2 #4）</b>：风险分级的唯一真相源是编译期 Schema；一旦有人在注册器里手写
    /// 风险值（"就地改一下"），策略轴（<c>MaxToolRisk</c>）就会与 golden 快照锁定的契约脱钩——
    /// 本用例让这种偏离在测试期立刻暴露。
    /// </remarks>
    [Fact]
    public void ToolRiskAndIdentity_ShouldMatchSchemaValues_WhenRegistered()
    {
        using var provider = CreateProvider(_ => { });
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        registry.AllTools.Should().NotBeEmpty();
        foreach (var definition in registry.AllTools)
        {
            using var document = JsonDocument.Parse(SchemaByToolName[definition.Name]);
            var extension = document.RootElement.GetProperty("x-feishu");

            var expectedRisk = extension.GetProperty("risk").GetString();
            FeishuToolRiskNames.ToLiteral(definition.Risk).Should().Be(expectedRisk,
                $"工具 {definition.Name} 的注册表 Risk 必须来自 Schema 的 x-feishu.risk（D2 单一真相源）");

            extension.GetProperty("identity").GetString().Should().Be(definition.Identity,
                $"工具 {definition.Name} 的注册表 Identity 必须来自 Schema 的 x-feishu.identity（同上）");
        }
    }

    [Fact]
    public void SchemaToolNames_ShouldNeverUseSourceMethodNames_ProtectingContractStability()
    {
        // 防源码方法名污染工具名（§8：bitable.list_fields 的源方法历史上语义错位）。
        SchemaByToolName.Keys.Should().NotContain("bitable.queryfieldspagelist");
        SchemaByToolName.Keys.Should().Contain("bitable.list_fields",
            "字段列表工具强制命名为 bitable.list_fields（工具名契约，不跟随源码方法名）");
    }

    [Fact]
    public void ToolOptions_ShouldHaveRealConsumptionPoints_InFeishuToolsSources()
    {
        // R5 规则 2：新增配置属性必须有真实消费点（FeishuToolBinding / AddFeishuTools / EditMessageChannel）。
        var sources = GetFeishuToolsSources();
        sources.Should().NotBeEmpty();

        sources.Any(path => File.ReadAllText(path).Contains("MaxToolResultLength", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.MaxToolResultLength 必须在 FeishuTools 包中被消费（结果截断）");

        sources.Any(path => File.ReadAllText(path).Contains("EnforceToolAuthorization", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.EnforceToolAuthorization 必须在 FeishuTools 包中被消费（授权门禁）");

        sources.Any(path => File.ReadAllText(path).Contains(".MapTool(", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.Tools 白名单必须在 FeishuTools 包中被消费（MapTool 等价物）");

        sources.Any(path => File.ReadAllText(path).Contains("WriteAllowList", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.WriteAllowList 必须在 FeishuTools 包中被消费（写工具白名单单独键控，Phase 2 §4）");

        sources.Any(path => File.ReadAllText(path).Contains("MaxStreamChunkLength", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.MaxStreamChunkLength 必须在 FeishuTools 包中被消费（流式分片编辑阈值，Phase 2 §3.1）");

        sources.Any(path => File.ReadAllText(path).Contains("RequireMentionInGroup", StringComparison.Ordinal))
            .Should().BeTrue("ImConversationOptions.RequireMentionInGroup 必须在 FeishuTools 包中被消费（群聊 @ 过滤，AI-FD-D12 P2D-5a）");

        sources.Any(path => File.ReadAllText(path).Contains("AllowP2pConversation", StringComparison.Ordinal))
            .Should().BeTrue("ImConversationOptions.AllowP2pConversation 必须在 FeishuTools 包中被消费（单聊会话开关，AI-FD-D12 P2D-5a）");

        // R3 新增策略键（AT-B13/AT-F14）：配置面治理要求"每个公开配置属性必须有真实消费点"
        // ——这三项分别在 ExecuteAsync 的策略判定与出站内容安全阶段被读取，此处把该事实锁住。
        sources.Any(path => File.ReadAllText(path).Contains("MaxToolRisk", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.MaxToolRisk 必须在 FeishuTools 包中被消费（策略轴：风险上限，AT-B13）");

        sources.Any(path => File.ReadAllText(path).Contains("AllowedIdentities", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.AllowedIdentities 必须在 FeishuTools 包中被消费（策略轴：身份闭集，AT-B13）");

        sources.Any(path => File.ReadAllText(path).Contains("ContentSafetyMode", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.ContentSafetyMode 必须在 FeishuTools 包中被消费（出站内容安全模式，AT-F14）");
    }

    [Fact]
    public void AddFeishuTools_ShouldRegisterExactlyTheContractTools_NoneEnabledByDefault()
    {
        using var provider = CreateProvider(_ => { });

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.AllTools.Select(t => t.Name).Should().BeEquivalentTo(FeishuToolNames.All,
            "全部域的客户端齐备时，注册表恰好覆盖契约表（含 3 个新增通讯录工具）");
        registry.EnabledTools.Should().BeEmpty("工具默认收进注册表不启用，需白名单显式 MapTool（安全默认）");
        registry.AllTools.Should().OnlyContain(
            t => FeishuToolNames.IsWriteTool(t.Name) == t.IsWrite,
            "注册表 IsWrite 元数据与契约表逐一一致");
    }

    [Fact]
    public void ToolsWhitelist_ShouldMapRegisteredTools_AndFailFastOnUnknownName()
    {
        using var provider = CreateProvider(
            options => options.Tools = [FeishuToolNames.BitableListTables, FeishuToolNames.WikiGetNode]);

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.EnabledTools.Select(t => t.Name).Should().BeEquivalentTo(
            [FeishuToolNames.BitableListTables, FeishuToolNames.WikiGetNode],
            "FeishuAgent:Tools 配置白名单是 MapTool 的配置面等价物");

        var unknownInvocation = () =>
        {
            using var p = CreateProvider(options => options.Tools = ["not.a.tool"]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };
        unknownInvocation.Should().Throw<InvalidOperationException>(
            "白名单中的未注册名字必须 fail-fast（防配置漂移）");
    }

    /// <summary>
    /// T2-4 / 决策 D-1 ⓑ：白名单启用了 <c>identity=user</c> 的工具而 <c>AllowedIdentities</c> 未放行
    /// 该身份时，<b>装配期</b>即抛可读异常（不是运行期被策略轴静默拒绝）。
    /// </summary>
    /// <remarks>
    /// 落点说明：<c>FeishuAgentOptions.Validate()</c> 感知不到工具面（身份在注册表、白名单映射在装配层），
    /// 故校验在 <c>BuildRegistry</c> 完成白名单映射之后进行（<c>FeishuToolsServiceCollectionExtensions</c>）。
    /// </remarks>
    [Fact]
    public void EnabledUserIdentityTool_WithoutAllowedIdentity_ShouldFailFastAtAssembly()
    {
        var enableUserTool = () =>
        {
            using var p = GuardProviderFactory.CreateProvider(o => o.Tools = [FeishuToolNames.TaskListMyTasks]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };

        enableUserTool.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowedIdentities*")
            .WithMessage("*task.list_my_tasks*");

        // 放行 user 身份后即正常启用（工具面与策略面一致，不存在"配了却用不了"）。
        using var allowed = GuardProviderFactory.CreateProvider(o =>
        {
            o.Tools = [FeishuToolNames.TaskListMyTasks];
            o.AllowedIdentities = ["tenant", "user"];
        });
        allowed.GetRequiredService<FeishuToolRegistry>().EnabledTools
            .Select(static t => t.Name)
            .Should().Equal(FeishuToolNames.TaskListMyTasks);
    }

    [Fact]
    public void Whitelists_ShouldEnforceReadWriteSeparation()
    {
        // 写工具不得经只读白名单（Tools）启用——写工具必须单独键控且过授权门禁（Phase 2 §4 安全默认）。
        var writeInReadonlyList = () =>
        {
            using var p = CreateProvider(options => options.Tools = [FeishuToolNames.ImSendMessage]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };
        writeInReadonlyList.Should().Throw<InvalidOperationException>().WithMessage("*WriteAllowList*");

        // 只读工具不得经写白名单（WriteAllowList）启用。
        var readonlyInWriteList = () =>
        {
            using var p = CreateProvider(options => options.WriteAllowList = [FeishuToolNames.BitableListTables]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };
        readonlyInWriteList.Should().Throw<InvalidOperationException>().WithMessage("*只读*");

        // 写白名单可正常启用写工具。
        using var provider = CreateProvider(options => options.WriteAllowList = [FeishuToolNames.ImSendMessage]);
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.EnabledTools.Select(t => t.Name).Should().BeEquivalentTo(
            [FeishuToolNames.ImSendMessage], "写工具经 FeishuAgent:WriteAllowList 单独键控启用");
    }

    [Fact]
    public void ToolSource_ShouldExposeEnabledToolsOnly_WithGeneratedSchemas()
    {
        using var provider = CreateProvider(
            options => options.Tools = [FeishuToolNames.BitableListTables]);

        var source = provider.GetRequiredService<FeishuAgentToolSource>();
        var tools = source.GetTools(provider);

        tools.Should().ContainSingle("仅白名单启用工具暴露给模型");
        tools[0].Name.Should().Be(FeishuToolNames.BitableListTables);
        tools[0].Description.Should().NotBeNullOrEmpty();

        // JsonSchema 是纯参数 Schema（MEAI 契约，见 FeishuToolAIFunctionTests 的回归守卫）。
        var schema = tools[0].JsonSchema;
        schema.GetProperty("properties").GetProperty("app_token").Should().NotBeNull();
    }

    /// <summary>
    /// 高基数纪律（AI-FD-D12 §三 原则 8）：AI 侧三指标（feishu.tool.executions / tool.duration /
    /// agent.llm.duration）的 tags 只允许 tool/app_key/outcome/agent 受控维度——
    /// conversation_key/chat_id/user_id <b>禁止</b>作为 Metrics tag（只允许进 Span 属性与审计载荷）。
    /// </summary>
    /// <remarks>源码扫描锁定：诊断类中的指标记录调用不得引用键维度常量/字面量。</remarks>
    [Fact]
    public void ToolMetrics_ShouldNotUseHighCardinalityTags_SourceScan()
    {
        var diagnosticsPath = GetFeishuToolsSources()
            .FirstOrDefault(path => path.EndsWith("FeishuToolDiagnostics.cs", StringComparison.Ordinal));

        diagnosticsPath.Should().NotBeNull("FeishuToolDiagnostics.cs 应存在于 FeishuTools 包");
        var diagnosticsSource = File.ReadAllText(diagnosticsPath!);

        diagnosticsSource.Should().Contain("FeishuMetrics.ToolExecutions", "工具执行计数指标存在");
        diagnosticsSource.Should().Contain("FeishuMetrics.ToolDuration", "工具耗时指标存在");

        // 受控维度白名单：tool / app_key / outcome（键维度一旦引入会爆炸指标序列）。
        diagnosticsSource.Should().NotContain("ConversationKey", "conversation 键不得进 Metrics tag");
        diagnosticsSource.Should().NotContain("ChatId", "chat_id 不得进 Metrics tag");
        diagnosticsSource.Should().NotContain("UserId", "user_id 不得进 Metrics tag");
    }

    /// <summary>构造带 mock 飞书客户端的容器（分域执行器解析强类型接口）。</summary>
    /// <remarks>与目录/回归用例共用 <see cref="GuardProviderFactory"/>（新增域时只需补一处 mock）。</remarks>
    private static ServiceProvider CreateProvider(Action<FeishuAgentOptions> configureOptions)
        => GuardProviderFactory.CreateProvider(configureOptions);

    private static IReadOnlyDictionary<string, string> SchemaByToolName => FeishuToolSchemas.SchemaByToolName;

    private static List<string> GetFeishuToolsSources()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return Directory.GetFiles(Path.Combine(dir!, "Mud.Feishu.AI.FeishuTools"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();
    }
}
