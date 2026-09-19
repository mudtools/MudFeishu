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

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// R5/X7：旧扁平键回填在<b>热更链</b>与启动链行为一致。
/// </summary>
/// <remarks>
/// <para>
/// 改造前 <c>ApplyLegacyFlatKeys</c> 只在启动链（<c>AddFeishuApp</c> 内的显式回填）执行，
/// 热更链（<c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;</c> 重建）不回填——
/// 结果是「首次热更即以默认值重建上下文」，旧扁平键（<c>TimeOut</c> / <c>RetryCount</c> /
/// <c>CircuitBreaker*</c>）的兼容读取静默失效（如 <c>TimeOut=60</c> 回退为 30s）。
/// </para>
/// <para>
/// 本条同时锁定 G-01/RK11 的三条不变量：<b>热更不回退</b>、<b>变更通知仍触发</b>
/// （修复必须保留 <c>Configure&lt;List&lt;FeishuAppConfig&gt;&gt;(IConfiguration)</c> 重载的
/// <c>ConfigurationChangeTokenSource</c>，手写委托会静默切断热更新）、<b>不得重复追加应用</b>。
/// </para>
/// </remarks>
public class AppConfigLegacyFlatKeyHotReloadTests
{
    private const string SectionName = "FeishuApps";

    private static Dictionary<string, string?> BaseData() => new()
    {
        [$"{SectionName}:0:AppKey"] = "app1",
        [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
        [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
        [$"{SectionName}:0:IsDefault"] = "true"
    };

    private static (ServiceProvider Provider, IConfigurationRoot Configuration, MemoryConfigurationProvider MemoryProvider) Build(
        Dictionary<string, string?> data)
    {
        var configuration = (IConfigurationRoot)new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        return (services.BuildServiceProvider(),
            configuration,
            configuration.Providers.OfType<MemoryConfigurationProvider>().First());
    }

    [Fact]
    public void Startup_ShouldBackfillLegacyFlatKey()
    {
        var data = BaseData();
        data[$"{SectionName}:0:TimeOut"] = "60";

        var (provider, _, _) = Build(data);
        using (provider)
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

            manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(60,
                "启动链的旧扁平键回填是既有行为（X7 只要求热更链与之对齐）");
        }
    }

    [Fact]
    public void HotReload_ShouldBackfillLegacyFlatKeys_NotFallBackToDefaults()
    {
        var data = BaseData();
        data[$"{SectionName}:0:TimeOut"] = "60";

        var (provider, configuration, memoryProvider) = Build(data);
        using (provider)
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
            manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(60);

            // 热更：修改旧扁平键并触发真实 Reload
            memoryProvider.Set($"{SectionName}:0:TimeOut", "120");
            configuration.Reload();

            manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(120,
                "热更链必须与启动链一样回填旧扁平键；改造前会以默认值 30 重建（X7 静默回退）");
        }
    }

    [Fact]
    public void HotReload_ShouldStillFireChangeNotification()
    {
        var data = BaseData();
        data[$"{SectionName}:0:TimeOut"] = "60";

        var (provider, configuration, memoryProvider) = Build(data);
        using (provider)
        {
            var monitor = provider.GetRequiredService<IOptionsMonitor<List<FeishuAppConfig>>>();
            var fired = 0;
            using var subscription = monitor.OnChange(_ => Interlocked.Increment(ref fired));

            memoryProvider.Set($"{SectionName}:0:TimeOut", "120");
            configuration.Reload();

            fired.Should().BeGreaterThan(0,
                "G-01/RK11：修复必须保留 Configure<T>(IConfiguration) 重载注册的 ConfigurationChangeTokenSource；" +
                "手写委托会让 OnChange 永不触发（热更新静默失效）");
            monitor.CurrentValue.Single().TimeoutSeconds.Should().Be(120);
        }
    }

    [Fact]
    public void HotReload_MultipleTimes_ShouldNotDuplicateApps()
    {
        var data = BaseData();

        var (provider, configuration, memoryProvider) = Build(data);
        using (provider)
        {
            var monitor = provider.GetRequiredService<IOptionsMonitor<List<FeishuAppConfig>>>();

            for (var i = 1; i <= 3; i++)
            {
                memoryProvider.Set($"{SectionName}:0:TimeOut", (30 + i * 10).ToString());
                configuration.Reload();
            }

            monitor.CurrentValue.Should().HaveCount(1,
                "热更以 PostConfigure 重建选项，不得重复追加应用（G-01 锁定项）");
        }
    }
}
