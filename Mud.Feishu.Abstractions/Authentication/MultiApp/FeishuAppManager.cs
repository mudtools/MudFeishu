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
/// 飞书应用管理器：管理所有飞书应用的创建、获取与移除。
/// </summary>
/// <remarks>
/// 每个应用拥有独立的配置、缓存与 TokenManager 实例；通用应用管理能力继承自 <c>DefaultAppManager&lt;FeishuAppContext&gt;</c>。
/// <para>采用<b>懒加载</b>：构造函数只预注册各应用的 Lazy 上下文并记录默认应用键，应用在首次访问时创建，以降低启动延迟。</para>
/// <para>本类非 sealed，可继承并重写 <see cref="CreateAppContext"/> 以提供自定义的 <see cref="IFeishuAppContext"/>。</para>
/// </remarks>
public class FeishuAppManager : DefaultAppManager<IFeishuAppContext>, IFeishuAppManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    // 根 SP 的作用域工厂：按需创建作用域解析 Scoped 依赖，避免单例捕获请求作用域服务（Captive Dependency）。
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<FeishuAppManager> _logger;
    private readonly ConcurrentDictionary<string, Lazy<FeishuAppContext>> _lazyContexts = new();
    // 配置快照为不可变数组，只经 Volatile.Read/Volatile.Write 原子交换（不可标记 volatile：与 Volatile/Interlocked 混用会触发 CS0420）。
    private FeishuAppConfig[] _configs;
    // 本类私有的默认应用键；volatile 保证 RemoveApp 的写入对 GetDefaultApp 的读取立即可见。
    private volatile string? _defaultAppKey;
    // 保护 _defaultAppKey 的"读取+提升"与 RemoveApp 的"清空+提升"两组复合操作互斥，消除 TOCTOU。
    private readonly object _defaultAppLock = new();
    // 保护 Lazy<> 重建，避免并发线程同时重建同一应用的 Lazy 上下文。
    private readonly object _lazyRebuildLock = new();
    private IDisposable? _configReloadSubscription;
    // 退休队列：宽限期后 Dispose 旧上下文，避免被 Timer root 的对象图无法回收（内存泄漏）。
    private FeishuAppContextRetirement? _retirement;
    // 运行时经 AddApp 添加的应用键集合，热更新时据此决定是否保留。
    private readonly HashSet<string> _runtimeAddedAppKeys = new(StringComparer.Ordinal);
    // 保护 AddApp 的"检查 → 创建 → 注册 → 写快照"原子操作。
    private readonly object _registryLock = new();

    // 锁序约束：_configApplyLock → _lazyRebuildLock / _registryLock / _defaultAppLock，禁止反向获取。
    private readonly object _configApplyLock = new();

    /// <summary>凭据变更清库失败的结构化事件 ID（供告警系统按 EventId 过滤接入）。</summary>
    internal static readonly EventId PurgeTokenStoreFailureEvent = new(5601, "PurgeTokenStoreFailed");

    /// <summary>应用首次实例化事件。</summary>
    /// <remarks>
    /// 在上下文成功注册到基类字典之后、<b>锁外</b>触发。<c>AddApp</c> 与配置热更新已由
    /// <c>RegisterApp</c> 触发的 <c>ConfigurationChanged</c> 覆盖，因此不重复触发本事件。
    /// </remarks>
    internal event EventHandler<FeishuAppInstantiatedEventArgs>? AppInstantiated;

    /// <summary>退休队列实例（供测试驱动扫描，避免依赖真实计时器）。</summary>
    internal FeishuAppContextRetirement? Retirement => _retirement;

    // 宿主自实现的外部标识反查提供者（可选，经 DI 解析，未注册时为 null）。
    // 基类构造不注入该提供者，故在本类叠加：内置索引未命中后再委托本提供者，语义与基类一致（fail-closed）。
    private readonly IAppLookupProvider? _externalLookupProvider;

    /// <summary>初始化飞书应用管理器，并预注册全部应用的懒加载上下文。</summary>
    /// <param name="serviceProvider">服务提供者。</param>
    /// <param name="configs">应用配置列表。</param>
    /// <param name="logger">日志记录器。</param>
    /// <exception cref="ArgumentNullException">任一必需参数为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">未配置任何应用，或未指定默认应用时抛出。</exception>
    public FeishuAppManager(
        IServiceProvider serviceProvider,
        IEnumerable<FeishuAppConfig> configs,
        ILogger<FeishuAppManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        // 从根 SP 获取 IServiceScopeFactory，用于按需创建作用域解析 Scoped 依赖。
        _scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
        // 可选：宿主注册的外部标识反查提供者（未注册时仅内置索引参与反查）。
        _externalLookupProvider = serviceProvider.GetService<IAppLookupProvider>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configs = (configs as IList<FeishuAppConfig> ?? configs.ToList()).ToArray();

        if (_configs.Length == 0)
            throw new InvalidOperationException("未配置任何飞书应用");

        // 预注册全部应用的 Lazy 上下文，但不立即创建。
        // 重复 AppKey 只告警不抛异常（后注册覆盖先注册），避免静默覆盖使配置错误难以排查。
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

        // 必须存在 IsDefault=true 的应用（契约：MultiApp_NoDefaultApp_ShouldThrowOnRegistration）。
        // 此处不预创建默认应用：避免构造阶段强制要求全部 DI 依赖（如 IMemoryCache）就绪，
        // 默认应用在首次访问 GetDefaultApp() / Default*TokenManager 时按需创建。
        var defaultConfig = _configs.FirstOrDefault(c => c.IsDefault);
        if (defaultConfig == null)
            throw new InvalidOperationException("未配置默认应用，请在至少一个 FeishuAppConfig 上设置 IsDefault = true。");

        _defaultAppKey = defaultConfig.AppKey;

        _logger.LogInformation("飞书应用管理器初始化完成，共配置 {Count} 个应用，默认应用: {AppKey}",
            _configs.Length, defaultConfig.AppKey);

        // 退休队列宽限期取自 FeishuAppOptions.ContextRetireDelaySeconds，默认 300s。
        var appOptions = _serviceProvider.GetService<IOptions<FeishuAppOptions>>()?.Value;
        var retireDelaySeconds = appOptions?.ContextRetireDelaySeconds ?? 300;
        _retirement = new FeishuAppContextRetirement(retireDelaySeconds, _logger);

        WarnResilienceConfigMismatch(_configs);

        // 启动期检测 IEncryptionProvider 注册状态：未注册时，[Body(EnableEncrypt=true)] 的 API 要到
        // 首次请求才抛 InvalidOperationException（运行时失败而非编译期诊断），此处告警使失败模式提前可见。
        if (_serviceProvider.GetService<IEncryptionProvider>() == null)
        {
            _logger.LogWarning(
                "未注册 IEncryptionProvider。若使用 [Body(EnableEncrypt=true)] 标记的 API，" +
                "首次请求将抛出 InvalidOperationException。请通过 AddMudHttpClient 配置 AesEncryptionOptions 或注册自定义 IEncryptionProvider。");
        }

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

    /// <summary>配置变更回调：按 AppKey 计算差异并增量应用。</summary>
    /// <remarks>
    /// 事件语义与基类 <c>ConfigurationChanged</c> 对齐：新增触发 <c>Added</c>、更新触发 <c>Updated</c>、删除触发 <c>Removed</c>。
    /// 异常在此处被吞掉并记录：<c>IOptionsMonitor.OnChange</c> 的回调抛异常会中断 IConfiguration 的变更通知链。
    /// <para>热更新分三段：锁外清库（IO）→ 锁内预构造（纯内存装配，任一失败则整体放弃）→ 短临界区提交。</para>
    /// </remarks>
    internal void OnConfigurationChanged(List<FeishuAppConfig>? newConfigs)
    {
        if (newConfigs == null || newConfigs.Count == 0)
        {
            _logger.LogWarning("收到空的飞书应用配置变更通知，已忽略（至少需要保留一个应用配置）。");
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

            // 先校验后应用（契约：任一校验不通过则整体忽略本次变更）。
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

            // 被配置源「正式接管」（本轮 incoming 中声明）的 AppKey 必须撤销运行时标记：
            // 标记语义是「运行时添加且未被配置源声明」，属配置源属性——否则「AddApp → 配置声明 → 配置删除」
            // 序列下，该应用在 RemoveRuntimeAddedAppsOnReload=false（默认）时被静默永久保留。
            // 位置必须在节流判定之前：若放到提交后，一旦本轮 incoming 与快照逐字段一致（节流直接 return），
            // 撤销将永不发生（契约：RuntimeAddedApp_ShouldBeRemovedByConfig_...）。与后续预构造是否失败无关。
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

            // 预清库（锁外）：清库（IO）必须在获取 _configApplyLock 之前完成——维持「清库 → 重建」的严格时序，
            // 同时消除锁内 sync-over-async 阻塞。比对源是配置快照而非活动上下文，故未实例化应用的凭据变更也能被检出。
            // 返回的清库计划供提交后二次清库使用（旧上下文不得在清库完成前从 store 恢复旧凭据令牌）。
            var purgePlan = PurgeCredentialChangedTokens(incoming);

            // 两阶段事务化：先锁内预构造，后提交。
            // 锁序：_configApplyLock → _lazyRebuildLock / _registryLock / _defaultAppLock。
            lock (_configApplyLock)
            {
                ApplyConfigurationChanges(incoming, purgePlan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "飞书应用配置热更新失败，已保持原配置继续运行。请检查新的应用配置是否合法（AppKey/AppId/AppSecret 校验）。");
        }
    }

    /// <summary>两阶段事务化地应用配置变更。</summary>
    /// <param name="incoming">新配置列表</param>
    /// <param name="purgePlan">
    /// 锁外预清库阶段检出的凭据变更清库计划，供提交后对"已提交更新键"发起二次清库。
    /// 正常路径下只清除 WriteBackRiskKeys（存在回写风险的键），避免删除新凭据刚写入的令牌；超时路径下对全部 ToPurge 收尾清库。
    /// </param>
    private void ApplyConfigurationChanges(List<FeishuAppConfig> incoming, CredentialPurgePlan purgePlan)
    {
        var incomingKeys = new HashSet<string>(incoming.Select(c => c.AppKey), StringComparer.Ordinal);
        var currentKeys = Volatile.Read(ref _configs).Select(c => c.AppKey).ToList();

        // 是否移除运行时添加的应用（默认 false = 保留）。
        var appOptions = _serviceProvider.GetService<IOptions<FeishuAppOptions>>()?.Value;
        var removeRuntimeApps = appOptions?.RemoveRuntimeAddedAppsOnReload ?? false;

        // 锁内预构造：纯内存装配（CreateAppContext 无网络往返），锁内执行不会阻塞其他热更新回调。
        // 对比：清库 IO 在锁外，提交在 _lazyRebuildLock 内仅做引用交换。
        var builtContexts = new List<(string appKey, FeishuAppContext context, FeishuAppConfig config, FeishuAppContext? oldContext)>();
        var phaseASuccess = true;

        foreach (var config in incoming)
        {
            // 只处理更新（已存在）的应用；新增应用走 AddApp（内部已有原子化装配）。
            if (HasApp(config.AppKey))
            {
                LogClientEndpointChanges(config);

                // 未实例化的应用在热更新中保持懒加载：只替换 Lazy 的配置闭包，不预构造上下文——
                // 从未被访问的应用不应在热更新中被强制实例化。
                var oldContext = ResolveExistingContext(config.AppKey);
                if (oldContext == null)
                {
                    // 未实例化：不构造新上下文，仅替换 Lazy 的配置闭包。
                    builtContexts.Add((config.AppKey, null!, config, null));
                    continue;
                }

                try
                {
                    // 构造新上下文并把旧上下文引用入退休队列；凭据变更清库已在锁外完成。
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
            // 预构造失败：Dispose 已构造的上下文，但不动注册表。
            // Dispose 是幂等的：单个上下文释放失败不应中断其余上下文的清理。
            foreach (var (_, context, _, _) in builtContexts)
            {
                try { (context as IDisposable)?.Dispose(); }
                catch { }
            }
            _logger.LogError("配置热更新变更未应用（Phase-A 失败），注册表与快照保持原状。");
            return;
        }

        // 短临界区提交：逐应用在 _lazyRebuildLock 内仅做引用交换，锁外 RegisterApp + 退休。
        foreach (var (appKey, context, config, oldContext) in builtContexts)
        {
            // B-1（锁内）：仅做引用交换。未实例化的应用（context == null）重建为 Lazy 闭包，不预构造。
            lock (_lazyRebuildLock)
            {
                _lazyContexts[appKey] = context != null
                    ? new Lazy<FeishuAppContext>(() => context, LazyThreadSafetyMode.ExecutionAndPublication)
                    : new Lazy<FeishuAppContext>(() => CreateAppContext(config), LazyThreadSafetyMode.ExecutionAndPublication);
            }

            // B-2（锁外）：RegisterApp + 退休旧上下文。
            // 未实例化的应用既无 context 也无 oldContext，故不注册、不退休。
            if (context != null)
            {
                if (oldContext != null)
                {
                    _retirement?.Enqueue(appKey, oldContext);
                }

                RegisterApp(appKey, context, config.IsDefault);
            }

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
            // 运行时添加的应用默认不被热更新移除（RemoveRuntimeAddedAppsOnReload=false）。
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

        // 3) 同步配置快照：原子交换不可变数组，并纳入 _registryLock 与 AddApp 的快照读改写互斥。
        lock (_registryLock)
        {
            Volatile.Write(ref _configs, incoming.ToArray());
        }

        // 4) 提交后二次清库（闭环）：窗口期（锁外清库完成 → 本应用提交 → 旧上下文入退休队列）内，
        // 旧上下文的后台刷新 Timer / 在途 GetOrRefreshTokenAsync 仍可能用旧凭据调 API 并把令牌写回 store，
        // 使新上下文恢复出"旧凭据来源"的令牌后必然 401。故对「凭据变更键 ∩ 本轮实际提交的键」二次清库——
        // 此时旧上下文已停止接管新请求，回写源已被切断。
        // 正常路径只覆盖 WriteBackRiskKeys（避免删除新凭据刚写入的令牌），超时路径覆盖全部 ToPurge。
        // 与并发写入之间存在 store SCAN 的固有残余窗口（与 Redis 语义一致）。
        // AddApp 新增键与被移除键无需二次清库（前者无旧凭据；后者旧上下文已入退休队列且键有 TTL）。
        // PurgeTokenStoreAsync 内部吞掉全部异常（含 OCE），故 fire-and-forget 安全。
        var postCommitPurgeKeys = (purgePlan.PhasePCompleted
                ? purgePlan.WriteBackRiskKeys     // 常规：只补"可能回写"的键
                : purgePlan.ToPurge)             // 预清库超时未完成：全部键收尾
            .Where(k => builtContexts.Any(b => string.Equals(b.appKey, k, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (postCommitPurgeKeys.Count > 0)
        {
            // 与预清库同构——打门（计数与预清库叠加，覆盖"提交窗口内旧上下文回写"）
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
    /// 凭据预清库——在获取 <c>_configApplyLock</c> 之前完成凭据变更检测与清库。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 清库是 IO（Redis 为 SCAN + 逐键 DELETE），禁止在锁内执行，也不得在
    /// <c>IOptionsMonitor.OnChange</c> 回调线程上同步等待。
    /// </para>
    /// <para>
    /// 本方法只做「打门（<see cref="TokenStorePurgeGate"/>.Mark，同步、微秒级）+ 异步清库」，<b>立即返回</b>。
    /// 实质约束（"新上下文不得在清库完成前从 store 恢复旧凭据令牌"）
    /// 由恢复路径的门短路承担，因此「阻塞 OnChange 直到清库完成」并非必需；
    /// 若改为在回调线程上 <c>Task.Run(...).Wait(10s)</c>，回调线程最长阻塞 10s、宿主存在
    /// <c>SynchronizationContext</c> 时有死锁面、线程池受限时占用双线程放大饥饿。
    /// </para>
    /// <para>
    /// 门带 30s 安全超时（fail-open）：清库异常/挂死时门自动失效并上报一次
    /// <c>PurgeGateState.TimedOut</c>，恢复路径重新启用，绝不因门而永久禁用 store 恢复。
    /// </para>
    /// <para>
    /// 并发语义：两个并发 <c>OnChange</c> 各自执行预清库可能对同一 appKey 清库两次——
    /// ClearAsync 幂等（删除不存在键为 no-op），且第二次清库发生在后者替换上下文之前，
    /// 时序安全性不弱于串行。清库内部吞掉全部异常（含 OCE），不会中断热更新。
    /// </para>
    /// <para>
    /// 比对源必须是配置快照（<c>_configs</c>）而非活动旧上下文：
    /// 若按 <see cref="ResolveExistingContext"/> 返回 null 即 <c>continue</c> 跳过，
    /// 未实例化应用的凭据变更检测会被完全跳过（新上下文不得恢复旧凭据令牌的约束不可达）。
    /// 按快照比对，无论应用是否实例化，凭据变更均能被检出。
    /// </para>
    /// <para>
    /// <see cref="CredentialPurgePlan.WriteBackRiskKeys"/> 仅含「有活动旧上下文」的键
    /// ——只有这些键在退休宽限期内可能以旧凭据回写 store，提交后二次清库只对这些键做，
    /// 避免删除新凭据刚写入的令牌。
    /// </para>
    /// </remarks>
    /// <param name="incoming">新配置列表</param>
    /// <returns>凭据变更清库计划（供提交后二次清库使用）。</returns>
    internal CredentialPurgePlan PurgeCredentialChangedTokens(List<FeishuAppConfig> incoming)
    {
        // 以配置快照（_configs）为比对源，而非活动旧上下文。
        var currentSnapshot = Volatile.Read(ref _configs);
        var toPurge = new List<string>();
        var writeBackRiskKeys = new List<string>();

        foreach (var config in incoming)
        {
            var oldConfig = currentSnapshot.FirstOrDefault(c =>
                string.Equals(c.AppKey, config.AppKey, StringComparison.Ordinal));
            if (oldConfig == null)
            {
                // 新增应用：无旧凭据，无需清库。
                continue;
            }

            if (!string.Equals(oldConfig.AppId, config.AppId, StringComparison.Ordinal) ||
                !string.Equals(oldConfig.AppSecret, config.AppSecret, StringComparison.Ordinal))
            {
                toPurge.Add(config.AppKey);

                // 只有「有活动旧上下文」的键才存在回写风险。
                var oldContext = ResolveExistingContext(config.AppKey);
                if (oldContext != null)
                {
                    writeBackRiskKeys.Add(config.AppKey);
                }
            }
        }

        if (toPurge.Count == 0)
        {
            return new CredentialPurgePlan(
                ToPurge: toPurge,
                WriteBackRiskKeys: writeBackRiskKeys,
                PhasePCompleted: true);
        }

        _logger.LogInformation(
            "检测到 {Count} 个应用的凭据已变更（AppId 或 AppSecret 变化），将异步清除其持久化令牌：{AppKeys}",
            toPurge.Count, string.Join(", ", toPurge));

        // 打门（同步、微秒级）→ 异步清库；**不**阻塞 IOptionsMonitor.OnChange 回调线程。
        // 实质约束（"新上下文不得在清库完成前从 store 恢复旧凭据令牌"）由恢复路径的门短路承担：
        // FeishuAppTokenManagerBase.TryRestoreFromStoreAsync / UserTokenManager.TryRestoreFromUserTokenStoreAsync /
        // LoadRefreshCandidateAsync 见门即跳过 store。
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

        // 预清库同步段（打门 + 派发异步清库）恒立即完成——回调线程不阻塞，
        // 「清库完成 → 重建」的时序由 TokenStorePurgeGate 的恢复路径短路承担。
        // 因此 PhasePCompleted 恒为 true：提交后二次清库保持窄范围
        // （WriteBackRiskKeys——只有存在活动旧上下文的键才有回写风险），
        // 避免删除新凭据刚写入的令牌；超时路径的「全部 ToPurge」分支不再可达。
        return new CredentialPurgePlan(
            ToPurge: toPurge,
            WriteBackRiskKeys: writeBackRiskKeys,
            PhasePCompleted: true);
    }

    /// <summary>
    /// 解析已实例化的旧上下文：<c>_lazyContexts</c> 中已创建的 Lazy → 基类注册字典兜底。
    /// </summary>
    /// <remarks>
    /// 凭据比对、退休入队与 <see cref="RemoveApp"/> 复用，
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
                // 退休期预期路径，Debug 级别即可（不得用空 catch 吞掉，保证故障可诊断）。
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
    /// 逐字段比较全部运行期字段，消除节流漏比。
    /// </summary>
    private bool IsSameAsCurrentSnapshot(List<FeishuAppConfig> incoming)
    {
        // 原子读取不可变快照，无需锁。
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
    /// 逐字段比较两个 FeishuAppConfig 是否完全一致。
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

    // RebuildAppContext 已删除——产品代码零调用方（唯一热更新路径为
    // ApplyConfigurationChanges，测试经 OnConfigurationChanged 间接触发同等逻辑），
    // 且其内部曾维护第二份比对+清库副本，与主路径双份漂移。

    /// <summary>
    /// 清除指定 appKey 的持久化令牌。
    /// 租户令牌经 <see cref="ITokenStore.ClearAsync"/>，用户令牌经
    /// <see cref="IFeishuUserTokenStorePurge.ClearAllUsersAsync"/> 能力探测（工厂每次 Create
    /// 新实例的 Memory 后端依赖跨实例共享记账才能清干净）。
    /// ClearAsync/ClearAllUsersAsync 失败不阻断重建（带 <see cref="PurgeTokenStoreFailureEvent"/>
    /// 记 Warning，宿主可据此建 metric；OCE 视为「未清」不记为失败）。
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
            // 取消不向上传播——清库被取消视为「未清」，不记为失败，热更新继续。
        }
        catch (Exception ex)
        {
            // 结构化可观测——EventId 5601 供 metric/告警系统过滤接入。
            // 新凭据 + 残留旧令牌可能导致 401，需检查存储后端连通性。
            _logger.LogWarning(PurgeTokenStoreFailureEvent, ex,
                "清除应用 {AppKey} 的持久化令牌失败（不阻断重建）。新凭据 + 残留旧令牌可能导致 401，" +
                "请检查存储后端连通性。", appKey);
        }
    }

    /// <summary>
    /// 记录命名客户端端点（<c>BaseUrl</c> / <c>TimeoutSeconds</c>）的热更新，使
    /// 「配置是否真的作用到了 HTTP 客户端」在日志中可观测——端点变更必须真实生效，否则多区域部署切换 BaseUrl 时无任何提示。
    /// </summary>
    /// <param name="incoming">新配置。</param>
    private void LogClientEndpointChanges(FeishuAppConfig incoming)
    {
        // 原子读取不可变快照，无需锁。
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
    /// 释放资源：释放全部在册上下文，再强制释放全部待退休上下文，最后退订配置变更通知。
    /// </summary>
    /// <remarks>
    /// Dispose 必须释放全部在册上下文（含 <c>_lazyContexts</c> 中已实例化者），
    /// 幂等。顺序：退订 → 释放在册 → 释放退休队列。
    /// </remarks>
    public void Dispose()
    {
        // 先退订，确保不再有新的事件触发。
        _configReloadSubscription?.Dispose();
        _configReloadSubscription = null;

        // 释放全部在册上下文（已实例化者），幂等。
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

        // 释放全部待退休上下文。
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
    /// <see cref="Lazy{T}"/> 在 <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>
    /// 模式下会缓存工厂委托抛出的异常，导致首次初始化失败的应用在进程剩余生命周期内不可用；
    /// 此方法在检测到缓存异常时重建 <see cref="Lazy{T}"/> 实例，允许后续调用重试初始化。
    /// </para>
    /// <para>
    /// 快照移除竞态约束：不得在热更新移除窗口内用 <c>First()</c> 抛
    /// <see cref="InvalidOperationException"/>——该异常落入瞬时白名单会触发"反复重建 Lazy、反复失败"的
    /// 资源放大循环。快照查无必须抛 <see cref="FeishuAppRemovedException"/>（确定性终态，排除出白名单），
    /// 且 <c>finally</c> 中对未注册成功的上下文经退休队列回收（Timer root 对象图，GC 不代劳）。
    /// </para>
    /// <para>
    /// 热更新替换 <c>_lazyContexts[appKey]</c> 与并发首访交错时，
    /// 并发首访持有的旧 <c>Lazy</c> 已创建旧配置上下文但无法注册（新上下文已注册）。
    /// 构造后必须做<strong>身份校验</strong>——若 <c>_lazyContexts</c> 中已非原 <c>Lazy</c>，
    /// 说明热更新已提交，回收旧上下文并重新获取当前 <c>Lazy</c>。
    /// </para>
    /// </remarks>
    private FeishuAppContext GetOrCreateContext(string appKey)
    {
        // 循环重试——热更新在并发首访执行期间替换 Lazy 时，
        // 并发首访构造的旧上下文经身份校验回收后重试获取当前 Lazy。
        while (true)
        {
            if (_lazyContexts.TryGetValue(appKey, out var lazy))
            {
                FeishuAppContext? createdContext = null;
                try
                {
                    var context = lazy.Value;
                    createdContext = context;

                    // 身份校验——若 Lazy 已被热更新替换，说明当前构造的是旧配置上下文。回收它并重新获取当前 Lazy。
                    if (!_lazyContexts.TryGetValue(appKey, out var currentLazy)
                        || !ReferenceEquals(currentLazy, lazy))
                    {
                        // 已替换 Lazy：旧上下文入退休队列，重新获取。
                        if (createdContext != null)
                        {
                            // Enqueue 已检查 _disposed，不再抛 ObjectDisposedException。
                            _retirement?.Enqueue(appKey, createdContext);
                        }
                        continue; // 重新获取当前 Lazy
                    }

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
                    // 首次实例化后在锁外触发事件。
                    if (!wasRegistered)
                    {
                        OnAppInstantiated(appKey, context);
                    }
                    return context;
                }
                // 异常过滤为"可重试异常白名单"。
                // 可重试异常（InvalidOperationException/HttpRequestException/TimeoutException 等）→ 重建 Lazy 允许重试。
                // 非瞬时异常（OutOfMemoryException/AccessViolationException/TypeLoadException 等）→ 直接上抛。
                catch (Exception ex) when (IsTransientInitFailure(ex))
                {
                    // Lazy<ExecutionAndPublication> 缓存异常后，后续 .Value 访问会重新抛出同一异常；
                    // 此处重建 Lazy<> 以允许下次调用重试初始化（如 Redis 短暂故障恢复后可自愈）。
                    // 使用双检锁避免并发线程同时重建：仅当字典中仍是原 Lazy 实例时才重建。
                    // 快照查无该应用时不再重建（确定性终态），抛 FeishuAppRemovedException。
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
                    // 注册失败路径上已创建的上下文必须显式回收（Timer roots 对象图，GC 不代劳）。
                    // 经退休队列回收（而非直接 Dispose）：保留宽限期语义，避免打断刚完成装配的管理器的在途初始化。
                    // 身份校验路径上已 Enqueue 的上下文不再重复回收（createdContext 已被消费）。
                    if (createdContext != null && !base.HasApp(appKey) && _lazyContexts.TryGetValue(appKey, out var finalLazy) && ReferenceEquals(finalLazy, lazy))
                    {
                        // Enqueue 已检查 _disposed，不再抛 ObjectDisposedException。
                        _retirement?.Enqueue(appKey, createdContext);
                    }
                }
            }

            throw new InvalidOperationException(
                $"未找到应用标识为 '{appKey}' 的应用上下文。请先调用 RegisterApp 注册应用。");
        }
    }

    /// <summary>
    /// 在配置快照中查找指定应用的配置；查无返回 null（应用已移除或尚未入快照）。
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

        // 未注册时尝试懒加载创建
        return GetOrCreateContext(appKey);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 基类 <see cref="DefaultAppManager{TAppContext}.HasApp"/> 仅检查已实例化的应用字典，
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
    /// <para>
    /// 外部标识 → appKey 反查。反查顺序：
    /// ① 基类内置索引（上下文实现 <see cref="IAppExternalIds"/> 时在注册 / 实例化时自动登记，
    /// <see cref="FeishuAppContext"/> 已声明 <c>app_id → appKey</c> 映射）→
    /// ② DI 解析的宿主自实现 <see cref="IAppLookupProvider"/>（构造 ① 不注入基类外部提供者，
    /// 故由本重写叠加兜底，语义与基类 ② 段一致：解析结果必须落在已注册应用上，
    /// 防止外部不可信输入被误路由）。
    /// </para>
    /// <para>
    /// 未命中 / 输入非法 / 映射歧义一律 fail-closed（返回 false，不猜测）；不做授权判定，
    /// 解析出的 appKey 供宿主走 <c>UseAppScope</c> / <c>GetApp</c> 的既有路径。
    /// </para>
    /// </remarks>
    public override bool TryResolveAppKey(IReadOnlyDictionary<string, string> externalIds, out string? appKey)
    {
        appKey = null;

        if (externalIds is null || externalIds.Count == 0)
            return false;

        // ① 内置索引（基类：多键互证 + 歧义 fail-closed）。
        if (base.TryResolveAppKey(externalIds, out appKey))
            return true;

        // ② 宿主自实现提供者兜底（fail-closed + 必须命中已注册应用）。
        if (_externalLookupProvider is null
            || !_externalLookupProvider.TryResolveAppKey(externalIds, out var externalResolved)
            || string.IsNullOrEmpty(externalResolved)
            || !HasApp(externalResolved!))
        {
            return false;
        }

        appKey = externalResolved;
        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// 多命中反查的飞书侧重写。基类实现消费的是<b>基类构造注入</b>的
    /// 外部提供者字段，而飞书构造不向基类注入提供者（提供者由本类字段
    /// <see cref="_externalLookupProvider"/> 持有并经 <see cref="TryResolveAppKey"/> 兜底消费）——
    /// 故必须重写以把本类持有的提供者接入多命中路径。
    /// </para>
    /// <para>
    /// <b>解析顺序（与 <see cref="TryResolveAppKey"/> 同构）</b>：
    /// 宿主提供者实现 <see cref="IMultiAppLookupProvider"/> 时委托其多命中枚举，结果经
    /// <see cref="HasApp"/> 过滤（懒加载<b>未实例化</b>条目按「已声明即存在」口径保留，不物化）并去重；
    /// 仅单结果面时回退既有语义（<see cref="TryResolveAppKey"/> 命中 → 单元素；未命中/歧义 → 空集，
    /// 内置索引歧义维持 fail-closed）。纯反查边界：不物化、不做授权判定、不抛异常。
    /// </para>
    /// </remarks>
    public override IEnumerable<string> TryResolveAllAppKeys(IReadOnlyDictionary<string, string> externalIds)
    {
        if (externalIds is null || externalIds.Count == 0)
            return Array.Empty<string>();

        if (_externalLookupProvider is IMultiAppLookupProvider multiProvider)
        {
            var results = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var appKey in multiProvider.ResolveAllAppKeys(externalIds))
            {
                // 与基类同口径：提供者契约约定不返回 null/空白，此处仍过滤（fail-safe）；
                // 解析结果必须落在已注册应用上（懒加载「已声明即存在」，不触发实例化）。
                if (string.IsNullOrWhiteSpace(appKey) || !seen.Add(appKey) || !HasApp(appKey))
                    continue;

                results.Add(appKey);
            }

            return results;
        }

        // 单结果面回退：复用全量语义（内置索引 → 宿主提供者兜底，fail-closed）。
        return TryResolveAppKey(externalIds, out var resolved) && !string.IsNullOrEmpty(resolved)
            ? new[] { resolved! }
            : (IEnumerable<string>)Array.Empty<string>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// 基类 <see cref="DefaultAppManager{TAppContext}.TryGetApp"/> 仅查询已实例化的应用字典，
    /// 对已配置但未实例化的应用返回 false。重写后：
    /// <list type="bullet">
    /// <item>已实例化：直接返回（与基类一致）。</item>
    /// <item>已配置但未实例化：触发 Lazy 创建并注册到基类字典，返回 true。</item>
    /// <item>创建过程抛异常：捕获并返回 false（保持 Try* 语义不抛异常）。</item>
    /// <item>未配置：返回 false（与基类一致）。</item>
    /// </list>
    /// <para>
    /// 与 <see cref="GetOrCreateContext"/> 同构的身份校验——
    /// 若 Lazy 已被热更新替换，回收旧上下文并重新获取当前 Lazy。
    /// </para>
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

        // 循环重试——热更新替换 Lazy 时重新获取。
        while (true)
        {
            // 已配置但尚未实例化的应用：触发 Lazy 创建
            if (_lazyContexts.TryGetValue(appKey, out var lazy))
            {
                FeishuAppContext? createdContext = null;
                try
                {
                    var context = lazy.Value;
                    createdContext = context;

                    // 身份校验——若 Lazy 已被热更新替换，回收旧上下文并重新获取当前 Lazy。
                    if (!_lazyContexts.TryGetValue(appKey, out var currentLazy)
                        || !ReferenceEquals(currentLazy, lazy))
                    {
                        if (createdContext != null)
                        {
                            // Enqueue 已检查 _disposed，不再抛 ObjectDisposedException。
                            _retirement?.Enqueue(appKey, createdContext);
                        }
                        continue;
                    }

                    // 注册到基类字典以便后续快速查找
                    var wasRegistered = base.HasApp(appKey);
                    if (!wasRegistered)
                    {
                        // 快照查无该应用（热更新移除窗口）——确定性终态，
                        // 不注册、不重建、不触发事件，直接返回 false（保持 Try* 语义不抛异常）。
                        var config = FindConfigInSnapshot(appKey);
                        if (config == null)
                        {
                            appContext = default;
                            return false;
                        }
                        RegisterApp(appKey, context, config.IsDefault);
                    }
                    // 首次实例化后在锁外触发事件。
                    if (!wasRegistered)
                    {
                        OnAppInstantiated(appKey, context);
                    }
                    appContext = context;
                    return true;
                }
                // 与 GetOrCreateContext 一致，仅捕获可重试异常。非瞬时异常直接上抛。
                catch (Exception ex) when (IsTransientInitFailure(ex))
                {
                    // 与 GetOrCreateContext 一致，检测到 Lazy 缓存异常时重建 Lazy<> 以允许下次重试。
                    // 保持 Try* 语义：重建后仍返回 false，调用方可下次重试。
                    // 快照查无该应用时跳过重建（确定性终态，不放大资源）。
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
                    // 取消异常不重建 Lazy，保持 Try* 语义返回 false
                    appContext = default;
                    return false;
                }
                finally
                {
                    // 与 GetOrCreateContext 同构——注册失败路径上已创建的上下文
                    // 经退休队列回收，避免孤儿上下文（Scope + 管理器 + Timer）泄漏。
                    // 身份校验路径上已 Enqueue 的上下文不再重复回收。
                    if (createdContext != null && !base.HasApp(appKey) && _lazyContexts.TryGetValue(appKey, out var finalLazy) && ReferenceEquals(finalLazy, lazy))
                    {
                        // Enqueue 已检查 _disposed，不再抛 ObjectDisposedException。
                        _retirement?.Enqueue(appKey, createdContext);
                    }
                }
            }

            // 未配置的应用
            appContext = default;
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// GetAllApps 语义为"已实例化应用视图"，不隐式触发装配。
    /// 需要启动期预热全部应用请显式设置 <c>FeishuAppOptions.WarmUpAllAppsOnStartup = true</c>。
    /// </remarks>
    public override IEnumerable<IFeishuAppContext> GetAllApps()
    {
        return base.GetAllApps();
    }

    /// <summary>
    /// 获取所有已配置（但未必实例化）的应用键。不触发懒加载。
    /// </summary>
    public IReadOnlyCollection<string> ConfiguredAppKeys => _lazyContexts.Keys.ToArray();

    /// <summary>
    /// 获取所有已实例化的应用上下文。不触发懒加载。
    /// </summary>
    internal IEnumerable<IFeishuAppContext> InstantiatedApps => base.GetAllApps();

    /// <summary>
    /// 触发 AppInstantiated 事件（在锁外调用）。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <param name="context">已实例化的应用上下文</param>
    private void OnAppInstantiated(string appKey, IFeishuAppContext context)
    {
        // 不桥接官方 AppInstantiated 事件面。
        // 原因：本类已声明同名 public event（FeishuAppInstantiatedEventArgs，既有公共 API），
        // 同名遮蔽使官方事件面经本类型不可订阅；桥接会形成「双通道双发」（Mud.HttpUtils 明确警告的场景），
        // 合并事件面需变更公共事件类型——破坏已发布 API，计划 4.0 迁移。
        // 飞书内部事件语义（首滚恰好一次 + 订阅者异常隔离）与官方事件等价。
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
    /// 判断异常是否为可重试的瞬时初始化失败。
    /// </summary>
    /// <param name="ex">异常</param>
    /// <returns>true 表示可重试（重建 Lazy）；false 表示非瞬时异常（应直接上抛）</returns>
    private static bool IsTransientInitFailure(Exception ex)
    {
        // 取消异常不重建 Lazy。
        if (ex is OperationCanceledException)
            return false;

        // 快照移除是确定性终态，不是瞬时故障——禁止重建 Lazy、禁止重试循环。
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
    /// 由于采用懒加载，应用可能仅存在于 <c>_lazyContexts</c> 而尚未注册到基类 <c>_apps</c> 字典。
    /// 此时 <see cref="DefaultAppManager{TAppContext}.RemoveApp"/> 会返回 false，与"应用已知即应可移除"的语义不符。
    /// 重写后：只要应用在任一字典中存在，即返回 true；并清理两个字典中的记录。
    /// </remarks>
    public override bool RemoveApp(string appKey)
    {
        // 在移除前获取旧上下文引用以入退休队列。
        var oldContext = ResolveExistingContext(appKey);

        var wasInLazy = _lazyContexts.TryRemove(appKey, out _);
        var wasInBase = base.RemoveApp(appKey);

        // 移除默认应用后的提升必须确定性：
        // 按 _configs 快照顺序 → 兜底按 AppKey 序数排序（Ordinal），并 LogWarning 声明提升结果。
        if ((wasInLazy || wasInBase) && _defaultAppKey == appKey)
        {
            lock (_defaultAppLock)
            {
                if (_defaultAppKey == appKey)
                {
                    _defaultAppKey = null;

                    // 按配置快照顺序取第一个剩余应用
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

        // 旧上下文进入退休队列，宽限期后 Dispose（停止其 Timer）。
        if (oldContext != null && (wasInLazy || wasInBase))
        {
            _retirement?.Enqueue(appKey, oldContext);
        }

        // 应用下线后提前回收其持久化令牌（fire-and-forget）。
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
    /// 直接读取本类私有字段 _defaultAppKey，不依赖基类反射设置。
    /// 必须使用 <c>public override</c> 而非 <c>public new</c>：后者经基类或接口引用调用时
    /// 会执行基类版本（_defaultAppKey 未设置）抛异常，违背里氏替换原则。
    /// 实现为"快照读 _defaultAppKey → 无锁 GetApp"：
    /// 锁内调用 GetApp 可能触发懒加载装配，故移除该锁内调用路径，配合原子写。
    /// </remarks>
    public override IFeishuAppContext GetDefaultApp()
    {
        // 快照读 _defaultAppKey（volatile 保证可见性），无锁调用 GetApp。
        // TOCTOU 由"键要么存在要么抛明确异常"覆盖：若 RemoveApp 在读取后修改 _defaultAppKey，
        // GetApp 会抛 InvalidOperationException（应用已移除），与基类语义一致。
        var defaultKey = _defaultAppKey;
        if (string.IsNullOrEmpty(defaultKey))
            throw new InvalidOperationException("未设置默认应用。请在注册应用时设置 isDefault = true。");

        return GetApp(defaultKey!);
    }

    /// <summary>
    /// 统一到本类字段。
    /// 基类的 DefaultAppKey 属性非 virtual，使用 new 隐藏确保通过本类引用访问时走本类字段。
    /// </summary>
    public new string? DefaultAppKey => _defaultAppKey;

    /// <summary>
    /// 覆写 SetDefaultApp 统一到本类字段 + _defaultAppLock。
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
    /// 覆写 TrySetDefaultApp 统一到本类字段 + _defaultAppLock。
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
    /// AddApp 原子化与快照同步。
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

        // 在 _registryLock 内完成"检查 → 创建 → 注册 → 写入快照"原子操作。
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

                // 标记为运行时添加，热更新时据此决定是否保留。
                _runtimeAddedAppKeys.Add(config.AppKey);

                // 写入 _configs 快照（原子交换不可变数组），
                // 使热更新差异比对能感知运行时新增应用。
                var currentSnapshot = Volatile.Read(ref _configs);
                var newSnapshot = new FeishuAppConfig[currentSnapshot.Length + 1];
                Array.Copy(currentSnapshot, newSnapshot, currentSnapshot.Length);
                newSnapshot[currentSnapshot.Length] = config;
                Volatile.Write(ref _configs, newSnapshot);

                // 默认应用键写入必须持有 _defaultAppLock。
                // AddApp 是第三条默认应用入口（构造期、热更新 OnConfigurationChanged 均有 IsDefault 唯一性校验）。
                // 本入口同样需要校验：若已存在 IsDefault=true 的应用而不再校验，会出现"两个 IsDefault=true"
                // 且默认应用随运维操作漂移（下次原默认应用热更新会切回）。语义：后到者胜出，
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
    /// 以<b>受控的无作用域切换</b>把上下文切到目标应用：切换后<b>不归还</b>，返回实例的 <c>Current</c> 即目标应用
    /// （宿主需自行切回，或改用 <see cref="IAppScopeSwitcher.UseAppScope"/>）。守卫与生成实现的 <c>UseApp(appKey)</c>
    /// 逐字等价（格式校验 → 未注册授权器默认拒绝 → 授权判定）；切换写入的 <see cref="IAppContextHolder"/> 即
    /// DI 单例 <c>AsyncLocalAppContextSwitcher</c>，与生成类注入的实例相同，故切换对全部消费者可见。
    /// </remarks>
    // 泛型参数名遮蔽 IFeishuAppContext 接口（基类签名决定，无法改名），
    // 异常消息已使用 typeof(T).FullName 确保类型名称正确显示。
    // 基类成员已弃用（4.0 移除），本重写同步传播（同时消除 CS0672）。
    [Obsolete("上游 BC-30：GetWebApi 已在 Mud.HttpUtils 3.0.7 标记弃用（4.0 移除）。" +
              "请改用 DI 解析 IAppScopeSwitcher.UseAppScope(appKey)（自动归还），" +
              "或 IAppContextHolder.SwitchToApp(appKey, appManager, sp)（受控切换并保持，语义等价）。")]
    public override IFeishuAppContext GetWebApi<IFeishuAppContext>(string appKey)
    {
        var service = _serviceProvider.GetService<IFeishuAppContext>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(IFeishuAppContext).FullName}");

        // 受控的无作用域切换（完整守卫 + 立即切换 + 不归还）。
        // 底座入口为 IAppContextHolder.SwitchTo(IMudAppContext)，
        // 授权守卫需由调用方显式完成（见 FeishuServiceCollectionExtensions 中「未注册即默认拒绝」的约定）。
        var holder = _serviceProvider.GetRequiredService<IAppContextHolder>();

        // 未注册 IAppAccessAuthorizer 时默认拒绝。本 SDK 默认注册「注册表白名单」授权器
        // （AppKeyAllowListAuthorizer.FromPredicate + IFeishuAppManager.HasApp）。
        var authorizer = _serviceProvider.GetService<IAppAccessAuthorizer>()
            ?? throw new InvalidOperationException(
                $"未注册 {nameof(IAppAccessAuthorizer)}，无法切换到指定应用上下文（默认拒绝）。" +
                $"请注册 {nameof(IAppAccessAuthorizer)} 实现（默认注册已含 AppKeyAllowListAuthorizer；宿主可替换为绑定租户/用户主体的授权器）。");

        if (!authorizer.CanSwitchTo(appKey))
            throw new InvalidOperationException($"当前 {nameof(IAppAccessAuthorizer)} 不允许切换到应用：{appKey}");

        // 未知 appKey 由 GetApp 校验并抛错；此处立即切换且不归还上下文。
        holder.SwitchTo(GetApp(appKey));

        return service;
    }

    /// <summary>
    /// 获取默认应用的飞书API实例
    /// </summary>
    /// <remarks>
    /// 重写基类方法：从 DI 容器获取已注册的服务，并切换到默认应用上下文（同样为<b>无作用域</b>语义）。
    /// <para>
    /// 语义为 <c>IAppContextHolder.SwitchTo(GetDefaultApp())</c> —— 用「本类单例」解析默认应用
    /// 后写入同一 Holder，与生成实现的 <c>UseDefaultApp()</c> 等价。
    /// 默认应用路径<b>不做</b> appKey 格式校验与授权判定（无 appKey 输入），与生成实现一致。
    /// </para>
    /// <para>
    /// 本成员已弃用（上游 4.0 移除），签名与行为在本大版本内不变。迁移路径：DI 解析
    /// <see cref="IAppScopeSwitcher"/> + <c>UseDefaultAppScope()</c>（自动归还），
    /// 或 <c>IAppContextHolder.SwitchToApp(appKey, appManager, sp)</c>（受控切换并保持）。
    /// </para>
    /// </remarks>
    // 与 GetWebApi 同理，泛型参数名由基类决定。
    // 基类成员已弃用（4.0 移除），本重写同步传播（同时消除 CS0672）。
    [Obsolete("上游 BC-30：GetDefaultWebApi 已在 Mud.HttpUtils 3.0.7 标记弃用（4.0 移除）。" +
              "请改用 DI 解析 IAppScopeSwitcher.UseDefaultAppScope()（自动归还），" +
              "或 IAppContextHolder.SwitchToApp(appKey, appManager, sp)（受控切换并保持，语义等价）。")]
    public override IFeishuAppContext GetDefaultWebApi<IFeishuAppContext>()
    {
        var service = _serviceProvider.GetService<IFeishuAppContext>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(IFeishuAppContext).FullName}");

        // 默认应用路径不做 appKey 校验与授权判定（无 appKey 输入），与生成实现 UseDefaultApp 一致；
        // 立即切换且不归还上下文。
        _serviceProvider.GetRequiredService<IAppContextHolder>().SwitchTo(GetDefaultApp());

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

    // 覆盖隐藏基类方法，直接抛 NotSupportedException，
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

    // 此显式接口实现委托给基类 RegisterSwitcherFactory，但 FeishuAppManager.GetWebApi
    // 重写为 DI 解析模式，不使用工厂委托。保留此方法仅为接口兼容性，注册的工厂不会被 GetWebApi 调用。
    // 通过 [Obsolete] 明确告知调用方此路径不推荐，统一使用 DI 解析模式。
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
        // 装配失败时释放 scope，避免 IServiceScope 泄漏。
        // 成功后所有权转移给 FeishuAppContext。
        var scope = _scopeFactory?.CreateScope();
        var scopedSp = scope?.ServiceProvider ?? _serviceProvider;

        try
        {
            // 客户端装配统一委托给 IFeishuHttpClientFactory，
            // 消除与 AddMudHttpClient 注册路径的配置双源（散装手工 new 会导致 10 个 EnhancedHttpClientOptions
            // 字段静默取默认值，且完全忽略 IOptions<EnhancedHttpClientOptions> 基线）。
            // 使用 GetService 而非 GetRequiredService：IFeishuHttpClientFactory 是装配收敛的可选服务，
            // 直接手工构造 FeishuAppManager（未经 AddFeishuApp）的场景下回退到默认实现，
            // 避免把新增依赖变成破坏性变更。
            // 使用 scopedSp 解析服务，确保 Scoped 依赖在作用域内解析：
            // Singleton 注册的服务从根/作用域 SP 解析结果一致；Scoped 服务仅在作用域内可安全解析。
            var httpClientFactory = scopedSp.GetService<IFeishuHttpClientFactory>()
                ?? new FeishuHttpClientFactory(scopedSp);

            // 步骤 2：创建 AuthenticationApi（使用 per-app HttpClient）
            // 认证/取令牌请求使用本应用的命名 HttpClient。
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

            // 步骤 3：创建 TokenManager（依赖 AuthenticationApi）
            // 通过 IFeishuTokenStoreFactory 获取 per-app 存储，替代 is FeishuTokenStore 类型检查。
            // 默认注册 PerAppFeishuTokenStoreFactory（per-app FeishuTokenStore 实例）；
            // Redis 等自定义存储场景注册 PerAppRedisTokenStoreFactory 返回 per-app 实例。
            var tokenStoreFactory = scopedSp.GetRequiredService<IFeishuTokenStoreFactory>();
            var (tokenStore, userTokenStore) = tokenStoreFactory.Create(config.AppKey);

            // 通过 IFeishuTokenManagerFactory 创建令牌管理器（替代直接 new TenantTokenManager(...) 的硬编码方式），
            // 使自定义 TokenManager 实现可通过注册自定义工厂接入 DI 容器。
            // 默认注册 DefaultFeishuTokenManagerFactory。
            var tokenManagerFactory = scopedSp.GetRequiredService<IFeishuTokenManagerFactory>();
            var (tenantTokenManager, appTokenManager, userTokenManager) = tokenManagerFactory.Create(
                config, authenticationApi, tokenStore, userTokenStore);

            // 步骤 4：创建恢复 HttpClient（含令牌恢复，供业务 API 使用）
            // 统一使用 IOptionsMonitor<TokenRecoveryOptions> 构造
            // （组件唯一的公共用户级构造），TokenRecoveryOptions 支持运行期热更新。
            // IOptionsMonitor 由上方 AddOptions<TokenRecoveryOptions>() 注册，缺失时 fail-fast 暴露装配错误。
            var recoveryLogger = scopedSp.GetService<ILogger<TokenRecoveryEnhancedClient>>();
            var recoveryOptionsMonitor = scopedSp.GetRequiredService<IOptionsMonitor<TokenRecoveryOptions>>();

            var recoveryExecutor = new TokenRecoveryExecutor(
                tenantTokenManager,
                userTokenManager as IUserTokenManager,
                // 不传 ICurrentUserContext 给 TokenRecoveryExecutor。
                // 组件 SR-L2 会据 ICurrentUserContext 推断用户级恢复，而飞书租户接口默认不生成
                // TokenRecoveryContext，会导致重试请求被注入用户令牌（凭据类别替换）。
                // 用户级恢复仍由组件生成的显式 TokenRecoveryContext.UserId 触发，不受影响。
                null,
                recoveryOptionsMonitor,
                recoveryLogger);

            var recoveryHttpClient = httpClientFactory.Create(config.AppKey, recoveryExecutor);

            // 步骤 5：创建应用上下文（使用恢复 HttpClient）
            // 传入 scopedSp 和 scope，使 FeishuAppContext.GetService<T>() 回退到作用域 SP，
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
            // 装配失败，释放 scope 避免泄漏。成功时所有权转移给 FeishuAppContext。
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
            // 方法名 WarnResilienceConfigMismatch 暗示 Warning 级别，日志须用 LogWarning，
            // 保证多应用弹性策略不一致的可观测性。
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
/// 应用已从配置快照移除（热更新 / <c>RemoveApp</c> 竞态窗口内的确定性终态）。
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

/// <summary>
/// 凭据变更清库计划。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToPurge"/> 是所有凭据已变更的应用键集（含未实例化应用）。
/// </para>
/// <para>
/// <see cref="WriteBackRiskKeys"/> 是 ToPurge 中「有活动旧上下文」的子集——
/// 只有这些键在退休宽限期内可能以旧凭据回写 store，提交后二次清库只对这些键做。
/// </para>
/// <para>
/// <see cref="PhasePCompleted"/> 指示预清库是否在 10s 上界内完成。
/// 未完成时，提交后二次清库覆盖全部 ToPurge（而非仅 WriteBackRiskKeys），维持「新上下文不得恢复旧凭据令牌」的语义闭环。
/// </para>
/// </remarks>
internal sealed record CredentialPurgePlan(
    IReadOnlyList<string> ToPurge,
    IReadOnlyList<string> WriteBackRiskKeys,
    bool PhasePCompleted);
