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

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// R5/X1：<see cref="FeishuAppOptions"/> 的配置节绑定与启动期校验。
/// </summary>
/// <remarks>
/// <para>
/// 改造前 <c>AddFeishuAppBaseServices</c> 只调用 <c>AddOptions&lt;FeishuAppOptions&gt;()</c>——
/// 「仅注册 Options」<b>不会绑定任何配置节</b>，<see cref="FeishuAppOptions.SectionName"/>
/// 因此是死常量，6 个行为开关无法经 appsettings 下发（文档的「读取点」表会让读者误以为可配）。
/// </para>
/// <para>
/// 本测试锁定两件事：<b>绑定真实生效</b>（DoD「绑定真实」）+ <b>越界值在解析期失败</b>。
/// </para>
/// </remarks>
public class FeishuAppOptionsBindingTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> overrides)
    {
        var data = new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "default",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
            ["FeishuApps:0:IsDefault"] = "true"
        };
        foreach (var kv in overrides)
            data[kv.Key] = kv.Value;

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> overrides)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(BuildConfig(overrides));
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
        options.ContextRetireDelaySeconds.Should().Be(600);
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

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*ContextRetireDelaySeconds*");
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
    public void ShouldNotRegisterChangeTokenSource_ForFeishuAppOptions()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());

        // R5/X1 有意裁决：全仓库无 IOptionsMonitor<FeishuAppOptions> 消费方，
        // 注册变更令牌只会把「启动快照」伪装成「可热更」——属要清理的死接线。
        provider.GetService<IOptionsChangeTokenSource<FeishuAppOptions>>()
            .Should().BeNull("FeishuAppOptions 的 6 个属性均被文档化为启动快照，不注册变更令牌");
    }
}
