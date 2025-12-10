// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息路由器 - 负责将消息分发给合适的处理器
/// </summary>
public class MessageRouter
{
    private readonly ILogger<MessageRouter> _logger;
    private readonly List<IMessageHandler> _handlers;
    private readonly FeishuWebSocketOptions _options;

    /// <summary>
    /// 初始化消息路由器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">FeishuWebSocketOptions</param>
    public MessageRouter(ILogger<MessageRouter> logger, FeishuWebSocketOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlers = new List<IMessageHandler>();
        _options = options;
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    public void RegisterHandler(IMessageHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _handlers.Add(handler);
        _logger.LogDebug("已注册消息处理器: {HandlerType}", handler.GetType().Name);
    }

    /// <summary>
    /// 移除消息处理器
    /// </summary>
    public bool UnregisterHandler(IMessageHandler handler)
    {
        var removed = _handlers.Remove(handler);
        if (removed)
        {
            if (_options.EnableLogging)
            {
                _logger.LogDebug("已移除消息处理器: {HandlerType}", handler.GetType().Name);
            }
        }
        return removed;
    }

    /// <summary>
    /// 路由消息到合适的处理器
    /// </summary>
    public async Task RouteMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            if (_options.EnableLogging)
            {
                _logger.LogWarning("收到空消息，跳过路由");
            }
            return;
        }

        await RouteMessageInternalAsync(message, "Text", cancellationToken);
    }

    /// <summary>
    /// 路由从二进制消息转换而来的JSON消息到合适的处理器
    /// </summary>
    public async Task RouteBinaryMessageAsync(string jsonContent, string messageType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            if (_options.EnableLogging)
            {
                _logger.LogWarning("收到空的二进制转换消息，跳过路由");
            }
            return;
        }

        await RouteMessageInternalAsync(jsonContent, $"Binary_{messageType}", cancellationToken);
    }

    /// <summary>
    /// 内部消息路由处理
    /// </summary>
    private async Task RouteMessageInternalAsync(string message, string sourceType, CancellationToken cancellationToken)
    {
        try
        {
            // 提取消息类型
            var messageType = ExtractMessageType(message);
            if (string.IsNullOrEmpty(messageType))
            {
                _logger.LogWarning("无法提取消息类型 (来源: {SourceType}): {Message}", sourceType, message);

                return;
            }

            // 查找能处理该消息类型的处理器
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(messageType));
            if (handler == null)
            {
                _logger.LogWarning("未找到能处理消息类型 {MessageType} 的处理器 (来源: {SourceType})", messageType, sourceType);
                return;
            }
            _logger.LogDebug("将消息路由到处理器: {HandlerType} (来源: {SourceType}, 消息类型: {MessageType})",
                    handler.GetType().Name, sourceType, messageType);
            await handler.HandleAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "路由消息时发生错误 (来源: {SourceType}): {Message}", sourceType, message);
        }
    }

    /// <summary>
    /// 提取消息类型
    /// </summary>
    private string ExtractMessageType(string message)
    {
        try
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;

            // 检查是否为v2.0版本
            if (root.TryGetProperty("schema", out var schemaElement) &&
                schemaElement.GetString() == "2.0")
            {
                if (root.TryGetProperty("header", out var headerElement) &&
                    headerElement.TryGetProperty("event_type", out var eventTypeElement))
                {
                    return "event"; // v2.0主要是事件消息
                }
            }

            // v1.0版本处理
            if (root.TryGetProperty("type", out var typeElement))
            {
                return typeElement.GetString()?.ToLowerInvariant() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "解析消息JSON失败: {Message}", message);
            return string.Empty;
        }
    }
}