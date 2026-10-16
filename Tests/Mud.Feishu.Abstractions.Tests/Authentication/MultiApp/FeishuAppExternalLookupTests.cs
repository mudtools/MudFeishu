// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Authentication;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.Authentication.MultiApp;

/// <summary>
/// F-4（Mud.HttpUtils 3.0.7）外部标识反查接入用例：
/// <see cref="FeishuAppContext"/> 声明 <c>app_id → appKey</c> 外部标识（内置索引，实例化后自动登记），
/// <see cref="FeishuAppManager"/> 叠加 DI 解析的宿主 <see cref="IAppLookupProvider"/> 兜底，
/// 未命中 / 输入非法 / 映射歧义一律 fail-closed。
/// </summary>
public class FeishuAppExternalLookupTests
{
    private static ServiceCollection CreateServiceCollection(IAppLookupProvider? externalLookup = null)
    {
        var services = new ServiceCollection();

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);
        var authFactoryMock = new Mock<IFeishuAuthenticationFactory>();
        authFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton(authFactoryMock.Object);
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();
        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();
        services.AddMemoryCache();
        services.AddTransient<IHttpRequestExecutor>(sp => new Mock<IHttpRequestExecutor>().Object);
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();
        services.TryAddSingleton<IFeishuTokenManagerFactory, DefaultFeishuTokenManagerFactory>();

        if (externalLookup is not null)
            services.AddSingleton(externalLookup);

        return services;
    }

    private static FeishuAppConfig CreateConfig(string appKey, string appId, bool isDefault = false) => new()
    {
        AppKey = appKey,
        AppId = appId,
        AppSecret = "sec_" + appKey,
        IsDefault = isDefault
    };

    private static FeishuAppManager BuildManager(
        ServiceCollection services, params FeishuAppConfig[] configs)
    {
        var provider = services.BuildServiceProvider();
        return new FeishuAppManager(
            provider,
            configs,
            NullLogger<FeishuAppManager>.Instance);
    }

    private static FeishuAppContext CreateContext(FeishuAppConfig config) => new(
        config,
        new Mock<ITenantTokenManager>().Object,
        new Mock<IAppTokenManager>().Object,
        new Mock<IFeishuUserTokenManager>().Object,
        new Mock<IFeishuAuthentication>().Object,
        new Mock<IEnhancedHttpClient>().Object);

    // ──────────────────────────────────────────────
    //  内置索引（IAppExternalIds：app_id → appKey）
    // ──────────────────────────────────────────────

    [Fact]
    public void TryResolveAppKey_ByInstantiatedContextAppId_ShouldResolve()
    {
        var manager = BuildManager(
            CreateServiceCollection(),
            CreateConfig("default", "cli_default", isDefault: true),
            CreateConfig("hr", "cli_hr"));

        // 内置索引只覆盖已实例化上下文（基类语义：懒加载不实例化，避免副作用）——先物化再反查。
        manager.TryGetApp("hr", out _).Should().BeTrue();

        manager.TryResolveAppKey(new Dictionary<string, string> { ["app_id"] = "cli_hr" }, out var appKey)
            .Should().BeTrue();
        appKey.Should().Be("hr");
    }

    [Fact]
    public void TryResolveAppKey_LazyUnmaterializedApp_ShouldFailClosed()
    {
        var manager = BuildManager(
            CreateServiceCollection(),
            CreateConfig("default", "cli_default", isDefault: true),
            CreateConfig("hr", "cli_hr"));

        // 未物化的应用不参与内置索引（基类 F-4 语义：避免实例化副作用）。
        manager.TryResolveAppKey(new Dictionary<string, string> { ["app_id"] = "cli_hr" }, out var appKey)
            .Should().BeFalse();
        appKey.Should().BeNull();
    }

    [Fact]
    public void TryResolveAppKey_UnknownAppId_ShouldFailClosed()
    {
        var manager = BuildManager(
            CreateServiceCollection(),
            CreateConfig("default", "cli_default", isDefault: true));

        manager.TryGetApp("default", out _).Should().BeTrue();
        manager.TryResolveAppKey(new Dictionary<string, string> { ["app_id"] = "cli_unknown" }, out var appKey)
            .Should().BeFalse();
        appKey.Should().BeNull();
    }

    [Fact]
    public void TryResolveAppKey_AmbiguousAppId_ShouldFailClosed()
    {
        // 同一 app_id 被两个应用声明 → 映射歧义 → 该键 fail-closed（宁可不可查，不可查错）。
        var manager = BuildManager(
            CreateServiceCollection(),
            CreateConfig("default", "cli_default", isDefault: true),
            CreateConfig("a", "cli_dupe"),
            CreateConfig("b", "cli_dupe"));

        manager.TryGetApp("a", out _).Should().BeTrue();
        manager.TryGetApp("b", out _).Should().BeTrue();

        manager.TryResolveAppKey(new Dictionary<string, string> { ["app_id"] = "cli_dupe" }, out var appKey)
            .Should().BeFalse();
        appKey.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryResolveAppKey_InvalidInput_ShouldFailClosed(bool provideEmptyDictionary)
    {
        var manager = BuildManager(
            CreateServiceCollection(),
            CreateConfig("default", "cli_default", isDefault: true));

        manager.TryGetApp("default", out _).Should().BeTrue();

        var externalIds = provideEmptyDictionary
            ? new Dictionary<string, string>()
            : null!;

        manager.TryResolveAppKey(externalIds, out var appKey).Should().BeFalse();
        appKey.Should().BeNull();
    }

    // ──────────────────────────────────────────────
    //  宿主自实现 IAppLookupProvider 兜底（DI 注入）
    // ──────────────────────────────────────────────

    [Fact]
    public void TryResolveAppKey_ExternalProvider_ShouldResolve_WhenBuiltInMisses()
    {
        // 宿主提供者可映射任意外部标识（如 suite_id / tenant_key）到已注册 appKey。
        var providerMock = new Mock<IAppLookupProvider>();
        providerMock
            .Setup(x => x.TryResolveAppKey(It.IsAny<IReadOnlyDictionary<string, string>>(), out It.Ref<string?>.IsAny))
            .Callback(new TryResolveCallback((IReadOnlyDictionary<string, string> _, out string? appKey) => appKey = "hr"))
            .Returns(true);

        var manager = BuildManager(
            CreateServiceCollection(providerMock.Object),
            CreateConfig("default", "cli_default", isDefault: true),
            CreateConfig("hr", "cli_hr"));

        manager.TryGetApp("hr", out _).Should().BeTrue();

        manager.TryResolveAppKey(new Dictionary<string, string> { ["suite_id"] = "ww123" }, out var appKey)
            .Should().BeTrue();
        appKey.Should().Be("hr");
    }

    [Fact]
    public void TryResolveAppKey_ExternalProvider_UnregisteredTarget_ShouldFailClosed()
    {
        // 解析结果必须落在已注册应用上（防外部不可信输入误路由到不存在/错误的应用）。
        var providerMock = new Mock<IAppLookupProvider>();
        providerMock
            .Setup(x => x.TryResolveAppKey(It.IsAny<IReadOnlyDictionary<string, string>>(), out It.Ref<string?>.IsAny))
            .Callback(new TryResolveCallback((IReadOnlyDictionary<string, string> _, out string? appKey) => appKey = "ghost"))
            .Returns(true);

        var manager = BuildManager(
            CreateServiceCollection(providerMock.Object),
            CreateConfig("default", "cli_default", isDefault: true));

        manager.TryResolveAppKey(new Dictionary<string, string> { ["suite_id"] = "ww123" }, out var appKey)
            .Should().BeFalse();
        appKey.Should().BeNull();
    }

    // Moq 对 out 参数的赋值需与接口签名完全一致的委托经 Callback 传出。
    private delegate void TryResolveCallback(
        IReadOnlyDictionary<string, string> externalIds, out string? appKey);

    // ──────────────────────────────────────────────
    //  FeishuAppContext.ExternalIds 快照语义
    // ──────────────────────────────────────────────

    [Fact]
    public void ExternalIds_ShouldExposeAppIdSnapshot()
    {
        using var context = CreateContext(CreateConfig("hr", "cli_hr"));

        var snapshot = context.ExternalIds;
        snapshot.Should().ContainKey("app_id").WhoseValue.Should().Be("cli_hr");

        // 快照语义：两次调用返回独立字典，调用方修改不影响后续读取。
        snapshot.Should().NotBeSameAs(context.ExternalIds);
    }

    [Fact]
    public void ExternalIds_EmptyAppId_ShouldReturnEmptySnapshot()
    {
        using var context = CreateContext(CreateConfig("hr", string.Empty));

        context.ExternalIds.Should().BeEmpty();
    }
}
