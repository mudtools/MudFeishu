// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// B5 feishu.tool_search 元工具测试（方案 §3.B5 测试要求）。
/// </summary>
/// <remarks>
/// 用例：关键字命中、写/读过滤、未启用工具的 enabled=false 如实告知、limit 上界、空结果给候选域建议。
/// 数据源 IToolCatalog 已有实现，可直接注入假注册表。
/// </remarks>
public class ToolSearchTests
{
    private static readonly ToolCatalogEntry[] SampleEntries =
    [
        new("bitable.list_tables", "列出数据表", ["bitable:app:readonly"], false, FeishuToolRisk.Read, "tenant", "Sdk.Foo", """{"type":"object","required":["app_token"],"properties":{}}"""),
        new("bitable.query_records", "查询记录", ["bitable:app:readonly"], false, FeishuToolRisk.Read, "tenant", "Sdk.Bar", """{"type":"object","required":["app_token","table_id"],"properties":{}}"""),
        new("im.send_text", "发送文本消息", ["im:message"], true, FeishuToolRisk.Write, "tenant", "Sdk.Baz", """{"type":"object","required":["receive_id","msg_type","content"],"properties":{}}"""),
        new("drive.list_folder_files", "列出云文档文件", ["drive:drive:readonly"], false, FeishuToolRisk.Read, "tenant", "Sdk.Qux", """{"type":"object","required":["folder_token"],"properties":{}}"""),
    ];

    private static IOptions<FeishuAgentOptions> DefaultOptions()
        => Options.Create(new FeishuAgentOptions { MaxToolResultLength = 8000 });

    private static IToolCatalog CreateCatalog(IEnumerable<ToolCatalogEntry>? entries = null)
    {
        var list = entries?.ToList() ?? SampleEntries.ToList();
        var mock = new Mock<IToolCatalog>();
        mock.Setup(c => c.Entries).Returns(list);
        mock.Setup(c => c.Find(It.IsAny<string>()))
            .Returns<string>(name => list.FirstOrDefault(e => e.Name == name));
        return mock.Object;
    }

    private static FeishuToolRegistry CreateRegistry(params string[] enabledTools)
    {
        var registry = new FeishuToolRegistry();
        // Registry needs definitions first
        foreach (var entry in SampleEntries)
        {
            registry.Register(new FeishuToolDefinition(
                entry.Name, entry.Description, entry.RequiredScopes,
                entry.IsWrite, entry.Risk, entry.Identity,
                (_, _, _) => Task.FromResult(FeishuToolResult.FromText(""))));
        }

        foreach (var name in enabledTools)
        {
            registry.MapTool(name);
        }

        return registry;
    }

    private static ToolSearchTools CreateExecutor(
        IToolCatalog? catalog = null,
        FeishuToolRegistry? registry = null)
    {
        catalog ??= CreateCatalog();
        registry ??= CreateRegistry();
        // ToolSearchTools 构造期不再注入 IToolCatalog/Registry（构建期解析会自我闭环——见该类 remarks），
        // 改经 IServiceProvider 执行期解析：mock GetService 返回测试桩即可。
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IToolCatalog))).Returns(catalog);
        provider.Setup(p => p.GetService(typeof(FeishuToolRegistry))).Returns(registry);
        return new(DefaultOptions(), provider.Object);
    }

    private static JsonDocument ParseResult(FeishuToolResult result)
    {
        var text = result.ToString();
        // The text may be truncated JSON, but for our tests it should be small enough
        return JsonDocument.Parse(text);
    }

    // ────────── 关键字命中 ──────────

    [Fact]
    public async Task SearchAsync_ShouldMatchByKeyword()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["keyword"] = "bitable",
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var total = doc.RootElement.GetProperty("total_matched").GetInt32();
        total.Should().Be(2, "bitable 前缀匹配 2 个工具");
    }

    [Fact]
    public async Task SearchAsync_ShouldMatchByDescription()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["keyword"] = "消息",
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var total = doc.RootElement.GetProperty("total_matched").GetInt32();
        total.Should().Be(1, "描述含'消息'匹配 im.send_text");
    }

    // ────────── 写/读过滤 ──────────

    [Fact]
    public async Task SearchAsync_ShouldFilterWriteOnly()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["write_only"] = true,
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var total = doc.RootElement.GetProperty("total_matched").GetInt32();
        total.Should().Be(1, "只返回写类工具");

        var resultsArray = doc.RootElement.GetProperty("results").EnumerateArray().ToList();
        resultsArray.Should().HaveCount(1);
        resultsArray[0].GetProperty("tool").GetString().Should().Be("im.send_text");
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterReadOnly()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["read_only"] = true,
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var total = doc.RootElement.GetProperty("total_matched").GetInt32();
        total.Should().Be(3, "只返回只读工具（bitable.list_tables, bitable.query_records, drive.list_folder_files）");
    }

    // ────────── 未启用工具 enabled=false ──────────

    [Fact]
    public async Task SearchAsync_ShouldReportEnabledFalse_ForNotEnabledTools()
    {
        // 只启用 bitable.list_tables
        var registry = CreateRegistry("bitable.list_tables");
        var executor = CreateExecutor(registry: registry);

        var arguments = new Dictionary<string, object?>
        {
            ["keyword"] = "bitable",
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var resultsArray = doc.RootElement.GetProperty("results").EnumerateArray().ToList();
        var listTables = resultsArray.First(r => r.GetProperty("tool").GetString() == "bitable.list_tables");
        listTables.GetProperty("enabled").GetBoolean().Should().BeTrue("已启用");

        var queryRecords = resultsArray.First(r => r.GetProperty("tool").GetString() == "bitable.query_records");
        queryRecords.GetProperty("enabled").GetBoolean().Should().BeFalse("未启用");
        queryRecords.TryGetProperty("hint", out var hint).Should().BeTrue("未启用的工具应有提示");
    }

    // ────────── limit 上界 ──────────

    [Fact]
    public async Task SearchAsync_ShouldClampLimitToMax()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["limit"] = 100,  // 超过 MaxLimit=30
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var returned = doc.RootElement.GetProperty("returned").GetInt32();
        returned.Should().BeLessThanOrEqualTo(30, "limit 上界为 30");
    }

    [Fact]
    public async Task SearchAsync_ShouldReportTruncated_WhenResultsExceedLimit()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["limit"] = 1,
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var totalMatched = doc.RootElement.GetProperty("total_matched").GetInt32();
        var returned = doc.RootElement.GetProperty("returned").GetInt32();
        var truncated = doc.RootElement.GetProperty("truncated").GetBoolean();

        totalMatched.Should().Be(4, "全部 4 个工具匹配");
        returned.Should().Be(1, "limit=1 只返回 1 个");
        truncated.Should().BeTrue("匹配数 > 返回数");
    }

    // ────────── 空结果给候选域建议 ──────────

    [Fact]
    public async Task SearchAsync_ShouldGiveDomainSuggestion_WhenNoMatch()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["keyword"] = "nonexistent_capability_xyz",
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var total = doc.RootElement.GetProperty("total_matched").GetInt32();
        total.Should().Be(0, "没有匹配");

        doc.RootElement.TryGetProperty("suggestion", out var suggestion).Should().BeTrue("空结果应给候选域建议");
        suggestion.GetString().Should().NotBeNullOrEmpty();
    }

    // ────────── 域过滤 ──────────

    [Fact]
    public async Task SearchAsync_ShouldFilterByDomain()
    {
        var executor = CreateExecutor();
        var arguments = new Dictionary<string, object?>
        {
            ["domain"] = "drive",
        };

        var result = await executor.SearchAsync(arguments, CancellationToken.None);
        var doc = ParseResult(result);

        var total = doc.RootElement.GetProperty("total_matched").GetInt32();
        total.Should().Be(1, "drive 域只有 1 个工具");

        var resultsArray = doc.RootElement.GetProperty("results").EnumerateArray().ToList();
        resultsArray[0].GetProperty("domain").GetString().Should().Be("drive");
    }
}
