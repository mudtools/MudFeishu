// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;

namespace Mud.Feishu.Webbook.Services;

/// <summary>
/// 默认飞书事件处理器工厂实现
/// </summary>
public class DefaultFeishuEventHandlerFactory : IFeishuEventHandlerFactory
{
    private readonly ILogger<DefaultFeishuEventHandlerFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, List<IFeishuEventHandler>> _eventHandlers;
    private readonly DefaultFeishuEventHandler _defaultHandler;
    private readonly object _lock = new();

    public DefaultFeishuEventHandlerFactory(
        ILogger<DefaultFeishuEventHandlerFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventHandlers = new Dictionary<string, List<IFeishuEventHandler>>();
        _defaultHandler = new DefaultFeishuEventHandler();
        
        // 初始化时自动注册所有已注册的处理器
        InitializeHandlers();
    }

    /// <inheritdoc />
    public IFeishuEventHandler GetHandler(string eventType)
    {
        lock (_lock)
        {
            if (_eventHandlers.TryGetValue(eventType, out var handlers) && handlers.Count > 0)
            {
                return handlers[0]; // 返回第一个处理器
            }
        }

        _logger.LogWarning("未找到事件类型 {EventType} 的处理器，使用默认处理器", eventType);
        return _defaultHandler;
    }

    /// <inheritdoc />
    public IReadOnlyList<IFeishuEventHandler> GetHandlers(string eventType)
    {
        lock (_lock)
        {
            if (_eventHandlers.TryGetValue(eventType, out var handlers) && handlers.Count > 0)
            {
                return handlers.AsReadOnly();
            }
        }

        _logger.LogWarning("未找到事件类型 {EventType} 的处理器，返回默认处理器", eventType);
        return new List<IFeishuEventHandler> { _defaultHandler };
    }

    /// <inheritdoc />
    public async Task HandleEventParallelAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var handlers = GetHandlers(eventType);
        var tasks = handlers.Select(handler => HandleEventSafely(handler, eventData, cancellationToken));

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("事件 {EventType} 处理完成，共 {HandlerCount} 个处理器", eventType, handlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "并行处理事件 {EventType} 时发生错误", eventType);
            throw;
        }
    }

    /// <inheritdoc />
    public void RegisterHandler(IFeishuEventHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (string.IsNullOrEmpty(handler.SupportedEventType))
        {
            throw new ArgumentException("事件处理器必须指定支持的 SuerEventType", nameof(handler));
        }

        lock (_lock)
        {
            if (!_eventHandlers.TryGetValue(handler.SupportedEventType, out var handlers))
            {
                handlers = new List<IFeishuEventHandler>();
                _eventHandlers[handler.SupportedEventType] = handlers;
            }

            handlers.Add(handler);
        }

        _logger.LogInformation("注册事件处理器: {EventType} -> {HandlerType}", 
            handler.SupportedEventType, handler.GetType().Name);
    }

    /// <inheritdoc />
    public bool UnregisterHandler(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
        {
            return false;
        }

        lock (_lock)
        {
            if (_eventHandlers.Remove(eventType))
            {
                _logger.LogInformation("取消注册事件类型 {EventType} 的所有处理器", eventType);
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRegisteredEventTypes()
    {
        lock (_lock)
        {
            return _eventHandlers.Keys.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public bool IsHandlerRegistered(string eventType)
    {
        lock (_lock)
        {
            return _eventHandlers.ContainsKey(eventType);
        }
    }

    /// <summary>
    /// 安全地处理事件
    /// </summary>
    private async Task HandleEventSafely(IFeishuEventHandler handler, EventData eventData, CancellationToken cancellationToken)
    {
        try
        {
            await handler.HandleAsync(eventData, cancellationToken);
            _logger.LogDebug("事件处理器 {HandlerType} 成功处理事件 {EventId}", 
                handler.GetType().Name, eventData.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理器 {HandlerType} 处理事件 {EventId} 时发生错误", 
                handler.GetType().Name, eventData.EventId);
            
            // 这里可以根据配置决定是否继续处理其他处理器
            // 当前实现是记录错误但不中断其他处理器的执行
        }
    }

    /// <summary>
    /// 初始化所有已注册的事件处理器
    /// </summary>
    private void InitializeHandlers()
    {
        try
        {
            // 从服务容器中获取所有 IFeishuEventHandler 实现
            var handlers = _serviceProvider.GetServices<IFeishuEventHandler>();
            
            foreach (var handler in handlers)
            {
                if (handler != null && !string.IsNullOrEmpty(handler.SupportedEventType))
                {
                    RegisterHandler(handler);
                }
                else
                {
                    _logger.LogWarning("发现无效的事件处理器: {HandlerType}", handler?.GetType().Name);
                }
            }

            _logger.LogInformation("事件处理器工厂初始化完成，共注册 {HandlerCount} 个处理器", _eventHandlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化事件处理器时发生错误");
        }
    }

    /// <summary>
    /// 获取所有已注册的处理器信息
    /// </summary>
    /// <returns>处理器信息字典</returns>
    public Dictionary<string, List<string>> GetHandlerInfo()
    {
        lock (_lock)
        {
            return _eventHandlers.ToDictionary(
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
        lock (_lock)
        {
            _eventHandlers.Clear();
            _logger.LogInformation("已清除所有事件处理器");
        }
    }
}