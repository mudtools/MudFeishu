// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 作用域感知的飞书事件处理器工厂。
/// </summary>
/// <remarks>
/// <para>
/// P0-7 修复：此前 <c>IFeishuEventHandlerFactory</c> 注册为 Singleton，并在工厂委托中
/// 直接从<b>根容器</b>解析注册为 <c>Scoped</c> 的 <c>IEnumerable&lt;IFeishuEventHandler&gt;</c>。
/// 这构成典型的 Captive Dependency：
/// <list type="bullet">
/// <item>在 <c>EnvironmentName=Development</c>（默认开启 <c>ValidateScopes</c>）下会抛
/// <c>InvalidOperationException: Cannot resolve scoped service ... from root provider</c>，应用启动失败；</item>
/// <item>在非 Development 下不报错，但处理器实例被单例永久持有；若处理器依赖
/// <c>DbContext</c> 等 Scoped 服务，会出现跨事件共享实例、连接泄漏与并发访问异常。</item>
/// </list>
/// </para>
/// <para>
/// 本实现：<b>事件分发路径</b>为每一次 <see cref="HandleEventParallelAsync"/> 调用创建一个
/// <see cref="IServiceScope"/>，在作用域内解析处理器，调用结束后释放作用域，
/// 从而保证 Scoped 依赖（DbContext 等）的生命周期与单个事件对齐。
/// </para>
/// <para>
/// 查询类 API（<see cref="GetHandler"/> / <see cref="GetHandlers"/> 等）用于读取
/// <see cref="IFeishuEventHandler.SupportedEventType"/> 等元数据，不涉及事件处理，
/// 复用一个惰性创建的长期作用域实例，避免每次查询都创建/释放作用域。
/// </para>
/// </remarks>
public class ScopedFeishuEventHandlerFactory : IFeishuEventHandlerFactory
{
    private readonly ILogger<ScopedFeishuEventHandlerFactory> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IReadOnlyList<Type> _handlerTypes;
    private readonly Type? _defaultHandlerType;
    private readonly object _inspectionLock = new();

    private IServiceScope? _inspectionScope;
    private DefaultFeishuEventHandlerFactory? _inspectionFactory;

    /// <summary>
    /// 初始化作用域感知的事件处理器工厂
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="scopeFactory">服务作用域工厂，用于按事件创建作用域</param>
    /// <param name="handlerTypes">通过建造者注册的事件处理器类型集合</param>
    /// <param name="defaultHandlerType">默认事件处理器类型（可选）</param>
    /// <exception cref="ArgumentNullException">当 logger 或 scopeFactory 为 null 时抛出</exception>
    public ScopedFeishuEventHandlerFactory(
        ILogger<ScopedFeishuEventHandlerFactory> logger,
        IServiceScopeFactory scopeFactory,
        IReadOnlyList<Type> handlerTypes,
        Type? defaultHandlerType = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
        _defaultHandlerType = defaultHandlerType;

        _logger.LogDebug("作用域感知事件处理器工厂已初始化，注册处理器类型 {Count} 个", _handlerTypes.Count);
    }

    /// <inheritdoc/>
    /// <remarks>返回的处理器来自元数据查询作用域，仅可用于读取
    /// <see cref="IFeishuEventHandler.SupportedEventType"/> 等信息，请勿用于实际事件处理。</remarks>
    public IFeishuEventHandler GetHandler(string eventType) => GetInspectionFactory().GetHandler(eventType);

    /// <inheritdoc/>
    /// <remarks>返回的处理器来自元数据查询作用域，仅可用于读取
    /// <see cref="IFeishuEventHandler.SupportedEventType"/> 等信息，请勿用于实际事件处理。</remarks>
    public IReadOnlyList<IFeishuEventHandler> GetHandlers(string eventType) => GetInspectionFactory().GetHandlers(eventType);

    /// <inheritdoc/>
    public void RegisterHandler(IFeishuEventHandler handler) => GetInspectionFactory().RegisterHandler(handler);

    /// <inheritdoc/>
    public bool UnregisterHandler(string eventType) => GetInspectionFactory().UnregisterHandler(eventType);

    /// <inheritdoc/>
    public bool UnregisterHandler(IFeishuEventHandler handler) => GetInspectionFactory().UnregisterHandler(handler);

    /// <inheritdoc/>
    public IReadOnlyList<string> GetRegisteredEventTypes() => GetInspectionFactory().GetRegisteredEventTypes();

    /// <inheritdoc/>
    public bool IsHandlerRegistered(string eventType) => GetInspectionFactory().IsHandlerRegistered(eventType);

    /// <summary>
    /// 并行处理事件：为本次调用创建独立作用域，在作用域内解析处理器并分发。
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task HandleEventParallelAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var scope = _scopeFactory.CreateScope();
        try
        {
            var factory = CreateFactory(scope.ServiceProvider);
            await factory.HandleEventParallelAsync(eventType, eventData, cancellationToken);
        }
        finally
        {
            // 作用域释放会连带释放本次事件解析出的 Scoped 依赖（如 DbContext）
            scope.Dispose();
        }
    }

    /// <summary>
    /// 在指定服务提供程序上构建一次性的 <see cref="DefaultFeishuEventHandlerFactory"/>。
    /// </summary>
    /// <param name="provider">服务提供程序（应为某个作用域的提供程序）</param>
    /// <returns>事件处理器工厂</returns>
    private DefaultFeishuEventHandlerFactory CreateFactory(IServiceProvider provider)
    {
        var logger = provider.GetService<ILogger<DefaultFeishuEventHandlerFactory>>()
                     ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultFeishuEventHandlerFactory>.Instance;

        var handlers = new List<IFeishuEventHandler>();
        foreach (var type in _handlerTypes)
        {
            var resolved = provider.GetService(type) as IFeishuEventHandler;
            if (resolved != null)
            {
                handlers.Add(resolved);
            }
        }

        if (handlers.Count == 0)
        {
            // 兜底：直接解析 IEnumerable，覆盖通过其它方式注册的实现
            handlers.AddRange(provider.GetServices<IFeishuEventHandler>());
        }

        IFeishuEventHandler? defaultHandler = null;
        if (_defaultHandlerType != null)
        {
            defaultHandler = provider.GetService(_defaultHandlerType) as IFeishuEventHandler;
        }
        defaultHandler ??= handlers.FirstOrDefault(h => _defaultHandlerType != null && h.GetType() == _defaultHandlerType)
                          ?? handlers.FirstOrDefault()
                          ?? new NoopFeishuEventHandler();

        return new DefaultFeishuEventHandlerFactory(logger, handlers, defaultHandler);
    }

    /// <summary>
    /// 获取用于元数据查询的长期工厂实例（惰性创建）。
    /// </summary>
    /// <returns>查询用工厂</returns>
    private DefaultFeishuEventHandlerFactory GetInspectionFactory()
    {
        if (_inspectionFactory != null)
            return _inspectionFactory;

        lock (_inspectionLock)
        {
            if (_inspectionFactory != null)
                return _inspectionFactory;

            _inspectionScope = _scopeFactory.CreateScope();
            _inspectionFactory = CreateFactory(_inspectionScope.ServiceProvider);
            return _inspectionFactory;
        }
    }

    /// <summary>
    /// 空事件处理器，仅用于"未注册任何处理器"时的兜底，避免构造工厂失败。
    /// </summary>
    internal sealed class NoopFeishuEventHandler : IFeishuEventHandler
    {
        /// <inheritdoc/>
        public string SupportedEventType => string.Empty;

        /// <inheritdoc/>
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
