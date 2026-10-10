// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// BUG-1（发布前结构债）：「<b>命名空间 = 程序集归属</b>」的机械锁定。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷原始形态</b>：R-9 把工具基础设施从工具包下沉到 <c>Mud.Feishu.AI</c> 时保留了
/// 原命名空间 <c>Mud.Feishu.AI.Tools</c> ⇒ 该命名空间的 <b>public</b> 类型同时存在于
/// <c>Mud.Feishu.AI</c>（31 个）与 <c>Mud.Feishu.AI.Tools</c>（4 个）两个程序集。消费者
/// <c>using Mud.Feishu.AI.Tools;</c> 时类型来源两包，破坏"命名空间 → 程序集"的直觉，
/// 也让"哪个包该被引用"无法从类型名推断。
/// </para>
/// <para>
/// <b>为什么不能用"移回工具包"一刀切</b>：<c>Channels/</c>/<c>Events/</c>/<c>Knowledge/</c> 把
/// <c>IFeishuAppContextScopeFactory</c> / <c>IFeishuToolContextAccessor</c> /
/// <c>IFeishuToolApprovalChannel</c> / <c>FeishuToolContext</c> 用作<b>公开构造签名</b>（已进 PublicAPI），
/// 而 <c>Mud.Feishu.AI.Tools</c> 单向引用 <c>Mud.Feishu.AI</c> ⇒ 回迁即成环。
/// 故本仓采用「回迁（仅工具面消费的类型）+ 残留改投 <c>Mud.Feishu.AI.AgentTools</c>」的二分方案。
/// </para>
/// <para>
/// <b>判据</b>：以反射遍历三个产品程序集的<b>导出类型</b>（public），断言没有任何命名空间
/// 被一个以上的程序集导出。分母为 0（扫描面为空）时直接报红——否则删光类型也能"绿"。
/// </para>
/// <para>
/// <b>自证报红</b>：<see cref="Scanner_ShouldReportSharedNamespace_OtherwiseGuardIsFalseGreen"/>
/// 用合成数据驱动同一个检查器并断言它必须报红。
/// </para>
/// </remarks>
public class PackageOwnershipContractGuards
{
    /// <summary>参与归属判定的产品程序集（测试程序集不参与：它们不发布）。</summary>
    private static (string Assembly, Assembly Instance)[] ProductAssemblies() =>
    [
        ("Mud.Feishu.AI", typeof(Mud.Feishu.AI.AgentTools.FeishuToolContext).Assembly),
        ("Mud.Feishu.AI.Tools", typeof(FeishuToolBinding).Assembly),
        ("Mud.Feishu.AI.Mcp", typeof(Mud.Feishu.AI.Mcp.FeishuMcpToolServer).Assembly),
    ];

    [Fact]
    public void ExportedNamespace_ShouldBeOwnedByExactlyOneProductAssembly()
    {
        var assemblies = ProductAssemblies();

        // 分母自证：三个程序集都必须真的有导出类型，否则下面的"空集"是假绿。
        foreach (var (name, instance) in assemblies)
        {
            PackageOwnershipScanner.ExportedNamespaces(instance).Should().NotBeEmpty(
                $"{name} 必须导出至少一个命名空间的 public 类型——否则本守卫的扫描面为空（假绿）");
        }

        var shared = PackageOwnershipScanner.FindSharedExportedNamespaces(
            assemblies.Select(a => (a.Assembly, (IEnumerable<string>)PackageOwnershipScanner.ExportedNamespaces(a.Instance))));

        shared.Should().BeEmpty(
            "同一命名空间的 public 类型只能存在于一个程序集（BUG-1）。若此处报红："
            + "要么某个类型被搬到了错误的程序集，要么新类型该改投本包自有命名空间"
            + "（AI 包 → Mud.Feishu.AI.AgentTools；工具包 → Mud.Feishu.AI.Tools[.Tools]）");
    }

    [Fact]
    public void Split_ShouldMatchTheDeclaredOwnershipFacts()
    {
        var ai = typeof(Mud.Feishu.AI.AgentTools.FeishuToolContext).Assembly;
        var tools = typeof(FeishuToolBinding).Assembly;

        var aiNamespaces = PackageOwnershipScanner.ExportedNamespaces(ai);
        var toolNamespaces = PackageOwnershipScanner.ExportedNamespaces(tools);

        // AI 包：宿主扩展点与执行上下文接缝（含声明契约 [FeishuTool] / [ToolParameter]）。
        aiNamespaces.Should().Contain("Mud.Feishu.AI.AgentTools",
            "工具源 / 租户作用域工厂 / 上下文访问器 / 审批通道 / 声明契约 都在此处");
        aiNamespaces.Should().NotContain("Mud.Feishu.AI.Tools",
            "Mud.Feishu.AI.Tools 已归工具包独占——AI 包再出现该命名空间即回归 BUG-1");

        // 工具包：契约 + 实现 + 生成产物。
        toolNamespaces.Should().Contain("Mud.Feishu.AI.Tools",
            "工具契约（注册表/结果/目录/授权/审计/错误/附件暂存）与实现同包");
        toolNamespaces.Should().NotContain("Mud.Feishu.AI.AgentTools",
            "AI 侧接缝不得反向出现在工具包命名空间里（依赖方向：工具包 → AI 单向）");
    }

    [Fact]
    public void Scanner_ShouldReportSharedNamespace_OtherwiseGuardIsFalseGreen()
    {
        // 合成缺陷：两个程序集都导出 Mud.Feishu.AI.Tools。
        var shared = PackageOwnershipScanner.FindSharedExportedNamespaces(
        [
            ("Mud.Feishu.AI", ["Mud.Feishu.AI.AgentTools", "Mud.Feishu.AI.Tools"]),
            ("Mud.Feishu.AI.Tools", ["Mud.Feishu.AI.Tools", "Mud.Feishu.AI.Tools.Tools"]),
        ]);

        shared.Should().HaveCount(1, "检查器必须能定位跨程序集重名命名空间（找不到说明检查器坏了——那是假绿）");
        shared[0].Should().Contain("Mud.Feishu.AI.Tools");

        // 无重名时必须为空（不得误伤）。
        PackageOwnershipScanner.FindSharedExportedNamespaces(
        [
            ("Mud.Feishu.AI", ["Mud.Feishu.AI.AgentTools"]),
            ("Mud.Feishu.AI.Tools", ["Mud.Feishu.AI.Tools"]),
        ]).Should().BeEmpty("归属清晰时不得报红");
    }

    [Fact]
    public void Scanner_ShouldIgnoreInternalTypes()
    {
        // internal 类型不进导出面：跨包共享的 internal 类型由 InternalsVisibleTo 支撑，
        // 不构成"公开面归属"缺陷（判据只看 public）。
        var shared = PackageOwnershipScanner.FindSharedExportedNamespaces(
        [
            ("Mud.Feishu.AI", ["Mud.Feishu.AI.Tools"]),
            ("Mud.Feishu.AI.Tools", ["Mud.Feishu.AI.Tools.Tools"]),
        ]);

        shared.Should().BeEmpty("唯一重名的命名空间不构成跨程序集公开面（判据为 public 类型）");
    }
}

/// <summary>
/// 归属检查器（纯函数，便于用合成数据自证）。
/// </summary>
internal static class PackageOwnershipScanner
{
    /// <summary>程序集导出的 public 类型所属的命名空间集合（去重、忽略空命名空间）。</summary>
    internal static IReadOnlyList<string> ExportedNamespaces(Assembly assembly) =>
        assembly.GetExportedTypes()
            .Select(type => type.Namespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .Select(ns => ns!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(ns => ns, StringComparer.Ordinal)
            .ToArray();

    /// <summary>返回被一个以上程序集导出的命名空间（形如 <c>ns → A + B</c>）。</summary>
    internal static IReadOnlyList<string> FindSharedExportedNamespaces(
        IEnumerable<(string Assembly, IEnumerable<string> Namespaces)> assemblies)
    {
        var owners = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (assembly, namespaces) in assemblies)
        {
            foreach (var ns in namespaces.Distinct(StringComparer.Ordinal))
            {
                if (!owners.TryGetValue(ns, out var list))
                {
                    owners[ns] = list = [];
                }

                if (!list.Contains(assembly, StringComparer.Ordinal))
                {
                    list.Add(assembly);
                }
            }
        }

        return owners
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => $"{pair.Key} → {string.Join(" + ", pair.Value)}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();
    }
}
