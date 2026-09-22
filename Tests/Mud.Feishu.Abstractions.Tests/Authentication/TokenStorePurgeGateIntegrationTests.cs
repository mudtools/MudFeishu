// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.DataModels;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMR2-P1-5：凭据变更清库的「打门 + 异步清库」端到端测试（D10 契约）。
/// </summary>
/// <remarks>
/// <para>
/// 修复前 <c>PurgeCredentialChangedTokens</c> 在 <c>IOptionsMonitor.OnChange</c> 回调线程上
/// <c>Task.Run(...).Wait(10s)</c>：回调线程最长阻塞 10s、宿主存在 <c>SynchronizationContext</c> 时有死锁面。
/// </para>
/// <para>
/// 修复后：Phase-P 只同步打门（微秒级）→ 异步清库；D10 的实质约束由恢复路径的门短路承担。
/// 本类用「阻塞清库」的存储桩把清库窗口<b>显式拉长</b>，从而确定性地观察两件事：
/// ① 回调线程不再被阻塞；② 门挂起期间恢复路径不读 store。
/// </para>
/// </remarks>
public class TokenStorePurgeGateIntegrationTests
{
    private const string AppKey = "app1";

    private static FeishuAppConfig Config(string appKey, string appId, string appSecret, bool isDefault = false) => new()
    {
        AppKey = appKey,
        AppId = appId,
        AppSecret = appSecret,
        IsDefault = isDefault
    };

    /// <summary>
    /// 测试夹具：阻塞清库的存储桩 + 返回固定令牌的认证桩。
    /// </summary>
    private sealed class Harness
    {
        private readonly TaskCompletionSource<bool> _purgeGate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string> TenantStoreReads { get; } = new();

        public int ClearInvocations { get; private set; }

        public ServiceProvider BuildProvider()
        {
            var tenantStore = new Mock<ITokenStore>();
            tenantStore
                .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((tokenType, _) => TenantStoreReads.Add(tokenType))
                .ReturnsAsync((string?)null);
            tenantStore
                .Setup(x => x.SetAccessTokenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            tenantStore
                .Setup(x => x.SetRefreshTokenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            tenantStore
                .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            tenantStore
                .Setup(x => x.GetTokenTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<string>());
            // 清库被显式阻塞：把「清库窗口」拉长到测试可控
            tenantStore
                .Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken _) =>
                {
                    ClearInvocations++;
                    return _purgeGate.Task;
                });

            var userStore = new Mock<IUserTokenStore>();

            var storeFactory = new Mock<IFeishuTokenStoreFactory>();
            storeFactory
                .Setup(x => x.Create(It.IsAny<string>()))
                .Returns((string _) => (tenantStore.Object, (IUserTokenStore?)userStore.Object));

            var authentication = new Mock<IFeishuAuthentication>();
            authentication
                .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantAppCredentialResult
                {
                    Code = 0,
                    Msg = "success",
                    Expire = 7200,
                    TenantAccessToken = "token-" + AppKey
                });

            var authFactory = new Mock<IFeishuAuthenticationFactory>();
            authFactory.Setup(x => x.Create(It.IsAny<string>())).Returns(authentication.Object);

            var services = new ServiceCollection();
            services.AddLogging();
            // 必须先于 AddFeishuApp 注册（TryAddSingleton 先注册者胜出）
            services.AddSingleton(storeFactory.Object);
            services.AddSingleton(authFactory.Object);

            services.AddFeishuApp(new List<FeishuAppConfig>
            {
                Config(AppKey, "cli_a1b2c3d4e5f6g7h8", "secret_default_0123456789", isDefault: true)
            });

            return services.BuildServiceProvider();
        }

        /// <summary>凭据变更后的配置（触发 Phase-P 清库）。</summary>
        public List<FeishuAppConfig> RotatedConfigs() => new()
        {
            Config(AppKey, "cli_a1b2c3d4e5f6g7h8", "rotated_secret_0123456789", isDefault: true)
        };

        /// <summary>放行被阻塞的清库。</summary>
        public void ReleasePurge() => _purgeGate.TrySetResult(true);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.Elapsed < timeout)
        {
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task OnConfigurationChanged_ShouldNotBlockCallbackThread_WhenPurgeIsSlow()
    {
        TokenStorePurgeGate.ResetForTest();
        var harness = new Harness();
        using var provider = harness.BuildProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        _ = manager.GetApp(AppKey);

        try
        {
            // Act
            var sw = Stopwatch.StartNew();
            manager.OnConfigurationChanged(harness.RotatedConfigs());
            sw.Stop();

            // Assert：回调线程不得等待清库（修复前为 Task.Run(...).Wait(10s)）
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
                "凭据变更热更新不得在 IOptionsMonitor.OnChange 回调线程上阻塞等待清库");

            // Assert：门已同步挂起（D10 约束的承载点）
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.Pending,
                "Phase-P 必须同步打门，使恢复路径在清库完成前短路 store");

            harness.ReleasePurge();
            await WaitUntilAsync(
                () => TokenStorePurgeGate.Query(AppKey) == TokenStorePurgeGate.PurgeGateState.NotPending,
                TimeSpan.FromSeconds(10));

            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending,
                "两次清库（Phase-P + 提交后）各自撤门，归零后门必须撤除");
        }
        finally
        {
            harness.ReleasePurge();
            TokenStorePurgeGate.ResetForTest();
        }
    }

    [Fact]
    public async Task GetTokenAsync_ShouldSkipStoreRestore_WhilePurgePending()
    {
        TokenStorePurgeGate.ResetForTest();
        var harness = new Harness();
        using var provider = harness.BuildProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        _ = manager.GetApp(AppKey);

        try
        {
            manager.OnConfigurationChanged(harness.RotatedConfigs());
            TokenStorePurgeGate.Query(AppKey).Should().Be(TokenStorePurgeGate.PurgeGateState.Pending);

            // Act：门挂起期间取令牌——必须跳过 store 恢复（等价于「清库完成前不得恢复旧凭据令牌」）
            var token = await manager.GetApp(AppKey).TenantTokenManager.GetTokenAsync();

            token.Should().Be("token-" + AppKey);
            harness.TenantStoreReads.Should().BeEmpty(
                "凭据变更清库进行中，恢复路径必须短路 store（D10）");

            // 有界等待：确认清库确实已发起（异步执行，避免与 Task.Run 调度竞态）
            for (var i = 0; i < 500 && harness.ClearInvocations == 0; i++)
            {
                await Task.Delay(20);
            }

            harness.ClearInvocations.Should().BeGreaterThan(0, "清库必须已被发起（Phase-P 异步执行）");
        }
        finally
        {
            harness.ReleasePurge();
            TokenStorePurgeGate.ResetForTest();
        }
    }
}
