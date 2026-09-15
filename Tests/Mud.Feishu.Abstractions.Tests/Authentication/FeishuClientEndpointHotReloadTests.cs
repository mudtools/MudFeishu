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
using Mud.Feishu.Abstractions.Tests.Helpers;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// ARC-7：验证命名客户端的端点（<c>BaseUrl</c> / <c>TimeOut</c>）参与配置热更新。
/// </summary>
/// <remarks>
/// <para>
/// 修复前 <c>AddFeishuAppBaseServices</c> 把 <c>BaseUrl</c> / <c>TimeOut</c> 固化进命名客户端的注册委托，
/// <c>IHttpClientFactory</c> 虽然每次 <c>CreateClient</c> 都会执行该委托，但闭包捕获的是<b>注册期</b>快照，
/// 因此 <c>IConfiguration</c> 变更后新建的客户端仍指向旧地址 —— 飞书 / Lark 双区域切换只能靠重启。
/// </para>
/// <para>
/// 修复方式：追加 <c>IHttpClientBuilder.ConfigureHttpClient(IServiceProvider, HttpClient)</c>（DI 感知配置入口），
/// 在每次创建客户端时从 <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;</c> 读取当前值。
/// </para>
/// <para>
/// 说明：不采用组件 <c>WithBaseAddress</c> 方案 —— <c>TokenRecoveryEnhancedClient</c> 为 sealed 且未重写该方法，
/// 基类实现返回普通 <c>HttpClientFactoryEnhancedClient</c>（丢失令牌恢复并导致强转抛 <c>InvalidCastException</c>），
/// 详见附录 B-1。
/// </para>
/// </remarks>
public class FeishuClientEndpointHotReloadTests
{
    private const string SectionName = "FeishuApps";
    private const string FeishuBaseUrl = "https://open.feishu.cn";
    private const string LarkBaseUrl = "https://open.larksuite.com";

    private static Dictionary<string, string?> BaseSettings(params (string AppKey, string AppId, string AppSecret, string BaseUrl, int TimeOut, bool IsDefault)[] apps)
    {
        var data = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var i = 0; i < apps.Length; i++)
        {
            var (appKey, appId, appSecret, baseUrl, timeOut, isDefault) = apps[i];
            data[$"{SectionName}:{i}:AppKey"] = appKey;
            data[$"{SectionName}:{i}:AppId"] = appId;
            data[$"{SectionName}:{i}:AppSecret"] = appSecret;
            data[$"{SectionName}:{i}:BaseUrl"] = baseUrl;
            data[$"{SectionName}:{i}:TimeOut"] = timeOut.ToString(System.Globalization.CultureInfo.InvariantCulture);
            data[$"{SectionName}:{i}:IsDefault"] = isDefault ? "true" : "false";
        }

        return data;
    }

    [Fact]
    public void NamedClient_ShouldUseUpdatedBaseAddress_WhenBaseUrlChangesViaConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(BaseSettings(("app1", TestDataFactory.AppConfigs.AppIds.App1, TestDataFactory.AppConfigs.Secrets.App1, FeishuBaseUrl, 30, true)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        using var provider = services.BuildServiceProvider();
        try
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            factory.CreateClient("feishu-app1").BaseAddress!.Host.Should().Be("open.feishu.cn");

            SetAndReload(configuration, $"{SectionName}:0:BaseUrl", LarkBaseUrl);

            factory.CreateClient("feishu-app1").BaseAddress!.Host.Should().Be(
                "open.larksuite.com",
                "BaseUrl 变更必须作用到新建的命名客户端，否则多区域切换仍需重启（ARC-7）");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void NamedClient_ShouldUseUpdatedTimeout_WhenTimeOutChangesViaConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(BaseSettings(("app1", TestDataFactory.AppConfigs.AppIds.App1, TestDataFactory.AppConfigs.Secrets.App1, FeishuBaseUrl, 30, true)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        using var provider = services.BuildServiceProvider();
        try
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            factory.CreateClient("feishu-app1").Timeout.Should().Be(TimeSpan.FromSeconds(30));

            SetAndReload(configuration, $"{SectionName}:0:TimeOut", "90");

            factory.CreateClient("feishu-app1").Timeout.Should().Be(
                TimeSpan.FromSeconds(90),
                "TimeOut 变更必须作用到新建的命名客户端");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void AppContextClient_ShouldUseUpdatedBaseAddress_AfterHotReload()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(BaseSettings(("app1", TestDataFactory.AppConfigs.AppIds.App1, TestDataFactory.AppConfigs.Secrets.App1, FeishuBaseUrl, 30, true)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        using var provider = services.BuildServiceProvider();
        try
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
            manager.GetApp("app1").HttpClient.BaseAddress!.Host.Should().Be("open.feishu.cn");

            // 真实热更新链路：Reload → IOptionsMonitor.OnChange → RebuildAppContext → 新建 TokenRecoveryEnhancedClient
            SetAndReload(configuration, $"{SectionName}:0:BaseUrl", LarkBaseUrl);

            manager.GetApp("app1").HttpClient.BaseAddress!.Host.Should().Be(
                "open.larksuite.com",
                "应用上下文重建后，新客户端必须命中新 BaseUrl（端到端链路）");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void NamedClient_ShouldKeepOtherAppEndpoint_WhenOnlyOneAppIsUpdated()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(BaseSettings(
                ("app1", TestDataFactory.AppConfigs.AppIds.App1, TestDataFactory.AppConfigs.Secrets.App1, FeishuBaseUrl, 30, true),
                ("app2", TestDataFactory.AppConfigs.AppIds.App2, TestDataFactory.AppConfigs.Secrets.App2, LarkBaseUrl, 30, false)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        using var provider = services.BuildServiceProvider();
        try
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            SetAndReload(configuration, $"{SectionName}:0:BaseUrl", LarkBaseUrl);

            factory.CreateClient("feishu-app1").BaseAddress!.Host.Should().Be("open.larksuite.com");
            factory.CreateClient("feishu-app2").BaseAddress!.Host.Should().Be(
                "open.larksuite.com",
                "未变更的应用必须保持自身端点（此处 app2 本就是 larksuite），不得被 app1 的变更串改");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void ResolveLatestAppConfig_ShouldReturnNull_WhenOptionsMonitorIsNotRegistered()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        FeishuServiceCollectionExtensions.ResolveLatestAppConfig(provider, "app1").Should().BeNull(
            "未接入配置管线时应返回 null，使调用方保持注册期快照（行为与修复前一致）");
    }

    [Fact]
    public void ResolveLatestAppConfig_ShouldMatchAppKeyWithOrdinalComparison()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "App1",
                AppId = TestDataFactory.AppConfigs.AppIds.App1,
                AppSecret = TestDataFactory.AppConfigs.Secrets.App1,
                IsDefault = true
            }
        });

        using var provider = services.BuildServiceProvider();

        FeishuServiceCollectionExtensions.ResolveLatestAppConfig(provider, "App1").Should().NotBeNull();
        FeishuServiceCollectionExtensions.ResolveLatestAppConfig(provider, "app1").Should().BeNull(
            "AppKey 必须严格大小写敏感（与 FeishuAppManager 的 Ordinary 比较口径一致）");
    }

    private static void SetAndReload(IConfigurationRoot configuration, string key, string value)
    {
        var memoryProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();
        memoryProvider.Set(key, value);
        configuration.Reload();
    }
}
