// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息路由器 - 负责将消息分发给合适的处理器
/// </summary>
public class MessageRouter
{
    private readonly ILogger<MessageRouter> _logger;
    private readonly List<IMessageHandler> _handlers;
    private readonly object _handlersLock = new();
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

        lock (_handlersLock)
        {
            _handlers.Add(handler);
        }
        _logger.LogDebug("已注册消息处理器: {HandlerType}", handler.GetType().Name);
    }

    /// <summary>
    /// 移除消息处理器
    /// </summary>
    public bool UnregisterHandler(IMessageHandler handler)
    {
        lock (_handlersLock)
        {
            var removed = _handlers.Remove(handler);
            if (removed)
            {
                _logger.LogDebug("已移除消息处理器: {HandlerType}", handler.GetType().Name);
            }
            return removed;
        }
    }

    /// <summary>
    /// 路由消息到合适的处理器
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task RouteMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("收到空消息，跳过路由");

            return;
        }

        await RouteMessageInternalAsync(message, "Text", cancellationToken);
    }

    /// <summary>
    /// 路由从二进制消息转换而来的JSON消息到合适的处理器
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task RouteBinaryMessageAsync(string jsonContent, string messageType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            _logger.LogWarning("收到空的二进制转换消息，跳过路由");

            return;
        }

        await RouteBinaryMessageWithResultAsync(jsonContent, messageType, cancellationToken);
    }

    /// <summary>
    /// 路由从二进制消息转换而来的JSON消息，并返回路由是否成功。
    /// </summary>
    /// <param name="jsonContent">JSON内容</param>
    /// <param name="messageType">消息类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理成功返回 true；未找到处理器、解析失败或处理器抛出异常时返回 false</returns>
    /// <remarks>
    /// P1-5 修复：<see cref="RouteBinaryMessageAsync"/> 会把处理器异常吞掉，
    /// 调用方（BinaryMessageProcessor）无法感知失败，导致 ACK 恒为 200、服务端不再重发。
    /// 本方法把失败结果回传给调用方，用于决定 ACK 的 code。
    /// <para>
    /// AOT-STRICT：本方法最终调用 <see cref="IMessageHandler.HandleAsync"/>（其实现可能使用反射式
    /// System.Text.Json 反序列化），故必须与 <see cref="RouteMessageAsync"/> 一致地携带裁剪/AOT 标注，
    /// 否则 net8+ 的 <c>IL2026</c>/<c>IL3050</c> 会在 <c>AotStrictMode</c> 下升级为错误。
    /// </para>
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task<bool> RouteBinaryMessageWithResultAsync(string jsonContent, string messageType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            _logger.LogWarning("收到空的二进制转换消息，跳过路由");

            return true;
        }

        return await RouteMessageInternalWithResultAsync(jsonContent, $"Binary_{messageType}", cancellationToken);
    }

    /// <summary>
    /// 内部消息路由处理
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task RouteMessageInternalAsync(string message, string sourceType, CancellationToken cancellationToken)
    {
        await RouteMessageInternalWithResultAsync(message, sourceType, cancellationToken);
    }

    /// <summary>
    /// 内部消息路由处理（带结果）
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="sourceType">来源类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否处理成功</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task<bool> RouteMessageInternalWithResultAsync(string message, string sourceType, CancellationToken cancellationToken)
    {
        try
        {
            // 提取消息类型
            var messageType = ExtractMessageType(message);
            if (string.IsNullOrEmpty(messageType))
            {
                var truncatedMsg = LogSanitizer.CleanMessage(message, 200);
                _logger.LogWarning("无法提取消息类型 (来源: {SourceType}): {Message}", sourceType, truncatedMsg);

                return true;
            }

            // 查找能处理该消息类型的处理器（加锁保证线程安全，保持注册顺序）
            IMessageHandler? handler;
            lock (_handlersLock)
            {
                handler = _handlers.FirstOrDefault(h => h.CanHandle(messageType));
            }
            if (handler == null)
            {
                _logger.LogWarning("未找到能处理消息类型 {MessageType} 的处理器 (来源: {SourceType})", messageType, sourceType);
                return true;
            }
            _logger.LogDebug("将消息路由到处理器: {HandlerType} (来源: {SourceType}, 消息类型: {MessageType})",
                    handler.GetType().Name, sourceType, messageType);
            return await HandleWithTimeoutAsync(handler, message, cancellationToken);
        }
        catch (Exception ex)
        {
            var truncatedMsg = LogSanitizer.CleanMessage(message, 200);
            _logger.LogError(ex, "路由消息时发生错误 (来源: {SourceType}): {Message}", sourceType, truncatedMsg);
            return false;
        }
    }

    /// <summary>
    /// 带超时控制的消息处理
    /// <para>当消息处理器执行时间超过配置的 MessageHandlerTimeoutMs 时，取消处理并记录警告</para>
    /// <para>设为 0 表示不限制超时</para>
    /// </summary>
    /// <param name="handler">消息处理器</param>
    /// <param name="message">消息内容</param>
    /// <param name="cancellationToken">外部取消令牌</param>
    /// <returns>处理成功返回 true；超时或处理器抛出异常返回 false</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Calls Mud.Feishu.WebSocket.IMessageHandler.HandleAsync(String, CancellationToken)")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls Mud.Feishu.WebSocket.IMessageHandler.HandleAsync(String, CancellationToken)")]
#endif
    private async Task<bool> HandleWithTimeoutAsync(IMessageHandler handler, string message, CancellationToken cancellationToken)
    {
        var timeoutMs = _options.MessageHandlerTimeoutMs;

        // 超时设为 0 时不限制
        if (timeoutMs <= 0)
        {
            await handler.HandleAsync(message, cancellationToken);
            return true;
        }

        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var handlerTask = handler.HandleAsync(message, linkedCts.Token);
            // P1-1 修复（WS-05）：此前 Task.Delay 使用外部 cancellationToken 而非 linkedCts.Token，
            // 处理器完成后 Delay 不会被取消，每条消息泄漏一个定时器直到超时。
            // 现使用 linkedCts.Token：处理器正常完成时通过 finally 取消 timeoutCts，
            // Delay 提前结束，定时器即时回收。
            var delayTask = Task.Delay(timeoutMs, linkedCts.Token);
            var completed = await Task.WhenAny(handlerTask, delayTask);

            if (completed != handlerTask)
            {
                // P2-10 修复：外部取消时 Task.Delay 会提前结束，此前沿用同一分支
                // 会误报"消息处理器超时"。这里区分两种情形。
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("消息处理被外部取消: {HandlerType}", handler.GetType().Name);
                    return false;
                }

                // 超时，取消处理器
                timeoutCts.Cancel();
                var truncatedMsg = LogSanitizer.CleanMessage(message, 200);
                _logger.LogWarning("消息处理器超时 ({TimeoutMs}ms): {HandlerType}, 消息类型可能为: {Message}",
                    timeoutMs, handler.GetType().Name, truncatedMsg);

                // 等待处理器响应取消（短超时避免无限阻塞）
                try
                {
                    var graceCompleted = await Task.WhenAny(handlerTask, Task.Delay(TimeSpan.FromSeconds(2)));
                    if (graceCompleted != handlerTask)
                    {
                        _logger.LogWarning("消息处理器未响应取消请求（2秒），可能仍在运行: {HandlerType}", handler.GetType().Name);
                    }
                }
                catch
                {
                    // 处理器未响应取消，忽略
                }

                // P2-9 修复：超时分支此前不观察 handlerTask。处理器若在超时之后才抛出，
                // 该异常无人观察（UnobservedTaskException：表现为进程退出期噪声日志且难以定位）。
                // 这里只对"故障"挂一个观察型续延，不改变返回值语义。
                _ = handlerTask.ContinueWith(
                    t => _logger.LogDebug(t.Exception,
                        "已被判定超时的消息处理器随后抛出异常（已观察，不影响调用方）: {HandlerType}",
                        handler.GetType().Name),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                return false;
            }

            // 正常完成，传播可能的异常
            await handlerTask;
            return true;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("消息处理器被超时取消: {HandlerType}", handler.GetType().Name);
            return false;
        }
        finally
        {
            // P1-1 修复：显式取消 timeoutCts，确保 Task.Delay 提前结束，定时器即时回收。
            // linkedCts 的 Dispose 会传播取消，但显式 Cancel 更确定。
            timeoutCts.Cancel();
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
                var typeValue = typeElement.GetString()?.ToLowerInvariant() ?? string.Empty;
                // v1.0版本的event_callback也映射为"event"
                if (typeValue == "event_callback")
                {
                    return "event";
                }
                return typeValue;
            }

            return string.Empty;
        }
        catch (System.Text.Json.JsonException ex)
        {
            // 记录更详细的JSON解析错误信息，便于排查问题
            // P2-4：截断改走 LogSanitizer.CleanMessage（先剥离 token/encrypt 等敏感字段值再截断）
            var truncatedMessage = LogSanitizer.CleanMessage(message, 500);
            _logger.LogError(ex, "解析消息JSON失败，消息长度: {Length}, 消息前500字符: {Message}, 错误位置: {ErrorPosition}",
                message.Length, truncatedMessage, ex.BytePositionInLine);
            return string.Empty;
        }
    }
}