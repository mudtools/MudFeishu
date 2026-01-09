// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.WebSocket.DataModels;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 飞书事件消息处理器 - 处理飞书WebSocket推送的事件消息
/// </summary>
public class FeishuEventMessageHandler : JsonMessageHandler
{
    private readonly IFeishuEventHandlerFactory _eventHandlerFactory;

    /// <summary>
    /// 初始化飞书事件消息处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventHandlerFactory">事件处理器工厂</param>
    public FeishuEventMessageHandler(
        ILogger<FeishuEventMessageHandler> logger,
        IFeishuEventHandlerFactory eventHandlerFactory)
        : base(logger)
    {
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
    }

    /// <inheritdoc/>
    public override bool CanHandle(string messageType)
    {
        return messageType.Equals("event", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("binary_event", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("收到空事件消息");
                return;
            }

            // 尝试解析JSON以判断版本
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;

            EventData eventData;

            // 检查是否为v2.0版本
            if (root.TryGetProperty("schema", out var schemaElement) &&
                schemaElement.GetString() == "2.0")
            {
                // v2.0版本解析
                eventData = ParseV2Event(root);
            }
            else
            {
                // v1.0版本解析
                var eventMessage = SafeDeserialize<EventMessage>(message);
                if (eventMessage?.Data == null)
                {
                    _logger.LogWarning("无法解析v1.0事件消息: {Message}", message);
                    return;
                }
                eventData = eventMessage.Data;
            }

            if (string.IsNullOrEmpty(eventData.EventType))
            {
                _logger.LogWarning("事件类型为空: {EventId}", eventData.EventId);
                return;
            }

            _logger.LogInformation("收到飞书事件: {EventType}, EventId: {EventId}",
                eventData.EventType, eventData.EventId);

            // 使用事件处理器工厂并行处理事件
            await _eventHandlerFactory.HandleEventParallelAsync(eventData.EventType, eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书事件消息时发生错误: {Message}", message);
        }
    }

    /// <summary>
    /// 解析v2.0版本的事件
    /// </summary>
    private EventData ParseV2Event(JsonElement root)
    {
        var eventData = new EventData();

        // 解析header
        if (root.TryGetProperty("header", out var headerElement))
        {
            if (headerElement.TryGetProperty("event_id", out var eventIdElement))
                eventData.EventId = eventIdElement.GetString() ?? string.Empty;

            if (headerElement.TryGetProperty("event_type", out var eventTypeElement))
                eventData.EventType = eventTypeElement.GetString() ?? string.Empty;

            if (headerElement.TryGetProperty("create_time", out var createTimeElement))
            {
                if (createTimeElement.ValueKind == JsonValueKind.String &&
                    long.TryParse(createTimeElement.GetString(), out var createTimeLong))
                {
                    eventData.CreateTime = createTimeLong / 1000; // 转换为秒
                }
                else if (createTimeElement.TryGetInt64(out var createTimeInt))
                {
                    eventData.CreateTime = createTimeInt / 1000;
                }
            }

            if (headerElement.TryGetProperty("tenant_key", out var tenantKeyElement))
                eventData.TenantKey = tenantKeyElement.GetString() ?? string.Empty;

            if (headerElement.TryGetProperty("app_id", out var appIdElement))
                eventData.AppId = appIdElement.GetString() ?? string.Empty;
        }

        // 解析event
        if (root.TryGetProperty("event", out var eventElement))
        {
            // 将event对象转换为JsonElement供后续使用
            eventData.Event = eventElement;
        }

        return eventData;
    }
}
