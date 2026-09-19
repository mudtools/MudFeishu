// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 二进制消息处理器 - 负责处理二进制数据的增量接收和解析
/// </summary>
public class BinaryMessageProcessor : IDisposable, IAsyncDisposable
{
    private readonly ILogger<BinaryMessageProcessor> _logger;
    private readonly FeishuWebSocketOptions _options;
    private MemoryStream? _binaryDataStream;
    private readonly object _binaryDataStreamLock = new object();
    private DateTime _binaryDataReceiveStartTime = DateTime.MinValue;
    // WS-15 修复（P1-12）：_disposed 改为 int + Interlocked.Exchange 实现原子 check-then-set。
    // 此前 bool check-then-set 非原子，并发调用 Dispose/DisposeAsync 可能双进入释放逻辑。
    private int _disposed = 0;
    private readonly MessageRouter? _messageRouter;
    private readonly WebSocketConnectionManager? _connectionManager;

    /// <summary>
    /// 在途处理任务数的硬上界，防止突发帧暴增导致 OOM（WS-03 修复）。
    /// </summary>
    private const int MaxActiveProcessingTasks = 1024;

    /// <summary>
    /// "在途处理上界"的异步背压闸门（P1-6 修复）。
    /// </summary>
    /// <remarks>
    /// 此前由 <c>_activeProcessingTasks</c> 列表 + <c>Task.WaitAny(snapshot, 200)</c> 表达同一上界，
    /// 但等待发生在<b>同时持有</b> <c>_binaryDataStreamLock</c> 与 <c>_processLock</c> 期间，
    /// 既阻塞线程池又卡住后续帧装配。
    /// <para>
    /// 现在：槽位在 <see cref="ProcessBinaryDataAsync"/> 中以异步方式获取；
    /// 若本次调用"派发了完整消息处理任务"，槽位所有权移交给该任务（由其在 finally 归还），
    /// 否则（分片累积 / 超限丢弃 / 异常）由本次调用归还。
    /// </para>
    /// <para>I9：与 <c>_processLock</c> 一致，本信号量<b>不随 Dispose 释放</b>（未访问 AvailableWaitHandle）。</para>
    /// </remarks>
    private readonly SemaphoreSlim _processingSlots = new(MaxActiveProcessingTasks, MaxActiveProcessingTasks);
    private readonly IFeishuSeqIDDeduplicator? _seqIdDeduplicator;
    private readonly MessageSequenceValidator? _sequenceValidator;
    private readonly IUnifiedDeduplicationMiddleware? _unifiedDeduplicationMiddleware;

    /// <summary>
    /// 串行化整条二进制消息的处理，避免并发调用把不同消息的片段交错写入
    /// 同一个 <c>_binaryDataStream</c>（P0-1 修复）。
    /// </summary>
    private readonly SemaphoreSlim _processLock = new(1, 1);

    /// <summary>
    /// 大对象阈值（字节），超过此阈值使用 ToArray() 避免 GetBuffer() 的额外数据
    /// </summary>
    private const int LargeObjectThreshold = 85_000;

    /// <summary>
    /// 二进制消息接收事件
    /// </summary>
    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;

    /// <summary>
    /// 错误事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 收到 Pong 控制帧事件，携带服务端下发的 ClientConfig（如果存在）
    /// <para>handleControlFrame → PONG → configure(ClientConfig)</para>
    /// </summary>
    public event EventHandler<ClientConfigInfo?>? PongReceived;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public BinaryMessageProcessor(
        ILogger<BinaryMessageProcessor> logger,
        WebSocketConnectionManager? webSocketConnectionManager,
        FeishuWebSocketOptions options,
        MessageRouter messageRouter,
        IFeishuSeqIDDeduplicator? seqIdDeduplicator = null,
        MessageSequenceValidator? sequenceValidator = null,
        IUnifiedDeduplicationMiddleware? unifiedDeduplicationMiddleware = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
        _connectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(webSocketConnectionManager));
        _messageRouter = messageRouter ?? throw new ArgumentNullException(nameof(messageRouter));
        _seqIdDeduplicator = seqIdDeduplicator;
        _sequenceValidator = sequenceValidator;
        // P1-6/M2-4：分层后传输层不再依赖统一中间件；参数仅为源兼容保留。
        // SDK 内唯一生产构造点（FeishuWebSocketClient）本就不传该参数。
        _unifiedDeduplicationMiddleware = unifiedDeduplicationMiddleware;
        if (unifiedDeduplicationMiddleware != null)
        {
            _logger.LogWarning(
                "BinaryMessageProcessor 已不再推荐注入 IUnifiedDeduplicationMiddleware：SeqID/EventId 去重已分层，该参数将在后续版本移除");
        }
    }

    /// <summary>
    /// 处理二进制数据
    /// </summary>
    /// <remarks>
    /// P0-1 修复：整体串行化。此前多个消息的片段会并发进入同一 <c>_binaryDataStream</c>，
    /// 产生"A 头 + B 身"的畸形 protobuf 帧。
    /// <para>
    /// AOT-STRICT：处理链路最终经 <see cref="MessageRouter.RouteBinaryMessageWithResultAsync"/>
    /// 调用处理器（可能使用反射式 System.Text.Json），故必须携带裁剪/AOT 标注，
    /// 否则 net8+ 的 <c>IL2026</c>/<c>IL3050</c> 会在 <c>AotStrictMode</c> 下升级为错误。
    /// </para>
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task ProcessBinaryDataAsync(byte[] data, int offset, int count, bool endOfMessage, CancellationToken cancellationToken = default)
    {
        // P1-5 修复：Dispose 之后直接返回，避免访问已释放的信号量（此前会抛 ObjectDisposedException）
        if (Volatile.Read(ref _disposed) == 1)
            return;

        // P1-6 修复：异步背压。槽位在此获取（异步等待，不阻塞线程、不持锁），
        // 由"是否派发处理任务"决定所有权归属。
        await _processingSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
        var slotTransferred = false;
        try
        {
            await _processLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                slotTransferred = ProcessBinaryDataCore(data, offset, count, endOfMessage, cancellationToken);
            }
            finally
            {
                _processLock.Release();
            }
        }
        finally
        {
            if (!slotTransferred)
            {
                // 分片累积 / 超限丢弃 / 异常路径：本次调用未派发任务，由本次归还槽位
                _processingSlots.Release();
            }
        }
    }

    /// <summary>
    /// 二进制数据处理的同步核心实现（调用方需持有 <see cref="_processLock"/>）。
    /// </summary>
    /// <param name="data">数据缓冲区</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">数据长度</param>
    /// <param name="endOfMessage">是否为消息的最后一片</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 是否已将"完整消息的处理"派发为后台任务（P1-6：派发后 <c>_processingSlots</c> 的所有权移交该任务）。
    /// </returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private bool ProcessBinaryDataCore(byte[] data, int offset, int count, bool endOfMessage, CancellationToken cancellationToken)
    {
        var dispatched = false;
        try
        {
            lock (_binaryDataStreamLock)
            {
                // 如果是新消息的开始，初始化内存流
                if (_binaryDataStream == null)
                {
                    _binaryDataStream = new MemoryStream();
                    _binaryDataReceiveStartTime = DateTime.UtcNow;

                    _logger.LogDebug("开始接收新的二进制消息");
                }

                // 预先检查数据大小限制（写入前检查，防止内存溢出）
                var currentLength = _binaryDataStream.Length;
                var newLength = currentLength + count;
                if (newLength > _options.MessageSizeLimits.MaxBinaryMessageSize)
                {
                    var errorMessage = $"二进制消息大小超过限制 ({newLength} > {_options.MessageSizeLimits.MaxBinaryMessageSize})";
                    _logger.LogError(errorMessage);

                    // 清理当前数据流
                    _binaryDataStream.Dispose();
                    _binaryDataStream = null;

                    // 触发错误事件
                    OnError(errorMessage, "MessageSizeExceeded");
                    return false;
                }

                // 写入数据片段
                _binaryDataStream.Write(data, offset, count);

                // 如果消息接收完成
                if (endOfMessage)
                {
                    // 使用 GetBuffer() 获取内部缓冲区引用，避免 ToArray() 的复制
                    // 注意：缓冲区长度可能大于实际数据长度，需要使用 Length 属性
                    var buffer = _binaryDataStream.GetBuffer();
                    var actualLength = (int)_binaryDataStream.Length;
                    var receiveDuration = DateTime.UtcNow - _binaryDataReceiveStartTime;

                    // P2-7 修复：每条消息一条 Information 属高频日志，降级为 Debug
_logger.LogDebug("二进制消息接收完成，大小: {Size} 字节，耗时: {Duration}ms",
                            actualLength, receiveDuration.TotalMilliseconds);

                    byte[] completeData;
                    if (actualLength > LargeObjectThreshold)
                    {
                        completeData = _binaryDataStream.ToArray();
                    }
                    else
                    {
                        completeData = new byte[actualLength];
                        Buffer.BlockCopy(buffer, 0, completeData, 0, actualLength);
                    }

                    // P1-6 修复：删除 _activeProcessingTasks 记账 + Task.WaitAny 同步阻塞 + ContinueWith 三件套。
                    // 背压已由 ProcessBinaryDataAsync 中的 _processingSlots 异步表达（槽位在进入本方法前已获取），
                    // 此处只需把槽位所有权随"派发成功"移交。
                    // 注意：Task.Run 不得传入 cancellationToken —— 令牌已取消时委托不会执行，
                    // 槽位将永久泄漏（最终卡死接收管道）；取消由委托内部观察。
                    var receiveStartTime = _binaryDataReceiveStartTime;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessCompleteBinaryMessageAsync(completeData, receiveStartTime, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            _processingSlots.Release();
                        }
                    });
                    dispatched = true;

                    // 清理资源
                    _binaryDataStream.Dispose();
                    _binaryDataStream = null;
                }
                else
                {
                    _logger.LogDebug("已接收二进制消息片段，当前总大小: {Size} 字节", _binaryDataStream.Length);
                }
            }
        }
        catch (Exception ex)
        {
            // 发生异常时清理资源
            lock (_binaryDataStreamLock)
            {
                _binaryDataStream?.Dispose();
                _binaryDataStream = null;
            }

            _logger.LogError(ex, "处理二进制消息时发生错误");
            OnError($"处理二进制消息时发生错误: {ex.Message}", ex.GetType().Name);
        }

        return dispatched;
    }

    /// <summary>
    /// 处理完整的二进制消息
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task ProcessCompleteBinaryMessageAsync(byte[] completeData, DateTime receiveStartTime, CancellationToken cancellationToken)
    {
        try
        {
            var eventArgs = new WebSocketBinaryMessageEventArgs
            {
                Data = completeData ?? Array.Empty<byte>(),
                // P2-4 修复：此前 ReceiveStartTime 从不赋值，ReceiveDurationMs 恒为 ~6.39e14ms。
                // 首帧到达时的时间戳已由 _binaryDataReceiveStartTime 记录，这里显式透传。
                ReceiveStartTime = receiveStartTime,
                ReceiveEndTime = DateTime.UtcNow
            };

            if (completeData == null || completeData.Length == 0)
            {
                _logger.LogWarning("接收到空的二进制消息");
                eventArgs.ParseError = "接收到空的二进制消息";
                BinaryMessageReceived?.Invoke(this, eventArgs);
                return;
            }

            // 尝试解析为 Frame 对象
            ulong? markedSeqId = null; // 跟踪已标记的 SeqID，用于失败时回滚
            // P2-14 修复：frame 声明上提到 try 之外，使异常路径也能拿到已解析的帧并回 ACK(500)。
            EventProtoData? frame = null;
            // P0-1：extractedEventId 同步上提，供失败收尾回滚统一中间件路径使用
            string? extractedEventId = null;
            try
            {
                _logger.LogDebug("尝试使用 ProtoBuf 反序列化二进制消息");

                // 使用 Memory<byte> 的 Pin 方法或创建 MemoryStream
                // 对于 netstandard2.0
#if NETSTANDARD2_0
                // 使用 Buffer.BlockCopy 替代 MemoryMarshal
                var dataArray = new byte[completeData.Length];
                Buffer.BlockCopy(completeData, 0, dataArray, 0, completeData.Length);
                // AOT：必须走编译期模型实例，静态门面 ProtoBuf.Serializer 走反射路径（Native AOT 下不可用）
                frame = FeishuWebSocketProtoModel.Instance.Deserialize<EventProtoData>(new MemoryStream(dataArray));
#else
                // 对于 .NET Core 2.1+
                var span = new ReadOnlySpan<byte>(completeData);
                // AOT：必须走编译期模型实例，静态门面 ProtoBuf.Serializer 走反射路径（Native AOT 下不可用）
                frame = FeishuWebSocketProtoModel.Instance.Deserialize<EventProtoData>(span);
#endif

                // protobuf-net 的 Deserialize 标记为 [return: MaybeNull]：显式判空，
                // 避免空 Frame 在后续 frame.SeqID 等解引用处退化为 NullReferenceException。
                frame = frame ?? throw new InvalidDataException("二进制消息反序列化结果为空");

                _logger.LogDebug("成功反序列化为 Frame 对象: Service={Service}, Method={Method}, PayloadType={PayloadType}, SeqID={SeqID}",
                        frame.Service, frame.Method, frame.PayloadType, frame.SeqID);

                // 区分 CONTROL 帧和 DATA 帧进行不同处理（
                if (FrameBuilder.IsControlFrame(frame))
                {
                    HandleControlFrame(frame, eventArgs);
                    return;
                }

                // 消息序号验证
                if (_sequenceValidator != null)
                {
                    var validationResult = _sequenceValidator.ValidateSequence(frame.SeqID, eventArgs.MessageType);
                    if (validationResult == SequenceValidationResult.Duplicate ||
                        validationResult == SequenceValidationResult.Rollback)
                    {
                        _logger.LogWarning("消息序号验证失败: {ValidationResult}, SeqID={SeqID}", validationResult, frame.SeqID);
                        eventArgs.SkipReason = $"消息序号验证失败: {validationResult}";
                        BinaryMessageReceived?.Invoke(this, eventArgs);
                        await SendAckMessageAsync(frame, true, cancellationToken);
                        return;
                    }
                }

                // P2-10a：payload 只 GetString 一次，后续路由/事件提取复用同一字符串
                string? jsonPayload = null;
                if (frame.Payload != null) // frame 已在上方 ?? throw 处收敛为非空；用 ?. 会在条件为假的路径上把状态重新降级为可空
                {
                    jsonPayload = Encoding.UTF8.GetString(frame.Payload);
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(jsonPayload);
                        if (jsonDoc.RootElement.TryGetProperty("event_id", out var eventIdElement))
                        {
                            extractedEventId = eventIdElement.GetString();
                        }
                    }
                    catch
                    {
                    }
                }

                if (_unifiedDeduplicationMiddleware != null && (!string.IsNullOrEmpty(extractedEventId) || frame.SeqID > 0))
                {
                    var dedupResult = await _unifiedDeduplicationMiddleware.CheckAsync(extractedEventId, frame.SeqID, cancellationToken);
                    if (dedupResult.ShouldSkip)
                    {
                        _logger.LogDebug("统一去重检查跳过: {Reason}, EventId={EventId}, SeqId={SeqId}",
                                dedupResult.Reason, extractedEventId, frame.SeqID);
                        eventArgs.SkipReason = dedupResult.Reason;
                        BinaryMessageReceived?.Invoke(this, eventArgs);
                        await SendAckMessageAsync(frame, true, cancellationToken);
                        return;
                    }
                }
                else
                {
                    if (_seqIdDeduplicator != null && await _seqIdDeduplicator.TryMarkAsProcessedAsync(frame.SeqID))
                    {
                        _logger.LogDebug("SeqID {SeqID} 已处理过，跳过", frame.SeqID);
                        eventArgs.SkipReason = $"SeqID {frame.SeqID} 已处理过";
                        BinaryMessageReceived?.Invoke(this, eventArgs);
                        await SendAckMessageAsync(frame, true, cancellationToken);
                        return;
                    }
                    // 记录已标记的 SeqID，用于处理失败时回滚
                    if (_seqIdDeduplicator != null)
                    {
                        markedSeqId = frame.SeqID;
                    }
                }

                if (jsonPayload != null) // 复用上方提取结果，避免二次 GetString
                {
                    eventArgs.JsonContent = jsonPayload;
                    eventArgs.MessageType = "Frame";

                    _logger.LogDebug("成功解析 Frame Payload 为 JSON 内容（长度: {PayloadLength}）", jsonPayload.Length);

                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    // P1-5 修复：此前 RouteBinaryMessageAsync 内部吞掉异常，而 eventArgs.ProcessingTask
                    // 永远为 null（MessageRouter 从不设置），导致 ProcessingSuccess 恒为 true、
                    // 业务处理失败也回 ACK 200，服务端不再重发 → 事件永久丢失。
                    // 现在路由返回可观测的结果，并据此决定 ACK 的 code。
                    if (_messageRouter != null)
                    {
                        _logger.LogDebug("路由二进制转换的JSON消息到MessageRouter");
                        var routed = await _messageRouter.RouteBinaryMessageWithResultAsync(jsonPayload, "Frame", cancellationToken);
                        if (!routed)
                        {
                            eventArgs.ProcessingSuccess = false;
                            // P0-1：业务失败 → 传输层状态统一回滚（原先此分支不做任何回滚，
                            // 同 SeqID 重发会被序列验证器 Duplicate 或 SeqID 去重吞掉 → ACK 200 → 事件丢失）
                            await RollbackTransportStateAsync(frame, markedSeqId, extractedEventId, cancellationToken);
                        }
                    }

                    // WS-25 修复（P2-12）：删除恒为 null 的 ProcessingTask 分支。
                    // 该属性从未被赋值，此分支从不执行，属于纯死代码。
                    // 路由结果已由上方 RouteBinaryMessageWithResultAsync 返回值处理。

                    if (eventArgs.ProcessingSuccess && _unifiedDeduplicationMiddleware != null &&
                        (!string.IsNullOrEmpty(extractedEventId) || frame.SeqID > 0))
                    {
                        // 失败路径已在 RollbackTransportStateAsync 中收尾；成功路径仅做完成标记
                        await _unifiedDeduplicationMiddleware.MarkCompletedAsync(extractedEventId, frame.SeqID, cancellationToken);
                    }

                    await SendAckMessageAsync(frame, eventArgs.ProcessingSuccess, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Frame 解析成功但 Payload 为空");
                    eventArgs.ParseError = "Frame 解析成功但 Payload 为空";
                    // P2-14 补充：与错误通知路径同理——此处订阅者异常不得阻断后续去重回滚与 ACK(false)。
                    SafeInvokeBinaryMessageReceived(eventArgs, "Payload 为空通知路径");

                    // P0-1：空 Payload 同样须回滚传输层占用的幂等状态
                    await RollbackTransportStateAsync(frame, markedSeqId, extractedEventId, cancellationToken);

                    await SendAckMessageAsync(frame, false, cancellationToken);
                }
            }
            catch (ProtoBuf.ProtoException ex)
            {
                _logger.LogError(ex, "ProtoBuf 反序列化失败，尝试直接解析为 JSON");

                eventArgs.ParseError = $"ProtoBuf 反序列化失败: {ex.Message}";

                // P2-3 修复：回退路径此前缺少大小校验，超限负载会直接进入 JSON 解析与路由
                if (completeData.Length > _options.MessageSizeLimits.MaxBinaryMessageSize)
                {
                    _logger.LogError("二进制消息大小 {Size} 超过最大限制 {MaxSize}，放弃 JSON 回退解析",
                        completeData.Length, _options.MessageSizeLimits.MaxBinaryMessageSize);
                    BinaryMessageReceived?.Invoke(this, eventArgs);
                    return;
                }

                var jsonString = Encoding.UTF8.GetString(completeData);
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    eventArgs.JsonContent = jsonString;
                    eventArgs.MessageType = "JSON_Fallback";
                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    if (_messageRouter != null)
                    {
                        _logger.LogDebug("路由二进制转换的JSON消息到MessageRouter (Fallback模式)");
                        await _messageRouter.RouteBinaryMessageAsync(jsonString, "JSON_Fallback", cancellationToken);
                    }
                }
                else
                {
                    BinaryMessageReceived?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理完整二进制消息时发生错误");
                eventArgs.ParseError = $"处理完整二进制消息时发生错误: {ex.Message}";
                // P2-14 补充：错误通知必须隔离订阅者异常——首次抛出导致进入本 catch 的订阅者
                // 在此必然再次抛出，若不隔离会穿透到外层 catch，导致下方 SeqID 回滚与 ACK(500)
                // 永远执行不到（恰是 P2-14 要修复的"失败不回 ACK"的另一形态）。
                SafeInvokeBinaryMessageReceived(eventArgs, "错误通知路径");

                // P0-1：传输层失败收尾——回滚验证器窗口 + SeqID 去重（及宿主注入时的统一中间件）
                await RollbackTransportStateAsync(frame, markedSeqId, extractedEventId, cancellationToken);

                // P2-14 修复：此前异常路径不回 ACK，服务端只能等超时后重投。
                // 帧已解析成功时补 ACK(500)：与成功路径同一语义（200=成功、500=失败触发服务端重投），
                // 让重投即时发生而不是等超时。ProtoBuf 解析失败路径（catch(ProtoException)）无可用帧，维持不回。
                if (frame != null)
                {
                    await SendAckMessageAsync(frame, false, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理完整二进制消息时发生未知错误");
            OnError($"处理完整二进制消息时发生未知错误: {ex.Message}", ex.GetType().Name);
        }
    }

    /// <summary>
    /// 传输层失败收尾：回滚该帧占用过的全部幂等状态，允许服务端重发时重新处理。
    /// </summary>
    /// <remarks>
    /// P0-1 契约：无论服务端重发是否复用 SeqID，回滚均安全
    ///（复用 → 可重新处理；不复用 → 回滚一个不再出现的键，无副作用）。
    /// 序列验证器<b>仅移除窗口记录、不回退游标</b>，避免破坏后续合法帧的连续性判定。
    /// </remarks>
    /// <param name="frame">已解析的帧（可为 null）</param>
    /// <param name="markedSeqId">legacy 路径已标记的 SeqID</param>
    /// <param name="extractedEventId">从 Payload 提取的 EventId（统一中间件路径用）</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task RollbackTransportStateAsync(
        EventProtoData? frame,
        ulong? markedSeqId,
        string? extractedEventId,
        CancellationToken cancellationToken)
    {
        // ① 序列验证器窗口记录（P0-1：业务失败原先完全不可达任何回滚）
        if (frame != null)
        {
            try
            {
                _sequenceValidator?.Remove(frame.SeqID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滚序列验证器状态失败 SeqId={SeqId}", frame.SeqID);
            }
        }

        // ② SeqID 去重标记（生产路径：markedSeqId 在统一中间件缺省时被标记）
        if (markedSeqId.HasValue && _seqIdDeduplicator != null)
        {
            try
            {
                await _seqIdDeduplicator.RollbackAsync(markedSeqId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滚 SeqId {SeqId} 去重失败", markedSeqId.Value);
            }
        }

        // ③ 统一中间件路径（宿主显式注入时；生产 SDK 构造点不注入，分层后 BP 侧依赖将 Obsolete/移除）
        if (_unifiedDeduplicationMiddleware != null)
        {
            try
            {
                var seqIdForRollback = frame?.SeqID;
                await _unifiedDeduplicationMiddleware.RollbackAsync(
                    extractedEventId,
                    seqIdForRollback.HasValue && seqIdForRollback.Value > 0 ? seqIdForRollback : null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滚统一去重状态失败 EventId={EventId}", extractedEventId);
            }
        }
    }

    /// <summary>
    /// 处理控制帧（CONTROL, Method=0）
    /// <para>Ping: 忽略（服务端不应发送 Ping 到客户端）</para>
    /// <para>Pong: 解析 Payload 中的 ClientConfig，触发 PongReceived 事件</para>
    /// </summary>
    /// <param name="frame">ProtoBuf 控制帧</param>
    /// <param name="eventArgs">二进制消息事件参数</param>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private void HandleControlFrame(EventProtoData frame, WebSocketBinaryMessageEventArgs eventArgs)
    {
        var messageType = frame.MessageType;

        eventArgs.MessageType = $"Control_{messageType}";

        switch (messageType)
        {
            case MessageType.Ping:
                // 服务端发送的 Ping，忽略（对照 Java SDK: case PING: return;）
                _logger.LogDebug("收到服务端 Ping 控制帧，已忽略");
                eventArgs.SkipReason = "服务端 Ping 控制帧，无需处理";
                BinaryMessageReceived?.Invoke(this, eventArgs);
                break;

            case MessageType.Pong:
                // 解析 Pong 中的 ClientConfig 并触发事件（对照 Java SDK: case PONG: configure(conf);）
                // Pong 接收日志不受 EnableLogging 限制，便于诊断心跳问题
                _logger.LogDebug("收到 Pong 控制帧，解析 ClientConfig...");

                var config = FrameBuilder.ExtractClientConfig(frame, _logger);
                eventArgs.JsonContent = frame.Payload != null ? Encoding.UTF8.GetString(frame.Payload) : null;
                BinaryMessageReceived?.Invoke(this, eventArgs);

                // 通知 HeartbeatManager 重置超时并应用动态配置
                PongReceived?.Invoke(this, config);
                break;

            default:
                _logger.LogDebug("收到未知控制帧类型: {MessageType}", messageType);
                eventArgs.SkipReason = $"未知控制帧类型: {messageType}";
                BinaryMessageReceived?.Invoke(this, eventArgs);
                break;
        }
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task SendAckMessageAsync(EventProtoData? eventProtoData, bool success, CancellationToken cancellationToken)
    {
        if (eventProtoData == null)
            return;

        // 按照飞书 WebSocket 协议（Java SDK 对照）构造 ACK 响应
        // Response 格式: {"code": 200/500, "headers": {}, "data": "base64-encoded"}
        // 同时在 Frame headers 中添加 biz_rt（业务处理耗时）
        var stopwatch = Stopwatch.StartNew();

        // P1-5 修复（WS-06）：使用具名 DTO 替代匿名类型，避免 AOT 反射依赖。
        // Headers 使用空字典而非 null，保证序列化后始终包含 "headers": {} 字段，
        // 与飞书协议 {"code":..,"headers":{},"data":".."} 一致。
        var responseData = System.Array.Empty<byte>();

        var responseObj = new AckResponse
        {
            Code = success ? 200 : 500,
            Headers = new Dictionary<string, string>(),
            Data = Convert.ToBase64String(responseData)
        };

        var ackJson = FeishuJsonAot.Serialize(responseObj, JsonOptions.Default);
        var ackPayload = Encoding.UTF8.GetBytes(ackJson);

        stopwatch.Stop();
        var elapsedMs = (long)stopwatch.Elapsed.TotalMilliseconds;

        try
        {
            // 克隆 Frame 对象避免修改原始数据
            var headerCount = eventProtoData.Headers?.Length ?? 0;
            var newHeaders = new ProtoHeader[headerCount + 1];
            if (eventProtoData.Headers != null)
            {
                for (int i = 0; i < eventProtoData.Headers.Length; i++)
                {
                    newHeaders[i] = new ProtoHeader
                    {
                        Key = eventProtoData.Headers[i].Key,
                        Value = eventProtoData.Headers[i].Value
                    };
                }
            }
            newHeaders[headerCount] = new ProtoHeader { Key = "biz_rt", Value = elapsedMs.ToString() };

            var ackFrame = new EventProtoData
            {
                Service = eventProtoData.Service,
                Method = eventProtoData.Method,
                SeqID = eventProtoData.SeqID,
                LogID = eventProtoData.LogID,
                LogIDNew = eventProtoData.LogIDNew,
                Payload = ackPayload,
                PayloadEncoding = "json",
                PayloadType = "ack",
                Headers = newHeaders
            };

            using var messageStream = new MemoryStream();
            // AOT：必须走编译期模型实例，静态门面 ProtoBuf.Serializer 走反射路径（Native AOT 下不可用）
            FeishuWebSocketProtoModel.Instance.Serialize(messageStream, ackFrame);

            if (messageStream.TryGetBuffer(out var arraySegment) && _connectionManager != null)
            {
                await _connectionManager.SendBinaryMessageAsync(arraySegment, cancellationToken);
                _logger.LogDebug("已发送ACK消息: code={Code}, biz_rt={BizRt}ms", responseObj.Code, elapsedMs);
            }
        }
        catch (Exception x)
        {
            _logger.LogError(x, "发送ACK消息时发生错误");
            OnError($"发送ACK消息时发生错误: {x.Message}", x.GetType().Name);
        }
    }

    /// <summary>
    /// 在失败路径上安全触发 <see cref="BinaryMessageReceived"/> 事件。
    /// </summary>
    /// <param name="eventArgs">二进制消息事件参数</param>
    /// <param name="context">触发上下文（用于日志定位）</param>
    /// <remarks>
    /// P2-14 补充：失败路径上事件触发之后还必须执行去重回滚与 ACK(500)——订阅者抛出的异常
    /// 若在此向上穿透，回滚与 ACK 将永远执行不到。对齐 WebSocketConnectionManager.SafeInvokeDisconnected
    /// 的隔离模式：记录异常，不向上传播。
    /// </remarks>
    private void SafeInvokeBinaryMessageReceived(WebSocketBinaryMessageEventArgs eventArgs, string context)
    {
        var handler = BinaryMessageReceived;
        if (handler == null)
            return;

        try
        {
            handler.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BinaryMessageReceived 事件处理器在{Context}上抛出异常（已隔离，不影响后续回滚与 ACK 派发）", context);
        }
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
    private void OnError(string errorMessage, string errorType)
    {
        Error?.Invoke(this, new WebSocketErrorEventArgs
        {
            ErrorMessage = errorMessage,
            ErrorType = errorType,
            IsNetworkError = false
        });
    }

    /// <summary>
    /// 重置接收状态，清空尚未完成的分片缓冲。
    /// </summary>
    /// <remarks>
    /// P1-6 修复：连接中途断开时，半包会残留在 <c>_binaryDataStream</c> 中。
    /// 重连后首条消息会被拼上旧残片，导致 protobuf 解析失败或解析出错误字段，
    /// 且该状态无法自愈（每次重连都保留），只能重启进程。必须在重连时调用。
    /// </remarks>
    public void Reset()
    {
        lock (_binaryDataStreamLock)
        {
            if (_binaryDataStream != null)
            {
                _binaryDataStream.Dispose();
                _binaryDataStream = null;
                _logger.LogDebug("二进制消息处理器已重置，清空未完成的分片缓冲");
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // WS-15：原子 check-then-set，确保并发调用安全
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            // P1-3 修复：不做同步阻塞等待（需要确定性等待请改用 DisposeAsync）。
            // P1-6 修复：在途任务数改由 _processingSlots 的占用数表达（此前是 _activeProcessingTasks 列表）。
            var pendingCount = MaxActiveProcessingTasks - _processingSlots.CurrentCount;
            if (pendingCount > 0)
            {
                _logger.LogWarning("释放时仍有 {Count} 个二进制处理任务未完成，将在后台继续运行", pendingCount);
            }

            lock (_binaryDataStreamLock)
            {
                _binaryDataStream?.Dispose();
                _binaryDataStream = null;
            }

            // P1-5 修复（I9）：不再释放 _processLock；_processingSlots 同样不释放。
            // 本类型从不访问 SemaphoreSlim.AvailableWaitHandle，不释放不会产生任何 OS 句柄泄漏，
            // 但释放会与在途 WaitAsync/Release（含被派发任务归还槽位）构成 ObjectDisposedException 竞态。
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放二进制处理器资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 异步释放资源并等待处理中的任务结束。
    /// </summary>
    /// <returns>表示异步释放操作的任务</returns>
    public async ValueTask DisposeAsync()
    {
        // WS-15：原子 check-then-set，确保并发调用安全
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            // P1-6 修复：改为等待"所有在途处理任务归还槽位"（等价于此前 Task.WhenAll(pendingTasks) 的语义），
            // 上限 5 秒；不再依赖 _activeProcessingTasks 列表。
            await WaitForProcessingSlotsAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            lock (_binaryDataStreamLock)
            {
                _binaryDataStream?.Dispose();
                _binaryDataStream = null;
            }

            // P1-5 修复（I9）：同 Dispose()，不释放 _processLock / _processingSlots
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步释放二进制处理器资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 等待所有在途处理任务归还 <see cref="_processingSlots"/> 槽位（P1-6）。
    /// </summary>
    /// <param name="timeout">最长等待时间</param>
    /// <remarks>
    /// 等价于此前的 <c>Task.WhenAll(pendingTasks)</c> 语义：只要所有槽位归还，即在途处理已全部结束。
    /// 由于 <c>_disposed</c> 已置位，<see cref="ProcessBinaryDataAsync"/> 不会再占用新槽位，因此不会死等。
    /// </remarks>
    private async Task WaitForProcessingSlotsAsync(TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (_processingSlots.CurrentCount < MaxActiveProcessingTasks)
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                _logger.LogWarning("等待所有处理任务完成超时（{Timeout}），部分任务可能仍在运行", timeout);
                return;
            }

            await Task.Delay(50).ConfigureAwait(false);
        }
    }
}