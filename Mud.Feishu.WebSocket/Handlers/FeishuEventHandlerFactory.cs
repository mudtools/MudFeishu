// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 飞书事件处理器工厂
/// </summary>
public class FeishuEventHandlerFactory : IFeishuEventHandlerFactory
{
    private readonly ILogger<FeishuEventHandlerFactory> _logger;
    private readonly Dictionary<string, IFeishuEventHandler> _handlers;
    private readonly IFeishuEventHandler _defaultHandler;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="handlers">事件处理器集合</param>
    /// <param name="defaultHandler">默认事件处理器</param>
    public FeishuEventHandlerFactory(
        ILogger<FeishuEventHandlerFactory> logger,
        IEnumerable<IFeishuEventHandler> handlers,
        IFeishuEventHandler defaultHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultHandler = defaultHandler ?? throw new ArgumentNullException(nameof(defaultHandler));

        _handlers = new Dictionary<string, IFeishuEventHandler>();

        // 注册所有事件处理器
        foreach (var handler in handlers)
        {
            if (!string.IsNullOrEmpty(handler.SupportedEventType))
            {
                if (_handlers.ContainsKey(handler.SupportedEventType))
                {
                    _logger.LogWarning("事件类型 {EventType} 的处理器已存在，将被覆盖", handler.SupportedEventType);
                }

                _handlers[handler.SupportedEventType] = handler;
                _logger.LogDebug("已注册事件处理器: {EventType}", handler.SupportedEventType);
            }
        }

        _logger.LogInformation("事件处理器工厂初始化完成，共注册 {Count} 个事件处理器", _handlers.Count);
    }

    /// <summary>
    /// 根据事件类型获取事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>事件处理器，如果未找到则返回默认处理器</returns>
    public IFeishuEventHandler GetHandler(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
        {
            _logger.LogWarning("事件类型为空，使用默认处理器");
            return _defaultHandler;
        }

        if (_handlers.TryGetValue(eventType, out var handler))
        {
            _logger.LogDebug("找到事件处理器: {EventType}", eventType);
            return handler;
        }

        _logger.LogWarning("未找到事件类型 {EventType} 的处理器，使用默认处理器", eventType);
        return _defaultHandler;
    }

    /// <summary>
    /// 根据事件类型获取所有事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>事件处理器列表，如果未找到则返回包含默认处理器的列表</returns>
    public IReadOnlyList<IFeishuEventHandler> GetHandlers(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
        {
            _logger.LogWarning("事件类型为空，使用默认处理器");
            return new List<IFeishuEventHandler> { _defaultHandler }.AsReadOnly();
        }

        if (_handlers.TryGetValue(eventType, out var handler))
        {
            _logger.LogDebug("找到事件处理器: {EventType}", eventType);
            return new List<IFeishuEventHandler> { handler }.AsReadOnly();
        }

        _logger.LogWarning("未找到事件类型 {EventType} 的处理器，使用默认处理器", eventType);
        return new List<IFeishuEventHandler> { _defaultHandler }.AsReadOnly();
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

        var tasks = handlers.Select(handler =>
            ProcessHandlerSafely(handler, eventData, cancellationToken));

        await Task.WhenAll(tasks);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理器 {HandlerType} 执行失败: {EventType}",
                handler.GetType().Name, eventData.EventType);
            // 不重新抛出异常，避免影响其他处理器
        }
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void RegisterHandler(IFeishuEventHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (string.IsNullOrEmpty(handler.SupportedEventType))
        {
            _logger.LogWarning("尝试注册不支持任何事件类型的处理器");
            return;
        }

        _handlers[handler.SupportedEventType] = handler;
        _logger.LogInformation("已注册事件处理器: {EventType}", handler.SupportedEventType);
    }

    /// <summary>
    /// 取消注册事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>是否成功取消注册</returns>
    public bool UnregisterHandler(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
            return false;

        var result = _handlers.Remove(eventType);

        if (result)
        {
            _logger.LogInformation("已取消注册事件处理器: {EventType}", eventType);
        }
        else
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
        return _handlers.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 检查是否已注册指定事件类型的处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>是否已注册</returns>
    public bool IsHandlerRegistered(string eventType)
    {
        return !string.IsNullOrEmpty(eventType) && _handlers.ContainsKey(eventType);
    }
}