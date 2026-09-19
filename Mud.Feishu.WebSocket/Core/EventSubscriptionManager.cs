// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.WebSocket.DataModels;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 事件订阅管理器 - 管理WebSocket事件订阅
/// </summary>
public class EventSubscriptionManager
{
    private readonly ILogger<EventSubscriptionManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly Func<string, Task> _sendMessageCallback;
    private readonly HashSet<string> _subscribedEventTypes = new();
    private readonly object _lock = new();
    private bool _hasSubscribed = false;

    /// <summary>
    /// 订阅成功事件
    /// </summary>
    public event EventHandler<SubscriptionEventArgs>? Subscribed;

    /// <summary>
    /// 订阅失败事件
    /// </summary>
    public event EventHandler<SubscriptionEventArgs>? SubscriptionFailed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EventSubscriptionManager(
        ILogger<EventSubscriptionManager> logger,
        FeishuWebSocketOptions options,
        Func<string, Task> sendMessageCallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
        _sendMessageCallback = sendMessageCallback ?? throw new ArgumentNullException(nameof(sendMessageCallback));
    }

    /// <summary>
    /// 添加要订阅的事件类型
    /// </summary>
    public void SubscribeEvent(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("事件类型不能为空", nameof(eventType));

        lock (_lock)
        {
            _subscribedEventTypes.Add(eventType);
            _logger.LogDebug("添加订阅事件类型: {EventType}", eventType);
        }
    }

    /// <summary>
    /// 批量添加要订阅的事件类型
    /// </summary>
    public void SubscribeEvents(IEnumerable<string> eventTypes)
    {
        if (eventTypes == null)
            throw new ArgumentNullException(nameof(eventTypes));

        foreach (var eventType in eventTypes)
        {
            SubscribeEvent(eventType);
        }
    }

    /// <summary>
    /// 获取所有订阅的事件类型
    /// </summary>
    public string[] GetSubscribedEvents()
    {
        lock (_lock)
        {
            return _subscribedEventTypes.ToArray();
        }
    }

    /// <summary>
    /// 发送订阅请求
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task SendSubscriptionRequestAsync(CancellationToken cancellationToken = default)
    {
        var events = GetSubscribedEvents();

        if (events.Length == 0)
        {
            _logger.LogDebug("没有要订阅的事件类型");
            return;
        }

        try
        {
            _logger.LogInformation("发送事件订阅请求，事件类型: {EventTypes}", string.Join(", ", events));

            // P1-5 修复（WS-06）：使用具名 DTO 替代匿名类型，避免 AOT 反射依赖。
            var subscriptionMessage = new SubscriptionRequest
            {
                Type = "subscribe",
                Data = new SubscriptionRequestData
                {
                    Events = new List<string>(events)
                }
            };

            var messageJson = FeishuJsonAot.Serialize(subscriptionMessage, JsonOptions.Default);
            await _sendMessageCallback(messageJson);

            _hasSubscribed = true;

            Subscribed?.Invoke(this, new SubscriptionEventArgs
            {
                EventTypes = events,
                IsSuccess = true,
                Message = "订阅请求已发送"
            });

            _logger.LogInformation("事件订阅请求发送成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送事件订阅请求失败");

            SubscriptionFailed?.Invoke(this, new SubscriptionEventArgs
            {
                EventTypes = events,
                IsSuccess = false,
                Message = $"订阅失败: {ex.Message}"
            });

            throw;
        }
    }

    /// <summary>
    /// 清空订阅
    /// </summary>
    public void ClearSubscriptions()
    {
        lock (_lock)
        {
            _subscribedEventTypes.Clear();
            _hasSubscribed = false;
            _logger.LogInformation("已清空所有事件订阅");
        }
    }

    /// <summary>
    /// 是否已发送订阅请求
    /// </summary>
    public bool HasSubscribed => _hasSubscribed;
}

/// <summary>
/// 订阅事件参数
/// </summary>
public class SubscriptionEventArgs : EventArgs
{
    /// <summary>
    /// 事件类型列表
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
