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
    private readonly ILogger<FeishuAppManager> _logger;
    private readonly ConcurrentDictionary<string, Lazy<FeishuAppContext>> _lazyContexts = new();
    private readonly List<FeishuAppConfig> _configs;
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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configs = (configs as IList<FeishuAppConfig> ?? configs.ToList()).ToList();

        if (_configs.Count == 0)
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
            _configs.Count, defaultConfig.AppKey);

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

            // 节流：与当前快照完全一致（无新增/删除/更新）时不做任何重建。
            if (IsSameAsCurrentSnapshot(incoming))
            {
                return;
            }

            var incomingKeys = new HashSet<string>(incoming.Select(c => c.AppKey), StringComparer.Ordinal);
            var currentKeys = _configs.Select(c => c.AppKey).ToList();

            // 1) 删除已下线应用
            foreach (var removedKey in currentKeys.Where(k => !incomingKeys.Contains(k)).ToList())
            {
                if (RemoveApp(removedKey))
                {
                    _logger.LogInformation("配置热更新：已移除应用 {AppKey}", removedKey);
                }
            }

            // 2) 新增 / 更新
            foreach (var config in incoming)
            {
                if (HasApp(config.AppKey))
                {
                    RebuildAppContext(config);
                    _logger.LogInformation("配置热更新：已重建应用 {AppKey}", config.AppKey);
                }
                else
                {
                    AddApp(config);
                    _logger.LogInformation("配置热更新：已新增应用 {AppKey}", config.AppKey);
                }
            }

            // 3) 同步配置快照
            lock (_defaultAppLock)
            {
                _configs.Clear();
                _configs.AddRange(incoming);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "飞书应用配置热更新失败，已保持原配置继续运行。请检查新的应用配置是否合法（AppKey/AppId/AppSecret 校验）。");
        }
    }

    /// <summary>
    /// 判断配置快照是否与当前一致（用于节流，避免每次变更通知都重建上下文）。
    /// </summary>
    private bool IsSameAsCurrentSnapshot(List<FeishuAppConfig> incoming)
    {
        List<FeishuAppConfig> current;
        lock (_defaultAppLock)
        {
            current = _configs.ToList();
        }

        if (current.Count != incoming.Count)
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

            // 只比较影响运行时的关键字段；AppSecret 用序数比较（不记录日志）。
            if (!string.Equals(existing.AppId, config.AppId, StringComparison.Ordinal) ||
                !string.Equals(existing.AppSecret, config.AppSecret, StringComparison.Ordinal) ||
                !string.Equals(existing.BaseUrl, config.BaseUrl, StringComparison.Ordinal) ||
                existing.TimeOut != config.TimeOut ||
                existing.IsDefault != config.IsDefault)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 用新配置重建指定应用的上下文。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 旧上下文<b>不立即 Dispose</b>（与 <c>DefaultAppManager.RemoveApp</c> 的 NEW-MA-01 语义一致），
    /// 避免在途请求抛 <c>ObjectDisposedException</c>；旧实例由 GC 回收。
    /// </para>
    /// <para>
    /// 令牌热迁移：per-app 存储键含 appKey 维度（<c>feishu:{appKey}:token:*</c>），
    /// 存储后端（MemoryCache / Redis）独立于上下文实例，重建后可从既有令牌恢复，无需额外逻辑。
    /// </para>
    /// </remarks>
    private void RebuildAppContext(FeishuAppConfig config)
    {
        config.Validate();

        FeishuAppContext context;
        lock (_lazyRebuildLock)
        {
            _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(
                () => CreateAppContext(config),
                LazyThreadSafetyMode.ExecutionAndPublication);

            // 立即实例化以便触发 Updated 事件；CreateAppContext 为纯内存装配（无网络往返）。
            context = _lazyContexts[config.AppKey].Value;
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
    /// 释放配置变更订阅。
    /// </summary>
    public void Dispose()
    {
        _configReloadSubscription?.Dispose();
        _configReloadSubscription = null;
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
                if (!base.HasApp(appKey))
                {
                    var config = _configs.First(c => c.AppKey == appKey);
                    RegisterApp(appKey, context, config.IsDefault);
                }
                return context;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // NEW-MA-08 修复：Lazy<ExecutionAndPublication> 缓存异常后，后续 .Value 访问会重新抛出同一异常。
                // 此处重建 Lazy<> 以允许下次调用重试初始化（如 Redis 短暂故障恢复后可自愈）。
                // 使用双检锁避免并发线程同时重建：仅当字典中仍是原 Lazy 实例时才重建。
                lock (_lazyRebuildLock)
                {
                    if (_lazyContexts.TryGetValue(appKey, out var current) && ReferenceEquals(current, lazy))
                    {
                        var config = _configs.First(c => c.AppKey == appKey);
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
                if (!base.HasApp(appKey))
                {
                    var config = _configs.First(c => c.AppKey == appKey);
                    RegisterApp(appKey, context, config.IsDefault);
                }
                appContext = context;
                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // NEW-MA-08 修复：与 GetOrCreateContext 一致，检测到 Lazy 缓存异常时重建 Lazy<> 以允许下次重试。
                // 保持 Try* 语义：重建后仍返回 false，调用方可下次重试。
                lock (_lazyRebuildLock)
                {
                    if (_lazyContexts.TryGetValue(appKey, out var current) && ReferenceEquals(current, lazy))
                    {
                        var config = _configs.First(c => c.AppKey == appKey);
                        var capturedConfig = config;
                        _lazyContexts[appKey] = new Lazy<FeishuAppContext>(
                            () => CreateAppContext(capturedConfig),
                            LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                }
                appContext = default;
                return false;
            }
            catch
            {
                // InvalidOperationException 或其他预期异常：保持 Try* 语义返回 false
                appContext = default;
                return false;
            }
        }

        // 未配置的应用
        appContext = default;
        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<IFeishuAppContext> GetAllApps()
    {
        // 强制创建所有预配置的应用，确保它们被注册到基类字典中
        foreach (var appKey in _lazyContexts.Keys)
        {
            GetOrCreateContext(appKey);
        }
        return base.GetAllApps();
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：由于采用懒加载，应用可能仅存在于 <c>_lazyContexts</c> 而尚未注册到基类 <c>_apps</c> 字典。
    /// 此时 <see cref="DefaultAppManager{TAppContext}.RemoveApp"/> 会返回 false，与"应用已知即应可移除"的语义不符。
    /// 重写后：只要应用在任一字典中存在，即返回 true；并清理两个字典中的记录。
    /// </remarks>
    public override bool RemoveApp(string appKey)
    {
        var wasInLazy = _lazyContexts.TryRemove(appKey, out _);
        var wasInBase = base.RemoveApp(appKey);

        // A-1 修复：若移除的是当前默认应用，清空本类默认应用键
        // MA-03 修复：移除默认应用后自动提升第一个剩余应用为新默认，避免状态不一致窗口。
        // 若无剩余应用则保持 _defaultAppKey = null，下次 GetDefaultApp 会抛出明确异常。
        // NEW-MA-09 修复：使用 _defaultAppLock 保护"清空+提升"复合操作，避免与 GetDefaultApp 的"读取+GetApp"竞态。
        if ((wasInLazy || wasInBase) && _defaultAppKey == appKey)
        {
            lock (_defaultAppLock)
            {
                // 双检锁：进入锁后再次确认 _defaultAppKey 仍是 appKey（可能已被其他线程提升）
                if (_defaultAppKey == appKey)
                {
                    _defaultAppKey = null;
                    var firstRemaining = _lazyContexts.Keys.FirstOrDefault();
                    if (firstRemaining != null)
                    {
                        _defaultAppKey = firstRemaining;
                    }
                    else
                    {
                        // 懒加载字典已空，检查基类字典是否有运行时添加但未走懒加载的应用
                        var baseFirst = base.GetAllApps().FirstOrDefault();
                        if (baseFirst != null)
                            _defaultAppKey = baseFirst.Config.AppKey;
                    }
                }
            }
        }

        return wasInLazy || wasInBase;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A-1 修复：不再依赖基类反射设置的 _defaultAppKey，直接读取本类私有字段。
    /// MA-01 修复：原实现使用 <c>public new</c> 隐藏基类虚方法，通过基类或接口引用调用时
    /// 会执行基类版本（_defaultAppKey 未设置）抛异常，违背里氏替换原则。
    /// 现改为 <c>public override</c> 保持多态一致性。
    /// </remarks>
    public override IFeishuAppContext GetDefaultApp()
    {
        // NEW-MA-09 修复：使用 _defaultAppLock 保护"读取 key → GetApp(key)"的 TOCTOU 区间，
        // 防止读取 defaultKey 后另一线程通过 RemoveApp 将其置为 null 或提升为其他应用。
        // volatile 读取保证可见性，锁保证复合操作的原子性。
        string? defaultKey;
        lock (_defaultAppLock)
        {
            defaultKey = _defaultAppKey;
            if (string.IsNullOrEmpty(defaultKey))
                throw new InvalidOperationException("未设置默认应用。请在注册应用时设置 isDefault = true。");

            // 在锁内调用 GetApp，确保 defaultKey 对应的应用在解析期间不会被 RemoveApp 移除。
            // 注意：GetApp 内部走懒加载路径，不会反向获取 _defaultAppLock，无死锁风险。
            return GetApp(defaultKey!);
        }
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
    public IFeishuAppContext AddApp(FeishuAppConfig config)
    {
        config.Validate();

        if (HasApp(config.AppKey))
            throw new InvalidOperationException($"应用 {config.AppKey} 已存在");

        var context = CreateAppContext(config);
        RegisterApp(config.AppKey, context, config.IsDefault);
        // 同步注册到懒加载字典，使 GetAllApps 能正确枚举
        _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(() => context);

        // A-1 修复：若新应用标记为默认，更新本类的默认应用键
        // NEW-MA-09 修复：使用 _defaultAppLock 保护 _defaultAppKey 的写入，与 GetDefaultApp/RemoveApp 保持一致。
        if (config.IsDefault)
        {
            lock (_defaultAppLock)
            {
                _defaultAppKey = config.AppKey;
            }
        }

        return context;
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
        var currentUserContext = _serviceProvider.GetService<IFeishuCurrentUserContext>();

        // ARC-2 Step 1：客户端装配统一委托给 IFeishuHttpClientFactory，
        // 消除与 AddMudHttpClient 注册路径的配置双源（此前手工 new 会导致 10 个 EnhancedHttpClientOptions
        // 字段静默取默认值，且完全忽略 IOptions<EnhancedHttpClientOptions> 基线）。
        // DED-1：同时删除此前声明后未使用的 jsonSerializerOptions / basicHttpClient 两个死变量。
        // 使用 GetService 而非 GetRequiredService：IFeishuHttpClientFactory 是装配收敛的可选服务，
        // 直接手工构造 FeishuAppManager（未经 AddFeishuApp）的场景下回退到默认实现，
        // 避免把新增依赖变成破坏性变更。
        var httpClientFactory = _serviceProvider.GetService<IFeishuHttpClientFactory>()
            ?? new FeishuHttpClientFactory(_serviceProvider);

        // === 步骤 2：创建 AuthenticationApi（使用基础 HttpClient） ===
        // N-01 修复：改用 DI 解析，消除 ActivatorUtilities.CreateInstance 的 "new" 调用编译限制
        var authenticationApi = _serviceProvider.GetRequiredService<IFeishuAuthentication>();

        // === 步骤 3：创建 TokenManager（依赖 AuthenticationApi） ===
        // S-3 修复：通过 IFeishuTokenStoreFactory 替代 is FeishuTokenStore 类型检查。
        // 默认注册 PerAppFeishuTokenStoreFactory（per-app FeishuTokenStore 实例）；
        // Redis 等自定义存储场景注册 SingletonFeishuTokenStoreFactory 返回 DI 单例。
        var tokenStoreFactory = _serviceProvider.GetRequiredService<IFeishuTokenStoreFactory>();
        var (tokenStore, userTokenStore) = tokenStoreFactory.Create(config.AppKey);

        // MA-02 修复：通过 IFeishuTokenManagerFactory 替代直接 new TenantTokenManager(...) 的硬编码方式，
        // 使自定义 TokenManager 实现可通过注册自定义工厂接入 DI 容器。
        // 默认注册 DefaultFeishuTokenManagerFactory，行为与原实现完全一致。
        var tokenManagerFactory = _serviceProvider.GetRequiredService<IFeishuTokenManagerFactory>();
        var (tenantTokenManager, appTokenManager, userTokenManager) = tokenManagerFactory.Create(
            config, authenticationApi, tokenStore, userTokenStore);

        // === 步骤 4：创建恢复 HttpClient（含令牌恢复，供业务 API 使用） ===
        var recoveryOptions = _serviceProvider.GetService<IOptions<TokenRecoveryOptions>>()?.Value;
        var recoveryLogger = _serviceProvider.GetService<ILogger<TokenRecoveryEnhancedClient>>();

        var recoveryExecutor = new TokenRecoveryExecutor(
            tenantTokenManager,
            userTokenManager as IUserTokenManager,
            currentUserContext as ICurrentUserContext,
            recoveryOptions,
            recoveryLogger);

        var recoveryHttpClient = httpClientFactory.Create(config.AppKey, recoveryExecutor);

        // === 步骤 5：创建应用上下文（使用恢复 HttpClient） ===
        return new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            recoveryHttpClient,
            _serviceProvider);
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
