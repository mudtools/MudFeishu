// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Helpers;

/// <summary>
/// TMF2-07 / TMU-03：热更新 × 并发首访的确定性竞态测试基建。
/// </summary>
/// <remarks>
/// <para>
/// 通过覆写 <see cref="FeishuAppManager.CreateAppContext"/> 安装门闸
/// <see cref="ManualResetEventSlim"/>（<c>EnteredCreate</c> / <c>ReleaseCreate</c>），
/// 实现"并发首访进入构造 → 阻塞 → 主线程执行热更新 → 放行"的确定性交错。
/// </para>
/// <para>
/// <b>不使用 <c>Thread.Sleep</c></b>（AGENTS.md 测试规范），全部基于门闸信号。
/// </para>
/// <para>
/// 竞态矩阵（固化在测试文件头注释中）：
/// <list type="table">
/// <item><term>并发首访 × 热更新（更新）</term><description>TMF2-01 方案 B 覆盖</description></item>
/// <item><term>并发首访 × 热更新（凭据变更）</term><description>TMF2-01 + TMF2-02 联合覆盖</description></item>
/// <item><term>并发首访 × 热更新（删除）</term><description>待覆盖（M5）</description></item>
/// <item><term>并发首访 × 热更新（新增）</term><description>不适用（新增应用无 Lazy 可替换）</description></item>
/// <item><term>并发首访 × 热更新（默认切换）</term><description>待覆盖（M5）</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class HotReloadRaceHarness : IDisposable
{
    /// <summary>
    /// 门闸：并发首访线程进入 <see cref="FeishuAppManager.CreateAppContext"/> 时设置。
    /// 测试线程在此 <c>Wait</c> 以确保并发首访已进入构造路径但尚未完成。
    /// </summary>
    public ManualResetEventSlim EnteredCreate => Manager.EnteredCreate;

    /// <summary>
    /// 门闸：并发首访线程在 <see cref="FeishuAppManager.CreateAppContext"/> 中等待放行。
    /// 测试线程在完成热更新后 <c>Set</c> 此门闸以放行并发首访。
    /// </summary>
    public ManualResetEventSlim ReleaseCreate => Manager.ReleaseCreate;

    /// <summary>
    /// 门闸：并发首访线程完成上下文构造后设置。
    /// 测试线程在此 <c>Wait</c> 以确保并发首访已走出构造路径。
    /// </summary>
    public ManualResetEventSlim CompletedCreate => Manager.CompletedCreate;

    /// <summary>
    /// 底层 GatedFeishuAppManager 实例。
    /// </summary>
    public GatedFeishuAppManager Manager { get; }

    /// <summary>
    /// DI ServiceProvider（测试结束需 Dispose）。
    /// </summary>
    public ServiceProvider Provider { get; }

    private HotReloadRaceHarness(GatedFeishuAppManager manager, ServiceProvider provider)
    {
        Manager = manager;
        Provider = provider;
    }

    /// <summary>
    /// 创建门闸基建：管理器配置了 <paramref name="configs"/>，其中
    /// <paramref name="gatedAppKey"/> 对应的应用在首次构造时会被门闸拦截。
    /// </summary>
    public static HotReloadRaceHarness Create(
        IReadOnlyList<FeishuAppConfig> configs,
        string gatedAppKey,
        Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configs.ToList());
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();

        var gatedManager = new GatedFeishuAppManager(
            provider,
            configs,
            provider.GetRequiredService<ILogger<FeishuAppManager>>(),
            gatedAppKey);

        return new HotReloadRaceHarness(gatedManager, provider);
    }

    /// <summary>
    /// 在测试线程上执行热更新（替换 gatedAppKey 的配置）。
    /// 调用前应先 <c>EnteredCreate.Wait()</c> 确保并发首访已进入构造路径。
    /// </summary>
    public void TriggerHotReload(List<FeishuAppConfig> newConfigs)
    {
        Manager.OnConfigurationChanged(newConfigs);
    }

    /// <summary>
    /// 放行被门闸阻塞的并发首访线程。
    /// </summary>
    public void ReleaseGate() => ReleaseCreate.Set();

    /// <summary>
    /// 等待并发首访线程完成上下文构造。
    /// </summary>
    public void WaitForCompletion(TimeSpan? timeout = null)
        => CompletedCreate.Wait(timeout ?? TimeSpan.FromSeconds(10));

    public void Dispose()
    {
        Manager.Dispose();
        Provider.Dispose();
    }
}

/// <summary>
/// 覆写 <see cref="FeishuAppManager.CreateAppContext"/> 以安装门闸的测试替身。
/// </summary>
public sealed class GatedFeishuAppManager : FeishuAppManager
{
    /// <summary>
    /// 门闸：进入 CreateAppContext 时设置。
    /// </summary>
    internal ManualResetEventSlim EnteredCreate { get; } = new(initialState: false);

    /// <summary>
    /// 门闸：在 CreateAppContext 中等待放行。
    /// </summary>
    internal ManualResetEventSlim ReleaseCreate { get; } = new(initialState: false);

    /// <summary>
    /// 门闸：完成 CreateAppContext 后设置。
    /// </summary>
    internal ManualResetEventSlim CompletedCreate { get; } = new(initialState: false);

    private readonly string _gatedAppKey;
    // 门闸仅拦截一次（并发首访），Phase-A 重建的 CreateAppContext 调用直接放行。
    private int _gateConsumed; // 0=未消费, 1=已消费

    /// <summary>
    /// 初始化 GatedFeishuAppManager 实例。
    /// </summary>
    public GatedFeishuAppManager(
        IServiceProvider serviceProvider,
        IReadOnlyList<FeishuAppConfig> configs,
        ILogger<FeishuAppManager> logger,
        string gatedAppKey)
        : base(serviceProvider, configs, logger)
    {
        _gatedAppKey = gatedAppKey ?? throw new ArgumentNullException(nameof(gatedAppKey));
    }

    /// <inheritdoc />
    protected override FeishuAppContext CreateAppContext(FeishuAppConfig config)
    {
        // 门闸仅对 gatedAppKey 的首次调用生效（并发首访），后续 Phase-A 重建直接放行。
        if (string.Equals(config.AppKey, _gatedAppKey, StringComparison.Ordinal)
            && Interlocked.CompareExchange(ref _gateConsumed, 1, 0) == 0)
        {
            // 通知测试线程：并发首访已进入 CreateAppContext
            EnteredCreate.Set();

            // 等待测试线程放行（热更新在此期间执行）
            ReleaseCreate.Wait(TimeSpan.FromSeconds(30));

            try
            {
                return base.CreateAppContext(config);
            }
            finally
            {
                // 通知测试线程：并发首访已完成构造
                CompletedCreate.Set();
            }
        }

        return base.CreateAppContext(config);
    }
}
