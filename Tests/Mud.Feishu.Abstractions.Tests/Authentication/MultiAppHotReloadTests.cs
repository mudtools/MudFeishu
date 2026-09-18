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
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Tests.Helpers;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// 验证多应用配置热更新（ARC-1）。
/// </summary>
/// <remarks>
/// 修复前 <c>FeishuAppManager</c> 在构造时捕获启动期配置快照、不订阅
/// <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;.OnChange</c>，修改配置后管理器与
/// 所有 HttpClient 仍使用旧配置（文档中明确记录「配置变更需重启」）。
/// </remarks>
public class MultiAppHotReloadTests
{
    private const string SectionName = "FeishuApps";

    private static FeishuAppConfig CreateConfig(
        string appKey,
        string appId,
        string appSecret,
        bool isDefault = false,
        int timeOut = 30) => new()
        {
            AppKey = appKey,
            AppId = appId,
            AppSecret = appSecret,
            IsDefault = isDefault,
            TimeOut = timeOut
        };

    private static (ServiceProvider Provider, FeishuAppManager Manager) CreateManager(params FeishuAppConfig[] configs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configs.ToList());
        var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        return (provider, manager);
    }

    [Fact]
    public void OnConfigurationChanged_ShouldAddNewApp_WhenAppKeyIsNew()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)));

        ctx.Manager.HasApp("app2").Should().BeFalse();

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
        });

        ctx.Manager.HasApp("app2").Should().BeTrue();
        ctx.Manager.GetAllApps().Select(a => a.Config.AppKey).Should().Contain("app2");
    }

    [Fact]
    public void OnConfigurationChanged_ShouldRemoveApp_WhenAppDisappearsFromConfig()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)));

        ctx.Manager.HasApp("app2").Should().BeTrue();

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)
        });

        ctx.Manager.HasApp("app2").Should().BeFalse();
    }

    [Fact]
    public void OnConfigurationChanged_ShouldRebuildContext_WhenAppConfigUpdated()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 30)));

        var before = ctx.Manager.GetApp("app1");
        before.Config.TimeOut.Should().Be(30);

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 60)
        });

        var after = ctx.Manager.GetApp("app1");
        after.Should().NotBeSameAs(before, "配置变更必须重建应用上下文，否则仍会走旧配置");
        after.Config.TimeOut.Should().Be(60);
    }

    [Fact]
    public void OnConfigurationChanged_ShouldSwitchDefaultApp_WhenIsDefaultMoves()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr, isDefault: false)));

        ctx.Manager.GetDefaultApp().Config.AppKey.Should().Be("app1");

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: false),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr, isDefault: true)
        });

        ctx.Manager.GetDefaultApp().Config.AppKey.Should().Be("app2");
    }

    [Fact]
    public void OnConfigurationChanged_ShouldNotRebuild_WhenSnapshotIsIdentical()
    {
        var snapshot = new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)
        };
        using var ctx = new ProviderScope(CreateManager(snapshot.ToArray()));

        var before = ctx.Manager.GetApp("app1");

        // 连续两次推送完全相同的快照：节流应阻止重建
        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)
        });

        ctx.Manager.GetApp("app1").Should().BeSameAs(before);
    }

    [Fact]
    public void OnConfigurationChanged_ShouldIgnoreEmptyConfig_WhenNoAppRemains()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)));

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>());

        ctx.Manager.HasApp("app1").Should().BeTrue("空配置会清空全部应用，应被忽略以保持服务可用");
    }

    [Fact]
    public void OnConfigurationChanged_ShouldNotThrow_WhenConcurrentWithReads()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)));

        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        Parallel.For(0, 64, i =>
        {
            try
            {
                if (i % 4 == 0)
                {
                    ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
                    {
                        CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 30 + i),
                        CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
                    });
                }
                else
                {
                    _ = ctx.Manager.GetApp("app1");
                    _ = ctx.Manager.GetAllApps().ToList();
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        errors.Should().BeEmpty("热更新与并发读取必须线程安全，且旧上下文延迟回收不得抛 ObjectDisposedException");
    }

    /// <summary>
    /// TMF-02：凭据变更热更新（Phase-P 锁外清库 + Task.Run 同步等待）与 GetApp/GetAllApps
    /// 并发交错——必须完成且无死锁（Task.WhenAll + 总超时护栏，不依赖 Thread.Sleep 时序）。
    /// 修复前清库 IO 在 <c>_configApplyLock</c> 锁内同步阻塞，并发热更新回调被串行拖住。
    /// </summary>
    [Fact]
    public async Task OnConfigurationChanged_WithCredentialChange_ShouldNotDeadlock_WhenConcurrentWithReads()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)));

        _ = ctx.Manager.GetApp("app1");

        var hotReload = Task.Run(() =>
        {
            for (var i = 0; i < 20; i++)
            {
                // 每轮交替轮换 AppSecret → 每轮都触发 Phase-P 凭据变更清库。
                ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
                {
                    CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default,
                        i % 2 == 0 ? "rotated_secret_a" : "rotated_secret_b", isDefault: true),
                    CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
                });
            }
        });

        var reads = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                _ = ctx.Manager.GetApp("app1");
                _ = ctx.Manager.GetAllApps().ToList();
                await Task.Yield();
            }
        });

        // 总超时护栏：若死锁则 30s 超时使测试失败（而非无限挂起）。
        await Task.WhenAll(hotReload, reads).WaitAsync(TimeSpan.FromSeconds(30));

        ctx.Manager.HasApp("app1").Should().BeTrue("并发热更新完成后应用注册表必须完整");
        ctx.Manager.HasApp("app2").Should().BeTrue();
    }

    [Fact]
    public void FeishuAppOptions_ShouldDefaultToEnabledReload()
    {
        var options = new FeishuAppOptions();

        options.EnableConfigReload.Should().BeTrue("本项目无存量行为需要保持，默认应启用热更新");
    }

    [Fact]
    public void AddFeishuApp_ShouldReloadManager_WhenConfigurationReloaded()
    {
        var data = new Dictionary<string, string?>
        {
            [$"{SectionName}:0:AppKey"] = "app1",
            [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
            [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
            [$"{SectionName}:0:IsDefault"] = "true"
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        var provider = services.BuildServiceProvider();
        try
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

            manager.GetApp("app1").Config.TimeOut.Should().Be(30);

            // 通过 IConfiguration 真实触发 IOptionsMonitor.OnChange
            var memoryProvider = configuration.Providers
                .OfType<MemoryConfigurationProvider>()
                .First();
            memoryProvider.Set($"{SectionName}:0:TimeOut", "90");
            ((IConfigurationRoot)configuration).Reload();

            manager.GetApp("app1").Config.TimeOut.Should().Be(90,
                "启用热更新后，IConfiguration 变更必须传导到 FeishuAppManager");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void AddFeishuApp_ShouldNotReloadManager_WhenReloadDisabled()
    {
        var data = new Dictionary<string, string?>
        {
            [$"{SectionName}:0:AppKey"] = "app1",
            [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
            [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
            [$"{SectionName}:0:IsDefault"] = "true"
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);
        services.Configure<FeishuAppOptions>(o => o.EnableConfigReload = false);

        var provider = services.BuildServiceProvider();
        try
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

            var memoryProvider = configuration.Providers
                .OfType<MemoryConfigurationProvider>()
                .First();
            memoryProvider.Set($"{SectionName}:0:TimeOut", "90");
            ((IConfigurationRoot)configuration).Reload();

            manager.GetApp("app1").Config.TimeOut.Should().Be(30,
                "EnableConfigReload = false 时应保持旧语义：配置变更需重启");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        var (provider, manager) = CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true));

        var act = () =>
        {
            manager.Dispose();
            manager.Dispose();
        };

        act.Should().NotThrow();
        provider.Dispose();
    }

    /// <summary>
    /// TMA2-09 / D13（§7.2 #12）核心：热更新中任一应用的上下文构造失败时，
    /// 必须<b>整体放弃</b>本次变更（Phase-A 失败 → 不动注册表与快照），
    /// 不得出现"app1 已重建为 TimeOut=60、app2 保持旧配置"的部分应用状态。
    /// </summary>
    [Fact]
    public void OnConfigurationChanged_ShouldNotPartiallyApply_WhenSecondAppAssemblyFails()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // 令牌存储工厂桩：前 2 次（两个应用的初始实例化）成功；
        // 之后 app2 的构造（Phase-A 中的重建预构造）抛出瞬时可重试异常。
        var callCount = 0;
        var tokenStoreFactoryMock = new Mock<IFeishuTokenStoreFactory>();
        tokenStoreFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns((string appKey) =>
            {
                callCount++;
                if (callCount > 2 && appKey == "app2")
                    throw new HttpRequestException("模拟 app2 上下文构造失败（如存储瞬时故障）");
                return (new Mock<ITokenStore>().Object, new Mock<IUserTokenStore>().Object);
            });

        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (existingDescriptor != null) services.Remove(existingDescriptor);
        services.AddSingleton(tokenStoreFactoryMock.Object);

        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 30),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
        });
        using var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

        // 初始实例化 app1（消耗工厂桩的前 2 次调用预算中 app1 的份额）
        var before = manager.GetApp("app1");
        before.Config.TimeOut.Should().Be(30);

        // Act：同时更新两个应用（app1 → TimeOut=60；app2 配置合法但工厂桩抛异常）
        var act = () => manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 60),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
        });

        act.Should().NotThrow("Phase-A 失败应被吞掉并记录日志（IOptionsMonitor 回调不允许抛异常）");

        // Assert：不得部分应用——app1 保持旧配置与旧实例
        manager.GetApp("app1").Should().BeSameAs(before,
            "Phase-A 任一应用构造失败时必须整体放弃，app1 不得被部分应用");
        manager.GetApp("app1").Config.TimeOut.Should().Be(30,
            "变更未应用，配置快照保持原状");
        manager.HasApp("app2").Should().BeTrue();
    }

    private sealed class ProviderScope : IDisposable
    {
        public ProviderScope((ServiceProvider Provider, FeishuAppManager Manager) pair)
        {
            Provider = pair.Provider;
            Manager = pair.Manager;
        }

        public ServiceProvider Provider { get; }

        public FeishuAppManager Manager { get; }

        public void Dispose() => Provider.Dispose();
    }
}
