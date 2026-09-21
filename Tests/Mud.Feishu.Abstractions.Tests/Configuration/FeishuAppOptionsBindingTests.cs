// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Tests.Helpers;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// R5/X1：<see cref="FeishuAppOptions"/> 的配置节绑定与启动期校验。
/// </summary>
/// <remarks>
/// <para>
/// 修复前 <c>AddFeishuAppBaseServices</c> 只调用 <c>AddOptions&lt;FeishuAppOptions&gt;()</c>——
/// 「仅注册 Options」<b>不会绑定任何配置节</b>，<see cref="FeishuAppOptions.SectionName"/> 是死常量，
/// 6 个行为开关无法经 appsettings 下发，越界值也只能在运行期由
/// <c>FeishuAppContextRetirement</c> 才暴露。
/// </para>
/// <para>
/// 本组测试锁定三件事：<b>绑定真实生效</b>（DoD「绑定真实」）、<b>越界值在选项解析期失败</b>
/// （fail-fast）、以及 <b>绑定 ≠ 可热更</b>。
/// </para>
/// <para>
/// 实现形态约束（G-02）：绑定走「委托式 <c>Configure&lt;T&gt;(o =&gt; section.Bind(o))</c>」而非
/// <c>Configure&lt;T&gt;(IConfiguration)</c> 重载（后者反射绑定调用点无法被配置绑定源生成器拦截，
/// 会破坏 IL2026/IL3050 净零）。AOT 层面由 verify-build 步骤 3 严格模式冒烟覆盖，此处不重复。
/// </para>
/// <para>
/// <b>不注册 <c>IOptionsChangeTokenSource</c></b>：本类型 6 个属性全部是「启动快照」语义
/// （消费方一律为 <c>IOptions&lt;T&gt;</c>），本文件用
/// <see cref="Bind_ShouldKeepStartupSnapshot_WhenConfigurationReloaded"/> 与
/// <see cref="ShouldNotRegisterChangeTokenSource_ForFeishuAppOptions"/> 从<b>行为</b>与
/// <b>注册面</b>两侧锁定「绑定 ≠ 可热更」（与 README 的「一次性读取语义清单」一致）。
/// </para>
/// </remarks>
public class FeishuAppOptionsBindingTests
{
    private const string AppSection = "FeishuApps";

    /// <summary>
    /// 单应用基础配置（保证 <c>AddFeishuApp</c> 的配置校验可通过，被测点全部落在 FeishuAppOptions 节）。
    /// </summary>
    private static Dictionary<string, string?> SingleAppData() => new()
    {
        [$"{AppSection}:0:AppKey"] = "app1",
        [$"{AppSection}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
        [$"{AppSection}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
        [$"{AppSection}:0:IsDefault"] = "true"
    };

    /// <summary>
    /// 基础应用配置 + 覆盖项（覆盖项既可写 <c>FeishuAppOptions:*</c>，也可写应用节键）。
    /// </summary>
    private static IConfigurationRoot BuildConfig(Dictionary<string, string?> overrides)
    {
        var data = SingleAppData();
        foreach (var kv in overrides)
            data[kv.Key] = kv.Value;

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> overrides)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(BuildConfig(overrides), AppSection);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void SectionName_ShouldBe_FeishuAppOptions()
    {
        FeishuAppOptions.SectionName.Should().Be("FeishuAppOptions");
    }

    [Fact]
    public void Bind_ShouldApplyBehaviourSwitches_FromFeishuAppOptionsSection()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["FeishuAppOptions:EnableTokenEncryption"] = "true",
            ["FeishuAppOptions:WarmUpAllAppsOnStartup"] = "true",
            ["FeishuAppOptions:ContextRetireDelaySeconds"] = "600",
            ["FeishuAppOptions:EnableConfigReload"] = "false",
            ["FeishuAppOptions:EnablePerAppAuthenticationClient"] = "false",
            ["FeishuAppOptions:RemoveRuntimeAddedAppsOnReload"] = "true"
        });

        var options = provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        options.EnableTokenEncryption.Should().BeTrue();
        options.WarmUpAllAppsOnStartup.Should().BeTrue();
        options.ContextRetireDelaySeconds.Should().Be(600,
            "FeishuAppOptions 配置节必须真实绑定（X1 修复点：此前该节完全无效）");
        options.EnableConfigReload.Should().BeFalse();
        options.EnablePerAppAuthenticationClient.Should().BeFalse();
        options.RemoveRuntimeAddedAppsOnReload.Should().BeTrue();
    }

    [Fact]
    public void Bind_ShouldKeepDefaults_WhenSectionAbsent()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());

        var options = provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        options.EnableConfigReload.Should().BeTrue("默认 true（ARC-1 热更新）");
        options.EnableTokenEncryption.Should().BeFalse("加密属存储策略选择，默认不开启");
        options.ContextRetireDelaySeconds.Should().Be(300);
        options.WarmUpAllAppsOnStartup.Should().BeFalse();
        options.EnablePerAppAuthenticationClient.Should().BeTrue();
        options.RemoveRuntimeAddedAppsOnReload.Should().BeFalse();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("3601")]
    [InlineData("-1")]
    public void Validation_ShouldFail_WhenContextRetireDelaySecondsOutOfRange(string value)
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["FeishuAppOptions:ContextRetireDelaySeconds"] = value
        });

        // 越界值此前只在运行期由 FeishuAppContextRetirement 抛出（R5/X1 新增 FeishuAppOptionsValidator）；
        // 现在解析 IOptions<T>.Value 即触发校验（net6+ 另由 ValidateOnStart 在宿主启动期触发）。
        var act = () => _ = provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        var exception = act.Should().Throw<OptionsValidationException>().Which;

        exception.Message.Should().Contain("ContextRetireDelaySeconds");
        exception.Message.Should().Contain(
            $"{FeishuAppOptionsValidator.MinContextRetireDelaySeconds}–{FeishuAppOptionsValidator.MaxContextRetireDelaySeconds}",
            "错误消息必须给出合法区间（1–3600），否则集成方无法从日志定位越界原因");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("3600")]
    public void Validation_ShouldPass_AtBoundaries(string value)
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["FeishuAppOptions:ContextRetireDelaySeconds"] = value
        });

        var act = () => _ = provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        act.Should().NotThrow();
    }

    [Fact]
    public void Bind_ShouldKeepStartupSnapshot_WhenConfigurationReloaded()
    {
        var data = SingleAppData();
        data["FeishuAppOptions:ContextRetireDelaySeconds"] = "600";

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, AppSection);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value.ContextRetireDelaySeconds
            .Should().Be(600);

        configuration.Providers.OfType<MemoryConfigurationProvider>()
            .First().Set("FeishuAppOptions:ContextRetireDelaySeconds", "1200");
        configuration.Reload();

        provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value.ContextRetireDelaySeconds
            .Should().Be(600,
                "FeishuAppOptions 是启动快照（IOptions 缓存不失效）：绑定不等于可热更，" +
                "也不得注册 ChangeTokenSource 制造「可热更」假象（R5/X1 修订）");
    }

    [Fact]
    public void ShouldNotRegisterChangeTokenSource_ForFeishuAppOptions()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());

        // R5/X1 有意裁决：全仓库无 IOptionsMonitor<FeishuAppOptions> 消费方，
        // 注册变更令牌只会把「启动快照」伪装成「可热更」——属要清理的死接线。
        provider.GetService<IOptionsChangeTokenSource<FeishuAppOptions>>()
            .Should().BeNull("FeishuAppOptions 的 6 个属性均被文档化为启动快照，不注册变更令牌");
    }
}
