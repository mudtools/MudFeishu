// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mud.Feishu.AI.FeishuTools.Registration;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 生成代码标记契约守卫：断言生成器产物（FeishuTools 程序集内的全部生成类型）的<b>每个类型</b>与
/// <b>每个显式发射的可执行成员（方法 / 属性）</b>都标注
/// <c>[GeneratedCode("Mud.Feishu.AI.Tools.FeishuToolSchemaGenerator", …)]</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：类型级 <c>[GeneratedCode]</c> 覆盖"按符号过滤"的消费方（Roslyn 分析器抑制、
/// 调试器单步跳过、覆盖率排除），漏标会让这些机制<b>静默失效</b>（产物被当成手写代码参与分析/统计，
/// 无任何报错）。发射器新增类型或成员时极易漏标，此处钉成机械约束。
/// </para>
/// <para>
/// <b>标注粒度</b>（与生成器 <c>GeneratedCodeMarker</c> 的策略一致）：类型必标；显式发射的方法/属性
/// 必标；const/readonly 字段与编译器合成成员（record 位置属性、相等性/克隆成员、自动属性访问器、
/// 主构造器）由类型级标注覆盖，不逐一断言。
/// </para>
/// <para>
/// 生成器工程对测试不可符号引用（分析器形态），故 <c>GeneratorName</c> 与产物家族名以字面量给出
/// （与 <c>ToolArgsContractGuards</c> 同体例）。
/// </para>
/// </remarks>
public class GeneratedCodeMarkerContractGuards
{
    /// <summary>生成器标识（生成器 <c>GeneratedCodeMarker.GeneratorName</c> 的字面量）。</summary>
    private const string GeneratorName = "Mud.Feishu.AI.Tools.FeishuToolSchemaGenerator";

    /// <summary>固定名产物类型（每类发射器一个；新增固定名产物须在此登记）。</summary>
    private static readonly HashSet<string> FixedArtifactTypeNames = new(StringComparer.Ordinal)
    {
        "FeishuToolSchemas",
        "FeishuToolNames",
        "FeishuToolContract",
        "FeishuToolContracts",
        "FeishuToolsServiceCollectionCoreExtensions",
        "FeishuToolGuidance",
        "FeishuToolCapabilityCatalog",
    };

    /// <summary>已知产物家族（Args 类型按工具、域注册器按执行器、固定名产物）缺一不可。</summary>
    [Fact]
    public void GeneratedTypes_ShouldCoverTheWholeArtifactSet()
    {
        var names = ExpectedGeneratedTypes().Select(static t => t.Name).ToHashSet(StringComparer.Ordinal);

        FixedArtifactTypeNames.Should().BeSubsetOf(
            names,
            "固定名产物必须与发射器一一对应（缺失 = 发射器被删或改名未同步守卫）");

        names.Should().Contain(
            static name => name.EndsWith("Args", StringComparison.Ordinal),
            "Args 类型家族必须在位");
        names.Should().Contain(
            static name => name.EndsWith("ToolDomainRegistrar", StringComparison.Ordinal),
            "域注册器家族必须在位");
    }

    /// <summary>每个生成类型必须标注 <c>[GeneratedCode]</c>（GeneratorName 与生成器标识一致）。</summary>
    [Fact]
    public void EveryGeneratedType_ShouldCarryGeneratedCodeAttribute()
    {
        foreach (var type in ExpectedGeneratedTypes())
        {
            Mark(type).Should().NotBeNull($"{type.Name} 是生成器产物，类型级 [GeneratedCode] 必须在位");
            Mark(type)!.Tool.Should().Be(GeneratorName, $"{type.Name} 的标记必须来自本生成器");
        }
    }

    /// <summary>
    /// 生成类型的显式发射成员（静态方法、注册器 <c>Register</c>、非记录类型的公共属性）
    /// 必须逐一标注 <c>[GeneratedCode]</c>。
    /// </summary>
    [Fact]
    public void ExplicitlyEmittedMembers_ShouldCarryGeneratedCodeAttribute()
    {
        foreach (var type in ExpectedGeneratedTypes())
        {
            // record 的位置属性与相等性/克隆成员由编译器合成，无法逐一标注，由类型级标注覆盖。
            if (!IsRecord(type))
            {
                foreach (var property in DeclaredProperties(type))
                {
                    AssertMarked(type, property);
                }
            }

            // 显式发射的方法全部为静态（Unpack / Register 之外的域装配与判定方法）；
            // 运算符与属性访问器是特殊名成员，编译器合成，同样由类型级标注覆盖。
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!method.IsStatic || method.IsSpecialName
                    || method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
                    || !(method.IsPublic || method.IsAssembly))
                {
                    continue;
                }

                AssertMarked(type, method);
            }

            // Register 是显式发射的实例方法（接口实现），静态过滤覆盖不到，按名补断言。
            if (typeof(IFeishuToolDomainRegistrar).IsAssignableFrom(type))
            {
                var register = type.GetMethod(
                    "Register",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                register.Should().NotBeNull($"{type.Name} 必须实现 Register");
                AssertMarked(type, register!);
            }
        }
    }

    // ────────── 断言辅助 ──────────

    private static void AssertMarked(Type type, MemberInfo member)
    {
        Mark(member).Should().NotBeNull(
            $"{type.Name}.{member.Name} 是生成器显式发射的成员，必须标注 [GeneratedCode]");
        Mark(member)!.Tool.Should().Be(
            GeneratorName,
            $"{type.Name}.{member.Name} 的标记必须来自本生成器");
    }

    /// <summary>成员上的 <c>[GeneratedCode]</c> 标注（可能缺位，由断言判定）。</summary>
    private static GeneratedCodeAttribute? Mark(MemberInfo member)
        => member.GetCustomAttribute<GeneratedCodeAttribute>();

    /// <summary>
    /// 产物家族发现（<b>结构化</b>而非按标注反查——新发射器漏标类型级标注时，按标注反查会让
    /// 该类型从断言集合里静默消失）：Args 类型（ToolName 常量 + Args 后缀）、域注册器（实现
    /// IFeishuToolDomainRegistrar）、固定名产物。
    /// </summary>
    private static IReadOnlyList<Type> ExpectedGeneratedTypes()
    {
        var assembly = typeof(FeishuToolNames).Assembly;
        var types = new List<Type>();
        foreach (var type in assembly.GetTypes())
        {
            var isArgs = type is { IsClass: true, IsAbstract: false }
                && type.Name.EndsWith("Args", StringComparison.Ordinal)
                && type.GetField("ToolName", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static) is not null;
            var isRegistrar = type is { IsClass: true, IsAbstract: false }
                && typeof(IFeishuToolDomainRegistrar).IsAssignableFrom(type);

            if (isArgs || isRegistrar || FixedArtifactTypeNames.Contains(type.Name))
            {
                types.Add(type);
            }
        }

        return types;
    }

    /// <summary>类型声明的公共属性（显式发射的属性全部为 public）。</summary>
    private static IEnumerable<PropertyInfo> DeclaredProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    /// <summary>记录类型探测（编译器合成的 <c>&lt;Clone&gt;$</c> 方法是 record 的可靠指纹）。</summary>
    private static bool IsRecord(Type type)
        => type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
