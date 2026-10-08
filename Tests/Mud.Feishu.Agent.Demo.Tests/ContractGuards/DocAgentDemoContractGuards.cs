// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests.ContractGuards;

/// <summary>
/// 文档业务智能体 Demo 的<b>契约守卫</b>：锁定「文档 — 代码 — 工具面」三方不漂移。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么放在本测试工程而不是 <c>Mud.Feishu.AI.Tools.Tests/ContractGuards</c></b>
/// （设计文档 §13.2 的原始落点）：那边是多 TFM（net8.0;net10.0），而被测工程
/// <c>Demos/Mud.Feishu.Agent.Demo</c> 是单 TFM net10.0 —— 加 ProjectReference 会让 net8.0 目标
/// <c>NU1201</c>。守卫的断言强度来自"直接读取 Demo 的真实常量"，而不是它的目录位置，故在此落点，
/// 守卫内容与设计文档 §13.2 的四条逐条对应。
/// </para>
/// <para>
/// <b>本仓库的守卫风格</b>：与 <c>ToolSurfaceScaleContractGuards</c> /
/// <c>TokenMultiAppContractGuards</c> / <c>FeishuToolProfileContractGuards</c> 一致——
/// 断言编译期事实，不做"跑一遍看会不会炸"的集成式验证（后者在 CI 里不稳定）。
/// </para>
/// </remarks>
public class DocAgentDemoContractGuards
{
    /// <summary>
    /// 守卫 ①：白名单工具名必须全部存在于工具面真相源 <see cref="FeishuToolNames.All"/>。
    /// </summary>
    /// <remarks>防"工具改名/删除后 Demo 要到启动期才炸"（RK-9）。</remarks>
    [Fact]
    public void Whitelist_ShouldBeSubsetOf_ToolNamesAll()
    {
        var all = FeishuToolNames.All.ToHashSet(StringComparer.Ordinal);

        var whitelisted = DocAgentSettings.ReadonlyTools
            .Concat(DocAgentSettings.WriteTools)
            .ToArray();

        whitelisted.Should().OnlyHaveUniqueItems("同一工具不得同时出现在读写两个白名单");
        whitelisted.Should().BeSubsetOf(all,
            "白名单里的每个工具名必须是工具面真相源中的真实契约名");

        DocAgentSettings.ReadonlyTools.Should().HaveCount(14, "设计文档 §4.3 的只读面为 14 枚");
        DocAgentSettings.WriteTools.Should().HaveCount(13, "设计文档 §4.3 的写面为 13 枚");
        whitelisted.Should().HaveCount(27, "启用的总工具面为 27 枚（只读 14 + 写 13）");
    }

    /// <summary>
    /// 守卫 ②：写/读分类必须与工具面事实一致——写白名单里的每个工具在编译期契约中
    /// <c>IsWrite == true</c>（⇒ 必然被 <c>ApprovalRequiredAIFunction</c> 包装），
    /// 只读白名单里的每个工具 <c>IsWrite == false</c>（⇒ 不进审批管线）。
    /// </summary>
    /// <remarks>
    /// 断言的是<b>事实分类</b>而非反射私有方法：包装判据在 SDK 侧就是
    /// <c>definition.IsWrite ? new ApprovalRequiredAIFunction(function) : function</c>。
    /// </remarks>
    [Fact]
    public void Whitelist_ShouldMatch_ApprovalGateClassification()
    {
        foreach (var name in DocAgentSettings.WriteTools)
        {
            FeishuToolContracts.ByToolName.Should().ContainKey(name);
            FeishuToolContracts.ByToolName[name].IsWrite.Should().BeTrue(
                $"{name} 在写白名单中，必须被分类为写工具（进而被审批管线包装）");
        }

        foreach (var name in DocAgentSettings.ReadonlyTools)
        {
            FeishuToolContracts.ByToolName.Should().ContainKey(name);
            FeishuToolContracts.ByToolName[name].IsWrite.Should().BeFalse(
                $"{name} 在只读白名单中，不得被分类为写工具");
        }
    }

    /// <summary>
    /// 守卫 ③：授权器预授权权限面必须覆盖全部白名单工具的 <c>RequiredScopes</c>。
    /// </summary>
    /// <remarks>
    /// 漏登记一条权限点 ⇒ 对应工具会被授权器以"权限点未预授权"拒掉（fail-closed 但**不可用**）。
    /// 这条守卫把"白名单扩了但忘了扩权限面"变成构建期红灯。
    /// </remarks>
    [Fact]
    public void AllowedScopes_ShouldCover_EveryWhitelistedTool()
    {
        var allowed = DocAgentSettings.AllowedScopes.ToHashSet(StringComparer.Ordinal);

        var missing = DocAgentSettings.ReadonlyTools
            .Concat(DocAgentSettings.WriteTools)
            .SelectMany(name => FeishuToolContracts.ByToolName[name].RequiredScopes)
            .Distinct(StringComparer.Ordinal)
            .Where(scope => !allowed.Contains(scope))
            .ToArray();

        missing.Should().BeEmpty(
            "以下权限点被白名单工具声明但未进 Authorizer 预授权集合，会导致工具被授权器拒绝："
            + string.Join("、", missing));
    }

    /// <summary>
    /// 守卫 ④：文档业务域必须全部是 <c>tenant</c> 身份。
    /// </summary>
    /// <remarks>
    /// 这是本 Demo 与 <c>ToolsDemo</c> 的<b>反向教学点</b>：那边必须写 <c>["tenant","user"]</c>
    /// （含 <c>task.list_my_tasks</c>），而文档业务域按事实推导就只是 <c>["tenant"]</c>。
    /// 一旦将来某轮把 user 身份工具塞进白名单，本守卫会先于运行期报红。
    /// </remarks>
    [Fact]
    public void Whitelist_ShouldBeTenantIdentityOnly()
    {
        var identities = DocAgentSettings.ReadonlyTools
            .Concat(DocAgentSettings.WriteTools)
            .Select(name => FeishuToolContracts.ByToolName[name].Identity)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        identities.Should().BeEquivalentTo([FeishuToolIdentityNames.Tenant],
            "文档业务域（docx/wiki/drive/sheets/bitable/search）全部是 tenant 身份");
    }

    /// <summary>
    /// 守卫 ⑤：斜杠命令集必须与设计文档 §5.2 的命令表一一对应。
    /// </summary>
    /// <remarks>
    /// 常量数组 + 计数断言（设计文档 §13.2 指定的固化方式）：既锁定"命令表有多少条"，
    /// 也锁定"每一条具体是什么"。文档表格中标注的别名（<c>?</c>/<c>t</c>/<c>y</c>/<c>quit</c> …）
    /// 以 <c>/</c> 前缀形态生效，见 <c>AgentConsoleLoopTests</c>。
    /// </remarks>
    [Fact]
    public void CommandNames_ShouldMatchTheDesignDocument()
    {
        string[] documented =
        [
            "help", "tools", "guidance", "pending", "approve", "deny", "abandon",
            "policy", "dryrun", "audit", "export", "usage", "history", "reset",
            "scenario", "verbose", "exit",
        ];

        AgentConsoleLoop.CommandNames.Should().BeEquivalentTo(documented);
        AgentConsoleLoop.CommandNames.Should().HaveCount(
            documented.Length,
            "命令集与设计文档 §5.2 的命令表必须逐条对应（改一处必须同批改另一处）");
    }

    /// <summary>
    /// 守卫 ⑥：六部剧本依赖的工具链必须全部在白名单内。
    /// </summary>
    /// <remarks>
    /// 期望链路逐条抄自设计文档 §5.4（剧本详设表）。本守卫的价值在于：
    /// 若白名单被收紧（例如删掉 <c>docx.replace_document</c>），剧本会变成"引导模型去调一个未启用工具"
    /// ——那是一个<b>只能靠人肉发现</b>的坏体验，此处前置为构建期红灯。
    /// </remarks>
    [Fact]
    public void ScenarioToolChains_ShouldAllBeWhitelisted()
    {
        var whitelisted = DocAgentSettings.ReadonlyTools
            .Concat(DocAgentSettings.WriteTools)
            .ToHashSet(StringComparer.Ordinal);

        var chains = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["kb-digest"] = ["wiki.list_nodes", "wiki.get_node", "docx.get_raw_content"],
            ["doc-authoring"] = ["docx.create_document", "docx.append_blocks", "docx.get_raw_content"],
            ["md-import"] =
            [
                "docx.import_markdown", "docx.create_document", "docx.append_blocks", "docx.replace_document",
            ],
            ["sheet-sync"] = ["sheets.list_sheets", "sheets.get_range_values", "sheets.append_rows"],
            ["safe-delete"] = ["docx.get_document_blocks", "docx.delete_blocks"],
            ["self-heal"] = ["docx.get_raw_content", "search.doc_wiki"],
        };

        var book = ScenarioBook.CreateDefault(TestDoubles.CreateSettings());

        book.All.Select(static s => s.Name).Should().BeEquivalentTo(
            chains.Keys,
            "剧本清单变更时必须同批更新本守卫的期望链路表");

        var violations = new List<string>();
        foreach (var (scenario, tools) in chains)
        {
            foreach (var tool in tools)
            {
                FeishuToolNames.All.Should().Contain(tool, $"剧本 {scenario} 引用的工具名必须有效");
                if (!whitelisted.Contains(tool))
                {
                    violations.Add($"{scenario} → {tool}");
                }
            }
        }

        violations.Should().BeEmpty(
            "以下剧本依赖的工具未启用（引导模型去调未启用工具只会得到「未注册工具」错误）："
            + string.Join("、", violations));
    }

    /// <summary>
    /// 守卫 ⑦：Demo 不得引入自动重试依赖。
    /// </summary>
    /// <remarks>
    /// 与 SDK 口径一致（全仓无 Polly / <c>Microsoft.Extensions.Resilience</c>）：
    /// 重试语义由模型依据 <c>ToolErrorKind</c> 自行决定，剧本 S6 专门演示。
    /// </remarks>
    [Fact]
    public void DemoProject_ShouldNotReferenceResilienceLibraries()
    {
        var csproj = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Demos", "Mud.Feishu.Agent.Demo", "Mud.Feishu.Agent.Demo.csproj"));

        csproj.Should().NotContain("Polly");
        csproj.Should().NotContain("Microsoft.Extensions.Resilience");
        csproj.Should().NotContain("Mud.Feishu.OpenTelemetry",
            "OTel Exporter 已决策不做（§0.4）：可观测性只走控制台 Trace + 本地指标");
    }

    /// <summary>
    /// 守卫 ⑧：Demo 只引用既有三个工程（不新增 ProjectReference，E5）。
    /// </summary>
    [Fact]
    public void DemoProject_ShouldKeepTheExistingProjectReferenceSet()
    {
        var csproj = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Demos", "Mud.Feishu.Agent.Demo", "Mud.Feishu.Agent.Demo.csproj"));

        var references = csproj
            .Split('\n')
            .Where(static line => line.Contains("ProjectReference", StringComparison.Ordinal))
            .ToArray();

        references.Should().HaveCount(3, "E5：不新增 ProjectReference");
        references.Should().ContainMatch("*Mud.Feishu.AI.Tools.csproj*");
        references.Should().ContainMatch("*Mud.Feishu.AI.csproj*");
        references.Should().ContainMatch("*Mud.Feishu.csproj*");
    }

    /// <summary>
    /// 守卫 ⑨：宿主钩子不得依赖 <see cref="FeishuToolRegistry"/>（运行期循环依赖防线）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>缺陷原始形态（实测）</b>：授权器/审计 sink 由 <c>FeishuToolBinding</c> <b>可选注入</b>，
    /// 而 <c>FeishuToolRegistry</c> 的构造又会解析 <c>FeishuToolBinding</c>。反向依赖即形成
    /// <c>注册表 → 注册器 → Binding → 授权器 → 注册表</c> 的环：.NET DI 的<b>静态</b>环检测看不见
    /// lambda 工厂里的服务定位调用，运行时表现为<b>启动期无输出卡死</b>（不抛异常、不打印任何东西），
    /// 排障成本极高。风险事实应取编译期契约表（<c>FeishuToolContracts</c>）。
    /// </para>
    /// <para>
    /// 本守卫用反射断言构造参数面，把"环"变成构建期红灯——而不是等下一次有人注入注册表时再卡死。
    /// </para>
    /// </remarks>
    [Fact]
    public void HostHooks_ShouldNotDependOnToolRegistry()
    {
        foreach (var type in new[] { typeof(ConsoleToolAuthorizer), typeof(InMemoryAuditSink) })
        {
            var parameters = type
                .GetConstructors()
                .SelectMany(static c => c.GetParameters())
                .Select(static p => p.ParameterType)
                .ToArray();

            parameters.Should().NotContain(
                typeof(FeishuToolRegistry),
                $"{type.Name} 由 FeishuToolBinding 注入，反向依赖 FeishuToolRegistry 会形成运行期循环依赖（启动期无输出卡死）");
        }
    }

    /// <summary>
    /// 守卫 ⑩：两个配置文件必须随产物复制到**输出目录**。
    /// </summary>
    /// <remarks>
    /// 配置根取 <c>AppContext.BaseDirectory</c>（与工作目录无关）。若忘了 <c>CopyToOutputDirectory</c>，
    /// 表现为"文件明明写了、配置却不生效"，且**不报任何错**（`optional: true`）——正是横幅"配置来源"
    /// 与模板守卫要挡的那类静默失效，故在构建期固化。
    /// </remarks>
    [Fact]
    public void DemoProject_ShouldCopyConfigurationFilesToOutput()
    {
        var csproj = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Demos", "Mud.Feishu.Agent.Demo", "Mud.Feishu.Agent.Demo.csproj"));

        csproj.Should().Contain(DocAgentSettings.AppSettingsFile);
        csproj.Should().Contain(DocAgentSettings.LocalAppSettingsFile);
        csproj.Should().Contain(
            "CopyToOutputDirectory",
            "配置文件必须在输出目录（配置根 = AppContext.BaseDirectory），否则纯文件方式静默失效");
    }

    /// <summary>以 <c>Mud.Feishu.slnx</c> 为锚定位仓库根目录（与既有守卫同款）。</summary>
    private static string FindRepositoryRoot() => TestDoubles.RepositoryRoot();
}
