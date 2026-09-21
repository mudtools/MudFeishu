// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 默认飞书事件处理器工厂实现
/// 提供统一的飞书事件处理器管理和分发功能
/// </summary>
/// <remarks>
/// <para>
/// <b>线程安全契约</b>：注册表（<see cref="_handlers"/>）的全部读写都必须在
/// <see cref="_handlersLock"/> 下进行。若只对<b>部分</b>写入方加锁，锁将不提供任何互斥语义——
/// 未加锁的 <see cref="UnregisterHandler(string)"/> / <see cref="ClearHandlers"/> 可在
/// <see cref="GetHandlers"/> 的 <c>ToArray()</c> 快照期间修改 <see cref="List{T}"/>，
/// 造成快照内容不一致或抛 <see cref="InvalidOperationException"/>。
/// </para>
/// <para>
/// 本类型是 public 且可被宿主注册为 Singleton（SDK 内 Webhook 为 Scoped、WebSocket 为每事件新建），
/// 因此必须自行保证线程安全，而非依赖调用方的生命周期选择。
/// </para>
/// </remarks>
public class DefaultFeishuEventHandlerFactory : IFeishuEventHandlerFactory
{
    private readonly ILogger<DefaultFeishuEventHandlerFactory> _logger;
    private readonly Dictionary<string, List<IFeishuEventHandler>> _handlers;
    private readonly IFeishuEventHandler _defaultHandler;
    private readonly object _handlersLock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="handlers">事件处理器集合</param>
    /// <param name="defaultHandler">默认事件处理器</param>
    public DefaultFeishuEventHandlerFactory(
        ILogger<DefaultFeishuEventHandlerFactory> logger,
        IEnumerable<IFeishuEventHandler> handlers,
        IFeishuEventHandler defaultHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultHandler = defaultHandler ?? throw new ArgumentNullException(nameof(defaultHandler));

        _handlers = [];

        // 注册所有事件处理器
        foreach (var handler in handlers)
        {
            if (!string.IsNullOrEmpty(handler.SupportedEventType))
            {
                if (!_handlers.TryGetValue(handler.SupportedEventType, out var handlersList))
                {
                    handlersList = [];
                    _handlers[handler.SupportedEventType] = handlersList;
                }
                // P2-13：构造期重复实例去重
                if (!handlersList.Contains(handler))
                    handlersList.Add(handler);

                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("已注册事件处理器: {EventType} → {HandlerType}",
                    handler.SupportedEventType, handler.GetType().Name);
            }
        }

        var totalHandlers = _handlers.Values.Sum(list => list.Count);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("默认事件处理器工厂初始化完成，共注册 {Count} 个事件处理器，覆盖 {TypeCount} 种事件类型", totalHandlers, _handlers.Count);
    }

    /// <summary>
    /// 根据事件类型获取所有事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>事件处理器列表，如果未找到则返回包含默认处理器的列表</returns>
    public IReadOnlyList<IFeishuEventHandler> GetHandlers(string eventType)
    {
        // P1-4：空类型守卫不得依赖日志级别；空/null 一律回退默认处理器，避免 TryGetValue(null) 崩溃
        if (string.IsNullOrEmpty(eventType))
        {
            _logger.LogWarning("事件类型为空，使用默认处理器");
            return [_defaultHandler];
        }

        lock (_handlersLock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers) && handlers.Count > 0)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("找到 {Count} 个事件处理器: [{EventType}] → {HandlerNames}",
                    handlers.Count, eventType, string.Join(", ", handlers.Select(h => h.GetType().Name)));
                // P1-5：返回快照，避免 AsReadOnly 活包装在 WhenAll 枚举期间被并发修改
                return handlers.ToArray();
            }
        }
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogWarning("未找到事件类型 [{EventType}] 的处理器，使用默认处理器。已注册的事件类型: {RegisteredTypes}",
            eventType, string.Join(", ", GetRegisteredEventTypes()));
        return [_defaultHandler];
    }

    /// <summary>
    /// 根据事件类型获取第一个事件处理器（向后兼容）
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>事件处理器，如果未找到则返回默认处理器</returns>
    public IFeishuEventHandler GetHandler(string eventType)
    {
        var handlers = GetHandlers(eventType);
        return handlers.Count > 0 ? handlers[0] : _defaultHandler;
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void RegisterHandler(IFeishuEventHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        // P1-4：同上，守卫不得依赖日志级别
        if (string.IsNullOrEmpty(handler.SupportedEventType))
        {
            _logger.LogWarning("尝试注册不支持任何事件类型的处理器: {HandlerType}", handler.GetType().Name);
            return;
        }

        int countAfterRegister;
        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(handler.SupportedEventType, out var handlersList))
            {
                handlersList = [];
                _handlers[handler.SupportedEventType] = handlersList;
            }

            // P2-13：构造期/注册期重复实例去重，避免双重执行
            if (!handlersList.Contains(handler))
                handlersList.Add(handler);

            countAfterRegister = handlersList.Count;
        }

        // 修复：原实现在 {Count} 占位符上误传 handler.GetType().Name（日志文本与实际数量不符），
        // 且用 IsEnabled(Debug) 门控 Information 级日志——级别判据与日志级别不一致。
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("已注册事件处理器: {EventType}，该类型现在有 {Count} 个处理器",
                handler.SupportedEventType, countAfterRegister);
    }

    /// <summary>
    /// 取消注册指定的事件处理器
    /// </summary>
    /// <param name="handler">要取消注册的事件处理器</param>
    /// <returns>是否成功取消注册</returns>
    public bool UnregisterHandler(IFeishuEventHandler handler)
    {
        if (handler == null || string.IsNullOrEmpty(handler.SupportedEventType))
            return false;

        // P1-5：写入必须与 GetHandlers 的 ToArray() 快照互斥，否则快照期间修改 List 会产生不一致结果
        lock (_handlersLock)
        {
            if (_handlers.TryGetValue(handler.SupportedEventType, out var handlers))
            {
                var result = handlers.Remove(handler);

                if (handlers.Count == 0)
                {
                    _handlers.Remove(handler.SupportedEventType);
                }

                if (result && _logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("已取消注册事件处理器: {EventType}", handler.SupportedEventType);
                }

                return result;
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogWarning("未找到要取消注册的事件类型处理器: {EventType}", handler.SupportedEventType);
        return false;
    }

    /// <summary>
    /// 取消注册指定事件类型的所有处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>是否成功取消注册</returns>
    public bool UnregisterHandler(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
            return false;

        // P1-5：同 UnregisterHandler(IFeishuEventHandler)
        bool result;
        lock (_handlersLock)
        {
            result = _handlers.Remove(eventType);
        }

        if (result && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("已取消注册事件类型的所有处理器: {EventType}", eventType);
        }
        else if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogWarning("未找到要取消注册的事件类型处理器: {EventType}", eventType);
        }
        return result;
    }

    /// <summary>
    /// 获取所有已注册的事件类型
    /// </summary>
    /// <returns>事件类型列表</returns>
    public IReadOnlyList<string> GetRegisteredEventTypes()
    {
        // P1-5：Dictionary.Keys 的并发读与写入不是安全操作，必须与写入方共用同一把锁
        lock (_handlersLock)
        {
            return _handlers.Keys.ToArray();
        }
    }

    /// <summary>
    /// 检查是否已注册指定事件类型的处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>是否已注册</returns>
    public bool IsHandlerRegistered(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
            return false;

        lock (_handlersLock)
        {
            return _handlers.ContainsKey(eventType);
        }
    }

    /// <summary>
    /// 并行处理事件（使用所有匹配的处理器）
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task HandleEventParallelAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var handlers = GetHandlers(eventType);

        if (handlers.Count == 0 || (handlers.Count == 1 && handlers[0] == _defaultHandler))
        {
            await _defaultHandler.HandleAsync(eventData, cancellationToken);
            return;
        }

        // P2-10d：单处理器直通——跳过 WhenAll 包装（异常语义一致：直接冒泡）
        if (handlers.Count == 1)
        {
            await handlers[0].HandleAsync(eventData, cancellationToken);
            return;
        }

        var tasks = handlers.Select(handler =>
            ProcessHandlerSafely(handler, eventData, cancellationToken));

        try
        {
            await Task.WhenAll(tasks);
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("事件 {EventType} 处理完成，共 {HandlerCount} 个处理器", eventType, handlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "并行处理事件 {EventType} 时发生错误", eventType);
            throw;
        }
    }

    /// <summary>
    /// 安全地处理事件（捕获单个处理器的异常）
    /// </summary>
    /// <param name="handler">事件处理器</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task ProcessHandlerSafely(IFeishuEventHandler handler, EventData eventData, CancellationToken cancellationToken)
    {
        try
        {
            await handler.HandleAsync(eventData, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("事件处理器 {HandlerType} 成功处理事件 {EventId}",
                 handler.GetType().Name, eventData.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理器 {HandlerType} 处理事件 {EventId} 时发生错误",
                handler.GetType().Name, eventData.EventId);
            // 重新抛出异常，让调用方（FeishuEventMessageHandler）能够感知失败并正确回滚去重状态
            // 注意：Task.WhenAll 会等待所有并行任务完成，单个处理器的异常不会中断其他处理器
            throw;
        }
    }

    /// <summary>
    /// 获取所有已注册的处理器信息
    /// </summary>
    /// <returns>处理器信息字典</returns>
    public Dictionary<string, List<string>> GetHandlerInfo()
    {
        // P1-5：ToDictionary 会枚举整个注册表，必须在锁内完成（否则并发写入可能抛异常或读到半更新状态）
        lock (_handlersLock)
        {
            return _handlers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(h => h.GetType().Name).ToList()
            );
        }
    }

    /// <summary>
    /// 清除所有已注册的处理器
    /// </summary>
    public void ClearHandlers()
    {
        // P1-5：Clear 亦为写入，必须入锁；否则可与 GetHandlers 的快照并发执行
        lock (_handlersLock)
        {
            _handlers.Clear();
        }
        _logger.LogInformation("已清除所有事件处理器");
    }
}