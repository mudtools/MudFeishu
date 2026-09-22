// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.WebSocket.DataModels;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// Ping/Pong消息处理器
/// </summary>
public class PingPongMessageHandler : JsonMessageHandler
{
    private readonly Func<string, Task> _sendMessageCallback;
    // WS2-12：删除死字段 _options（构造期赋值后从未被读取；编译期无警告但属可读性负担）。
    // 构造签名保持 `FeishuWebSocketOptions options`（源兼容，未发布版本亦不构成破坏性变更）。

    /// <summary>
    /// 接收到 Pong 消息时触发的事件
    /// </summary>
    public event EventHandler? PongReceived;

    /// <summary>
    /// 初始化Ping/Pong消息处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="sendMessageCallback">发送消息回调函数</param>
    /// <remarks>
    /// WS2-12：删除 <c>FeishuWebSocketOptions options</c> 参数——它的唯一去向是一个从不被读取的
    /// 私有字段，对行为零影响。保留该参数会让调用方误以为 Ping/Pong 处理受配置影响
    /// （实际 Ping 响应构造与心跳间隔无关）。模块未发布，无源兼容包袱。
    /// </remarks>
    public PingPongMessageHandler(
        ILogger<PingPongMessageHandler> logger,
        Func<string, Task> sendMessageCallback)
        : base(logger)
    {
        _sendMessageCallback = sendMessageCallback ?? throw new ArgumentNullException(nameof(sendMessageCallback));
    }
    /// <inheritdoc/>
    public override bool CanHandle(string messageType)
    {
        var type = messageType.ToLowerInvariant();
        return type == "ping" || type == "pong";
    }
    /// <inheritdoc/>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var messageType = ExtractMessageType(message);

        if (messageType == "ping")
        {
            await HandlePingAsync(message);
        }
        else if (messageType == "pong")
        {
            await HandlePongAsync(message);
        }
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task HandlePingAsync(string message)
    {
        var pingMessage = SafeDeserialize<PingMessage>(message);
        _logger.LogDebug("收到Ping消息，时间戳: {Timestamp}", pingMessage?.Timestamp);

        // 发送Pong响应（使用毫秒级时间戳以提高精度）
        var pongMessage = new PongMessage
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var pongJson = FeishuJsonAot.Serialize(pongMessage, JsonOptions.Default);
        await _sendMessageCallback(pongJson);

        _logger.LogDebug("已发送Pong响应");
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task HandlePongAsync(string message)
    {
        var pongMessage = SafeDeserialize<PongMessage>(message);
        _logger.LogDebug("收到Pong消息，时间戳: {Timestamp}", pongMessage?.Timestamp);

        // 计算延迟（使用毫秒级时间戳，避免秒级精度丢失）
        long? latencyMs = null;
        if (pongMessage?.Timestamp > 0)
        {
            // 兼容秒级和毫秒级时间戳：秒级时间戳通常 < 10^12，毫秒级 > 10^12
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pongTimestamp = pongMessage.Timestamp;

            // 如果时间戳看起来是秒级（小于 10^12），转换为毫秒
            if (pongTimestamp < 1_000_000_000_000L)
            {
                pongTimestamp *= 1000;
            }

            latencyMs = currentTime - pongTimestamp;
        }

        _logger.LogDebug("Pong延迟: {Latency}ms", latencyMs);

        // 触发PongReceived事件，通知客户端更新最后一次Pong时间
        PongReceived?.Invoke(this, EventArgs.Empty);
    }

    private string ExtractMessageType(string message)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(message);
            if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return typeElement.GetString()?.ToLowerInvariant() ?? string.Empty;
            }
            return string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}