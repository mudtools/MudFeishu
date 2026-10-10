// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.Tools.Tests.Mcp;

/// <summary>
/// MCP server（R7 / C6b）机械守卫：包边界、命名映射、solution 登记。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么"不得直连 SDK 客户端"要用反射断言而不是扫源码</b>：源码扫描会被注释/字符串里的
/// <c>IFeishu*</c> 命中（本包文档里大量提及），只能做成"带排除规则"的脆弱规则；而
/// <b>类型引用的结构事实</b>（字段/属性/方法签名里有没有 SDK 程序集的类型）是编译器保证的，
/// 且没有假阳性。检查器本身用"故意持有 SDK 客户端类型的合成类型"自证（见
/// <see cref="SdkClientDependencyScanner_ShouldDetect_CanaryType"/>）。
/// </para>
/// </remarks>
public class McpServerContractTests
{
    private const string McpProject = "Mud.Feishu.AI.Mcp";
    private const string SdkAssemblyName = "Mud.Feishu";

    /// <summary>金丝雀：故意持有 SDK 客户端类型的合成类型（供检查器自证）。</summary>
    private sealed class CanaryWithSdkClient
    {
        public Mud.Feishu.IFeishuTenantV1Message? MessageClient { get; init; }
    }

    [Fact]
    public void Mcp_ShouldNotDependOnSdkClientTypes()
    {
        var violations = SdkClientDependencyScanner.FindViolations(typeof(FeishuMcpToolServer).Assembly);

        violations.Should().BeEmpty(
            "MCP 包若直接持有/调用 SDK 强类型客户端，就等于在进程内开了一条绕过授权、审计与净化的旁路——"
            + "工具调用必须经 FeishuToolBinding。违规成员：" + string.Join(" | ", violations));
    }

    [Fact]
    public void SdkClientDependencyScanner_ShouldDetect_CanaryType()
    {
        var violations = SdkClientDependencyScanner.FindViolations([typeof(CanaryWithSdkClient)]);

        violations.Should().NotBeEmpty(
            "检查器必须能识破'直接持有 SDK 客户端类型'的形态，否则上面的守卫是恒真断言（假绿）");
        violations.Should().Contain(
            v => v.Contains("CanaryWithSdkClient", StringComparison.Ordinal),
            "违规必须定位到具体类型（否则宿主拿着报错也不知道该改哪）");
    }

    [Fact]
    public void McpToolNames_ShouldBeClientSafeAndCollisionFree_ForEveryContractTool()
    {
        // 真实工具面全量驱动：>64 字符或映射冲突都会 fail-fast（此处无异常即通过）。
        McpToolNames.EnsureMappable(FeishuToolNames.All);

        FeishuToolNames.All.Should().NotBeEmpty("扫描面为空说明契约表解析坏了（假绿）");

        foreach (var name in FeishuToolNames.All)
        {
            var mcpName = McpToolNames.ToMcpName(name);
            mcpName.Should().MatchRegex("^[a-zA-Z0-9_-]{1,64}$",
                "严格 MCP 客户端（Anthropic 工具名规范）只接受该字符集；不满足会让整个工具列表被客户端拒绝");
            mcpName.Should().NotContain(".",
                "点号是契约名的一部分，必须映射掉（否则客户端侧静默失败、服务端毫无信号）");
        }
    }

    [Fact]
    public void McpToolNames_EnsureMappable_ShouldRejectCollision()
    {
        var act = () => McpToolNames.EnsureMappable(["a.b_c", "a_b.c"]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*映射冲突*", "'.' 与 '_' 在映射后不可区分时必须 fail-fast，而不是后者覆盖前者");
    }

    [Fact]
    public void McpToolNames_EnsureMappable_ShouldRejectOverlongName()
    {
        var act = () => McpToolNames.EnsureMappable([new string('a', 65)]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*超过 64 字符*");
    }

    [Fact]
    public void McpPackage_ShouldBeListedInSolution()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Mud.Feishu.slnx"));

        slnx.Should().Contain($"{McpProject}/{McpProject}.csproj",
            "可选包也必须在 solution 里登记——否则 CI/质量门禁（AOT 冒烟按仓库根 Mud.Feishu* 目录枚举）看不到它");
    }

    [Fact]
    public void McpPackage_ShouldDeclareItsOwnPublicApiBaseline()
    {
        var projectDirectory = Path.Combine(FindRepositoryRoot(), McpProject);

        File.Exists(Path.Combine(projectDirectory, "PublicAPI.Shipped.txt")).Should().BeTrue();
        File.Exists(Path.Combine(projectDirectory, "PublicAPI.Unshipped.txt")).Should().BeTrue();
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}

/// <summary>
/// "包内是否直接依赖 SDK 强类型客户端"的检查器（可被金丝雀类型直接驱动，故支持自证）。
/// </summary>
/// <remarks>
/// <b>判据</b>：某类型的字段/属性/方法签名（含泛型实参）出现来自
/// <c>Mud.Feishu</c> 程序集的类型即视为违规——那正是"直连 SDK 客户端"的载体
/// （MCP 侧只需 <c>FeishuToolContext</c> / <c>AIFunction</c> 这些 AI 包契约）。
/// </remarks>
internal static class SdkClientDependencyScanner
{
    /// <summary>在给定程序集内查找直接引用 SDK 程序集类型的成员。</summary>
    /// <param name="assembly">待检查程序集。</param>
    /// <param name="extraTypes">额外检查的类型（自证用金丝雀）。</param>
    /// <returns>违规定位（类型.成员 → 依赖类型）。</returns>
    public static IReadOnlyList<string> FindViolations(Assembly assembly, params Type[] extraTypes)
        => FindViolations(assembly.GetTypes().Concat(extraTypes));

    /// <summary>在给定类型集合内查找直接引用 SDK 程序集类型的成员（金丝雀直驱入口）。</summary>
    /// <param name="types">待检查类型。</param>
    /// <returns>违规定位（类型.成员 → 依赖类型）。</returns>
    public static IReadOnlyList<string> FindViolations(IEnumerable<Type> types)
    {
        var violations = new List<string>();

        foreach (var type in types)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var field in type.GetFields(flags))
            {
                Record(type, $"字段 {field.Name}", field.FieldType, violations);
            }

            foreach (var property in type.GetProperties(flags))
            {
                Record(type, $"属性 {property.Name}", property.PropertyType, violations);
            }

            foreach (var method in type.GetMethods(flags))
            {
                Record(type, $"方法 {method.Name} 返回值", method.ReturnType, violations);
                foreach (var parameter in method.GetParameters())
                {
                    Record(type, $"方法 {method.Name} 参数 {parameter.Name}", parameter.ParameterType, violations);
                }
            }

            foreach (var constructor in type.GetConstructors(flags))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    Record(type, $"构造函数参数 {parameter.Name}", parameter.ParameterType, violations);
                }
            }
        }

        return violations;
    }

    private static void Record(Type owner, string member, Type dependency, List<string> violations)
    {
        foreach (var candidate in Flatten(dependency))
        {
            if (string.Equals(candidate.Assembly.GetName().Name, "Mud.Feishu", StringComparison.Ordinal))
            {
                violations.Add($"{owner.FullName}.{member} → {candidate.FullName}");
                return;
            }
        }
    }

    /// <summary>展开类型（含泛型实参与数组元素），保证 <c>Task&lt;IFeishuX&gt;</c> 这类签名也被看到。</summary>
    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } element)
        {
            foreach (var nested in Flatten(element))
            {
                yield return nested;
            }
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Flatten(argument))
            {
                yield return nested;
            }
        }
    }
}
