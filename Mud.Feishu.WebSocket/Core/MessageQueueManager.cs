// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Collections.Concurrent;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息队列管理器，负责管理WebSocket消息的队列、背压处理和并发处理
/// </summary>
public class MessageQueueManager : IDisposable
{
    private readonly ILogger<MessageQueueManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly List<Func<string, Task>> _messageProcessors = new();
    private readonly SemaphoreSlim _processingSemaphore;

    /// <summary>
    /// 错误事件，当消息队列处理发生错误时触发
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 获取当前队列中的消息数量
    /// </summary>
    public int QueueCount => _messageQueue.Count;

    /// <summary>
    /// 初始化消息队列管理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">WebSocket配置选项</param>
    public MessageQueueManager(
        ILogger<MessageQueueManager> logger,
        FeishuWebSocketOptions options)
    {
        _logger = logger;
        _options = options;
        _processingSemaphore = new SemaphoreSlim(options.MaxConcurrentMessageProcessing, options.MaxConcurrentMessageProcessing);
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    /// <param name="processor">消息处理委托</param>
    public void RegisterProcessor(Func<string, Task> processor)
    {
        _messageProcessors.Add(processor);
    }

    /// <summary>
    /// 注销消息处理器
    /// </summary>
    /// <param name="processor">要注销的消息处理委托</param>
    /// <returns>是否成功移除</returns>
    public bool UnregisterProcessor(Func<string, Task> processor)
    {
        return _messageProcessors.Remove(processor);
    }

    /// <summary>
    /// 将消息加入队列
    /// </summary>
    /// <param name="message">要加入队列的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功加入队列</returns>
    /// <remarks>
    /// 根据配置的背压策略处理队列满的情况：
    /// - DropOldest: 丢弃最旧的消息
    /// - DropNewest: 丢弃新消息
    /// - Block: 阻塞等待直到有空间或超时
    /// </remarks>
    public async Task<bool> EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableMessageQueue)
            return false;

        var enqueued = false;

        switch (_options.BackpressureStrategy)
        {
            case QueueBackpressureStrategy.DropOldest:
                if (_messageQueue.Count >= _options.MessageQueueCapacity)
                {
                    var droppedCount = 0;
                    while (_messageQueue.Count >= _options.MessageQueueCapacity && _messageQueue.TryDequeue(out _))
                    {
                        droppedCount++;
                    }

                    _logger.LogWarning("消息队列已满 (容量: {Capacity})，已丢弃 {DroppedCount} 条最旧消息以腾出空间",
                        _options.MessageQueueCapacity, droppedCount);

                    OnError(new WebSocketErrorEventArgs
                    {
                        Exception = new InvalidOperationException($"消息队列已满，丢弃了 {droppedCount} 条旧消息"),
                        ErrorMessage = $"消息队列已满，丢弃 {droppedCount} 条旧消息",
                        ErrorType = "QueueOverflowWarning",
                        IsRecoverable = true
                    });
                }
                _messageQueue.Enqueue(message);
                enqueued = true;
                break;

            case QueueBackpressureStrategy.DropNewest:
                if (_messageQueue.Count >= _options.MessageQueueCapacity)
                {
                    _logger.LogWarning("消息队列已满 (容量: {Capacity})，丢弃新消息",
                        _options.MessageQueueCapacity);

                    OnError(new WebSocketErrorEventArgs
                    {
                        Exception = new InvalidOperationException("消息队列已满，丢弃新消息"),
                        ErrorMessage = "消息队列已满，丢弃新消息",
                        ErrorType = "QueueOverflowWarning",
                        IsRecoverable = true
                    });
                }
                else
                {
                    _messageQueue.Enqueue(message);
                    enqueued = true;
                }
                break;

            case QueueBackpressureStrategy.Block:
                var startTime = DateTime.UtcNow;
                var timeoutMs = _options.BackpressureBlockTimeoutMs;

                while (_messageQueue.Count >= _options.MessageQueueCapacity)
                {
                    if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                    {
                        _logger.LogWarning("消息队列背压阻塞超时 ({Timeout}ms)，丢弃消息", timeoutMs);

                        OnError(new WebSocketErrorEventArgs
                        {
                            Exception = new TimeoutException($"消息队列背压阻塞超时 ({timeoutMs}ms)"),
                            ErrorMessage = "消息队列背压阻塞超时，丢弃消息",
                            ErrorType = "QueueBlockTimeout",
                            IsRecoverable = true
                        });
                        break;
                    }

                    await Task.Delay(10, cancellationToken);
                }

                if (_messageQueue.Count < _options.MessageQueueCapacity)
                {
                    _messageQueue.Enqueue(message);
                    enqueued = true;
                }
                break;
        }

        if (enqueued && _options.EnableLogging)
        {
            _logger.LogDebug("消息已加入队列，当前队列大小: {QueueCount}", _messageQueue.Count);
        }

        return enqueued;
    }

    /// <summary>
    /// 处理队列中的消息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 持续从队列中取出消息并分发给所有注册的处理器。
    /// 使用信号量控制并发处理数量，每处理100条消息后让出线程。
    /// </remarks>
    public async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var processedMessages = 0;
            const int maxMessagesBeforeYield = 100;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    try
                    {
                        await _processingSemaphore.WaitAsync(cancellationToken);

                        try
                        {
                            var processingTasks = _messageProcessors.Select(processor =>
                                ProcessMessageSafely(processor, message));

                            await Task.WhenAll(processingTasks);
                            processedMessages++;

                            if (processedMessages % maxMessagesBeforeYield == 0)
                            {
                                await Task.Yield();
                            }
                        }
                        finally
                        {
                            _processingSemaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理队列中的消息时发生错误: {Message}", message);
                    }
                }
                else
                {
                    await Task.Delay(_options.EmptyQueueCheckIntervalMs, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 清空消息队列
    /// </summary>
    public void Clear()
    {
        while (_messageQueue.TryDequeue(out _)) { }
    }

    /// <summary>
    /// 安全地处理单条消息
    /// </summary>
    /// <param name="processor">消息处理器</param>
    /// <param name="message">消息内容</param>
    /// <returns>异步任务</returns>
    private async Task ProcessMessageSafely(Func<string, Task> processor, string message)
    {
        try
        {
            await processor(message);
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogWarning(ex, "消息处理器执行失败: {Message}", message);
        }
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
    /// <param name="e">错误事件参数</param>
    private void OnError(WebSocketErrorEventArgs e)
    {
        Error?.Invoke(this, e);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Clear();
        _processingSemaphore?.Dispose();
    }
}
