// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 事件消息处理器
/// </summary>
public class EventMessageHandler : JsonMessageHandler
{
    private readonly IFeishuEventHandlerFactory _eventHandlerFactory;
    private readonly FeishuWebSocketOptions _options;
    private readonly Func<string?, string?, CancellationToken, Task> _sendAckCallback;

    public EventMessageHandler(
        ILogger<EventMessageHandler> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        FeishuWebSocketOptions options,
        Func<string?, string?, CancellationToken, Task> sendAckCallback)
        : base(logger)
    {
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sendAckCallback = sendAckCallback ?? throw new ArgumentNullException(nameof(sendAckCallback));
    }

    public override bool CanHandle(string messageType)
    {
        return messageType.ToLowerInvariant() == "event";
    }

    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // 检测消息版本并解析
            var messageInfo = ParseMessageVersion(message);
            if (messageInfo == null)
            {
                _logger.LogWarning("无法解析事件消息: {Message}", message);
                return;
            }

            // 触发事件接收事件
            await TriggerEventReceivedAsync(message, messageInfo);

            // 处理事件 - 同时支持v1.0和v2.0版本
            EventData? eventData = null;
            if (!messageInfo.IsV2 && messageInfo.EventMessage?.Data != null)
            {
                // v1.0版本
                eventData = messageInfo.EventMessage.Data;
            }
            else if (messageInfo.IsV2 && !string.IsNullOrEmpty(messageInfo.EventType))
            {
                // v2.0版本 - 需要从原始消息中提取事件数据
                eventData = ExtractV2EventData(messageInfo.RawMessage, messageInfo.EventType);
            }

            if (eventData != null)
            {
                await ProcessEventAsync(eventData, cancellationToken);
            }

            // 发送ACK确认
            if (_options.EnableAutoAck)
            {
                await _sendAckCallback(messageInfo.EventType, messageInfo.EventId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理事件消息时发生错误");
        }
    }

    /// <summary>
    /// 解析消息版本和内容
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns>解析结果</returns>
    private MessageInfo? ParseMessageVersion(string message)
    {
        using var jsonDoc = JsonDocument.Parse(message);
        var root = jsonDoc.RootElement;

        // 检查是否为v2.0版本
        if (root.TryGetProperty("schema", out var schemaElement) &&
            schemaElement.GetString() == "2.0")
        {
            string? eventType = null;
            string? eventId = null;

            if (root.TryGetProperty("header", out var headerElement))
            {
                headerElement.TryGetProperty("event_type", out var eventTypeElement);
                headerElement.TryGetProperty("event_id", out var eventIdElement);

                eventType = eventTypeElement.ValueKind != JsonValueKind.Undefined ? eventTypeElement.GetString() : null;
                eventId = eventIdElement.ValueKind != JsonValueKind.Undefined ? eventIdElement.GetString() : null;
            }

            return new MessageInfo
            {
                IsV2 = true,
                EventType = eventType,
                EventId = eventId,
                RawMessage = message
            };
        }

        // v1.0版本处理
        if (root.TryGetProperty("type", out var typeElement) &&
            typeElement.GetString()?.ToLowerInvariant() == "event")
        {
            var eventMessage = SafeDeserialize<EventMessage>(message);
            if (eventMessage != null)
            {
                string? eventId = null;
                if (root.TryGetProperty("uuid", out var uuidElement))
                    eventId = uuidElement.GetString();

                return new MessageInfo
                {
                    IsV2 = false,
                    EventMessage = eventMessage,
                    EventType = eventMessage.Data?.EventType,
                    EventId = eventId,
                    RawMessage = message
                };
            }
        }

        return null;
    }

    /// <summary>
    /// 触发事件接收事件
    /// </summary>
    private async Task TriggerEventReceivedAsync(string message, MessageInfo messageInfo)
    {
        // 简化处理，专注于事件数据处理
        await Task.CompletedTask;
    }

    /// <summary>
    /// 从v2.0消息中提取事件数据
    /// </summary>
    private EventData? ExtractV2EventData(string rawMessage, string eventType)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(rawMessage);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("data", out var dataElement))
            {
                var eventDataJson = dataElement.GetRawText();
                var eventData = SafeDeserialize<EventData>(eventDataJson);
                if (eventData != null)
                {
                    eventData.EventType = eventType; // 确保事件类型正确
                }
                return eventData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取v2.0事件数据失败: EventType={EventType}", eventType);
        }
        
        // 如果无法提取，创建基本的事件数据
        return new EventData
        {
            EventType = eventType
        };
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    private async Task ProcessEventAsync(EventData eventData, CancellationToken cancellationToken)
    {
        if (_options.EnableMultiHandlerMode)
        {
            if (_options.ParallelMultiHandlers)
            {
                await _eventHandlerFactory.HandleEventParallelAsync(
                    eventData.EventType, eventData, cancellationToken);
            }
            else
            {
                var handlers = _eventHandlerFactory.GetHandlers(eventData.EventType);
                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(eventData, cancellationToken);
                }
            }
        }
        else
        {
            var handler = _eventHandlerFactory.GetHandler(eventData.EventType);
            await handler.HandleAsync(eventData, cancellationToken);
        }
    }

    /// <summary>
    /// 消息信息
    /// </summary>
    private class MessageInfo
    {
        public bool IsV2 { get; set; }
        public EventMessage? EventMessage { get; set; }
        public string? EventType { get; set; }
        public string? EventId { get; set; }
        public string RawMessage { get; set; } = string.Empty;
    }
}