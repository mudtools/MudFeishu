// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMR2-P1-1：per-app 认证客户端装配回归测试（D7 契约）。
/// </summary>
/// <remarks>
/// <para>
/// 修复前 <c>PerAppFeishuAuthenticationFactory.Create</c> 使用
/// <c>ActivatorUtilities.CreateInstance&lt;IFeishuAuthentication&gt;(sp, client)</c>——
/// <c>T</c> 为<b>接口</b>时该方法恒抛 <see cref="InvalidOperationException"/>
/// （无公共构造可选中），于是每次都走 <c>catch</c> 降级为
/// <c>GetRequiredService&lt;IFeishuAuthentication&gt;()</c>（默认应用端点）。
/// </para>
/// <para>
/// 后果：<c>EnablePerAppAuthenticationClient=true</c>（默认）名存实亡——
/// 非默认应用的取令牌请求被发往<b>默认应用端点</b>（多区域部署下即"凭据发往错区域"）。
/// </para>
/// </remarks>
public class PerAppFeishuAuthenticationFactoryTests
{
    private const string AppKey = "hr-app";

    private sealed class TestHarness : IDisposable
    {
        public ServiceProvider Provider { get; set; } = null!;
        public Mock<IFeishuHttpClientFactory> HttpClientFactory { get; set; } = null!;
        public Mock<IEnhancedHttpClient> PerAppClient { get; set; } = null!;
        public Mock<IEnhancedHttpClient> DefaultClient { get; set; } = null!;
        public Mock<IHttpRequestExecutor> Executor { get; set; } = null!;
        public Mock<IFeishuAuthentication> DefaultAppAuthentication { get; set; } = null!;
        public List<IBaseHttpClient> ObservedClients { get; } = new();

        public void Dispose() => Provider?.Dispose();
    }

    private static TestHarness CreateHarness()
    {
        var perAppClient = new Mock<IEnhancedHttpClient>();
        var defaultClient = new Mock<IEnhancedHttpClient>();
        var defaultAppAuthentication = new Mock<IFeishuAuthentication>();

        var executor = new Mock<IHttpRequestExecutor>();
        var harness = new TestHarness
        {
            PerAppClient = perAppClient,
            DefaultClient = defaultClient,
            DefaultAppAuthentication = defaultAppAuthentication,
            Executor = executor
        };

        executor
            .Setup(x => x.ExecuteAsync<TenantAppCredentialResult?>(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<IBaseHttpClient>(),
                It.IsAny<ExecutionDescriptor>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, IBaseHttpClient, ExecutionDescriptor, object?, CancellationToken>(
                (_, client, _, _, _) => harness.ObservedClients.Add(client))
            .ReturnsAsync(new TenantAppCredentialResult
            {
                Code = 0,
                Msg = "success",
                Expire = 7200,
                TenantAccessToken = "tenant-token"
            });

        var httpClientFactory = new Mock<IFeishuHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateBasic(It.IsAny<string>())).Returns(perAppClient.Object);
        harness.HttpClientFactory = httpClientFactory;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(executor.Object);
        // 默认应用的认证客户端（降级路径会拿到它）——修复后**不得**再被使用
        services.AddSingleton(defaultAppAuthentication.Object);

        harness.Provider = services.BuildServiceProvider();
        return harness;
    }

    [Fact]
    public async Task Create_ShouldUsePerAppNamedClient_WhenEnabled()
    {
        using var harness = CreateHarness();
        var factory = new PerAppFeishuAuthenticationFactory(
            harness.Provider, harness.HttpClientFactory.Object);

        // Act
        var authentication = factory.Create(AppKey);
        await authentication.GetTenantAccessTokenAsync(new AppCredentials
        {
            AppId = "cli_h1i2j3k4l5m6n7o8",
            AppSecret = "secret_hr_0123456789012345"
        });

        // Assert：请求必须落在 per-app 命名客户端上（修复前恒为默认应用客户端）
        harness.HttpClientFactory.Verify(x => x.CreateBasic(AppKey), Times.Once);
        harness.ObservedClients.Should().ContainSingle();
        harness.ObservedClients[0].Should().BeSameAs(harness.PerAppClient.Object,
            "per-app 认证客户端必须装配到本应用的命名 HttpClient（D7 契约）");
    }

    [Fact]
    public void Create_ShouldNotFallbackToDefaultEndpoint()
    {
        using var harness = CreateHarness();
        var factory = new PerAppFeishuAuthenticationFactory(
            harness.Provider, harness.HttpClientFactory.Object);

        // Act
        var authentication = factory.Create(AppKey);

        // Assert：缺陷形态不可回归——修复前 Create 恒走 catch 降级返回 DI 单例
        // （默认应用端点），此处会与之相同而失败。
        authentication.Should().NotBeSameAs(harness.DefaultAppAuthentication.Object);
        harness.DefaultAppAuthentication.Verify(
            x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenAppKeyIsBlank()
    {
        using var harness = CreateHarness();
        var factory = new PerAppFeishuAuthenticationFactory(
            harness.Provider, harness.HttpClientFactory.Object);

        var act = () => factory.Create("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldFailFast_WhenExecutorNotRegistered()
    {
        // 装配失败必须显式失败（fail-fast），不得静默回退到"用错端点"的降级路径。
        var httpClientFactory = new Mock<IFeishuHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateBasic(It.IsAny<string>()))
            .Returns(new Mock<IEnhancedHttpClient>().Object);

        var services = new ServiceCollection();
        services.AddLogging();
        using var provider = services.BuildServiceProvider();
        var factory = new PerAppFeishuAuthenticationFactory(provider, httpClientFactory.Object);

        var act = () => factory.Create(AppKey);
        act.Should().Throw<InvalidOperationException>(
            "IHttpRequestExecutor 未注册属装配缺失，必须显式失败而非静默使用默认端点");
    }
}
