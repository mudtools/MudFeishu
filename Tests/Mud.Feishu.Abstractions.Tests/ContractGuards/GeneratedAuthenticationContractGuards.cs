// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Mud.HttpUtils;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// TMR2-P1-1 守卫 8：生成实现 <c>Internal.FeishuAuthentication</c> 的构造契约。
/// </summary>
/// <remarks>
/// <para>
/// 修复后 <c>PerAppFeishuAuthenticationFactory.Create</c> <b>编译期直引</b>
/// <c>Mud.Feishu.Abstractions.Internal.FeishuAuthentication</c>（与本体同程序集，无反射、AOT 安全）。
/// 该直引对生成器参数集变化是"编译期失败"（正向），本守卫提供<b>更早、更明确</b>的失败信息，
/// 并固化"工厂实参顺序 == 生成 ctor 形参顺序"这一不变式。
/// </para>
/// <para>
/// 生产代码零反射：本守卫的反射仅存在于测试程序集（门禁口径允许）。
/// </para>
/// </remarks>
public class GeneratedAuthenticationContractGuards
{
    private const string GeneratedImplementationTypeName = "Mud.Feishu.Abstractions.Internal.FeishuAuthentication";

    private static readonly Type[] ExpectedConstructorSignature =
    {
        typeof(IEnhancedHttpClient),
        typeof(IHttpRequestExecutor),
        typeof(IHttpResponseCache),
        typeof(IResiliencePolicyResolver),
        typeof(IHttpContentSerializer),
        typeof(ILogger),
    };

    private static readonly string[] ExpectedParameterNames =
    {
        "httpClient",
        "executor",
        "cacheProvider",
        "resilienceResolver",
        "contentSerializer",
        "logger",
    };

    [Fact]
    public void Generated_FeishuAuthentication_Ctor_ShouldMatchPerAppFactory()
    {
        var implementationType = typeof(IFeishuAuthentication).Assembly
            .GetType(GeneratedImplementationTypeName);
        implementationType.Should().NotBeNull(
            $"生成器应产出 '{GeneratedImplementationTypeName}'（IFeishuAuthentication 标有 [HttpClientApi]）");

        var ctor = implementationType!.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: ExpectedConstructorSignature,
            modifiers: null);

        ctor.Should().NotBeNull(
            "PerAppFeishuAuthenticationFactory 以 (IEnhancedHttpClient, IHttpRequestExecutor, " +
            "IHttpResponseCache?, IResiliencePolicyResolver?, IHttpContentSerializer?, ILogger?) 直引构造生成实现；" +
            "生成器若变更参数集，必须同步更新工厂与守卫（TMR2-P1-1 / COMP-TMR2-2）");

        var parameters = ctor!.GetParameters();
        parameters.Select(p => p.Name).Should().Equal(ExpectedParameterNames,
            "工厂按位置传参，形参顺序/名称属于契约的一部分");

        parameters.Skip(2).Should().OnlyContain(p => p.HasDefaultValue,
            "cacheProvider / resilienceResolver / contentSerializer / logger 必须为可选参数，" +
            "否则未注册这些服务的宿主将无法构造 per-app 认证客户端");
    }

    [Fact]
    public void Generated_FeishuAuthentication_ShouldBeConstructible_WithFactoryArgumentShape()
    {
        var implementationType = typeof(IFeishuAuthentication).Assembly
            .GetType(GeneratedImplementationTypeName)!;

        // 以工厂的实际实参形态构造一次：per-app client + executor（其余可选依赖缺失时传 null）
        var instance = Activator.CreateInstance(implementationType,
            new Mock<IEnhancedHttpClient>().Object,
            new Mock<IHttpRequestExecutor>().Object,
            null,
            null,
            null,
            null);

        instance.Should().BeAssignableTo<IFeishuAuthentication>(
            "per-app 装配路径必须能构造出 IFeishuAuthentication 实现（修复前该路径恒抛并静默降级）");
    }

    [Fact]
    public void PerAppFactory_ShouldNotUseReflectiveConstruction()
    {
        // 缺陷形态不可回归：工厂**代码**不得再出现 ActivatorUtilities 反射构造（T 为接口时恒抛）。
        // 先剥离注释——本修复的说明文字会合法地"提到"该 API。
        var code = StripComments(File.ReadAllText(FindSourceFile("IFeishuAuthenticationFactory.cs")));

        code.Should().NotContain("ActivatorUtilities",
            "T 为接口时 ActivatorUtilities.CreateInstance<T> 恒抛 InvalidOperationException，" +
            "原实现据此静默降级到默认应用端点（TMR2-P1-1）；per-app 装配必须直引生成实现类型");
    }

    /// <summary>
    /// 剥离 <c>//</c> 行注释与行尾注释（测试辅助；源码中不含含 <c>//</c> 的字符串字面量）。
    /// </summary>
    private static string StripComments(string source)
    {
        var sb = new System.Text.StringBuilder(source.Length);
        foreach (var rawLine in source.Split('\n'))
        {
            var line = rawLine;
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                line = line.Substring(0, commentIndex);
            }

            sb.Append(line).Append('\n');
        }

        return sb.ToString();
    }

    private static string FindSourceFile(string fileName)
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        dir.Should().NotBeNull("应能定位解决方案根目录（含 Mud.Feishu.slnx）");

        var matches = Directory.GetFiles(dir!, fileName, SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();

        matches.Should().ContainSingle($"源码树中应恰好存在一个 '{fileName}'");
        return matches[0];
    }
}
