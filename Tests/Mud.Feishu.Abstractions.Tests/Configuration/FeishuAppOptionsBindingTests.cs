// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Tests.Helpers;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// R5/X1：<see cref="FeishuAppOptions"/> 必须真正绑定 <c>FeishuAppOptions</c> 配置节。
/// </summary>
/// <remarks>
/// <para>
/// 修复前 <c>AddFeishuAppBaseServices</c> 只调用 <c>AddOptions&lt;FeishuAppOptions&gt;()</c>——
/// 「仅注册 Options」<b>不会绑定任何配置节</b>，6 个行为开关（<c>EnableConfigReload</c> 等）
/// 无法经 appsettings 下发，<see cref="FeishuAppOptions.SectionName"/> 是死常量。
/// </para>
/// <para>
/// 实现形态约束（G-02）：绑定走「委托式 <c>Configure&lt;T&gt;(o =&gt; section.Bind(o))</c>」而非
/// <c>Configure&lt;T&gt;(IConfiguration)</c> 重载（后者反射绑定调用点无法被配置绑定源生成器拦截，
/// 会破坏 IL2026/IL3050 净零）。AOT 层面由 verify-build 步骤 3 严格模式冒烟覆盖，此处不重复。
/// </para>
/// <para>
/// <b>不注册 <c>IOptionsChangeTokenSource</c></b>：本类型 6 个属性全部是「启动快照」语义
/// （消费方一律为 <c>IOptions&lt;T&gt;</c>），本文件用
/// <see cref="Bind_ShouldKeepStartupSnapshot_WhenConfigurationReloaded"/> 锁定「绑定 ≠ 可热更」：
/// 配置源变更后 <c>IOptions&lt;T&gt;.Value</c> 仍是快照（与 README 的「一次性读取语义清单」一致）。
/// </para>
/// </remarks>
public class FeishuAppOptionsBindingTests
{
    private const string AppSection = "FeishuApps";

    private static ServiceProvider BuildProvider(Dictionary<string, string?> data)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, AppSection);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> SingleAppData() => new()
    {
        [$"{AppSection}:0:AppKey"] = "app1",
        [$"{AppSection}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
        [$"{AppSection}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
        [$"{AppSection}:0:IsDefault"] = "true"
    };

    [Fact]
    public void Bind_ShouldApplySectionValues_WhenSectionExists()
    {
        var data = SingleAppData();
        data["FeishuAppOptions:ContextRetireDelaySeconds"] = "600";
        data["FeishuAppOptions:EnableTokenEncryption"] = "true";
        data["FeishuAppOptions:WarmUpAllAppsOnStartup"] = "true";
        data["FeishuAppOptions:EnableConfigReload"] = "false";

        using var provider = BuildProvider(data);

        var options = provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        options.ContextRetireDelaySeconds.Should().Be(600,
            "FeishuAppOptions 配置节必须真实绑定（X1 修复点：此前该节完全无效）");
        options.EnableTokenEncryption.Should().BeTrue();
        options.WarmUpAllAppsOnStartup.Should().BeTrue();
        options.EnableConfigReload.Should().BeFalse();
    }

    [Fact]
    public void Bind_ShouldKeepDefaults_WhenSectionMissing()
    {
        using var provider = BuildProvider(SingleAppData());

        var options = provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        options.ContextRetireDelaySeconds.Should().Be(300);
        options.EnableTokenEncryption.Should().BeFalse();
        options.WarmUpAllAppsOnStartup.Should().BeFalse();
        options.EnableConfigReload.Should().BeTrue();
        options.EnablePerAppAuthenticationClient.Should().BeTrue();
        options.RemoveRuntimeAddedAppsOnReload.Should().BeFalse();
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenContextRetireDelaySecondsOutOfRange()
    {
        // 启动期校验（fail-fast）：net6+ 由 ValidateOnStart 在宿主启动时触发；
        // 无宿主环境（本测试）由 IValidateOptions 在首次解析时触发——两者共享同一校验器，
        // 也正是 netstandard2.0（无 ValidateOnStart）路径的触发方式。
        var data = SingleAppData();
        data["FeishuAppOptions:ContextRetireDelaySeconds"] = "0";

        using var provider = BuildProvider(data);

        var act = () => provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*ContextRetireDelaySeconds*3600*");
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

        configuration.Providers.OfType<Microsoft.Extensions.Configuration.Memory.MemoryConfigurationProvider>()
            .First().Set("FeishuAppOptions:ContextRetireDelaySeconds", "1200");
        ((IConfigurationRoot)configuration).Reload();

        provider.GetRequiredService<IOptions<FeishuAppOptions>>().Value.ContextRetireDelaySeconds
            .Should().Be(600,
                "FeishuAppOptions 是启动快照（IOptions 缓存不失效）：绑定不等于可热更，" +
                "也不得注册 ChangeTokenSource 制造「可热更」假象（R5/X1 修订）");
    }
}
