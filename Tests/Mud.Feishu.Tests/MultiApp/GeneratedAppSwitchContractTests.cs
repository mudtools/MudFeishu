// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace Mud.Feishu.Tests.MultiApp;

/// <summary>
/// Mud.HttpUtils 3.0.0 适配（BC-27）的**生成产物契约守卫**。
/// </summary>
/// <remarks>
/// <para>
/// 改造计划 Phase 1 的核心机制是上游的「<b>接口自行声明即豁免</b>」：<see cref="IFeishuAppContextSwitcher"/>
/// 显式重声明三个旧成员后，生成器仍为 188+ 个生成实现类发射它们；同时上游 <c>SW-01</c> 让生成类
/// 自动具备 <c>IAppScopeSwitcher</c> 契约。
/// </para>
/// <para>
/// 该机制<b>一旦失效即为编译期失败</b>（<c>CS0535</c> 未实现接口成员 / <c>HTTPCLIENT024</c> 契约占位），
/// 但"编译通过"不足以证明<b>全部</b>生成类都正确（例如新增接口未继承 <see cref="IFeishuAppContextSwitcher"/>、
/// 或上游豁免判定被收紧）。故本守卫对<b>全部</b>实现类做运行期反射断言：
/// 五个切换成员（3 个旧入口 + 2 个作用域入口）必须真实存在。
/// </para>
/// <para>
/// 另：实现类成员<b>不得</b>标 <c>[Obsolete]</c>（上游 <c>GEN-02</c>）—— 废弃标注只应存在于抽象层，
/// 否则实现接口这一必然行为会产生无意义的 <c>CS0618</c> 噪音。
/// </para>
/// </remarks>
public class GeneratedAppSwitchContractTests
{
    /// <summary>必须发射的切换成员（3 个旧入口 + 2 个作用域入口）。</summary>
    private static readonly (string Name, Type[] Parameters)[] RequiredMembers =
    [
        ("UseApp", [typeof(string)]),                    // 旧入口（BC-27 后由本 SDK 接续声明豁免）
        ("UseDefaultApp", Type.EmptyTypes),              // 旧入口
        ("BeginScope", [typeof(string)]),                // 旧入口（Holder 面的 BeginScope(IMudAppContext) 另算）
        ("UseAppScope", [typeof(string)]),               // 推荐面（SW-01）
        ("UseDefaultAppScope", Type.EmptyTypes),         // 推荐面（SW-01）
    ];

    [Fact]
    public void Interface_ShouldInheritAppScopeSwitcher_AndDeclareLegacyMembers()
    {
        // ① 推荐面可经接口类型使用（下游可直接以 IAppScopeSwitcher 变量编程）。
        typeof(IFeishuAppContextSwitcher).GetInterfaces().Should().Contain(typeof(IAppScopeSwitcher),
            "IWechat/IFeishu 侧均应继承上游 SW-01 新增的作用域切换面");

        // ② 三个旧成员的接续声明必须逐字匹配上游原签名（否则豁免判定不成立）。
        typeof(IFeishuAppContextSwitcher).GetMethod("UseApp", [typeof(string)])!
            .ReturnType.Should().Be(typeof(IMudAppContext));
        typeof(IFeishuAppContextSwitcher).GetMethod("UseDefaultApp", Type.EmptyTypes)!
            .ReturnType.Should().Be(typeof(IMudAppContext));
        typeof(IFeishuAppContextSwitcher).GetMethod("BeginScope", [typeof(string)])!
            .ReturnType.Should().Be(typeof(IDisposable));
    }

    [Fact]
    public void GeneratedImplementations_ShouldEmitAllFiveSwitchMembers()
    {
        var assembly = typeof(IFeishuTenantV3JobTitle).Assembly;

        var switchInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && typeof(IFeishuAppContextSwitcher).IsAssignableFrom(t))
            .ToList();
        switchInterfaces.Count.Should().BeGreaterThan(100,
            "本 SDK 的客户端接口以 IFeishuAppContextSwitcher 为统一切换面（改造计划 §2.3：188 个接口）");

        var offenders = new List<string>();
        var checkedImpls = 0;

        foreach (var iface in switchInterfaces)
        {
            var impl = assembly.GetTypes().FirstOrDefault(t =>
                t.IsClass && !t.IsAbstract && iface.IsAssignableFrom(t));
            if (impl is null)
                continue;   // 无生成实现的接口（纯标记接口）不参与本断言

            checkedImpls++;
            foreach (var (name, parameters) in RequiredMembers)
            {
                if (impl.GetMethod(name, parameters) is null)
                    offenders.Add($"{impl.Name}.{name}({string.Join(", ", parameters.Select(p => p.Name))})");
            }
        }

        checkedImpls.Should().BeGreaterThan(100, "应覆盖全部生成实现类，而非抽样");
        offenders.Should().BeEmpty(
            "以下生成实现类缺少切换成员 ⇒ 说明上游「接口自行声明即豁免」或 SW-01 追加机制失效" +
            "（运行期将命中契约占位并抛 NotSupportedException）。违规成员：{0}",
            string.Join(", ", offenders.OrderBy(n => n, StringComparer.Ordinal)));
    }

    [Fact]
    public void GeneratedImplementations_ShouldNotMarkSwitchMembersObsolete()
    {
        // GEN-02：废弃标注只放抽象层（接口），实现类标注只会污染"实现接口"这一必然行为。
        var assembly = typeof(IFeishuTenantV3JobTitle).Assembly;
        var impl = assembly.GetTypes().First(t =>
            t.IsClass && !t.IsAbstract && typeof(IFeishuTenantV3JobTitle).IsAssignableFrom(t));

        foreach (var (name, parameters) in RequiredMembers)
        {
            var method = impl.GetMethod(name, parameters);
            method.Should().NotBeNull($"{impl.Name}.{name} 必须存在");
            method!.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
                .Should().BeEmpty($"生成实现类的 {name} 不得标 [Obsolete]（GEN-02）");
        }
    }
}
