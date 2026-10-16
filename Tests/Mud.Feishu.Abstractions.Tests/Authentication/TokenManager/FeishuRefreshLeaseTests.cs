// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

/// <summary>
/// U-3（上游 3.0.10 Batch 1）跨进程刷新去重租约的<b>飞书侧接线</b>测试。
/// </summary>
/// <remarks>
/// 租约的完整行为语义（获取/等待/读穿透/fail-open/释放所有权）由上游
/// <c>TokenManagerRefreshLeaseTests</c> 钉死；本类只验证飞书侧的接入面：
/// <list type="bullet">
/// <item>三个管理器的 <c>RefreshLease</c> 覆写正确返回注入实例（未注入时为 null = 零行为变化）；</item>
/// <item><c>DefaultFeishuTokenManagerFactory</c> 经 DI 解析 <see cref="ITokenRefreshLease"/> 注入三元组；</item>
/// <item>真实刷新路径经租约去重：获取成功后刷新并释放句柄；未抢到时等待读穿透命中另一实例写入的令牌。</item>
/// </list>
/// </remarks>
public class FeishuRefreshLeaseTests
{
    private static readonly PropertyInfo RefreshLeaseProperty =
        typeof(TokenManagerBase).GetProperty("RefreshLease", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string Encode(string token, long expireMs)
        => TokenStoreHelper.EncodeStoredToken(token, expireMs);

    private static FeishuAppConfig CreateConfig() => new()
    {
        AppKey = "test",
        AppId = "test_app_id_1234567890",
        AppSecret = "test_app_secret_123456",
        TokenRefreshThreshold = 300
    };

    private static Mock<IFeishuAuthentication> CreateAuthMock(string token = "api-token")
    {
        var mock = new Mock<IFeishuAuthentication>();
        mock.Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantAppCredentialResult
            {
                TenantAccessToken = token,
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });
        return mock;
    }

    private static TenantTokenManager CreateTenantManager(
        Mock<IFeishuAuthentication> auth, Mock<ITokenStore> store, ITokenRefreshLease? lease)
        => new(auth.Object,
            Options.Create(CreateConfig()),
            new Mock<ILogger<TenantTokenManager>>().Object,
            store.Object,
            lease);

    [Fact]
    public void RefreshLease_WithoutInjectedLease_ShouldBeNull()
    {
        // 未启用 Redis（Mud.Feishu.Redis 未注册 ITokenRefreshLease）时必须为 null：
        // 上游契约「RefreshLease 默认 null = 行为与 3.0.9 逐位一致」。
        var auth = CreateAuthMock();
        using var manager = CreateTenantManager(auth, new Mock<ITokenStore>(), null);

        Assert.Null(RefreshLeaseProperty.GetValue(manager));
    }

    [Fact]
    public void RefreshLease_WithInjectedLease_ShouldReturnSameInstance()
    {
        var auth = CreateAuthMock();
        var lease = new Mock<ITokenRefreshLease>();
        using var manager = CreateTenantManager(auth, new Mock<ITokenStore>(), lease.Object);

        Assert.Same(lease.Object, RefreshLeaseProperty.GetValue(manager));
    }

    [Fact]
    public async Task LeaseAcquired_Should_Refresh_Then_Release_Handle_Once()
    {
        var auth = CreateAuthMock("leased-refresh-token");
        var store = new Mock<ITokenStore>();
        store.Setup(x => x.GetAccessTokenAsync("TenantAccessToken:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var handle = new Mock<ITokenRefreshLeaseHandle>();
        var lease = new Mock<ITokenRefreshLease>();
        var capturedKeys = new List<string>();
        lease.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string k, TimeSpan _, CancellationToken _) =>
            {
                capturedKeys.Add(k);
                return handle.Object;
            });

        using var manager = CreateTenantManager(auth, store, lease.Object);
        var token = await manager.GetTokenAsync(CancellationToken.None);

        Assert.Equal("leased-refresh-token", token);
        Assert.True(capturedKeys.Count == 1, $"TryAcquire 应恰好调用一次，实际 {capturedKeys.Count} 次；实际键=[{string.Join("][", capturedKeys)}]");
        Assert.True(capturedKeys[0] == "TenantTokenManager:test:default",
            $"租约键必须由 BuildRefreshLeaseKey（{{管理器键含AppKey}}:{{scopeKey}}）派生，实际键={capturedKeys[0]}");
        handle.Verify(x => x.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Once,
            "刷新完成（含异常路径）后必须恰好释放一次租约句柄");
    }

    [Fact]
    public async Task LeaseNotAcquired_Should_Return_Token_Written_During_Wait()
    {
        // 多实例模拟：本实例未抢到租约 → 限时轮询读穿透 → 命中持有方已写穿的新令牌 → 免本机刷新。
        var auth = new Mock<IFeishuAuthentication>();   // 刻意不打桩：若进入本机刷新将抛异常使测试失败
        var store = new Mock<ITokenStore>();
        var encoded = Encode("other-instance-token", NowMs + 7_200_000);
        var readCalls = 0;
        store.Setup(x => x.GetAccessTokenAsync("TenantAccessToken:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => readCalls++ == 0 ? null : encoded);   // 首读未命中（进入刷新路径），等待期命中

        var lease = new Mock<ITokenRefreshLease>();
        lease.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ITokenRefreshLeaseHandle?)null);

        using var manager = CreateTenantManager(auth, store, lease.Object);
        var token = await manager.GetTokenAsync(CancellationToken.None);

        Assert.Equal("other-instance-token", token);
        auth.Verify(
            x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "等待期读穿透命中后不得再发起本机刷新（这正是租约去重的目的）");
    }

    [Fact]
    public void Factory_Should_Inject_DI_Resolved_Lease_Into_All_Managers()
    {
        var authMock = CreateAuthMock();
        var lease = new Mock<ITokenRefreshLease>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(authMock.Object);
        services.AddSingleton<ITokenRefreshLease>(lease.Object);
        using var serviceProvider = services.BuildServiceProvider();

        var factory = new DefaultFeishuTokenManagerFactory(serviceProvider);
        var (tenant, app, user) = factory.Create(
            CreateConfig(), authMock.Object, new Mock<ITokenStore>().Object, userTokenStore: null);

        try
        {
            Assert.Same(lease.Object, RefreshLeaseProperty.GetValue(tenant));
            Assert.Same(lease.Object, RefreshLeaseProperty.GetValue(app));
            Assert.Same(lease.Object, RefreshLeaseProperty.GetValue(user));
        }
        finally
        {
            (tenant as IDisposable)?.Dispose();
            (app as IDisposable)?.Dispose();
            (user as IDisposable)?.Dispose();
        }
    }
}
