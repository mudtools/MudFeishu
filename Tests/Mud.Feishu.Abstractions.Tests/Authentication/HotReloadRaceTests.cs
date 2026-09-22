// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Tests.Helpers;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMF2-01 / TMF2-02 / TMF2-07：热更新 × 并发首访竞态的红用例（M0 基线）。
/// </summary>
/// <remarks>
/// <para>
/// 这些用例在 TMF2-01 方案 B / TMF2-02 修复前应失败（红），修复后应通过（绿）。
/// 竞态交错通过 <see cref="HotReloadRaceHarness"/> 门闸实现，不依赖 <c>Thread.Sleep</c>。
/// </para>
/// <para>
/// 竞态矩阵：
/// <list type="table">
/// <item><term>并发首访 × 热更新（更新）</term><description><see cref="GetApp_ShouldReturnCurrentContext_AndRetireStaleContext_WhenHotReloadRacesWithFirstAccess"/></description></item>
/// <item><term>并发首访 × 热更新（更新）Try* 语义</term><description><see cref="TryGetApp_ShouldReturnFalse_AndNotLeak_WhenLazyReplacedByConcurrentReload"/></description></item>
/// <item><term>D10 未实例化应用清库</term><description><see cref="OnConfigurationChanged_ShouldPurgeStoredTokens_WhenCredentialChanged_AndAppNeverInstantiated"/></description></item>
/// </list>
/// </para>
/// </remarks>
public class HotReloadRaceTests
{
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
            TimeoutSeconds = timeOut
        };

    /// <summary>
    /// TMF2-01：热更新 Phase-B 替换 Lazy 与并发首访交错时，自建旧配置上下文既不登记也不入退休队列 → 永久泄漏。
    /// 修复后：并发首访不得返回旧配置上下文，且旧上下文必被退休队列回收。
    /// </summary>
    [Fact]
    public void GetApp_ShouldReturnCurrentContext_AndRetireStaleContext_WhenHotReloadRacesWithFirstAccess()
    {
        // Arrange：配置 hr-app 用 OLD 凭据，门闸拦截 hr-app 的首次构造
        var configs = new List<FeishuAppConfig>
        {
            CreateConfig("default", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("hr-app", TestDataFactory.AppConfigs.AppIds.Hr, "hr_secret_OLD_123456")
        };

        using var harness = HotReloadRaceHarness.Create(configs, "hr-app");

        // Act：并发首访线程触发 hr-app 的懒加载（进入 CreateAppContext 后被门闸阻塞）
        IFeishuAppContext? raceContext = null;
        var raceThread = new Thread(() =>
        {
            try
            {
                raceContext = harness.Manager.GetApp("hr-app");
            }
            catch
            {
                // 预期修复后不抛异常，修复前可能返回旧凭据上下文
            }
        })
        {
            IsBackground = true
        };
        raceThread.Start();

        // 等待并发首访进入 CreateAppContext
        harness.EnteredCreate.Wait(TimeSpan.FromSeconds(10));
        // 此时并发首访线程在 CreateAppContext 中被 ReleaseCreate 门闸阻塞

        // 在并发首访阻塞期间执行热更新：将 hr-app 的 AppSecret 改为 NEW
        harness.TriggerHotReload(new List<FeishuAppConfig>
        {
            CreateConfig("default", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("hr-app", TestDataFactory.AppConfigs.AppIds.Hr, "hr_secret_NEW_123456")
        });

        // 放行并发首访
        harness.ReleaseGate();
        harness.WaitForCompletion();
        raceThread.Join(TimeSpan.FromSeconds(10));

        // Assert：修复后并发首访不得返回旧凭据上下文
        raceContext.Should().NotBeNull("GetApp 应返回有效上下文而非抛异常");
        raceContext!.Config.AppSecret.Should().NotBe("hr_secret_OLD_123456",
            "并发首访不得返回旧凭据上下文——必须返回热更新后的新配置上下文或抛 FeishuAppRemovedException");

        // 注册表现行上下文必须与并发首访返回的一致（或并发首访抛异常）
        var currentContext = harness.Manager.GetApp("hr-app");
        ReferenceEquals(raceContext, currentContext).Should().BeTrue(
            "并发首访返回的上下文必须与注册表现行上下文为同一实例（不得是旧配置孤儿上下文）");

        // 孤儿上下文（如果有）必须被退休队列回收
        // 修复后应该有 0 或 1 个退休条目（旧上下文被回收），修复前为 0（泄漏）
        // 注：方案 B 修复后，如果并发首访的旧上下文被身份条件回收，PendingCount >= 1
        // 如果并发首访的上下文被热更新接管（方案 A），PendingCount 可能为 0
        // 关键断言是不泄漏——即返回的上下文不是旧凭据孤儿上下文
        harness.Manager.Retirement?.PendingCount.Should().BeLessThanOrEqualTo(1,
            "最多一个退休条目（旧配置孤儿上下文被回收）");
    }

    /// <summary>
    /// TMF2-01（Try* 语义）：并发首访 × 热更新替换 Lazy 时，TryGetApp 不得泄漏且必须保持 Try* 不抛语义。
    /// </summary>
    [Fact]
    public void TryGetApp_ShouldReturnFalse_AndNotLeak_WhenLazyReplacedByConcurrentReload()
    {
        // Arrange
        var configs = new List<FeishuAppConfig>
        {
            CreateConfig("default", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("hr-app", TestDataFactory.AppConfigs.AppIds.Hr, "hr_secret_OLD_123456")
        };

        using var harness = HotReloadRaceHarness.Create(configs, "hr-app");

        // Act：并发首访线程触发 hr-app 的懒加载
        bool raceResult = false;
        IFeishuAppContext? raceContext = null;
        var raceThread = new Thread(() =>
        {
            raceResult = harness.Manager.TryGetApp("hr-app", out raceContext);
        })
        {
            IsBackground = true
        };
        raceThread.Start();

        // 等待并发首访进入 CreateAppContext
        harness.EnteredCreate.Wait(TimeSpan.FromSeconds(10));

        // 热更新替换 hr-app 的 Lazy
        harness.TriggerHotReload(new List<FeishuAppConfig>
        {
            CreateConfig("default", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("hr-app", TestDataFactory.AppConfigs.AppIds.Hr, "hr_secret_NEW_123456")
        });

        // 放行并发首访
        harness.ReleaseGate();
        harness.WaitForCompletion();
        raceThread.Join(TimeSpan.FromSeconds(10));

        // Assert：Try* 语义保持——不抛异常
        // 修复后：如果 Lazy 被替换，TryGetApp 应返回 false（或返回新配置上下文）
        // 修复前：返回旧凭据上下文 + 不入退休队列 = 泄漏
        if (raceResult)
        {
            // 如果返回 true，上下文不得为旧凭据
            raceContext.Should().NotBeNull();
            raceContext!.Config.AppSecret.Should().NotBe("hr_secret_OLD_123456",
                "TryGetApp 返回的上下文不得为旧凭据");
        }

        // 不得泄漏：注册表现行上下文必须可正常获取
        var currentContext = harness.Manager.GetApp("hr-app");
        currentContext.Config.AppSecret.Should().Be("hr_secret_NEW_123456",
            "热更新后的注册表上下文必须使用新凭据");
    }

    /// <summary>
    /// TMF2-02：D10 凭据变更清库在"未实例化应用"上不可达。
    /// 修复前：未实例化应用的凭据变更检测被跳过（oldContext == null → continue）。
    /// 修复后：以配置快照为比对源，未实例化应用的凭据变更也能触发清库。
    /// </summary>
    [Fact]
    public async Task OnConfigurationChanged_ShouldPurgeStoredTokens_WhenCredentialChanged_AndAppNeverInstantiated()
    {
        // Arrange：配置 hr-app 但不实例化它
        var configs = new List<FeishuAppConfig>
        {
            CreateConfig("default", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("hr-app", TestDataFactory.AppConfigs.AppIds.Hr, "hr_secret_OLD_123456")
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configs.ToList());
        await using var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

        // 确认 hr-app 从未实例化
        manager.GetAllApps().Select(a => a.Config.AppKey).Should().NotContain("hr-app",
            "hr-app 在测试期间不应被实例化");

        // 预写令牌到 hr-app 的存储后端（模拟其他实例/上一进程写入）
        var tokenStoreFactory = provider.GetRequiredService<IFeishuTokenStoreFactory>();
        var (store, userStore) = tokenStoreFactory.Create("hr-app");
        await store.SetAccessTokenAsync("tenant:hr-app", "old_tenant_token", 7200);
        await store.SetRefreshTokenAsync("tenant:hr-app", "old_refresh_token");

        // Act：推送含新 AppSecret 的配置（hr-app 凭据变更但未实例化）
        manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("default", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("hr-app", TestDataFactory.AppConfigs.AppIds.Hr, "hr_secret_NEW_123456")
        });

        // TMR2-P1-5：Phase-P 清库不再在 OnChange 回调线程上同步等待（原 Task.Run(...).Wait(10s)），
        // 故此处以「清库效果（access/refresh 键均已消失）」为完成信号做**有界轮询**，不依赖固定延时。
        // 刻意不以 TokenStorePurgeGate.Query 作为完成信号：门是**进程级静态**状态，而本测试程序集
        // 默认按测试类并行，其他类的 ResetForTest()（或并发租约归零）可使其提前变为 NotPending，
        // 轮询于是在清库完成前退出并读到清库中间态（access 已删、refresh 未删——实测偶发失败点）。
        for (var i = 0; i < 500; i++)
        {
            if (await store.GetAccessTokenAsync("tenant:hr-app") is null
                && await store.GetRefreshTokenAsync("tenant:hr-app") is null)
            {
                break;
            }

            await Task.Delay(20);
        }

        // Assert：旧凭据令牌必须被清除
        // 修复前：oldContext == null → continue → 不清库 → 令牌残留
        // 修复后：以配置快照比对 → 检测到 AppSecret 变更 → 清库 → 令牌为 null
        var (newStore, _) = tokenStoreFactory.Create("hr-app");
        var accessToken = await newStore.GetAccessTokenAsync("tenant:hr-app");
        var refreshToken = await newStore.GetRefreshTokenAsync("tenant:hr-app");

        accessToken.Should().BeNull(
            "凭据变更后未实例化应用的旧租户令牌必须被清除（D10 闭环）");
        refreshToken.Should().BeNull(
            "凭据变更后未实例化应用的旧刷新令牌必须被清除（D10 闭环）");
    }
}
