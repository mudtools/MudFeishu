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
    private volatile FeishuAppConfig[] _configs;
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
    /// TMA2-07 / P1-4：应用首次实例化事件。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="GetOrCreateContext"/> / <see cref="TryGetApp"/> 成功注册到基类字典后、
    /// <b>锁外</b>触发。<c>AddApp</c> / <c>RebuildAppContext</c> 已由 <see cref="RegisterApp"/>
    /// 触发的 <c>ConfigurationChanged</c> 事件覆盖，不重复触发此事件。
    /// <see cref="FeishuTokenRegistrationService" /> 订阅此事件做增量注册。
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
    /// TMA2-09 / D13：热更新改为两阶段事务化：
    /// <list type="bullet">
    /// <item>Phase-A（锁外预构造）：计算 diff，对新增/更新应用预构造上下文。任一失败则整体放弃。</item>
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

            // 节流：与当前快照完全一致（无新增/删除/更新）时不做任何重建。
            if (IsSameAsCurrentSnapshot(incoming))
            {
                return;
            }

            // TMA2-09 / D13：两阶段事务化——Phase-A 预构造，Phase-B 提交。
            // 锁序：_configApplyLock → _lazyRebuildLock / _registryLock / _defaultAppLock。
            lock (_configApplyLock)
            {
                ApplyConfigurationChanges(incoming);
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
    private void ApplyConfigurationChanges(List<FeishuAppConfig> incoming)
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
                    // 获取旧上下文引用以入退休队列。
                    FeishuAppContext? oldContext = null;
                    if (_lazyContexts.TryGetValue(config.AppKey, out var oldLazy) && oldLazy.IsValueCreated)
                    {
                        try { oldContext = oldLazy.Value; }
                        catch { /* 旧 Lazy 初始化失败的上下文无需退休。 */ }
                    }
                    if (oldContext == null && base.TryGetApp(config.AppKey, out var registeredOld) && registeredOld is FeishuAppContext registeredCtx)
                    {
                        oldContext = registeredCtx;
                    }

                    // TMA2-05 / D10：凭据变更即清库。
                    if (oldContext != null)
                    {
                        var oldConfig = oldContext.Config;
                        if (!string.Equals(oldConfig?.AppId, config.AppId, StringComparison.Ordinal) ||
                            !string.Equals(oldConfig?.AppSecret, config.AppSecret, StringComparison.Ordinal))
                        {
                            _logger.LogInformation(
                                "应用 {AppKey} 的凭据已变更（AppId 或 AppSecret 变化），清除该应用的持久化令牌。",
                                config.AppKey);
                            PurgeTokenStoreAsync(config.AppKey).ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                    }

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

        // 3) 同步配置快照（TMA-05：原子交换不可变数组）
        Volatile.Write(ref _configs, incoming.ToArray());
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
        return string.Equals(a.AppId, b.AppId, StringComparison.Ordinal)
            && string.Equals(a.AppSecret, b.AppSecret, StringComparison.Ordinal)
            && string.Equals(a.BaseUrl, b.BaseUrl, StringComparison.Ordinal)
            && a.AllowCustomBaseUrl == b.AllowCustomBaseUrl
            && a.TimeOut == b.TimeOut
            && a.RetryCount == b.RetryCount
            && a.RetryDelayMs == b.RetryDelayMs
            && a.CircuitBreakerEnabled == b.CircuitBreakerEnabled
            && a.CircuitBreakerFailureThreshold == b.CircuitBreakerFailureThreshold
            && a.CircuitBreakerSamplingDurationSeconds == b.CircuitBreakerSamplingDurationSeconds
            && a.CircuitBreakerBreakDurationSeconds == b.CircuitBreakerBreakDurationSeconds
            && a.CircuitBreakerMinimumThroughput == b.CircuitBreakerMinimumThroughput
            && a.TokenRefreshThreshold == b.TokenRefreshThreshold
            && a.EnableLogging == b.EnableLogging
            && a.IsDefault == b.IsDefault;
    }

    /// <summary>
    /// 用新配置重建指定应用的上下文。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 旧上下文<b>不立即 Dispose</b>（与 <c>DefaultAppManager.RemoveApp</c> 的 NEW-MA-01 语义一致），
    /// 避免在途请求抛 <c>ObjectDisposedException</c>。
    /// TMA-07 / P1-6 修复（D5 契约）：旧上下文进入退休队列，宽限期后 Dispose（停止其 Timer），
    /// 不再依赖 GC 回收（Timer 会 root 旧对象图，GC 不会代劳）。
    /// </para>
    /// <para>
    /// 令牌热迁移：per-app 存储键含 appKey 维度（<c>feishu:{appKey}:token:*</c>），
    /// 存储后端（MemoryCache / Redis）独立于上下文实例，重建后可从既有令牌恢复，无需额外逻辑。
    /// </para>
    /// </remarks>
    private void RebuildAppContext(FeishuAppConfig config)
    {
        config.Validate();

        // TMA-07 / P1-6 修复：在替换 Lazy 之前，尝试获取旧上下文引用以入退休队列。
        FeishuAppContext? oldContext = null;
        if (_lazyContexts.TryGetValue(config.AppKey, out var oldLazy) && oldLazy.IsValueCreated)
        {
            try
            {
                oldContext = oldLazy.Value;
            }
            catch
            {
                // 旧 Lazy 初始化失败的上下文无需退休（无 Timer 等资源）。
            }
        }

        // 同时检查基类字典中已注册的旧上下文
        if (oldContext == null && base.TryGetApp(config.AppKey, out var registeredOld) && registeredOld is FeishuAppContext registeredCtx)
        {
            oldContext = registeredCtx;
        }

        // TMA2-05 / D10：凭据变更即清库。
        // 热更新/重建上下文时，若 (AppId, AppSecret) 任一发生变化 → 立即清除该 appKey 的持久化令牌。
        // 仅 BaseUrl/TimeOut/弹性参数等变化时保留令牌热迁移。
        if (oldContext != null)
        {
            var oldConfig = oldContext.Config;
            if (!string.Equals(oldConfig?.AppId, config.AppId, StringComparison.Ordinal) ||
                !string.Equals(oldConfig?.AppSecret, config.AppSecret, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "应用 {AppKey} 的凭据已变更（AppId 或 AppSecret 变化），清除该应用的持久化令牌。",
                    config.AppKey);
                PurgeTokenStoreAsync(config.AppKey).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        FeishuAppContext context;
        lock (_lazyRebuildLock)
        {
            _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(
                () => CreateAppContext(config),
                LazyThreadSafetyMode.ExecutionAndPublication);

            // 立即实例化以便触发 Updated 事件；CreateAppContext 为纯内存装配（无网络往返）。
            context = _lazyContexts[config.AppKey].Value;
        }

        // TMA-07：旧上下文进入退休队列，宽限期后 Dispose（停止其 Timer），
        // 不再依赖 GC 回收（Timer 会 root 旧对象图，GC 不会代劳）。
        if (oldContext != null)
        {
            _retirement?.Enqueue(config.AppKey, oldContext);
        }

        // 使用 RegisterApp 而非 UpdateApp：基类 UpdateApp 要求应用已存在于其 _apps 字典，
        // 而本类采用懒加载——应用可能仅存在于 _lazyContexts 中（从未被访问过），此时 UpdateApp 会抛
        // "未找到应用标识为 'xxx' 的应用上下文，无法更新"。
        // RegisterApp 内部以 _apps.ContainsKey 判定 isUpdate，首次注册触发 Added、已存在触发 Updated，
        // 语义与本方法的两种来源（新建 / 重建）天然吻合。
        RegisterApp(config.AppKey, context, config.IsDefault);

        if (config.IsDefault)
        {
            lock (_defaultAppLock)
            {
                _defaultAppKey = config.AppKey;
            }
        }
    }

    /// <summary>
    /// TMA2-05 / D10：清除指定 appKey 的持久化令牌。
    /// ClearAsync 失败不阻断重建（LogWarning + 计数）。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    private async Task PurgeTokenStoreAsync(string appKey)
    {
        try
        {
            var tokenStoreFactory = _serviceProvider.GetService<IFeishuTokenStoreFactory>();
            if (tokenStoreFactory != null)
            {
                var (store, _) = tokenStoreFactory.Create(appKey);
                await store.ClearAsync().ConfigureAwait(false);
                _logger.LogInformation("已清除应用 {AppKey} 的持久化令牌。", appKey);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "清除应用 {AppKey} 的持久化令牌失败（不阻断重建）。", appKey);
        }
    }

    /// <summary>
    /// ARC-7：记录命名客户端端点（<c>BaseUrl</c> / <c>TimeOut</c>）的热更新，使
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
            previous.TimeOut != incoming.TimeOut)
        {
            _logger.LogInformation(
                "配置热更新：应用 {AppKey} 的 HTTP 客户端端点已变更（BaseUrl: {PreviousBaseUrl} → {IncomingBaseUrl}，TimeOut: {PreviousTimeOut}s → {IncomingTimeOut}s），" +
                "重建后的客户端将使用新端点，无需重启进程。",
                incoming.AppKey, previousBaseUrl, incomingBaseUrl, previous.TimeOut, incoming.TimeOut);
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
    /// <remarks>
    /// <para>
    /// NEW-MA-08 修复：<see cref="Lazy{T}"/> 在 <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>
    /// 模式下会缓存工厂委托抛出的异常，导致首次初始化失败的应用在进程剩余生命周期内不可用。
    /// 此方法在检测到缓存异常时重建 <see cref="Lazy{T}"/> 实例，允许后续调用重试初始化。
    /// </para>
    /// </remarks>
    private FeishuAppContext GetOrCreateContext(string appKey)
    {
        if (_lazyContexts.TryGetValue(appKey, out var lazy))
        {
            try
            {
                var context = lazy.Value;
                // 注册到基类字典中（如果尚未注册）
                // 注意：必须使用 base.HasApp 检查"是否已注册到基类字典"，
                // 而非使用 HasApp（后者会同时检查 _lazyContexts，导致永远跳过 RegisterApp）。
                var wasRegistered = base.HasApp(appKey);
                if (!wasRegistered)
                {
                    var config = Volatile.Read(ref _configs).First(c => c.AppKey == appKey);
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
                lock (_lazyRebuildLock)
                {
                    if (_lazyContexts.TryGetValue(appKey, out var current) && ReferenceEquals(current, lazy))
                    {
                        var config = Volatile.Read(ref _configs).First(c => c.AppKey == appKey);
                        var capturedConfig = config;
                        _lazyContexts[appKey] = new Lazy<FeishuAppContext>(
                            () => CreateAppContext(capturedConfig),
                            LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                }
                throw new InvalidOperationException(
                    $"应用 '{appKey}' 初始化失败: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException(
            $"未找到应用标识为 '{appKey}' 的应用上下文。请先调用 RegisterApp 注册应用。");
    }

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
            try
            {
                var context = lazy.Value;
                // 注册到基类字典以便后续快速查找
                var wasRegistered = base.HasApp(appKey);
                if (!wasRegistered)
                {
                    var config = Volatile.Read(ref _configs).First(c => c.AppKey == appKey);
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
                lock (_lazyRebuildLock)
                {
                    if (_lazyContexts.TryGetValue(appKey, out var current) && ReferenceEquals(current, lazy))
                    {
                        var config = Volatile.Read(ref _configs).First(c => c.AppKey == appKey);
                        var capturedConfig = config;
                        _lazyContexts[appKey] = new Lazy<FeishuAppContext>(
                            () => CreateAppContext(capturedConfig),
                            LazyThreadSafetyMode.ExecutionAndPublication);
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
        // TMA-07 / P1-6 修复：在移除前获取旧上下文引用以入退休队列。
        FeishuAppContext? oldContext = null;
        if (_lazyContexts.TryGetValue(appKey, out var oldLazy) && oldLazy.IsValueCreated)
        {
            try
            {
                oldContext = oldLazy.Value;
            }
            catch
            {
                // 旧 Lazy 初始化失败的上下文无需退休。
            }
        }

        // 同时检查基类字典中已注册的旧上下文
        if (oldContext == null && base.TryGetApp(appKey, out var registeredOld) && registeredOld is FeishuAppContext registeredCtx)
        {
            oldContext = registeredCtx;
        }

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
                if (config.IsDefault)
                {
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
    /// NEW-MA-11 说明：<see cref="UseApp"/> 的生成实现通过 <c>_appContextHolder.Current = context</c>
    /// 切换上下文，而 <c>IAppContextHolder</c> 的默认实现 <c>AsyncLocalAppContextSwitcher</c> 基于
    /// <see cref="AsyncLocal{T}"/>，因此即使 T 注册为 Singleton，并发调用 <see cref="UseApp"/>
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
        var recoveryOptions = scopedSp.GetService<IOptions<TokenRecoveryOptions>>()?.Value;
        var recoveryLogger = scopedSp.GetService<ILogger<TokenRecoveryEnhancedClient>>();

        var recoveryExecutor = new TokenRecoveryExecutor(
            tenantTokenManager,
            userTokenManager as IUserTokenManager,
            // TMA-02 / P1-1 修复（D2 契约）：不传 ICurrentUserContext 给 TokenRecoveryExecutor。
            // 组件 SR-L2 会据 ICurrentUserContext 推断用户级恢复，而飞书租户接口默认不生成
            // TokenRecoveryContext，会导致重试请求被注入用户令牌（凭据类别替换）。
            // 用户级恢复仍由组件生成的显式 TokenRecoveryContext.UserId 触发，不受影响。
            null,
            recoveryOptions,
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
        return app.RetryCount != defaultApp.RetryCount
            || app.RetryDelayMs != defaultApp.RetryDelayMs
            || app.TimeOut != defaultApp.TimeOut
            || app.CircuitBreakerEnabled != defaultApp.CircuitBreakerEnabled
            || app.CircuitBreakerFailureThreshold != defaultApp.CircuitBreakerFailureThreshold
            || app.CircuitBreakerSamplingDurationSeconds != defaultApp.CircuitBreakerSamplingDurationSeconds
            || app.CircuitBreakerBreakDurationSeconds != defaultApp.CircuitBreakerBreakDurationSeconds
            || app.CircuitBreakerMinimumThroughput != defaultApp.CircuitBreakerMinimumThroughput;
    }
}
