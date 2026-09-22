// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
using Mud.Feishu.Abstractions.Internal;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器实现
/// </summary>
/// <remarks>
/// 负责管理系统中所有飞书应用的创建、获取、移除等操作。
/// 每个应用拥有独立的配置、缓存和TokenManager实例。
/// 继承 DefaultAppManager&lt;FeishuAppContext&gt; 获得通用的应用管理能力。
/// <para>
/// M-1 修复：采用懒加载（Lazy Init）策略，构造函数仅预注册所有应用的 Lazy 上下文并通过本类私有字段
/// <c>_defaultAppKey</c> 记录默认应用键（A-1 修复：不再通过反射设置基类私有字段），不立即创建任何应用实例。
/// 应用在首次访问 GetApp/GetDefaultApp/TryGetApp 时按需创建，
/// 减少启动延迟并避免构造阶段强制要求所有 DI 依赖（如 IMemoryCache）就绪。
/// </para>
/// <para>
/// M-2 修复：类可见性从 internal 改为 public，允许用户继承并覆盖 <see cref="CreateAppContext"/> 以支持自定义 <see cref="IFeishuAppContext"/>。
/// </para>
/// </remarks>
public class FeishuAppManager : DefaultAppManager<IFeishuAppContext>, IFeishuAppManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    // TMA-13 / P2-10 修复：根 SP 访问器，用于按需创建作用域解析 Scoped 依赖。
    // 避免 Singleton 捕获请求作用域的 IServiceProvider（Captive Dependency）。
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<FeishuAppManager> _logger;
    private readonly ConcurrentDictionary<string, Lazy<FeishuAppContext>> _lazyContexts = new();
    // TMA-05 / P1-4 修复（D4 契约）：配置快照改为不可变数组 + Volatile.Write/Read 原子交换，
    // 消灭"锁内 Clear/AddRange × 锁外枚举"的数据竞争。
    // MT-1：本字段仅经 Volatile.Read/Volatile.Write 原子访问（自带完整栅栏语义，等效 volatile 的发布/获取保证），
    // 不再标记 volatile——volatile 与 Volatile/Interlocked API 混用会产生 CS0420。
    // 构造函数内的裸读/赋值为发布前单线程初始化，无并发访问。
    private FeishuAppConfig[] _configs;
    // A-1 修复：本类私有的默认应用键，替代反射设置基类 _defaultAppKey 的反模式。
    // 构造阶段赋值（单线程），AddApp/RemoveApp 时同步更新，GetDefaultApp 直接读取。
    // NEW-MA-09 修复：标记 volatile 确保 RemoveApp 的写入对 GetDefaultApp 的读取立即可见，
    // 配合 _defaultAppLock 保证"读取 key → GetApp(key)"的 TOCTOU 区间内 key 不被移除。
    private volatile string? _defaultAppKey;
    // NEW-MA-09 修复：保护 _defaultAppKey 的"读取+提升"复合操作与 RemoveApp 的"清空+提升"复合操作互斥。
    private readonly object _defaultAppLock = new();
    // NEW-MA-08 修复：保护 Lazy<> 重建逻辑，避免并发线程同时重建同一应用的 Lazy 上下文。
    private readonly object _lazyRebuildLock = new();
    // ARC-1：配置变更订阅句柄。FeishuAppManager 为容器单例，Dispose 由容器在关闭时调用。
    private IDisposable? _configReloadSubscription;
    // TMA-07 / P1-6 修复（D5 契约）：退休队列，宽限期后 Dispose 旧上下文。
    // 解决 Timer root 导致的内存泄漏（GC 不会回收被 Timer root 的对象图）。
    private FeishuAppContextRetirement? _retirement;
    // TMA-11 / P2-6 修复：运行时通过 AddApp 添加的应用键集合，热更新时据此决定是否保留。
    private readonly HashSet<string> _runtimeAddedAppKeys = new(StringComparer.Ordinal);
    // TMA-11 / P2-6 修复：保护 AddApp 的"检查→创建→注册→写入快照"原子操作。
    private readonly object _registryLock = new();

    // TMA2-09 / D13：热更新两阶段事务化的配置应用锁。
    // 锁序固定为 _configApplyLock → _lazyRebuildLock / _registryLock / _defaultAppLock，禁止反向获取。
    private readonly object _configApplyLock = new();

    /// <summary>
    /// TMR-P1-6（F6）：凭据变更清库失败的结构化事件 ID（供 metric/告警系统按 EventId 过滤接入）。
    /// </summary>
    internal static readonly EventId PurgeTokenStoreFailureEvent = new(5601, "PurgeTokenStoreFailed");

    /// <summary>
    /// TMA2-07 / P1-4：应用首次实例化事件。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="GetOrCreateContext"/> / <see cref="TryGetApp"/> 成功注册到基类字典后、
    /// <b>锁外</b>触发。<c>AddApp</c> 与配置热更新（<c>ApplyConfigurationChanges</c>，TMF-04：原
    /// <c>RebuildAppContext</c> 已删除）已由 <c>RegisterApp</c> 触发的 <c>ConfigurationChanged</c>
    /// 事件覆盖，不重复触发此事件。
    /// <c>FeishuTokenRegistrationService</c> 订阅此事件做增量注册。
    /// </remarks>
    internal event EventHandler<FeishuAppInstantiatedEventArgs>? AppInstantiated;

    /// <summary>
    /// TMA-07：供测试驱动退休队列扫描（避免依赖真实计时器）。
    /// </summary>
    internal FeishuAppContextRetirement? Retirement => _retirement;

    /// <summary>
    /// 初始化飞书应用管理器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="configs">应用配置列表</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public FeishuAppManager(
        IServiceProvider serviceProvider,
        IEnumerable<FeishuAppConfig> configs,
        ILogger<FeishuAppManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        // TMA-13：从根 SP 获取 IServiceScopeFactory，用于按需创建作用域解析 Scoped 依赖。
        _scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configs = (configs as IList<FeishuAppConfig> ?? configs.ToList()).ToArray();

        if (_configs.Length == 0)
            throw new InvalidOperationException("未配置任何飞书应用");

        // M-1 修复：预注册所有应用的 Lazy 上下文，但不立即创建。
        // MA-05 修复：检测重复 AppKey 并发出警告（后注册覆盖先注册），避免静默覆盖导致配置错误难以排查。
        var seenAppKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var config in _configs)
        {
            if (!seenAppKeys.Add(config.AppKey))
            {
                _logger.LogWarning(
                    "检测到重复的 AppKey '{AppKey}'，后注册的配置将覆盖先注册的配置。请检查应用配置以避免意外行为。",
                    config.AppKey);
            }
            var capturedConfig = config;
            _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(
                () => CreateAppContext(capturedConfig),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        // 校验：必须存在标记为 IsDefault=true 的应用，否则抛出异常（对应 MultiApp_NoDefaultApp_ShouldThrowOnRegistration 契约）。
        // 注意：不在此处预创建默认应用，避免强制要求所有 DI 依赖（如 IMemoryCache）在构造阶段就绪，
        // 默认应用将在首次访问 GetDefaultApp()/Default*TokenManager 时按需懒加载创建。
        var defaultConfig = _configs.FirstOrDefault(c => c.IsDefault);
        if (defaultConfig == null)
            throw new InvalidOperationException("未配置默认应用，请在至少一个 FeishuAppConfig 上设置 IsDefault = true。");

        // A-1 修复：不再通过反射设置基类私有字段 _defaultAppKey（反模式且基类字段无法加锁）。
        // 改为本类私有字段，GetDefaultApp() 直接读取，AddApp/RemoveApp 同步更新。
        _defaultAppKey = defaultConfig.AppKey;

        _logger.LogInformation("飞书应用管理器初始化完成，共配置 {Count} 个应用，默认应用: {AppKey}",
            _configs.Length, defaultConfig.AppKey);

        // TMA-07 / P1-6 修复：初始化退休队列。宽限期从 FeishuAppOptions.ContextRetireDelaySeconds 读取，默认 300s。
        var appOptions = _serviceProvider.GetService<IOptions<FeishuAppOptions>>()?.Value;
        var retireDelaySeconds = appOptions?.ContextRetireDelaySeconds ?? 300;
        _retirement = new FeishuAppContextRetirement(retireDelaySeconds, _logger);

        WarnResilienceConfigMismatch(_configs);

        // S-1 修复：启动期检测 IEncryptionProvider 注册状态。
        // 若未注册，使用 [Body(EnableEncrypt=true)] 的 API 将在首次请求时抛 InvalidOperationException（运行时失败而非编译期诊断）。
        // 此处发出警告使失败模式提前可见，便于用户在首次请求前补注册。
        if (_serviceProvider.GetService<IEncryptionProvider>() == null)
        {
            _logger.LogWarning(
                "未注册 IEncryptionProvider。若使用 [Body(EnableEncrypt=true)] 标记的 API，" +
                "首次请求将抛出 InvalidOperationException。请通过 AddMudHttpClient 配置 AesEncryptionOptions 或注册自定义 IEncryptionProvider。");
        }

        // ARC-1：订阅应用配置热更新。
        _configReloadSubscription = TrySubscribeConfigReload();
    }

    /// <summary>
    /// 按 <see cref="FeishuAppOptions.EnableConfigReload"/> 决定是否订阅配置变更通知。
    /// </summary>
    private IDisposable? TrySubscribeConfigReload()
    {
        var options = _serviceProvider.GetService<IOptions<FeishuAppOptions>>()?.Value;
        if (options != null && !options.EnableConfigReload)
        {
            _logger.LogInformation("飞书应用配置热更新已禁用（FeishuAppOptions.EnableConfigReload = false），配置变更需重启应用。");
            return null;
        }

        var monitor = _serviceProvider.GetService<IOptionsMonitor<List<FeishuAppConfig>>>();
        if (monitor == null)
        {
            // 代码配置模式（未经 IConfiguration 绑定）下无变更通知源，属预期情况。
            return null;
        }

        return monitor.OnChange(OnConfigurationChanged);
    }

    /// <summary>
    /// 配置变更回调：按 AppKey 计算差异并增量应用。
    /// </summary>
    /// <remarks>
    /// 与继承自 <see cref="DefaultAppManager{TAppContext}"/> 的 <c>ConfigurationChanged</c> 事件语义对齐：
    /// 新增触发 <c>Added</c>、更新触发 <c>Updated</c>、删除触发 <c>Removed</c>。
    /// 异常在此处被吞掉并记录，因为 <c>IOptionsMonitor.OnChange</c> 的回调抛异常会中断 IConfiguration 的变更通知链。
    /// <para>
    /// TMA2-09 / D13：热更新改为「Phase-P 锁外预清库 + 两阶段事务化」（TMF-02）：
    /// <list type="bullet">
    /// <item>Phase-P（锁外）：检测凭据变更并清库（IO），完成后才进入锁内，避免锁内同步阻塞。</item>
    /// <item>Phase-A（锁内预构造）：计算 diff，对新增/更新应用预构造上下文。任一失败则整体放弃。</item>
    /// <item>Phase-B（短临界区提交）：逐应用在 <c>_lazyRebuildLock</c> 内仅做引用交换，锁外 RegisterApp + 退休 + 快照提交。</item>
    /// </list>
    /// </para>
    /// </remarks>
    internal void OnConfigurationChanged(List<FeishuAppConfig>? newConfigs)
    {
        if (newConfigs == null || newConfigs.Count == 0)
        {
            _logger.LogWarning("收到空的飞书应用配置变更通知，已忽略（至少需要保留一个应用配置）。" );
            return;
        }

        try
        {
            var incoming = newConfigs
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.AppKey))
                .ToList();

            if (incoming.Count == 0)
            {
                _logger.LogWarning("飞书应用配置变更后没有任何合法 AppKey，已忽略。");
                return;
            }

            // TMA-14 / P2-13 修复（D4 契约）：先校验后应用。
            // ① AppKey 唯一性校验
            var duplicateKeys = incoming
                .GroupBy(c => c.AppKey, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateKeys.Count > 0)
            {
                _logger.LogError(
                    "配置热更新校验失败：检测到重复的 AppKey {AppKeys}，已忽略本次变更。",
                    string.Join(", ", duplicateKeys));
                return;
            }

            // ② IsDefault 唯一性校验
            var defaultApps = incoming.Where(c => c.IsDefault).ToList();
            if (defaultApps.Count > 1)
            {
                _logger.LogError(
                    "配置热更新校验失败：检测到 {Count} 个 IsDefault=true 的应用，已忽略本次变更。",
                    defaultApps.Count);
                return;
            }

            // ③ 逐条 Validate() 校验
            foreach (var config in incoming)
            {
                try
                {
                    config.Validate();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "配置热更新校验失败：应用 {AppKey} 的配置无效，已忽略本次变更。",
                        config.AppKey);
                    return;
                }
            }

            // TMR2-P1-3：配置已「正式接管」（本轮到 incoming 中声明）的 AppKey 撤销运行时标记。
            // 修复前唯一 Add 点为 AddApp（:1229），**没有任何路径**在应用被配置正式接管时撤销标记，
            // 于是「AddApp(C) → 配置声明 C → 配置删除 C」序列下 C 在
            // RemoveRuntimeAddedAppsOnReload=false（默认）时被静默永久保留，日志还称"运行时添加…已保留"（误导运维）。
            //
            // 位置必须在**节流判定之前**：标记语义是「运行时添加且未被配置源声明」，属配置源属性；
            // 若放在提交后（ApplyConfigurationChanges 内），一旦本轮 incoming 与快照逐字段一致（节流直接 return）
            // 撤销就永不发生，配置删除仍被永久忽略（本轮测试 RuntimeAddedApp_ShouldBeRemovedByConfig_... 已固化）。
            // 与校验后的"变更未应用"无关：即使后续 Phase-A 失败，"配置源已声明"这一事实仍然成立。
            // 锁内与 AddApp 的 _runtimeAddedAppKeys 写入互斥（锁序不变：_configApplyLock → _registryLock）。
            lock (_registryLock)
            {
                foreach (var config in incoming)
                {
                    _runtimeAddedAppKeys.Remove(config.AppKey);
                }
            }

            // 节流：与当前快照完全一致（无新增/删除/更新）时不做任何重建。
            if (IsSameAsCurrentSnapshot(incoming))
            {
                return;
            }

            // TMF-02 / D13：Phase-P——清库（IO）在获取 _configApplyLock 之前完成，
            // 维持「清库 → 重建」严格时序，同时消除锁内 sync-over-async 阻塞。
            // TMR-P1-6（F6）：返回凭据变更键集，供提交后二次清库（D10 闭环）使用。
            var credentialChangedKeys = PurgeCredentialChangedTokens(incoming);

            // TMA2-09 / D13：两阶段事务化——Phase-A 预构造，Phase-B 提交。
            // 锁序：_configApplyLock → _lazyRebuildLock / _registryLock / _defaultAppLock。
            lock (_configApplyLock)
            {
                ApplyConfigurationChanges(incoming, credentialChangedKeys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "飞书应用配置热更新失败，已保持原配置继续运行。请检查新的应用配置是否合法（AppKey/AppId/AppSecret 校验）。" );
        }
    }

    /// <summary>
    /// TMA2-09 / D13：两阶段事务化应用配置变更。
    /// </summary>
    /// <param name="incoming">新配置列表</param>
    /// <param name="credentialChangedKeys">
    /// TMR-P1-6（F6）：Phase-P 检出的凭据变更键集，提交后对其中的"已提交更新键"发起二次清库（D10 闭环）。
    /// </param>
    private void ApplyConfigurationChanges(List<FeishuAppConfig> incoming, IReadOnlyList<string> credentialChangedKeys)
    {
        var incomingKeys = new HashSet<string>(incoming.Select(c => c.AppKey), StringComparer.Ordinal);
        var currentKeys = Volatile.Read(ref _configs).Select(c => c.AppKey).ToList();

        // TMA-11 / P2-6 修复：读取 RemoveRuntimeAddedAppsOnReload 选项。
        var appOptions = _serviceProvider.GetService<IOptions<FeishuAppOptions>>()?.Value;
        var removeRuntimeApps = appOptions?.RemoveRuntimeAddedAppsOnReload ?? false;

        // === Phase-A：锁外预构造上下文 ===
        // 对新增/更新应用预构造上下文（CreateAppContext 为纯内存装配，无网络往返）。
        // 任一失败 → Dispose 已构造者、不动注册表、LogError("变更未应用") 后返回。
        var builtContexts = new List<(string appKey, FeishuAppContext context, FeishuAppConfig config, FeishuAppContext? oldContext)>();
        var phaseASuccess = true;

        foreach (var config in incoming)
        {
            // 只处理更新（已存在）的应用；新增应用走 AddApp（内部已有原子化装配）。
            if (HasApp(config.AppKey))
            {
                LogClientEndpointChanges(config);

                try
                {
                    // TMF-02：获取旧上下文引用以入退休队列；凭据变更清库已上移至 Phase-P（锁外）执行。
                    var oldContext = ResolveExistingContext(config.AppKey);

                    // 锁外预构造上下文。
                    var context = CreateAppContext(config);
                    builtContexts.Add((config.AppKey, context, config, oldContext));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex,
                        "配置热更新：应用 {AppKey} 上下文构造失败，变更未应用。",
                        config.AppKey);
                    phaseASuccess = false;
                    break;
                }
            }
        }

        if (!phaseASuccess)
        {
            // Phase-A 失败：Dispose 已构造的上下文，不动注册表。
            foreach (var (_, context, _, _) in builtContexts)
            {
                try { (context as IDisposable)?.Dispose(); }
                catch { /* 幂等。 */ }
            }
            _logger.LogError("配置热更新变更未应用（Phase-A 失败），注册表与快照保持原状。" );
            return;
        }

        // === Phase-B：短临界区提交 ===
        // 逐应用在 _lazyRebuildLock 内仅做引用交换，锁外 RegisterApp + 退休。
        foreach (var (appKey, context, config, oldContext) in builtContexts)
        {
            // Phase-B-1：锁内仅做引用交换。
            lock (_lazyRebuildLock)
            {
                _lazyContexts[appKey] = new Lazy<FeishuAppContext>(() => context, LazyThreadSafetyMode.ExecutionAndPublication);
            }

            // Phase-B-2：锁外 RegisterApp + 退休旧上下文。
            if (oldContext != null)
            {
                _retirement?.Enqueue(appKey, oldContext);
            }

            RegisterApp(appKey, context, config.IsDefault);

            if (config.IsDefault)
            {
                lock (_defaultAppLock)
                {
                    _defaultAppKey = config.AppKey;
                }
            }

            _logger.LogInformation("配置热更新：已重建应用 {AppKey}", appKey);
        }

        // 1) 删除已下线应用
        foreach (var removedKey in currentKeys.Where(k => !incomingKeys.Contains(k)).ToList())
        {
            // TMA-11：运行时添加的应用默认不被热更新移除（RemoveRuntimeAddedAppsOnReload=false）。
            if (!removeRuntimeApps && _runtimeAddedAppKeys.Contains(removedKey))
            {
                _logger.LogInformation(
                    "配置热更新：应用 {AppKey} 为运行时添加，RemoveRuntimeAddedAppsOnReload=false，已保留。",
                    removedKey);
                continue;
            }

            if (RemoveApp(removedKey))
            {
                _runtimeAddedAppKeys.Remove(removedKey);
                _logger.LogInformation("配置热更新：已移除应用 {AppKey}", removedKey);
            }
        }

        // 2) 新增应用（不在注册表中的）
        foreach (var config in incoming)
        {
            if (!builtContexts.Any(b => b.appKey == config.AppKey) && !HasApp(config.AppKey))
            {
                AddApp(config);
                _logger.LogInformation("配置热更新：已新增应用 {AppKey}", config.AppKey);
            }
        }

        // 3) 同步配置快照（TMA-05：原子交换不可变数组；TMR-P2-14：纳入 _registryLock 与 AddApp 的快照 RMW 互斥）
        lock (_registryLock)
        {
            Volatile.Write(ref _configs, incoming.ToArray());
        }

        // 4) TMR-P1-6（F6）：提交后二次清库（D10 闭环）。
        // 窗口期（Phase-P 清库完成 → 本应用提交 → 旧上下文 Enqueue 退休）内，旧上下文的组件后台刷新
        // Timer / 在途 GetOrRefreshTokenAsync 可能用旧凭据重新调 API 并将令牌写回 store；
        // 新上下文恢复出"旧凭据来源"的令牌后必然 401。此处对"凭据变更键 ∩ 本轮实际提交（更新）的键"
        // 发起提交后二次清库——此时旧上下文已停止接管新请求，回写源已被切断，闭环成立。
        // 与并发写入存在 store SCAN 固有残余窗口（与 Redis 语义一致）。
        // AddApp 新增键与被移除键无需二次清库（前者无旧凭据、后者旧上下文已 Enqueue 退休且键有 TTL）。
        // PurgeTokenStoreAsync 内部吞掉全部异常（含 OCE），fire-and-forget 安全。
        var postCommitPurgeKeys = credentialChangedKeys
            .Where(k => builtContexts.Any(b => string.Equals(b.appKey, k, StringComparison.Ordinal)))
            .ToList();
        if (postCommitPurgeKeys.Count > 0)
        {
            // TMR2-P1-5：与 Phase-P 同构——打门（计数与 Phase-P 叠加，覆盖"提交窗口内旧上下文回写"）
            // + 异步清库 + finally 撤门。两次计数叠加期间门持续有效。
            foreach (var appKey in postCommitPurgeKeys)
            {
                TokenStorePurgeGate.Mark(appKey);
            }

            _ = Task.Run(async () =>
            {
                foreach (var appKey in postCommitPurgeKeys)
                {
                    try
                    {
                        await PurgeTokenStoreAsync(appKey).ConfigureAwait(false);
                    }
                    finally
                    {
                        TokenStorePurgeGate.Release(appKey);
                    }
                }
            });
        }
    }

    /// <summary>
    /// TMF-02 / D13：Phase-P——在获取 <c>_configApplyLock</c> 之前完成凭据变更检测与清库。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 清库是 IO（Redis 为 SCAN + 逐键 DELETE），禁止在锁内执行，也不得在
    /// <c>IOptionsMonitor.OnChange</c> 回调线程上同步等待。
    /// </para>
    /// <para>
    /// <b>TMR2-P1-5 修复</b>：本方法只做「打门（<see cref="TokenStorePurgeGate"/>.Mark，
    /// 同步、微秒级）+ 异步清库」，<b>立即返回</b>。
    /// D10 的实质约束（"新上下文不得在清库完成前从 store 恢复旧凭据令牌"）
    /// 由恢复路径的门短路承担，因此「阻塞 OnChange 直到清库完成」并非必需。
    /// 修复前为 <c>Task.Run(...).Wait(10s)</c>：回调线程最长阻塞 10s、宿主存在
    /// <c>SynchronizationContext</c> 时有死锁面、线程池受限时占用双线程放大饥饿。
    /// </para>
    /// <para>
    /// 门带 30s 安全超时（fail-open）：清库异常/挂死时门自动失效并上报一次
    /// <c>PurgeGateState.TimedOut</c>，恢复路径重新启用，绝不因门而永久禁用 store 恢复。
    /// </para>
    /// <para>
    /// 并发语义：两个并发 <c>OnChange</c> 各自执行 Phase-P 可能对同一 appKey 清库两次——
    /// ClearAsync 幂等（删除不存在键为 no-op），且第二次清库发生在后者 Phase-A 之前，
    /// 时序安全性不弱于串行。清库内部吞掉全部异常（含 OCE，TMF-03），不会中断热更新。
    /// </para>
    /// </remarks>
    /// <param name="incoming">新配置列表</param>
    /// <returns>检出的凭据已变更的应用键集（供提交后二次清库使用）。</returns>
    internal List<string> PurgeCredentialChangedTokens(List<FeishuAppConfig> incoming)
    {
        var toPurge = new List<string>();
        foreach (var config in incoming)
        {
            var oldContext = ResolveExistingContext(config.AppKey);
            if (oldContext == null)
            {
                continue;
            }

            var oldConfig = oldContext.Config;
            if (!string.Equals(oldConfig?.AppId, config.AppId, StringComparison.Ordinal) ||
                !string.Equals(oldConfig?.AppSecret, config.AppSecret, StringComparison.Ordinal))
            {
                toPurge.Add(config.AppKey);
            }
        }

        if (toPurge.Count == 0)
        {
            return toPurge;
        }

        _logger.LogInformation(
            "检测到 {Count} 个应用的凭据已变更（AppId 或 AppSecret 变化），将异步清除其持久化令牌：{AppKeys}",
            toPurge.Count, string.Join(", ", toPurge));

        // TMR2-P1-5：打门（同步、微秒级）→ 异步清库；**不再**阻塞 IOptionsMonitor.OnChange 回调线程。
        // D10 的实质约束（"新上下文不得在清库完成前从 store 恢复旧凭据令牌"）由恢复路径的门短路承担：
        // FeishuAppTokenManagerBase.TryRestoreFromStoreAsync / UserTokenManager.TryRestoreFromUserTokenStoreAsync /
        // LoadRefreshCandidateAsync 见门即跳过 store。
        // 修复前在回调线程上 Task.Run(...).Wait(10s)：线程最长阻塞 10s、宿主有 SynchronizationContext 时
        // 有死锁面、线程池饥饿时占双线程——而"阻塞 OnChange"从来不是 D10 的实质要求。
        var purgeKeysPhaseP = toPurge.ToArray();
        foreach (var appKey in purgeKeysPhaseP)
        {
            TokenStorePurgeGate.Mark(appKey);
        }

        _ = Task.Run(async () =>
        {
            foreach (var appKey in purgeKeysPhaseP)
            {
                try
                {
                    await PurgeTokenStoreAsync(appKey).ConfigureAwait(false);
                }
                finally
                {
                    // 计数 -1；PurgeTokenStoreAsync 内部吞掉全部异常（含 OCE），finally 保证门必然回落。
                    TokenStorePurgeGate.Release(appKey);
                }
            }
        });

        return toPurge;
    }

    /// <summary>
    /// 解析已实例化的旧上下文：<c>_lazyContexts</c> 中已创建的 Lazy → 基类注册字典兜底。
    /// </summary>
    /// <remarks>
    /// TMF-02：Phase-P 凭据比对、Phase-A 退休入队与 <see cref="RemoveApp"/> 复用，
    /// 消除三份「TryGetValue + TryGetApp」副本。
    /// </remarks>
    private FeishuAppContext? ResolveExistingContext(string appKey)
    {
        if (_lazyContexts.TryGetValue(appKey, out var lazy) && lazy.IsValueCreated)
        {
            try
            {
                return lazy.Value;
            }
            catch (ObjectDisposedException ex)
            {
                // TMR-P2-7（F7）：退休期预期路径，Debug 级别即可（原空 catch 使故障不可诊断）。
                _logger.LogDebug(ex, "解析已实例化上下文时该上下文已释放（退休期预期）。AppKey: {AppKey}", appKey);
            }
            catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException))
            {
                // 旧 Lazy 初始化失败的上下文无需退休/比对，但保留可观测性。
                _logger.LogDebug(ex, "解析已实例化上下文失败（视为不存在）。AppKey: {AppKey}", appKey);
            }
        }

        if (base.TryGetApp(appKey, out var registered) && registered is FeishuAppContext ctx)
        {
            return ctx;
        }

        return null;
    }

    /// <summary>
    /// 判断配置快照是否与当前一致（用于节流，避免每次变更通知都重建上下文）。
    /// TMA-06 / P1-5 修复（D4 契约）：逐字段比较全部运行期字段，消除节流漏比。
    /// </summary>
    private bool IsSameAsCurrentSnapshot(List<FeishuAppConfig> incoming)
    {
        // TMA-05：原子读取不可变快照，无需锁。
        var current = Volatile.Read(ref _configs);

        if (current.Length != incoming.Count)
        {
            return false;
        }

        foreach (var config in incoming)
        {
            var existing = current.FirstOrDefault(c => string.Equals(c.AppKey, config.AppKey, StringComparison.Ordinal));
            if (existing == null)
            {
                return false;
            }

            if (!IsSameAs(existing, config))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// TMA-06 / P1-5 修复：逐字段比较两个 FeishuAppConfig 是否完全一致。
    /// AppSecret 用 string.Equals(..., StringComparison.Ordinal) 比较，不入日志。
    /// </summary>
    internal static bool IsSameAs(FeishuAppConfig a, FeishuAppConfig b)
    {
        var aRetry = a.HttpRetry ?? new Configuration.HttpRetryOptions();
        var bRetry = b.HttpRetry ?? new Configuration.HttpRetryOptions();
        var aCb = a.CircuitBreaker ?? new Configuration.CircuitBreakerOptions();
        var bCb = b.CircuitBreaker ?? new Configuration.CircuitBreakerOptions();

        return string.Equals(a.AppId, b.AppId, StringComparison.Ordinal)
            && string.Equals(a.AppSecret, b.AppSecret, StringComparison.Ordinal)
            && string.Equals(a.BaseUrl, b.BaseUrl, StringComparison.Ordinal)
            && a.AllowCustomBaseUrl == b.AllowCustomBaseUrl
            && a.TimeoutSeconds == b.TimeoutSeconds
            && aRetry.MaxAttempts == bRetry.MaxAttempts
            && aRetry.DelayMs == bRetry.DelayMs
            && aCb.Enabled == bCb.Enabled
            && aCb.FailureThreshold == bCb.FailureThreshold
            && aCb.SamplingDurationSeconds == bCb.SamplingDurationSeconds
            && aCb.BreakDurationSeconds == bCb.BreakDurationSeconds
            && aCb.MinimumThroughput == bCb.MinimumThroughput
            && a.TokenRefreshThreshold == b.TokenRefreshThreshold
            && a.IsDefault == b.IsDefault;
    }

    // TMF-04：RebuildAppContext 已删除——产品代码零调用方（唯一热更新路径为
    // ApplyConfigurationChanges，测试经 OnConfigurationChanged 间接触发同等逻辑），
    // 且其内部维护第二份 D10 比对+清库副本，与主路径双份漂移。详见
    // .docs/令牌与多应用管理-审查修复与完善方案.md §五。

    /// <summary>
    /// TMA2-05 / D10：清除指定 appKey 的持久化令牌。
    /// TMF-01：租户令牌经 <see cref="ITokenStore.ClearAsync"/>，用户令牌经
    /// <see cref="IFeishuUserTokenStorePurge.ClearAllUsersAsync"/> 能力探测（工厂每次 Create
    /// 新实例的 Memory 后端依赖跨实例共享记账才能清干净，用户存储此前被整段丢弃）。
    /// ClearAsync/ClearAllUsersAsync 失败不阻断重建（带 <see cref="PurgeTokenStoreFailureEvent"/>
    /// 记 Warning，宿主可据此建 metric；OCE 视为「未清」不记为失败——TMF-03 语义）。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    private async Task PurgeTokenStoreAsync(string appKey)
    {
        try
        {
            var tokenStoreFactory = _serviceProvider.GetService<IFeishuTokenStoreFactory>();
            if (tokenStoreFactory != null)
            {
                var (store, userStore) = tokenStoreFactory.Create(appKey);
                await store.ClearAsync().ConfigureAwait(false);
                if (userStore is IFeishuUserTokenStorePurge purgeable)
                {
                    await purgeable.ClearAllUsersAsync().ConfigureAwait(false);
                }
                _logger.LogInformation("已清除应用 {AppKey} 的持久化令牌。", appKey);
            }
        }
        catch (OperationCanceledException)
        {
            // TMF-03：取消不向上传播——清库被取消视为「未清」，不记为失败，热更新继续。
        }
        catch (Exception ex)
        {
            // TMR-P1-6（F6）：结构化可观测——EventId 5601 供 metric/告警系统过滤接入。
            // 新凭据 + 残留旧令牌可能导致 401，需检查存储后端连通性。
            _logger.LogWarning(PurgeTokenStoreFailureEvent, ex,
                "清除应用 {AppKey} 的持久化令牌失败（不阻断重建）。新凭据 + 残留旧令牌可能导致 401，" +
                "请检查存储后端连通性。", appKey);
        }
    }

    /// <summary>
    /// ARC-7：记录命名客户端端点（<c>BaseUrl</c> / <c>TimeoutSeconds</c>）的热更新，使
    /// 「配置是否真的作用到了 HTTP 客户端」在日志中可观测（修复前这两项变更完全不生效且无任何提示）。
    /// </summary>
    /// <param name="incoming">新配置。</param>
    private void LogClientEndpointChanges(FeishuAppConfig incoming)
    {
        // TMA-05：原子读取不可变快照，无需锁。
        var current = Volatile.Read(ref _configs);
        var previous = current.FirstOrDefault(c =>
            string.Equals(c.AppKey, incoming.AppKey, StringComparison.Ordinal));

        if (previous == null)
        {
            return;
        }

        var previousBaseUrl = string.IsNullOrWhiteSpace(previous.BaseUrl)
            ? Consts.DefaultFeishuBaseUrl
            : previous.BaseUrl;
        var incomingBaseUrl = string.IsNullOrWhiteSpace(incoming.BaseUrl)
            ? Consts.DefaultFeishuBaseUrl
            : incoming.BaseUrl;

        if (!string.Equals(previousBaseUrl, incomingBaseUrl, StringComparison.Ordinal) ||
            previous.TimeoutSeconds != incoming.TimeoutSeconds)
        {
            _logger.LogInformation(
                "配置热更新：应用 {AppKey} 的 HTTP 客户端端点已变更（BaseUrl: {PreviousBaseUrl} → {IncomingBaseUrl}，TimeoutSeconds: {PreviousTimeOut}s → {IncomingTimeOut}s），" +
                "重建后的客户端将使用新端点，无需重启进程。",
                incoming.AppKey, previousBaseUrl, incomingBaseUrl, previous.TimeoutSeconds, incoming.TimeoutSeconds);
        }
    }

    /// <summary>
    /// 释放资源：释放全部在册上下文，再强制释放全部待退休上下文（TMA-07），最后退订配置变更通知。
    /// </summary>
    /// <remarks>
    /// TMA2-11 / D13：Dispose 必须释放全部在册上下文（含 <c>_lazyContexts</c> 中已实例化者），
    /// 幂等。顺序：退订 → 释放在册 → 释放退休队列。
    /// </remarks>
    public void Dispose()
    {
        // TMA2-11：先退订，确保不再有新的事件触发。
        _configReloadSubscription?.Dispose();
        _configReloadSubscription = null;

        // TMA2-11 / D13：释放全部在册上下文（已实例化者），幂等。
        foreach (var app in base.GetAllApps())
        {
            try
            {
                (app as IDisposable)?.Dispose();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "释放应用上下文时发生异常。");
            }
        }

        // TMA-07 / P1-6 修复：释放全部待退休上下文。
        _retirement?.Dispose();
        _retirement = null;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 获取或创建指定应用的上下文（懒加载）
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <returns>应用上下文实例</returns>
    /// <exception cref="InvalidOperationException">当应用未配置或创建失败时抛出</exception>
    /// <exception cref="FeishuAppRemovedException">当应用已从配置快照移除（热更新/RemoveApp 竞态窗口）时抛出</exception>
    /// <remarks>
    /// <para>
    /// NEW-MA-08 修复：<see cref="Lazy{T}"/> 在 <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>
    /// 模式下会缓存工厂委托抛出的异常，导致首次初始化失败的应用在进程剩余生命周期内不可用。
    /// 此方法在检测到缓存异常时重建 <see cref="Lazy{T}"/> 实例，允许后续调用重试初始化。
    /// </para>
    /// <para>
    /// TMR-P1-3（F3）：快照移除竞态改造——原实现四处 <c>First()</c> 在"热更新移除窗口内并发首访"
    /// 时抛 <see cref="InvalidOperationException"/> 并落入瞬时白名单，触发"反复重建 Lazy、反复失败"的
    /// 资源放大循环，且已创建的上下文（Scope + 3 管理器 + 恢复客户端）既未注册也未 Dispose → 泄漏。
    /// 现改为：快照查无 → <see cref="FeishuAppRemovedException"/>（确定性终态，排除出白名单）+
    /// <c>finally</c> 中对未注册成功的上下文经退休队列回收（Timer root 对象图，GC 不代劳，D5）。
    /// </para>
    /// </remarks>
    private FeishuAppContext GetOrCreateContext(string appKey)
    {
        if (_lazyContexts.TryGetValue(appKey, out var lazy))
        {
            FeishuAppContext? createdContext = null;
            try
            {
                var context = lazy.Value;
                createdContext = context;
                // 注册到基类字典中（如果尚未注册）
                // 注意：必须使用 base.HasApp 检查"是否已注册到基类字典"，
                // 而非使用 HasApp（后者会同时检查 _lazyContexts，导致永远跳过 RegisterApp）。
                var wasRegistered = base.HasApp(appKey);
                if (!wasRegistered)
                {
                    var config = FindConfigInSnapshot(appKey)
                        ?? throw new FeishuAppRemovedException(appKey);
                    RegisterApp(appKey, context, config.IsDefault);
                }
                // TMA2-07 / P1-4：首次实例化后在锁外触发事件。
                if (!wasRegistered)
                {
                    OnAppInstantiated(appKey, context);
                }
                return context;
            }
            // TMA2-12 / D6：异常过滤从"除取消外全部"改为"可重试异常白名单"。
            // 可重试异常（InvalidOperationException/HttpRequestException/TimeoutException 等）→ 重建 Lazy 允许重试。
            // 非瞬时异常（OutOfMemoryException/AccessViolationException/TypeLoadException 等）→ 直接上抛。
            catch (Exception ex) when (IsTransientInitFailure(ex))
            {
                // NEW-MA-08 修复：Lazy<ExecutionAndPublication> 缓存异常后，后续 .Value 访问会重新抛出同一异常。
                // 此处重建 Lazy<> 以允许下次调用重试初始化（如 Redis 短暂故障恢复后可自愈）。
                // 使用双检锁避免并发线程同时重建：仅当字典中仍是原 Lazy 实例时才重建。
                // TMR-P1-3：快照查无该应用时不再重建（确定性终态），抛 FeishuAppRemovedException。
                lock (_lazyRebuildLock)
                {
                    if (_lazyContexts.TryGetValue(appKey, out var current) && ReferenceEquals(current, lazy))
                    {
                        var config = FindConfigInSnapshot(appKey)
                            ?? throw new FeishuAppRemovedException(appKey);
                        var capturedConfig = config;
                        _lazyContexts[appKey] = new Lazy<FeishuAppContext>(
                            () => CreateAppContext(capturedConfig),
                            LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                }
                throw new InvalidOperationException(
                    $"应用 '{appKey}' 初始化失败: {ex.Message}", ex);
            }
            finally
            {
                // TMR-P1-3：注册失败路径上已创建的上下文必须显式回收（Timer roots 对象图，GC 不代劳，D5）。
                // 经退休队列回收（而非直接 Dispose）：保留宽限期语义，避免打断刚完成装配的管理器的在途初始化。
                if (createdContext != null && !base.HasApp(appKey))
                {
                    try { _retirement?.Enqueue(appKey, createdContext); }
                    catch (ObjectDisposedException)
                    {
                        // 队列已释放（容器关闭中），此处仅放弃回收。
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"未找到应用标识为 '{appKey}' 的应用上下文。请先调用 RegisterApp 注册应用。");
    }

    /// <summary>
    /// TMR-P1-3（F3）：在配置快照中查找指定应用的配置；查无返回 null（应用已移除或尚未入快照）。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <returns>应用配置；快照中不存在时返回 null。</returns>
    private FeishuAppConfig? FindConfigInSnapshot(string appKey)
        => Volatile.Read(ref _configs).FirstOrDefault(c =>
            string.Equals(c.AppKey, appKey, StringComparison.Ordinal));

    /// <inheritdoc />
    public override IFeishuAppContext GetApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("应用标识不能为空", nameof(appKey));

        // 先检查已注册的应用（包括懒加载已创建的）
        if (TryGetApp(appKey, out var context) && context != null)
            return context;

        // M-1 修复：未注册时尝试懒加载创建
        return GetOrCreateContext(appKey);
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：基类 <see cref="DefaultAppManager{TAppContext}.HasApp"/> 仅检查已实例化的应用字典，
    /// 不会感知 <c>_lazyContexts</c> 中预注册但尚未实例化的应用。重写后同时检查两个字典，
    /// 确保"已配置但未访问"的应用也能被正确识别为"已注册"。
    /// </remarks>
    public override bool HasApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return false;

        // 已实例化的应用：基类字典已记录
        if (base.HasApp(appKey))
            return true;

        // 已配置但尚未实例化的应用：Lazy 字典已记录
        return _lazyContexts.ContainsKey(appKey);
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：基类 <see cref="DefaultAppManager{TAppContext}.TryGetApp"/> 仅查询已实例化的应用字典，
    /// 对已配置但未实例化的应用返回 false。重写后：
    /// <list type="bullet">
    /// <item>已实例化：直接返回（与基类一致）。</item>
    /// <item>已配置但未实例化：触发 Lazy 创建并注册到基类字典，返回 true。</item>
    /// <item>创建过程抛异常：捕获并返回 false（保持 Try* 语义不抛异常）。</item>
    /// <item>未配置：返回 false（与基类一致）。</item>
    /// </list>
    /// </remarks>
    public override bool TryGetApp(string appKey, out IFeishuAppContext? appContext)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            appContext = default;
            return false;
        }

        // 已实例化的应用：基类字典已记录
        if (base.TryGetApp(appKey, out appContext) && appContext != null)
            return true;

        // 已配置但尚未实例化的应用：触发 Lazy 创建
        if (_lazyContexts.TryGetValue(appKey, out var lazy))
        {
            FeishuAppContext? createdContext = null;
            try
            {
                var context = lazy.Value;
                createdContext = context;
                // 注册到基类字典以便后续快速查找
                var wasRegistered = base.HasApp(appKey);
                if (!wasRegistered)
                {
                    // TMR-P1-3（F3）：快照查无该应用（热更新移除窗口）——确定性终态，
                    // 不注册、不重建、不触发事件，直接返回 false（保持 Try* 语义不抛异常）。
                    var config = FindConfigInSnapshot(appKey);
                    if (config == null)
                    {
                        appContext = default;
                        return false;
                    }
                    RegisterApp(appKey, context, config.IsDefault);
                }
                // TMA2-07 / P1-4：首次实例化后在锁外触发事件。
                if (!wasRegistered)
                {
                    OnAppInstantiated(appKey, context);
                }
                appContext = context;
                return true;
            }
            // TMA2-12 / D6：与 GetOrCreateContext 一致，仅捕获可重试异常。非瞬时异常直接上抛。
            catch (Exception ex) when (IsTransientInitFailure(ex))
            {
                // NEW-MA-08 修复：与 GetOrCreateContext 一致，检测到 Lazy 缓存异常时重建 Lazy<> 以允许下次重试。
                // 保持 Try* 语义：重建后仍返回 false，调用方可下次重试。
                // TMR-P1-3：快照查无该应用时跳过重建（确定性终态，不放大资源）。
                lock (_lazyRebuildLock)
                {
                    if (_lazyContexts.TryGetValue(appKey, out var current) && ReferenceEquals(current, lazy))
                    {
                        var config = FindConfigInSnapshot(appKey);
                        if (config != null)
                        {
                            var capturedConfig = config;
                            _lazyContexts[appKey] = new Lazy<FeishuAppContext>(
                                () => CreateAppContext(capturedConfig),
                                LazyThreadSafetyMode.ExecutionAndPublication);
                        }
                    }
                }
                appContext = default;
                return false;
            }
            catch (OperationCanceledException)
            {
                // TMA2-12：取消异常不重建 Lazy，保持 Try* 语义返回 false
                appContext = default;
                return false;
            }
            finally
            {
                // TMR-P1-3（F3）：与 GetOrCreateContext 同构——注册失败路径上已创建的上下文
                // 经退休队列回收，避免孤儿上下文（Scope + 管理器 + Timer）泄漏。
                if (createdContext != null && !base.HasApp(appKey))
                {
                    try { _retirement?.Enqueue(appKey, createdContext); }
                    catch (ObjectDisposedException)
                    {
                        // 队列已释放（容器关闭中），此处仅放弃回收。
                    }
                }
            }
        }

        // 未配置的应用
        appContext = default;
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMA-08 / P1-7 修复（D6 契约）：GetAllApps 语义回归"已实例化应用视图"，不再隐式触发装配。
    /// 需要启动期预热全部应用请显式设置 <c>FeishuAppOptions.WarmUpAllAppsOnStartup = true</c>。
    /// </remarks>
    public override IEnumerable<IFeishuAppContext> GetAllApps()
    {
        return base.GetAllApps();
    }

    /// <summary>
    /// TMA-08：获取所有已配置（但未必实例化）的应用键。不触发懒加载。
    /// </summary>
    public IReadOnlyCollection<string> ConfiguredAppKeys => _lazyContexts.Keys.ToArray();

    /// <summary>
    /// TMA-08：获取所有已实例化的应用上下文。不触发懒加载。
    /// </summary>
    internal IEnumerable<IFeishuAppContext> InstantiatedApps => base.GetAllApps();

    /// <summary>
    /// TMA2-07 / P1-4：触发 AppInstantiated 事件（在锁外调用）。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <param name="context">已实例化的应用上下文</param>
    private void OnAppInstantiated(string appKey, IFeishuAppContext context)
    {
        try
        {
            AppInstantiated?.Invoke(this, new FeishuAppInstantiatedEventArgs(appKey, context));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 事件订阅者异常不应影响主流程。
            _logger.LogWarning(ex,
                "AppInstantiated 事件订阅者在处理应用 {AppKey} 时抛出异常。", appKey);
        }
    }

    /// <summary>
    /// TMA2-12 / D6：判断异常是否为可重试的瞬时初始化失败。
    /// </summary>
    /// <param name="ex">异常</param>
    /// <returns>true 表示可重试（重建 Lazy）；false 表示非瞬时异常（应直接上抛）</returns>
    private static bool IsTransientInitFailure(Exception ex)
    {
        // 取消异常不重建 Lazy。
        if (ex is OperationCanceledException)
            return false;

        // TMR-P1-3（F3）：快照移除是确定性终态，不是瞬时故障——禁止重建 Lazy、禁止重试循环。
        // 注意：FeishuAppRemovedException 继承 InvalidOperationException 以兼容外部 catch 约定，
        // 必须先于白名单显式排除，否则会落入"可重试"分支触发"反复重建 Lazy、反复失败"的资源放大循环。
        if (ex is FeishuAppRemovedException)
            return false;

        // 可重试异常白名单：DI 解析失败、网络/存储瞬时故障。
        if (ex is InvalidOperationException or
            HttpRequestException or
            TimeoutException or
            IOException or
            System.Net.Sockets.SocketException)
            return true;

        // 包含可重试内部异常的复合异常（如 TargetInvocationException / TypeInitializerException）。
        if (ex.InnerException != null && IsTransientInitFailure(ex.InnerException))
            return true;

        // 其余异常（OutOfMemoryException、AccessViolationException、TypeLoadException 等）不可重试。
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：由于采用懒加载，应用可能仅存在于 <c>_lazyContexts</c> 而尚未注册到基类 <c>_apps</c> 字典。
    /// 此时 <see cref="DefaultAppManager{TAppContext}.RemoveApp"/> 会返回 false，与"应用已知即应可移除"的语义不符。
    /// 重写后：只要应用在任一字典中存在，即返回 true；并清理两个字典中的记录。
    /// </remarks>
    public override bool RemoveApp(string appKey)
    {
        // TMA-07 / P1-6 修复：在移除前获取旧上下文引用以入退休队列
        //（TMF-02：ResolveExistingContext 方法化，消除第三份副本）。
        var oldContext = ResolveExistingContext(appKey);

        var wasInLazy = _lazyContexts.TryRemove(appKey, out _);
        var wasInBase = base.RemoveApp(appKey);

        // TMA-03 / P2-11 修复（D3 契约）：移除默认应用后确定性提升。
        // 按 _configs 快照顺序 → 兜底按 AppKey 序数排序（Ordinal），并 LogWarning 声明提升结果。
        if ((wasInLazy || wasInBase) && _defaultAppKey == appKey)
        {
            lock (_defaultAppLock)
            {
                if (_defaultAppKey == appKey)
                {
                    _defaultAppKey = null;

                    // TMA-03：确定性提升——按配置快照顺序取第一个剩余应用
                    var snapshot = Volatile.Read(ref _configs);
                    string? promotedKey = null;

                    foreach (var cfg in snapshot)
                    {
                        if (cfg.AppKey != appKey && HasApp(cfg.AppKey))
                        {
                            promotedKey = cfg.AppKey;
                            break;
                        }
                    }

                    // 快照中未找到，兜底按 _lazyContexts 的 AppKey 序数排序
                    if (promotedKey == null)
                    {
                        promotedKey = _lazyContexts.Keys
                            .Where(k => k != appKey)
                            .OrderBy(k => k, StringComparer.Ordinal)
                            .FirstOrDefault();
                    }

                    // 懒加载字典也空了，检查基类字典
                    if (promotedKey == null)
                    {
                        var baseFirst = base.GetAllApps()
                            .Where(a => a.Config.AppKey != appKey)
                            .OrderBy(a => a.Config.AppKey, StringComparer.Ordinal)
                            .FirstOrDefault();
                        promotedKey = baseFirst?.Config.AppKey;
                    }

                    if (promotedKey != null)
                    {
                        _defaultAppKey = promotedKey;
                        _logger.LogWarning(
                            "默认应用 '{RemovedAppKey}' 已被移除，自动提升 '{PromotedAppKey}' 为新默认应用。",
                            appKey, promotedKey);
                    }
                }
            }
        }

        // TMA-07：旧上下文进入退休队列，宽限期后 Dispose（停止其 Timer）。
        if (oldContext != null && (wasInLazy || wasInBase))
        {
            _retirement?.Enqueue(appKey, oldContext);
        }

        // TMR-P3-16b（F16b）：应用下线后提前回收其持久化令牌（fire-and-forget）。
        // 现状 TTL（refresh ≤30d）已保证有界，此处收益为提前回收存储空间与
        // 避免残留键被误诊断；PurgeTokenStoreAsync 内部吞掉全部异常（含 OCE），安全。
        if (wasInLazy || wasInBase)
        {
            _ = Task.Run(async () =>
            {
                await PurgeTokenStoreAsync(appKey).ConfigureAwait(false);
            });
        }

        return wasInLazy || wasInBase;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A-1 修复：不再依赖基类反射设置的 _defaultAppKey，直接读取本类私有字段。
    /// MA-01 修复：原实现使用 <c>public new</c> 隐藏基类虚方法，通过基类或接口引用调用时
    /// 会执行基类版本（_defaultAppKey 未设置）抛异常，违背里氏替换原则。
    /// 现改为 <c>public override</c> 保持多态一致性。
    /// TMA-12 / P2-9 修复（D5 契约）：GetDefaultApp 改为"快照读 _defaultAppKey → 无锁 GetApp"，
    /// 移除 _defaultAppLock 以避免锁内调用 GetApp（GetApp 可能触发懒加载装配），配合 TMA-03/05 的原子写。
    /// </remarks>
    public override IFeishuAppContext GetDefaultApp()
    {
        // TMA-12：快照读 _defaultAppKey（volatile 保证可见性），无锁调用 GetApp。
        // TOCTOU 由"键要么存在要么抛明确异常"覆盖：若 RemoveApp 在读取后修改 _defaultAppKey，
        // GetApp 会抛 InvalidOperationException（应用已移除），与基类语义一致。
        var defaultKey = _defaultAppKey;
        if (string.IsNullOrEmpty(defaultKey))
            throw new InvalidOperationException("未设置默认应用。请在注册应用时设置 isDefault = true。");

        return GetApp(defaultKey!);
    }

    /// <summary>
    /// TMA-03 / P1-2 修复（D3 契约）：覆写 DefaultAppKey 统一到本类字段。
    /// 基类的 DefaultAppKey 属性非 virtual，使用 new 隐藏确保通过本类引用访问时走本类字段。
    /// </summary>
    public new string? DefaultAppKey => _defaultAppKey;

    /// <summary>
    /// TMA-03 / P1-2 修复（D3 契约）：覆写 SetDefaultApp 统一到本类字段 + _defaultAppLock。
    /// 对未知应用抛 InvalidOperationException（与基类一致）。
    /// </summary>
    /// <param name="appKey">要设为默认的应用键。</param>
    /// <exception cref="InvalidOperationException">当应用未注册时抛出。</exception>
    public override void SetDefaultApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey) || !HasApp(appKey))
            throw new InvalidOperationException($"无法设置默认应用：应用标识 '{appKey}' 未注册。");

        lock (_defaultAppLock)
        {
            _defaultAppKey = appKey;
        }
    }

    /// <summary>
    /// TMA-03 / P1-2 修复（D3 契约）：覆写 TrySetDefaultApp 统一到本类字段 + _defaultAppLock。
    /// 以 HasApp（含懒加载已配置应用）判定，不再受"是否已实例化"限制。
    /// 基类的 TrySetDefaultApp 方法非 virtual，使用 new 隐藏确保通过本类引用访问时走本类逻辑。
    /// </summary>
    /// <param name="appKey">要设为默认的应用键。</param>
    /// <returns>成功返回 true；应用不存在或 appKey 为空返回 false。</returns>
    public new bool TrySetDefaultApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey) || !HasApp(appKey))
            return false;

        lock (_defaultAppLock)
        {
            _defaultAppKey = appKey;
        }
        return true;
    }

    /// <summary>
    /// 租户令牌管理器
    /// </summary>
    public ITenantTokenManager DefaultTenantTokenManager => GetDefaultApp().TenantTokenManager;

    /// <summary>
    /// 应用令牌管理器
    /// </summary>
    public IAppTokenManager DefaultAppTokenManager => GetDefaultApp().AppTokenManager;

    /// <summary>
    /// 用户令牌管理器
    /// </summary>
    public IFeishuUserTokenManager DefaultUserTokenManager => GetDefaultApp().UserTokenManager;

    /// <summary>
    /// 默认应用配置
    /// </summary>
    public FeishuAppConfig DefaultConfig => GetDefaultApp().Config;

    /// <summary>
    /// 运行时添加应用
    /// </summary>
    /// <remarks>
    /// TMA-11 / P2-6 修复：AddApp 原子化与快照同步。
    /// <list type="bullet">
    /// <item>在 <c>_registryLock</c> 内完成"检查 → 创建 → 注册 → 写入快照"，消除并发竞态。</item>
    /// <item>并发竞态败者（同一 AppKey 已被另一线程注册）必须 <c>Dispose()</c> 新建上下文，避免 Timer 泄漏。</item>
    /// <item>运行时应用标记 <c>_runtimeAddedAppKeys</c>，热更新时默认不移除（<c>RemoveRuntimeAddedAppsOnReload=false</c>）。</item>
    /// <item>写入 <c>_configs</c> 快照（Volatile.Write），使热更新差异比对能感知运行时新增应用。</item>
    /// </list>
    /// </remarks>
    public IFeishuAppContext AddApp(FeishuAppConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        config.Validate();

        FeishuAppContext? contextToDispose = null;
        FeishuAppContext resultContext;

        // TMA-11：在 _registryLock 内完成"检查 → 创建 → 注册 → 写入快照"原子操作。
        lock (_registryLock)
        {
            if (HasApp(config.AppKey))
                throw new InvalidOperationException($"应用 {config.AppKey} 已存在");

            // 在锁内创建上下文（CreateAppContext 为纯内存装配，无网络往返）。
            // 若创建失败，异常直接传播，无需 Dispose（尚未创建成功）。
            resultContext = CreateAppContext(config);

            try
            {
                RegisterApp(config.AppKey, resultContext, config.IsDefault);
                // 同步注册到懒加载字典，使 GetAllApps 能正确枚举
                _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(() => resultContext);

                // TMA-11：标记为运行时添加，热更新时据此决定是否保留。
                _runtimeAddedAppKeys.Add(config.AppKey);

                // TMA-11：写入 _configs 快照（原子交换不可变数组），
                // 使热更新差异比对能感知运行时新增应用。
                var currentSnapshot = Volatile.Read(ref _configs);
                var newSnapshot = new FeishuAppConfig[currentSnapshot.Length + 1];
                Array.Copy(currentSnapshot, newSnapshot, currentSnapshot.Length);
                newSnapshot[currentSnapshot.Length] = config;
                Volatile.Write(ref _configs, newSnapshot);

                // TMA2-08 / D13：默认应用键写入必须持有 _defaultAppLock。
                // TMR-P1-5（F5）：AddApp 是第三条默认应用入口（构造期、热更新 OnConfigurationChanged
                // ②均有 IsDefault 唯一性校验），此前缺失导致"两个 IsDefault=true"且默认应用随运维操作漂移
                // （下次原默认应用热更新会切回）。语义：后到者胜出（与 AddApp 的既有覆盖语义一致），
                // 但必须告警使漂移可观测。不做硬失败——硬失败会破坏"运行时切换默认应用"的合法用法。
                if (config.IsDefault)
                {
                    var previousDefault = Volatile.Read(ref _configs)
                        .FirstOrDefault(c => c.IsDefault &&
                            !string.Equals(c.AppKey, config.AppKey, StringComparison.Ordinal));
                    if (previousDefault != null)
                    {
                        _logger.LogWarning(
                            "AddApp({AppKey}) 标记 IsDefault=true，将覆盖原默认应用 {PreviousDefaultKey}。" +
                            "原应用配置中的 IsDefault 标记未变更，下次其配置热更新时默认应用将被切回，请同步调整配置源。",
                            config.AppKey, previousDefault.AppKey);
                    }

                    lock (_defaultAppLock)
                    {
                        _defaultAppKey = config.AppKey;
                    }
                }
            }
            catch
            {
                // 注册或快照写入失败：标记新建上下文为待 Dispose，避免 Timer 泄漏。
                contextToDispose = resultContext;
                throw;
            }
        }

        // 锁外 Dispose 失败时新建的上下文（避免在锁内执行可能耗时的 Dispose）。
        if (contextToDispose != null)
        {
            _retirement?.Enqueue(config.AppKey, contextToDispose);
        }

        return resultContext;
    }

    /// <summary>
    /// 根据应用键获取飞书API实例
    /// </summary>
    /// <remarks>
    /// 重写基类方法，从 DI 容器获取已注册的服务并调用 UseApp 切换应用上下文。
    /// A-2 修复说明：本类使用 DI 解析模式（GetService + UseApp），而非基类 <see cref="DefaultAppManager{TAppContext}.RegisterSwitcherFactory"/>
    /// 的工厂委托模式。两种模式共存但 <see cref="GetWebApi{TContextSwitcher}"/> 仅走 DI 路径，
    /// 调用 <c>RegisterSwitcherFactory</c> 注册的工厂不会被本方法使用。
    /// 统一为 DI 模式可降低理解成本，基类工厂委托路径在中期标记为 [Obsolete]。
    /// <para>
    /// NEW-MA-11 说明：<c>UseApp</c> 的生成实现通过 <c>_appContextHolder.Current = context</c>
    /// 切换上下文，而 <c>IAppContextHolder</c> 的默认实现 <c>AsyncLocalAppContextSwitcher</c> 基于
    /// <see cref="AsyncLocal{T}"/>，因此即使 T 注册为 Singleton，并发调用 <c>UseApp</c>
    /// 也不会互相覆盖（每个异步流持有独立的 Current 值）。
    /// 但仍建议将 T 注册为 Scoped（通过 <c>AddFeishuApi&lt;T&gt;</c> 或 <c>AddFeishuApp&lt;T&gt;</c>），
    /// 以避免 Singleton 实例长期持有已释放的 <see cref="IFeishuAppContext"/> 引用。
    /// </para>
    /// </remarks>
    // TMA-17 / P2-4 修复：泛型参数名遮蔽 IFeishuAppContext 接口（基类签名决定，无法改名），
    // 异常消息已使用 typeof(T).FullName 确保类型名称正确显示。
    public override IFeishuAppContext GetWebApi<IFeishuAppContext>(string appKey)
    {
        var service = _serviceProvider.GetService<IFeishuAppContext>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(IFeishuAppContext).FullName}");
        service.UseApp(appKey);
        return service;
    }

    /// <summary>
    /// 获取默认应用的飞书API实例
    /// </summary>
    /// <remarks>
    /// 重写基类方法，从 DI 容器获取已注册的服务并调用 UseDefaultApp 切换应用上下文。
    /// </remarks>
    // TMA-17 / P2-4 修复：与 GetWebApi 同理，泛型参数名由基类决定。
    public override IFeishuAppContext GetDefaultWebApi<IFeishuAppContext>()
    {
        var service = _serviceProvider.GetService<IFeishuAppContext>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(IFeishuAppContext).FullName}");
        service.UseDefaultApp();
        return service;
    }


    bool IAppManager<IFeishuAppContext>.TryGetApp(string appKey, out IFeishuAppContext? appContext)
    {
        var result = TryGetApp(appKey, out var ctx);
        appContext = ctx;
        return result;
    }

    void IAppManager<IFeishuAppContext>.RegisterApp(string appKey, IFeishuAppContext appContext, bool isDefault)
    {
        if (appContext is not FeishuAppContext ctx)
            throw new ArgumentException("应用上下文必须是 FeishuAppContext 类型", nameof(appContext));
        RegisterApp(appKey, ctx, isDefault);
    }

    async Task IAppManager<IFeishuAppContext>.RegisterAppAsync(string appKey, IFeishuAppContext appContext, bool isDefault, CancellationToken cancellationToken)
    {
        if (appContext is not FeishuAppContext ctx)
            throw new ArgumentException("应用上下文必须是 FeishuAppContext 类型", nameof(appContext));
        await RegisterAppAsync(appKey, ctx, isDefault, cancellationToken);
    }

    void IAppManager<IFeishuAppContext>.UpdateApp(string appKey, IFeishuAppContext appContext)
    {
        if (appContext is not FeishuAppContext ctx)
            throw new ArgumentException("应用上下文必须是 FeishuAppContext 类型", nameof(appContext));
        UpdateApp(appKey, ctx);
    }

    // TMA-17 / P2-4 修复：覆盖隐藏基类方法，直接抛 NotSupportedException，
    // 杜绝"经具体类调用无警告却静默无效"的问题。
    /// <summary>
    /// 覆盖基类的工厂委托注册方法：FeishuAppManager 采用 DI 解析模式，本方法始终抛出 <see cref="NotSupportedException"/>。
    /// </summary>
    /// <typeparam name="TContextSwitcher">上下文切换器类型</typeparam>
    /// <param name="factory">工厂委托（不会被调用）</param>
    /// <exception cref="NotSupportedException">始终抛出，请改用 IServiceProvider 直接解析服务</exception>
    [Obsolete("FeishuAppManager 使用 DI 解析模式，RegisterSwitcherFactory 注册的工厂不会被 GetWebApi 调用。请改用 IServiceProvider 直接解析服务。", true)]
    public new void RegisterSwitcherFactory<TContextSwitcher>(Func<IFeishuAppContext, TContextSwitcher> factory)
        => throw new NotSupportedException(
            $"FeishuAppManager 使用 DI 解析模式，不支持 RegisterSwitcherFactory。请改用 IServiceProvider 直接解析 {typeof(TContextSwitcher).FullName} 服务。");

    // A-2 修复说明：此显式接口实现委托给基类 RegisterSwitcherFactory，但 FeishuAppManager.GetWebApi
    // 重写为 DI 解析模式，不使用工厂委托。保留此方法仅为接口兼容性，注册的工厂不会被 GetWebApi 调用。
    // MA-04 修复：通过 [Obsolete] 明确告知调用方此路径不推荐，统一使用 DI 解析模式。
    [Obsolete("FeishuAppManager 使用 DI 解析模式，RegisterSwitcherFactory 注册的工厂不会被 GetWebApi 调用。请改用 IServiceProvider 直接解析服务。")]
    void IAppManager<IFeishuAppContext>.RegisterSwitcherFactory<TContextSwitcher>(Func<IFeishuAppContext, TContextSwitcher> factory)
    {
        RegisterSwitcherFactory<TContextSwitcher>(ctx => factory(ctx));
    }

    /// <summary>
    /// 创建应用上下文（可由子类重写以支持自定义 <see cref="IFeishuAppContext"/>）
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <returns>飞书应用上下文实例</returns>
    protected virtual FeishuAppContext CreateAppContext(FeishuAppConfig config)
    {
        // TMA2-11 / D13：装配失败时释放 scope，避免 IServiceScope 泄漏。
        // 成功后所有权转移给 FeishuAppContext。
        var scope = _scopeFactory?.CreateScope();
        var scopedSp = scope?.ServiceProvider ?? _serviceProvider;

        try
        {
        // ARC-2 Step 1：客户端装配统一委托给 IFeishuHttpClientFactory，
        // 消除与 AddMudHttpClient 注册路径的配置双源（此前手工 new 会导致 10 个 EnhancedHttpClientOptions
        // 字段静默取默认值，且完全忽略 IOptions<EnhancedHttpClientOptions> 基线）。
        // DED-1：同时删除此前声明后未使用的 jsonSerializerOptions / basicHttpClient 两个死变量。
        // 使用 GetService 而非 GetRequiredService：IFeishuHttpClientFactory 是装配收敛的可选服务，
        // 直接手工构造 FeishuAppManager（未经 AddFeishuApp）的场景下回退到默认实现，
        // 避免把新增依赖变成破坏性变更。
        // TMA-13：使用 scopedSp 解析服务，确保 Scoped 依赖在作用域内解析。
        // Singleton 注册的服务从根/作用域 SP 解析结果一致；Scoped 服务仅在作用域内可安全解析。
        var httpClientFactory = scopedSp.GetService<IFeishuHttpClientFactory>()
            ?? new FeishuHttpClientFactory(scopedSp);

        // === 步骤 2：创建 AuthenticationApi（使用 per-app HttpClient） ===
        // TMA-09 / P1-8 修复（D7 契约）：认证/取令牌请求改为使用本应用的命名 HttpClient。
        // 通过 IFeishuAuthenticationFactory.Create(appKey) 获取 per-app 认证 API 实例。
        // 若 EnablePerAppAuthenticationClient=false 或工厂未注册，降级为 DI 单例（使用默认应用端点）。
        IFeishuAuthentication authenticationApi;
        var appOptions = scopedSp.GetService<IOptions<FeishuAppOptions>>()?.Value;
        var authFactory = scopedSp.GetService<IFeishuAuthenticationFactory>();
        if (appOptions?.EnablePerAppAuthenticationClient != false && authFactory != null)
        {
            authenticationApi = authFactory.Create(config.AppKey);
        }
        else
        {
            // 降级路径：使用 DI 单例（默认应用端点）
            if (appOptions?.EnablePerAppAuthenticationClient == false)
            {
                _logger.LogWarning(
                    "EnablePerAppAuthenticationClient=false，应用 {AppKey} 的认证请求将使用默认应用端点。" +
                    "多区域/多 BaseUrl 部署请显式开启此选项。",
                    config.AppKey);
            }
            authenticationApi = scopedSp.GetRequiredService<IFeishuAuthentication>();
        }

        // === 步骤 3：创建 TokenManager（依赖 AuthenticationApi） ===
        // S-3 修复：通过 IFeishuTokenStoreFactory 替代 is FeishuTokenStore 类型检查。
        // 默认注册 PerAppFeishuTokenStoreFactory（per-app FeishuTokenStore 实例）；
        // Redis 等自定义存储场景注册 PerAppRedisTokenStoreFactory 返回 per-app 实例。
        var tokenStoreFactory = scopedSp.GetRequiredService<IFeishuTokenStoreFactory>();
        var (tokenStore, userTokenStore) = tokenStoreFactory.Create(config.AppKey);

        // MA-02 修复：通过 IFeishuTokenManagerFactory 替代直接 new TenantTokenManager(...) 的硬编码方式，
        // 使自定义 TokenManager 实现可通过注册自定义工厂接入 DI 容器。
        // 默认注册 DefaultFeishuTokenManagerFactory，行为与原实现完全一致。
        var tokenManagerFactory = scopedSp.GetRequiredService<IFeishuTokenManagerFactory>();
        var (tenantTokenManager, appTokenManager, userTokenManager) = tokenManagerFactory.Create(
            config, authenticationApi, tokenStore, userTokenStore);

        // === 步骤 4：创建恢复 HttpClient（含令牌恢复，供业务 API 使用） ===
        // TMR-07/TMX-19（MudHttpUtils 2.0.5）：统一使用 IOptionsMonitor<TokenRecoveryOptions> 构造
        // （组件唯一的公共用户级构造），TokenRecoveryOptions 支持运行期热更新。
        // IOptionsMonitor 由上方 AddOptions<TokenRecoveryOptions>() 注册，缺失时 fail-fast 暴露装配错误。
        var recoveryLogger = scopedSp.GetService<ILogger<TokenRecoveryEnhancedClient>>();
        var recoveryOptionsMonitor = scopedSp.GetRequiredService<IOptionsMonitor<TokenRecoveryOptions>>();

        var recoveryExecutor = new TokenRecoveryExecutor(
            tenantTokenManager,
            userTokenManager as IUserTokenManager,
            // TMA-02 / P1-1 修复（D2 契约）：不传 ICurrentUserContext 给 TokenRecoveryExecutor。
            // 组件 SR-L2 会据 ICurrentUserContext 推断用户级恢复，而飞书租户接口默认不生成
            // TokenRecoveryContext，会导致重试请求被注入用户令牌（凭据类别替换）。
            // 用户级恢复仍由组件生成的显式 TokenRecoveryContext.UserId 触发，不受影响。
            null,
            recoveryOptionsMonitor,
            recoveryLogger);

        var recoveryHttpClient = httpClientFactory.Create(config.AppKey, recoveryExecutor);

        // === 步骤 5：创建应用上下文（使用恢复 HttpClient） ===
        // TMA-13：传入 scopedSp 和 scope，使 FeishuAppContext.GetService<T>() 回退到作用域 SP，
        // 且 scope 随上下文 Dispose 释放（Scoped 依赖如 IFeishuCurrentUserContext 随之释放）。
        return new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            recoveryHttpClient,
            scopedSp,
            scope);
        }
        catch
        {
            // TMA2-11 / D13：装配失败，释放 scope 避免泄漏。成功时所有权转移给 FeishuAppContext。
            scope?.Dispose();
            throw;
        }
    }

    private void WarnResilienceConfigMismatch(IList<FeishuAppConfig> configs)
    {
        if (configs.Count <= 1)
            return;

        var defaultConfig = configs.FirstOrDefault(c => c.IsDefault) ?? configs.FirstOrDefault();
        if (defaultConfig == null)
            return;

        var nonDefaultConfigs = configs
            .Where(c => c != defaultConfig && HasResilienceMismatch(c, defaultConfig))
            .ToList();

        if (nonDefaultConfigs.Count > 0)
        {
            // A-3 修复：方法名 WarnResilienceConfigMismatch 暗示 Warning 级别，原用 LogInformation 与语义不符，
            // 且降低了多应用弹性策略不一致的可观测性。改回 LogWarning。
            _logger.LogWarning(
                "多应用模式下检测到不同应用具有不同的弹性策略配置（重试、超时、熔断）。" +
                "Per-App 弹性策略已启用，各应用将使用独立的策略配置。默认应用 '{DefaultAppKey}' 的配置用于全局回退（如 ResilientHttpClient 装饰器）。",
                defaultConfig.AppKey);
        }
    }

    private static bool HasResilienceMismatch(FeishuAppConfig app, FeishuAppConfig defaultApp)
    {
        var appRetry = app.HttpRetry ?? new Configuration.HttpRetryOptions();
        var defRetry = defaultApp.HttpRetry ?? new Configuration.HttpRetryOptions();
        var appCb = app.CircuitBreaker ?? new Configuration.CircuitBreakerOptions();
        var defCb = defaultApp.CircuitBreaker ?? new Configuration.CircuitBreakerOptions();

        return appRetry.MaxAttempts != defRetry.MaxAttempts
            || appRetry.DelayMs != defRetry.DelayMs
            || app.TimeoutSeconds != defaultApp.TimeoutSeconds
            || appCb.Enabled != defCb.Enabled
            || appCb.FailureThreshold != defCb.FailureThreshold
            || appCb.SamplingDurationSeconds != defCb.SamplingDurationSeconds
            || appCb.BreakDurationSeconds != defCb.BreakDurationSeconds
            || appCb.MinimumThroughput != defCb.MinimumThroughput;
    }
}

/// <summary>
/// TMR-P1-3（F3）：应用已从配置快照移除（热更新 / <c>RemoveApp</c> 竞态窗口内的确定性终态）。
/// </summary>
/// <remarks>
/// <para>
/// 继承 <see cref="InvalidOperationException"/> 以兼容既有 catch 约定（外部按
/// <see cref="InvalidOperationException"/> 捕获的宿主代码不受影响），
/// 但被 <c>FeishuAppManager.IsTransientInitFailure</c> 显式排除出瞬时白名单，
/// 避免落入"反复重建 Lazy、反复失败"的资源放大循环。此类型为 internal，不进公共 API 面，
/// 为普通类型（不参与 JSON 序列化），AOT 无涉。
/// </para>
/// </remarks>
internal sealed class FeishuAppRemovedException : InvalidOperationException
{
    /// <summary>
    /// 初始化异常实例。
    /// </summary>
    /// <param name="appKey">已从快照移除的应用键。</param>
    public FeishuAppRemovedException(string appKey)
        : base($"应用 '{appKey}' 已从配置快照移除（可能正在热更新或已被下线），不再初始化。")
        => AppKey = appKey;

    /// <summary>
    /// 已从快照移除的应用键。
    /// </summary>
    public string AppKey { get; }
}
