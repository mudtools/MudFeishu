// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tests.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-3 / F-9：MCP envelope 导出方言（<see cref="ToolSchemaDialect.McpEnvelope"/>）的契约守卫。
/// </summary>
/// <remarks>
/// <para>
/// <b>锁定什么</b>：本方言是"面向进程外消费者的一次性自省出口"，它的价值完全取决于
/// <b>与工具面同源</b>——一旦条目集合、名映射或标注与真实工具面漂移，宿主/桥接器就会按一份
/// 过时清单工作，而这种漂移在两侧各自都没有编译期交叉点。
/// </para>
/// <para>
/// <b>名映射为何需要守卫</b>：契约名 → MCP 名（<c>.</c> → <c>_</c>）在
/// <c>Mud.Feishu.AI.Tools</c>（本方言）与 <c>Mud.Feishu.AI.Mcp</c>（协议层）各有一份实现——
/// 依赖方向（Mcp → Tools）禁止复用。测试工程同时引用两包，故由本守卫把两处实现<b>机械绑在一起</b>。
/// </para>
/// <para>
/// <b>自证</b>：<see cref="Diff_ShouldReportDrift_OtherwiseGuardIsFalseGreen"/> 用合成输入驱动
/// 同一个比较器并断言它必须报出差异。
/// </para>
/// </remarks>
public class McpEnvelopeExportContractGuards
{
    private const string EnvelopeDialect = "mcp-envelope";

    private static JsonObject BuildEnvelope()
        => JsonNode.Parse(new FeishuToolSchemaExporter().Export(ToolSchemaDialect.McpEnvelope))!.AsObject();

    [Fact]
    public void McpEnvelope_ShouldCoverEveryCuratedTool_Bidirectionally()
    {
        var envelope = BuildEnvelope();
        var entries = envelope["tools"]!.AsArray();

        entries.Should().NotBeEmpty("导出为空说明扫描面坏了（假绿）");

        var titles = entries.Select(static t => t!["title"]!.GetValue<string>()).ToArray();

        titles.Should().BeEquivalentTo(
            FeishuToolNames.All,
            "导出条目必须等于工具名契约表（双向：不许多、不许少）");
    }

    [Fact]
    public void McpEnvelope_NameMapping_ShouldMatchMcpProtocolMapping()
    {
        var entries = BuildEnvelope()["tools"]!.AsArray();

        var mismatches = new List<string>();
        foreach (var entry in entries)
        {
            var title = entry!["title"]!.GetValue<string>();
            var name = entry!["name"]!.GetValue<string>();

            var expected = Mud.Feishu.AI.Mcp.McpToolNames.ToMcpName(title);
            if (!string.Equals(name, expected, StringComparison.Ordinal))
            {
                mismatches.Add($"{title}: 导出 {name} ≠ 协议层 {expected}");
            }
        }

        mismatches.Should().BeEmpty(
            "名映射两处实现必须逐条一致（依赖方向禁止复用，故由本守卫绑定）：{0}",
            string.Join(" | ", mismatches));
    }

    [Fact]
    public void McpEnvelope_Annotations_ShouldMatchContractFacts()
    {
        var entries = BuildEnvelope()["tools"]!.AsArray();

        var mismatches = new List<string>();
        foreach (var entry in entries)
        {
            var title = entry!["title"]!.GetValue<string>();
            var annotations = entry["annotations"]!.AsObject();
            var contract = FeishuToolContracts.ByToolName[title];

            if (annotations["readOnlyHint"]!.GetValue<bool>() != !contract.IsWrite)
            {
                mismatches.Add($"{title}: readOnlyHint 与 IsWrite 不一致");
            }

            var expectedDestructive = contract.Risk == FeishuToolRisk.HighRiskWrite;
            if (annotations["destructiveHint"]!.GetValue<bool>() != expectedDestructive)
            {
                mismatches.Add($"{title}: destructiveHint 与 Risk 不一致");
            }
        }

        mismatches.Should().BeEmpty("标注必须由契约表推导（不得手写）：{0}", string.Join(" | ", mismatches));
    }

    [Fact]
    public void McpEnvelope_InputSchema_ShouldShareTheOpenAiDialectSource()
    {
        var exporter = new FeishuToolSchemaExporter();

        var envelopeSchemaByTitle = BuildEnvelope()["tools"]!.AsArray()
            .ToDictionary(
                static t => t!["title"]!.GetValue<string>(),
                static t => t!["inputSchema"]!.ToJsonString(),
                StringComparer.Ordinal);

        var openAiSchemaByTitle = JsonNode.Parse(exporter.Export(ToolSchemaDialect.OpenAiFunctions))!
            .AsArray()
            .ToDictionary(
                static t => t!["function"]!["name"]!.GetValue<string>(),
                static t => t!["function"]!["parameters"]!.ToJsonString(),
                StringComparer.Ordinal);

        envelopeSchemaByTitle.Keys.Should().BeEquivalentTo(openAiSchemaByTitle.Keys,
            "两方言必须覆盖同一工具集（否则'同源'只覆盖一半）");

        foreach (var (title, schema) in envelopeSchemaByTitle)
        {
            schema.Should().Be(openAiSchemaByTitle[title],
                $"'{title}' 的入参 Schema 必须与 OpenAI 方言同源（任一另外取一份即产生第二真相源）");
        }
    }

    [Fact]
    public void McpEnvelope_ShouldBeDeterministic_AndOrderedByContractName()
    {
        var first = new FeishuToolSchemaExporter().Export(ToolSchemaDialect.McpEnvelope);
        var second = new FeishuToolSchemaExporter().Export(ToolSchemaDialect.McpEnvelope);

        first.Should().Be(second, "导出必须是确定性纯函数（CI 差异比对依赖逐字节可比）");

        var envelope = JsonNode.Parse(first)!.AsObject();
        envelope["dialect"]!.GetValue<string>().Should().Be(EnvelopeDialect);

        var titles = envelope["tools"]!.AsArray().Select(static t => t!["title"]!.GetValue<string>()).ToArray();
        titles.Should().BeInAscendingOrder(StringComparer.Ordinal,
            "条目顺序必须稳定（Ordinal 升序），否则 CI 会出现假差异");
    }

    [Fact]
    public void McpEnvelope_ShouldNotDependOnHostEnablement_UnlikeTheMcpServer()
    {
        // 无白名单的宿主（GuardProviderFactory 默认 Tools/WriteAllowList 皆空）也必须导出全量工具：
        // 本方言是"能力存在"语义，不是"是否启用"语义（这正是它适合 CI 的原因）。
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        var entries = JsonNode.Parse(exporter.Export(ToolSchemaDialect.McpEnvelope))!["tools"]!.AsArray();

        entries.Count.Should().Be(FeishuToolNames.All.Length,
            "导出与宿主白名单无关（与 MCP server 的 tools/list 语义刻意不同）");
    }

    /// <summary>负例自证：比较器必须真的能报出漂移（否则上面几条是假绿）。</summary>
    [Fact]
    public void Diff_ShouldReportDrift_OtherwiseGuardIsFalseGreen()
    {
        var drifts = Diff(
            ["bitable.query_records"],
            ["bitable_query_records", "bitable_missing"]);

        drifts.Should().NotBeEmpty("比较器必须能报出名映射/集合漂移（报不出说明它坏了）");
        drifts.Should().ContainMatch("*bitable_missing*", "只多不少也必须被点名");

        Diff(["bitable_query_records"], ["bitable_query_records"]).Should().BeEmpty("一致时不得报红");

        // 反向自证：把"契约名"与"映射后的 MCP 名"混用必须被报出（这正是用户需锁的漂移形态）。
        Diff(["bitable.query_records"], ["bitable_query_records"])
            .Should().NotBeEmpty("契约名与 MCP 名混用必须报红（映射实现漂移的典型症状）");
    }

    [Fact]
    public void NameMappingRule_ShouldBeTheDotToUnderscoreRule()
    {
        Mud.Feishu.AI.Mcp.McpToolNames.ToMcpName("bitable.query_records").Should().Be("bitable_query_records");
        Mud.Feishu.AI.Mcp.McpToolNames.ToMcpName("feishu.schema_read").Should().Be("feishu_schema_read");

        // 自证：这条规则不是"任何字符串都相等"的恒真判据。
        Mud.Feishu.AI.Mcp.McpToolNames.ToMcpName("a.b").Should().NotBe("a-b");
    }

    /// <summary>比较器的判据（纯函数，便于用合成输入自证）：返回两边的集合差。</summary>
    private static List<string> Diff(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var drifts = new List<string>();
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var actualSet = actual.ToHashSet(StringComparer.Ordinal);

        drifts.AddRange(expectedSet.Except(actualSet).Select(static n => $"缺失: {n}"));
        drifts.AddRange(actualSet.Except(expectedSet).Select(static n => $"多余: {n}"));
        return drifts;
    }
}
