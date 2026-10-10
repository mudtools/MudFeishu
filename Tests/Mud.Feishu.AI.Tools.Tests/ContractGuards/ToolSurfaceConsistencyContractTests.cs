// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// BUG-4：<b>双工具来源链的"能力对齐"守卫</b>（进程内 Agent 链 vs MCP 链）。
/// </summary>
/// <remarks>
/// <para>
/// <b>两条链的存在与差异</b>：进程内 Agent 走 <c>FeishuAgentToolSource.GetTools</c>，把<b>写类</b>工具包进
/// MEAI 的 <c>ApprovalRequiredAIFunction</c>；MCP 走注册表 + <see cref="IFeishuToolFunctionFactory"/> 构桥，
/// <b>不包</b>（MCP 不走 MAF 管线，该标记的强制点在 <c>FunctionInvokingChatClient</c>，照搬会让"需人工确认"
/// 静默丢失——MCP 的真实门禁是执行链授权门禁）。差异是<b>刻意的</b>，故本守卫不要求两条链产出相同类型，
/// 而是要求：<b>工具集与关键元数据逐条一致，且唯一差异被显式白名单化</b>。
/// </para>
/// <para>
/// <b>为什么必须加这条守卫</b>：未来新增工具面能力（Approval 标记、guidance 注入、新结果整形）必须
/// <b>同时改两处</b>；漏改一条会静默退化——两条链都没有编译期交叉点。守卫把"另一条链也要动"变成红灯。
/// </para>
/// <para>
/// <b>自证</b>：<see cref="Alignment_ShouldReportMetadataDrift_OtherwiseGuardIsFalseGreen"/> 用合成数据
/// 驱动同一个比较器并断言它必须报出差异。
/// </para>
/// </remarks>
public class ToolSurfaceConsistencyContractTests
{
    private const string ReadTool = "feishu.schema_read";
    private const string WriteTool = "im.send_message";
    private const string AppKey = "cli_test";

    private static ServiceProvider BuildProvider()
    {
        var options = new FeishuAgentOptions
        {
            Instructions = "test",
            Tools = [ReadTool],
            WriteAllowList = [WriteTool],
        };

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(options))
            // 作用域工厂的三个核心依赖（与生成的 HTTP 客户端同构）。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            // 写工具要真的"启用"，故其域客户端必须在场。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object);

        services.AddFeishuTools();
        services.AddFeishuMcpServer(o =>
        {
            o.AppKey = AppKey;
            o.IncludeGuidance = false;
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AgentChain_And_McpChain_ShouldExposeTheSameToolSetAndMetadata()
    {
        using var provider = BuildProvider();

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var enabled = registry.EnabledTools;
        enabled.Select(static d => d.Name).Should().BeEquivalentTo([ReadTool, WriteTool],
            "前置：白名单必须真的同时启用一个只读工具与一个写类工具（否则本守卫看不到差异面）");

        // 工具集：Agent 链实际产出的函数名必须等于白名单启用集。
        GetAllAgentFunctions(provider)
            .Select(static f => f.Name)
            .Should().BeEquivalentTo(
                enabled.Select(static d => d.Name),
                "Agent 链（FeishuAgentToolSource.GetTools）产出的工具集必须等于注册表白名单启用集");

        // 元数据：MCP 链的对外元数据必须逐条等于注册表定义（写类工具经白名单启用后不得丢失标记）。
        var definitionEntries = enabled
            .Select(static d => new ToolSurfaceAlignment.SurfaceEntry(d.Name, d.IsWrite, d.Risk, d.Identity))
            .ToArray();
        var mcpEntries = provider.GetRequiredService<FeishuMcpToolServer>().Tools
            .Select(static t => new ToolSurfaceAlignment.SurfaceEntry(t.ContractName, t.IsWrite, t.Risk, t.Identity))
            .ToArray();

        ToolSurfaceAlignment.Diff(definitionEntries, mcpEntries).Should().BeEmpty(
            "两条链必须从同一份注册表产出同一工具集与同一元数据（name / is_write / risk / identity）；"
            + "新增工具面能力时若只改一条链，这里会报红");
    }

    [Fact]
    public void ApprovalWrappingDifference_ShouldBeExactlyTheWriteTools()
    {
        using var provider = BuildProvider();

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var agentFunctions = GetAllAgentFunctions(provider);

        foreach (var definition in registry.EnabledTools)
        {
            var function = agentFunctions.Single(f => f.Name == definition.Name);
            var wrapped = function is ApprovalRequiredAIFunction;

            wrapped.Should().Be(definition.IsWrite,
                $"Agent 链的审批包装判据只能是静态项 IsWrite（'{definition.Name}' 的 IsWrite={definition.IsWrite}）");
        }

        // MCP 链：一律不包装（刻意差异）——用工厂产出直接钉住，避免"哪天顺手也包了一层"的静默退化。
        var factory = provider.GetRequiredService<IFeishuToolFunctionFactory>();
        foreach (var definition in registry.EnabledTools)
        {
            var schemaJson = FeishuToolSchemas.SchemaByToolName[definition.Name];
            var bridged = factory.Create(definition, schemaJson);

            (bridged is ApprovalRequiredAIFunction).Should().BeFalse(
                $"MCP 链不得包 ApprovalRequiredAIFunction（'{definition.Name}'）：MCP 无 MAF 管线，"
                + "该标记的强制点缺席会退化为「需人工确认」被静默丢弃；MCP 的门禁是执行链授权门禁");
        }
    }

    [Fact]
    public void BothChains_ShouldShareTheSameParameterSchema()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var factory = provider.GetRequiredService<IFeishuToolFunctionFactory>();
        var agentFunctions = GetAllAgentFunctions(provider);

        foreach (var definition in registry.EnabledTools)
        {
            var schemaJson = FeishuToolSchemas.SchemaByToolName[definition.Name];
            var bridged = factory.Create(definition, schemaJson);
            var agent = agentFunctions.Single(f => f.Name == definition.Name);

            bridged.JsonSchema.GetRawText().Should().Be(agent.JsonSchema.GetRawText(),
                $"两条链的入参 Schema 必须同源（'{definition.Name}'）：任一链另取来源即产生第二真相源");
        }
    }

    [Fact]
    public void Alignment_ShouldReportMetadataDrift_OtherwiseGuardIsFalseGreen()
    {
        ToolSurfaceAlignment.SurfaceEntry[] left =
        [
            new("im.send_message", true, FeishuToolRisk.Write, "tenant"),
        ];
        ToolSurfaceAlignment.SurfaceEntry[] right =
        [
            new("im.send_message", false, FeishuToolRisk.Read, "user"),
        ];

        ToolSurfaceAlignment.Diff(left, right).Should().NotBeEmpty(
            "比较器必须能报出元数据漂移（报不出说明它坏了——那是假绿）");

        ToolSurfaceAlignment.Diff(left, left).Should().BeEmpty("完全一致时不得报红");
        ToolSurfaceAlignment.Diff(left, []).Should().NotBeEmpty("工具集缺失必须报红");
    }

    /// <summary>聚合进程内 Agent 链的全部工具（与 <c>AddFeishuAgent</c> 的聚合口径一致：全部工具源）。</summary>
    private static IReadOnlyList<AIFunction> GetAllAgentFunctions(IServiceProvider provider) =>
        provider.GetServices<FeishuAgentToolSource>()
            .SelectMany(source => source.GetTools(provider))
            .ToArray();
}

/// <summary>两条链的"能力对齐"比较器（纯函数，便于用合成数据自证）。</summary>
internal static class ToolSurfaceAlignment
{
    /// <summary>一条链上的工具面元数据（判据仅取跨链可比项）。</summary>
    /// <param name="ContractName">契约名（如 <c>im.send_message</c>）。</param>
    /// <param name="IsWrite">是否写类。</param>
    /// <param name="Risk">风险分级。</param>
    /// <param name="Identity">工具身份。</param>
    internal sealed record SurfaceEntry(string ContractName, bool IsWrite, FeishuToolRisk Risk, string Identity);

    /// <summary>返回两条链的差异描述（空 = 对齐）。</summary>
    /// <remarks>
    /// 判据刻意<b>不含</b> approval 包装类型（Agent 包 / MCP 不包是刻意差异，由专门用例锁定），
    /// 只比"工具集 + 元数据"——这些是"必须一致"的部分。
    /// </remarks>
    internal static IReadOnlyList<string> Diff(
        IReadOnlyList<SurfaceEntry> left,
        IReadOnlyList<SurfaceEntry> right)
    {
        var diffs = new List<string>();
        var leftBy = left.ToDictionary(static e => e.ContractName, StringComparer.Ordinal);
        var rightBy = right.ToDictionary(static e => e.ContractName, StringComparer.Ordinal);

        foreach (var name in leftBy.Keys.Where(n => !rightBy.ContainsKey(n)).OrderBy(static n => n, StringComparer.Ordinal))
        {
            diffs.Add($"仅左链存在: {name}");
        }

        foreach (var name in rightBy.Keys.Where(n => !leftBy.ContainsKey(n)).OrderBy(static n => n, StringComparer.Ordinal))
        {
            diffs.Add($"仅右链存在: {name}");
        }

        foreach (var name in leftBy.Keys.Where(rightBy.ContainsKey).OrderBy(static n => n, StringComparer.Ordinal))
        {
            var (a, b) = (leftBy[name], rightBy[name]);

            if (a.IsWrite != b.IsWrite)
            {
                diffs.Add($"{name}: is_write {a.IsWrite} ≠ {b.IsWrite}");
            }

            if (a.Risk != b.Risk)
            {
                diffs.Add($"{name}: risk {a.Risk} ≠ {b.Risk}");
            }

            if (!string.Equals(a.Identity, b.Identity, StringComparison.Ordinal))
            {
                diffs.Add($"{name}: identity {a.Identity} ≠ {b.Identity}");
            }
        }

        return diffs;
    }
}
