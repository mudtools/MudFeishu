// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication.MultiApp;

/// <summary>
/// TMR2-P0-1：默认应用上下文（<see cref="IFeishuAppContext"/>）DI 桥接回归测试。
/// </summary>
/// <remarks>
/// <para>
/// 首轮 TMR-P0-1（F1）把 <c>ITenantTokenManager</c> / <c>IAppTokenManager</c> /
/// <c>IFeishuUserTokenManager</c> 三种桥接改为「解析桥接」（转发代理），但<b>同批注册</b>的
/// <see cref="IFeishuAppContext"/> 仍是「实例桥接」——工厂委托返回 <c>GetDefaultApp()</c> 的
/// 实例快照并被容器永久缓存：
/// </para>
/// <list type="bullet">
/// <item><c>SetDefaultApp("B")</c> 后注入方仍拿到 A 的 <c>Config</c>/令牌/认证客户端（跨应用凭据误用）；</item>
/// <item>配置热更新重建默认应用后，注入方持有已进入退休队列、宽限期后被 Dispose 的上下文（ODE）。</item>
/// </list>
/// <para>本测试类断言修复后上述两个形态均不可回归；并含 <c>ForwardDefaultAppContext=false</c> 对照组。</para>
/// </remarks>
public class AppContextBridgeProxyTests
{
    private const string DefaultKey = "default";
    private const string HrKey = "hr-app";

    private static List<FeishuAppConfig> CreateConfigs() => new()
    {
        new FeishuAppConfig
        {
            AppKey = DefaultKey,
            AppId = "cli_a1b2c3d4e5f6g7h8",
            AppSecret = "secret_default_0123456789",
            IsDefault = true
        },
        new FeishuAppConfig
        {
            AppKey = HrKey,
            AppId = "cli_h1i2j3k4l5m6n7o8",
            AppSecret = "secret_hr_0123456789012345",
            IsDefault = false
        }
    };

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configure?.Invoke(services);
        services.AddFeishuApp(CreateConfigs());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void DefaultAppContextBridge_ShouldFollowNewDefault_WhenSetDefaultAppCalled()
    {
        using var provider = BuildProvider();

        var bridge = provider.GetRequiredService<IFeishuAppContext>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();
        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();

        // 切换前：桥接指向默认应用 A
        bridge.AppKey.Should().Be(DefaultKey);
        bridge.Config.AppKey.Should().Be(DefaultKey);
        bridge.TenantTokenManager.Should().BeSameAs(resolver.GetTenantTokenManager());

        // Act
        appManager.SetDefaultApp(HrKey);

        // Assert：桥接（DI 单例，代理实例不变）必须现取新默认应用 B——
        // 修复前（实例桥接）此处仍是 A 的上下文（跨应用凭据误用）。
        bridge.AppKey.Should().Be(HrKey);
        bridge.Config.AppKey.Should().Be(HrKey);
        bridge.TenantTokenManager.Should().BeSameAs(resolver.GetTenantTokenManager());
        bridge.AppTokenManager.Should().BeSameAs(resolver.GetAppTokenManager());
        bridge.UserTokenManager.Should().BeSameAs(resolver.GetUserTokenManager());
        bridge.Authentication.Should().BeSameAs(appManager.GetApp(HrKey).Authentication);
        bridge.HttpClient.Should().BeSameAs(appManager.GetApp(HrKey).HttpClient);
    }

    [Fact]
    public void DefaultAppContextBridge_ShouldFollowRebuiltContext_AfterHotReload()
    {
        using var provider = BuildProvider();

        var bridge = provider.GetRequiredService<IFeishuAppContext>();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

        var oldContext = manager.GetDefaultApp();
        var oldConfig = bridge.Config;
        oldConfig.Should().BeSameAs(oldContext.Config);

        // Act：凭据变更热更新 → 默认应用上下文重建，旧上下文进入退休队列
        manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = DefaultKey,
                AppId = "cli_a1b2c3d4e5f6g7h8",
                AppSecret = "rotated_secret_0123456789",
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = HrKey,
                AppId = "cli_h1i2j3k4l5m6n7o8",
                AppSecret = "secret_hr_0123456789012345",
                IsDefault = false
            }
        });

        var newContext = manager.GetDefaultApp();
        newContext.Should().NotBeSameAs(oldContext, "凭据变更必须重建默认应用上下文");

        // 越过退休宽限期，强制释放旧上下文（模拟运行一段时间后）
        manager.Retirement.Should().NotBeNull();
        manager.Retirement!.Sweep(DateTimeOffset.UtcNow.AddDays(1));

        // Assert：桥接必须指向重建后的新上下文。
        // 修复前桥接被钉死在 oldContext（已于上方 Sweep 中 Dispose）——
        // 注入方读取 Config/令牌即落在已释放上下文（ODE / 旧凭据）。
        bridge.Config.Should().BeSameAs(newContext.Config);
        bridge.TenantTokenManager.Should().BeSameAs(newContext.TenantTokenManager);
        bridge.Config.Should().NotBeSameAs(oldConfig);
    }

    [Fact]
    public void DefaultAppContextBridge_ShouldReturnInstanceSnapshot_WhenForwardDisabled()
    {
        // 对照组：ForwardDefaultAppContext=false 时必须保持首轮前的实例快照语义，
        // 使依赖强转 (FeishuAppContext)ctx 的宿主有可回退路径。
        using var provider = BuildProvider(services =>
            services.Configure<FeishuAppOptions>(o => o.ForwardDefaultAppContext = false));

        var bridge = provider.GetRequiredService<IFeishuAppContext>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        bridge.Should().BeOfType<FeishuAppContext>("开关关闭时应直接返回真实上下文实例（非代理）");
        bridge.Config.AppKey.Should().Be(DefaultKey);

        appManager.SetDefaultApp(HrKey);

        bridge.Config.AppKey.Should().Be(DefaultKey,
            "ForwardDefaultAppContext=false 时恢复实例快照语义（行为迁移对照组）");
    }

    [Fact]
    public void DefaultAppContextBridge_ShouldBeSingleton_AndDisposeNoOp()
    {
        using var provider = BuildProvider();

        var bridge1 = provider.GetRequiredService<IFeishuAppContext>();
        var bridge2 = provider.GetRequiredService<IFeishuAppContext>();

        bridge1.Should().BeSameAs(bridge2, "桥接为 DI 单例——消费方持有的引用稳定，仅转发目标变化");
        bridge1.Should().NotBeOfType<FeishuAppContext>("默认（ForwardDefaultAppContext=true）下桥接为无状态代理");

        // 代理 Dispose 为显式 no-op：不得误伤真实上下文
        var act = () => bridge1.Dispose();
        act.Should().NotThrow();

        bridge1.Config.AppKey.Should().Be(DefaultKey, "Dispose 后代理仍可解析（真实释放归退休队列）");
    }
}
