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
/// Ping/Pong消息处理器
/// </summary>
public class PingPongMessageHandler : JsonMessageHandler
{
    private readonly Func<string, Task> _sendMessageCallback;
    /// <inheritdoc/>
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

    private async Task HandlePingAsync(string message)
    {
        var pingMessage = SafeDeserialize<PingMessage>(message);
        _logger.LogDebug("收到Ping消息，时间戳: {Timestamp}", pingMessage?.Timestamp);

        // 发送Pong响应
        var pongMessage = new PongMessage
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var pongJson = JsonSerializer.Serialize(pongMessage, _jsonOptions);
        await _sendMessageCallback(pongJson);

        _logger.LogDebug("已发送Pong响应");
    }

    private async Task HandlePongAsync(string message)
    {
        var pongMessage = SafeDeserialize<PongMessage>(message);
        _logger.LogDebug("收到Pong消息，时间戳: {Timestamp}", pongMessage?.Timestamp);

        // 计算延迟
        long? latencyMs = null;
        if (pongMessage?.Timestamp > 0)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            latencyMs = (currentTime - pongMessage.Timestamp) * 1000;
        }

        _logger.LogDebug("Pong延迟: {Latency}ms", latencyMs);
        await Task.CompletedTask;
    }

    private string ExtractMessageType(string message)
    {
        using var jsonDoc = JsonDocument.Parse(message);
        if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
        {
            return typeElement.GetString()?.ToLowerInvariant() ?? string.Empty;
        }
        return string.Empty;
    }
}