// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET6_0_OR_GREATER

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Authentication;
using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// U-5（多命中外部标识反查，上游 3.0.10 Batch 2）的飞书侧接入测试。
/// </summary>
/// <remarks>
/// U-8（官方 AppInstantiated 事件面）评估为不桥接：<see cref="FeishuAppManager"/> 已有同名
/// public event（<c>FeishuAppInstantiatedEventArgs</c>，既有公共 API），同名遮蔽 + 双通道双发
/// 风险 + 公共事件类型破坏，登记为 4.0 择期迁移（详见 <see cref="FeishuAppManager"/> 内注释）。
/// </remarks>
public class FeishuAppMultiLookupAndInstantiatedTests
{
    private static List<FeishuAppConfig> CreateConfigs(params (string AppKey, string AppId, string AppSecret, bool IsDefault)[] apps)
        => apps.Select(a => new FeishuAppConfig
        {
            AppKey = a.AppKey,
            AppId = a.AppId,
            AppSecret = a.AppSecret,
            IsDefault = a.IsDefault
        }).ToList();

    private static (ServiceProvider Provider, FeishuAppManager Manager, FakeLookupProvider Lookup) CreateHost(
        List<FeishuAppConfig> configs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var lookup = new FakeLookupProvider();
        services.AddSingleton<IAppLookupProvider>(lookup);
        services.AddFeishuApp(configs);
        var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        return (provider, manager, lookup);
    }

    /// <summary>
    /// 双接口替身：同时实现 <see cref="IAppLookupProvider"/>（单结果面）与
    /// <see cref="IMultiAppLookupProvider"/>（多命中面），由测试场景决定形态。
    /// </summary>
    private sealed class FakeLookupProvider : IAppLookupProvider, IMultiAppLookupProvider
    {
        public Func<IReadOnlyDictionary<string, string>, string?>? SingleResolve { get; set; }

        public Func<IReadOnlyDictionary<string, string>, IEnumerable<string>>? MultiResolve { get; set; }

        public bool TryResolveAppKey(IReadOnlyDictionary<string, string> externalIds, out string? appKey)
        {
            var resolved = SingleResolve?.Invoke(externalIds);
            appKey = resolved;
            return !string.IsNullOrEmpty(resolved);
        }

        public IEnumerable<string> ResolveAllAppKeys(IReadOnlyDictionary<string, string> externalIds)
            => MultiResolve?.Invoke(externalIds) ?? Array.Empty<string>();
    }

    private static IReadOnlyDictionary<string, string> ExternalIds(string appId) =>
        new Dictionary<string, string>(StringComparer.Ordinal) { ["app_id"] = appId };

    /// <summary>
    /// 纯单结果面替身：只实现 <see cref="IAppLookupProvider"/>（宿主尚未升级多命中面的形态）。
    /// </summary>
    private sealed class SingleHitLookupProvider : IAppLookupProvider
    {
        public Func<IReadOnlyDictionary<string, string>, string?>? Resolve { get; set; }

        public bool TryResolveAppKey(IReadOnlyDictionary<string, string> externalIds, out string? appKey)
        {
            var resolved = Resolve?.Invoke(externalIds);
            appKey = resolved;
            return !string.IsNullOrEmpty(resolved);
        }
    }

    // ─────────────── U-5：TryResolveAllAppKeys ───────────────

    [Fact]
    public void TryResolveAllAppKeys_ShouldDelegate_To_MultiHitProvider_WhenImplemented()
    {
        // 提供者多命中返回三个键，其中 "ghost" 未注册 —— 必须被 HasApp 过滤；
        // "app2" 重复返回 —— 必须被去重。
        var (_, manager, lookup) = CreateHost(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true),
            ("app2", AppConfigs.AppIds.Hr, AppConfigs.Secrets.Hr, IsDefault: false)));
        try
        {
            lookup.MultiResolve = _ => new[] { "app1", "app2", "app2", "ghost" };

            var resolved = manager.TryResolveAllAppKeys(ExternalIds("app_hr")).ToList();

            Assert.Equal(new[] { "app1", "app2" }.OrderBy(x => x).ToList(), resolved.OrderBy(x => x).ToList());
        }
        finally
        {
            (manager as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public void TryResolveAllAppKeys_ShouldFallBack_To_SingleResult_WhenProviderIsSingleHitOnly()
    {
        // 提供者只实现单结果面（未实现 IMultiAppLookupProvider）：回退 F-4 全量语义（内置索引 → 提供者兜底）。
        var services = new ServiceCollection();
        services.AddLogging();
        var singleOnly = new SingleHitLookupProvider
        {
            Resolve = ids => ids.TryGetValue("app_id", out var appId) && appId == "known" ? "app1" : null
        };
        services.AddSingleton<IAppLookupProvider>(singleOnly);
        services.AddFeishuApp(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true)));
        using var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        try
        {
            var hit = manager.TryResolveAllAppKeys(ExternalIds("known")).ToList();
            var miss = manager.TryResolveAllAppKeys(ExternalIds("unknown")).ToList();

            Assert.Equal(new[] { "app1" }, hit);
            Assert.Empty(miss);
        }
        finally
        {
            (manager as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public void TryResolveAllAppKeys_ShouldReturnEmpty_ForNullOrEmptyExternalIds()
    {
        var (_, manager, _) = CreateHost(CreateConfigs(
            ("app1", AppConfigs.AppIds.Default, AppConfigs.Secrets.Default, IsDefault: true)));
        try
        {
            Assert.Empty(manager.TryResolveAllAppKeys(new Dictionary<string, string>()));
            Assert.Empty(manager.TryResolveAllAppKeys(null!));
        }
        finally
        {
            (manager as IDisposable)?.Dispose();
        }
    }

}

#endif
