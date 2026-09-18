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
using Mud.Feishu.Abstractions.Configuration;
using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

#if NET6_0_OR_GREATER

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMA2-07 / P1-4（§7.2 #9/#10）：FeishuTokenRegistrationService 单元测试。
/// 验证后台令牌刷新的增量注册契约：
/// <list type="bullet">
/// <item>启动期仅注册默认应用（WarmUpAllAppsOnStartup=false）；</item>
/// <item>非默认应用首次访问经 AppInstantiated 事件增量注册；</item>
/// <item>热更新重建后新实例接管后台注册（同名键覆盖）；</item>
/// <item>单应用装配失败不阻断宿主启动。</item>
/// </list>
/// </summary>
public class FeishuTokenRegistrationServiceTests
{
    private readonly Mock<ITokenRefreshBackgroundService> _refreshServiceMock = new();

    private static List<FeishuAppConfig> CreateConfigs(params (string AppKey, string AppId, string AppSecret, bool IsDefault)[] apps)
        => apps.Select(a => new FeishuAppConfig
        {
            AppKey = a.AppKey,
            AppId = a.AppId,
            AppSecret = a.AppSecret,
            IsDefault = a.IsDefault
        }).ToList();

    private static (ServiceProvider Provider, FeishuAppManager Manager) CreateHost(List<FeishuAppConfig> configs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configs);
        var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        return (provider, manager);
    }

    private FeishuTokenRegistrationService CreateService(
        FeishuAppManager manager,
        bool warmUpAllAppsOnStartup = false)
        => new(
            manager,
            _refreshServiceMock.Object,
            new Mock<ILogger<FeishuTokenRegistrationService>>().Object,
            Options.Create(new FeishuAppOptions { WarmUpAllAppsOnStartup = warmUpAllAppsOnStartup }));

    /// <summary>
    /// TMA2-07（§7.2 #9 配套）：WarmUpAllAppsOnStartup=false 时，
    /// StartAsync 仅注册默认应用的 tenant:/app: 两个令牌管理器，非默认应用不注册。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldRegisterOnlyDefaultApp_WhenWarmUpDisabled()
    {
        var (provider, manager) = CreateHost(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true),
            ("app2", AppConfigs.AppIds.Hr, AppConfigs.Secrets.Hr, IsDefault: false)));
        try
        {
            var service = CreateService(manager);
            await service.StartAsync();

            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "tenant:app1"),
                Times.Once);
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "app:app1"),
                Times.Once);
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), It.Is<string>(n => n!.Contains("app2"))),
                Times.Never,
                "WarmUpAllAppsOnStartup=false 时非默认应用不得在启动期注册");
        }
        finally
        {
            provider.Dispose();
        }
    }

    /// <summary>
    /// TMA2-07（§7.2 #9）核心：非默认应用首次访问后，经 AppInstantiated 事件
    /// 增量注册到后台刷新服务（tenant:{appKey} / app:{appKey}）。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldRegisterApp_OnFirstAccess_ViaAppInstantiated()
    {
        var (provider, manager) = CreateHost(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true),
            ("app2", AppConfigs.AppIds.Hr, AppConfigs.Secrets.Hr, IsDefault: false)));
        try
        {
            var service = CreateService(manager);
            await service.StartAsync();

            // Act：首次访问非默认应用
            _ = manager.GetApp("app2");

            // Assert：经 AppInstantiated 增量注册。
            // 说明：首次访问同时触发 ConfigurationChanged(Added) 与 AppInstantiated 两条注册路径，
            // 同名键覆盖语义下幂等（TMA2-07 设计：按应用去重 = 同名键覆盖），故断言"至少一次"。
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "tenant:app2"),
                Times.AtLeastOnce);
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "app:app2"),
                Times.AtLeastOnce);
        }
        finally
        {
            provider.Dispose();
        }
    }

    /// <summary>
    /// TMA2-07（§7.2 #10）：热更新重建应用上下文后，新实例必须接管后台注册
    /// （ConfigurationChanged → 同名键覆盖注册），旧实例退休后由组件 ODE 自清。
    /// </summary>
    [Fact]
    public async Task HotReload_ShouldReplaceBackgroundRegistration_ToNewInstance()
    {
        var (provider, manager) = CreateHost(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true)));
        try
        {
            var service = CreateService(manager);
            await service.StartAsync();
            var before = manager.GetApp("app1");

            // Act：热更新重建 app1（TimeOut 变更使节流不命中）
            manager.OnConfigurationChanged(new List<FeishuAppConfig>
            {
                new FeishuAppConfig
                {
                    AppKey = "app1",
                    AppId = AppConfigs.AppIds.Default,
                    AppSecret = AppConfigs.Secrets.Default,
                    IsDefault = true,
                    TimeOut = 60
                }
            });

            var after = manager.GetApp("app1");
            after.Should().NotBeSameAs(before, "配置变更必须重建应用上下文");

            // Assert：同名键再次注册（新实例接管），总计 2 次（Start + Updated）
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "tenant:app1"),
                Times.Exactly(2));
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "app:app1"),
                Times.Exactly(2));
        }
        finally
        {
            provider.Dispose();
        }
    }

    /// <summary>
    /// TMA2-07：WarmUpAllAppsOnStartup=true 时单个应用装配失败，StartAsync 不得抛出
    /// （逐应用 try/catch + LogError，单应用失败不阻断宿主启动）。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldNotFailHost_WhenOneAppAssemblyFails()
    {
        var tokenStoreFactoryMock = new Mock<IFeishuTokenStoreFactory>();
        tokenStoreFactoryMock
            .Setup(x => x.Create("app2"))
            .Throws(new HttpRequestException("模拟 app2 装配失败"));
        tokenStoreFactoryMock
            .Setup(x => x.Create(It.Is<string>(k => k != "app2")))
            .Returns(() => (new Mock<ITokenStore>().Object, (IUserTokenStore?)new Mock<IUserTokenStore>().Object));

        var services = new ServiceCollection();
        services.AddLogging();
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (existingDescriptor != null) services.Remove(existingDescriptor);
        services.AddSingleton(tokenStoreFactoryMock.Object);
        services.AddFeishuApp(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true),
            ("app2", AppConfigs.AppIds.Hr, AppConfigs.Secrets.Hr, IsDefault: false)));
        var provider = services.BuildServiceProvider();
        try
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
            var service = CreateService(manager, warmUpAllAppsOnStartup: true);

            // Act & Assert：失败被逐应用捕获，不阻断宿主启动
            var act = async () => await service.StartAsync();
            await act.Should().NotThrowAsync();

            // 默认应用仍注册成功
            _refreshServiceMock.Verify(
                x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "tenant:app1"),
                Times.Once);
        }
        finally
        {
            provider.Dispose();
        }
    }
}

#endif
