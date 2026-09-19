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
/// R5/X7：旧扁平键（<c>TimeOut</c> / <c>RetryCount</c> / <c>CircuitBreaker*</c>）的回填必须在
/// <b>启动链</b>与<b>热更链</b>上行为一致。
/// </summary>
/// <remarks>
/// <para>
/// 修复前回填只在启动链（<c>AddFeishuApp</c> 内显式调用 <c>ApplyLegacyFlatKeys</c>）执行，
/// 热更链（<c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;</c> 重建）不回填 →
/// <b>首次热更即以默认值重建应用上下文</b>（如 60 秒超时静默回退 30 秒）。
/// </para>
/// <para>
/// 实现形态约束（G-01/RK11，本组测试同时是不变量锁）：
/// 热更绑定必须保留 <c>Configure&lt;List&lt;FeishuAppConfig&gt;&gt;(IConfiguration)</c> 重载
/// （其内部经 <c>OptionsBuilder.Bind</c> 注册 <c>IConfigurationChangeTokenSource</c>），
/// 回填并入既有 <c>PostConfigure</c>。若后续重构把手写委托换进来而丢失变更令牌，
/// <see cref="HotReload_ShouldReapplyLegacyFlatKeys_WhenOnlyLegacyKeyChanges"/> 与
/// <see cref="HotReload_ShouldStillFireChangeNotification"/> 这类依赖真实
/// <c>IConfiguration.Reload()</c> 传导的用例会整体变红。
/// </para>
/// <para>
/// 另锁定两条不变量：热更对 <c>List&lt;FeishuAppConfig&gt;</c> 是<b>重绑</b>而非<b>追加</b>语义，
/// 且回填不得打乱 <c>PostConfigure</c> 中的 <c>IsDefault</c> 推断。
/// </para>
/// </remarks>
public class AppConfigLegacyFlatKeyHotReloadTests
{
    private const string SectionName = "FeishuApps";

    /// <summary>
    /// 单应用基础配置（<c>IsDefault=true</c>，AppKey 固定为 app1）。
    /// </summary>
    private static Dictionary<string, string?> BaseData() => new()
    {
        [$"{SectionName}:0:AppKey"] = "app1",
        [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
        [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
        [$"{SectionName}:0:IsDefault"] = "true"
    };

    /// <summary>
    /// 热更夹具：持有容器、应用管理器与可触发真实 <c>Reload()</c> 的配置根。
    /// </summary>
    private sealed record HotReloadContext(
        ServiceProvider Provider,
        IFeishuAppManager Manager,
        IConfigurationRoot Configuration) : IDisposable
    {
        public void Dispose() => Provider.Dispose();

        public void Set(string key, string value)
        {
            Configuration.Providers.OfType<MemoryConfigurationProvider>().First().Set(key, value);
            Configuration.Reload();
        }
    }

    private static HotReloadContext Build(string legacyTimeOut)
    {
        var data = BaseData();
        if (legacyTimeOut is not null)
            data[$"{SectionName}:0:TimeOut"] = legacyTimeOut;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IFeishuAppManager>();
        return new HotReloadContext(provider, manager, configuration);
    }

    private static (ServiceProvider Provider, IConfigurationRoot Configuration, MemoryConfigurationProvider MemoryProvider) Build(
        Dictionary<string, string?> data)
    {
        var configuration = new ConfigurationBuilder()
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
    public void HotReload_ShouldReapplyLegacyFlatKeys_WhenOnlyLegacyKeyChanges()
    {
        using var ctx = Build(legacyTimeOut: "60");

        ctx.Manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(60,
            "启动链回填（既有行为）必须生效");

        // 核心（X7）：热更链也必须回填——否则旧键变更会以默认值 30 重建上下文。
        ctx.Set($"{SectionName}:0:TimeOut", "90");

        ctx.Manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(90,
            "热更后旧扁平键 TimeOut=90 必须回填生效，不得回退为默认 30（X7 修复点）");
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
    public void HotReload_ShouldPreferNestedTimeoutSeconds_OverLegacyTimeOut()
    {
        using var ctx = Build(legacyTimeOut: "60");

        // 嵌套键显式存在时回填必须让位（ApplyLegacyFlatKeys 仅在嵌套值为默认时覆盖）
        ctx.Set($"{SectionName}:0:TimeoutSeconds", "75");
        ctx.Set($"{SectionName}:0:TimeOut", "90");

        ctx.Manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(75,
            "回填仅是兼容路径：显式嵌套 TimeoutSeconds 恒优先于旧 TimeOut");
    }

    [Fact]
    public void HotReload_ShouldNotDuplicateApps_WhenConfigurationReloaded()
    {
        // G-01 不变量：Configure<List<T>>(IConfiguration) 对 List 是「重绑」而非「追加」，
        // 热更多次后应用数量不得增长。
        using var ctx = Build(legacyTimeOut: "60");

        ctx.Set($"{SectionName}:0:TimeOut", "90");
        ctx.Set($"{SectionName}:0:TimeOut", "120");

        ctx.Manager.ConfiguredAppKeys.Should().HaveCount(1,
            "热更对 List<FeishuAppConfig> 是重绑语义，重复 Reload 不得追加应用");
        ctx.Manager.GetApp("app1").Config.TimeoutSeconds.Should().Be(120);
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

    [Fact]
    public void HotReload_ShouldKeepIsDefaultInference_WhenConfigurationReloaded()
    {
        // G-01 不变量：PostConfigure 内回填与 IsDefault 推断共存，热更后推断仍生效。
        var data = new Dictionary<string, string?>
        {
            [$"{SectionName}:0:AppKey"] = "default",
            [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
            [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
            // 故意不写 IsDefault —— 依赖 PostConfigure 的「AppKey=default 自动推断」
            [$"{SectionName}:0:TimeOut"] = "60"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);
        using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IFeishuAppManager>();

        manager.GetDefaultApp().Config.AppKey.Should().Be("default", "启动期推断生效");

        configuration.Providers.OfType<MemoryConfigurationProvider>().First()
            .Set($"{SectionName}:0:TimeOut", "90");
        configuration.Reload();

        manager.GetDefaultApp().Config.AppKey.Should().Be("default",
            "热更后 IsDefault 推断必须仍然生效（回填不得打乱 PostConfigure 顺序）");
        manager.GetApp("default").Config.TimeoutSeconds.Should().Be(90);
    }
}
