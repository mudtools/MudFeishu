// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.DataModels.WsEndpoint;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 心跳管理器 - 负责向飞书服务端发送应用级 ProtoBuf Ping 帧
/// <para>设计理念（对齐 Python SDK <c>_ping_loop</c>）：</para>
/// <para>1. 心跳循环只负责"发 Ping"，失败时仅记录日志，不主动触发重连。</para>
/// <para>2. 连接是否真正断开由 WebSocketConnectionManager 的接收循环异常检测，
/// 以及 <c>ClientWebSocket.KeepAliveInterval</c> 协议级 Ping/Pong 超时检测。</para>
/// <para>3. 飞书服务端不一定对每个 Ping 都回复 Pong，不能以"未收到 Pong"判定连接断开。</para>
/// </summary>
public class HeartbeatManager
{
    private readonly ILogger<HeartbeatManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly Func<byte[], CancellationToken, Task> _sendBinaryCallback;
    private int? _serviceId;

    /// <summary>
    /// 初始化心跳管理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="sendBinaryCallback">发送二进制消息回调</param>
    public HeartbeatManager(
        ILogger<HeartbeatManager> logger,
        FeishuWebSocketOptions options,
        Func<byte[], CancellationToken, Task> sendBinaryCallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sendBinaryCallback = sendBinaryCallback ?? throw new ArgumentNullException(nameof(sendBinaryCallback));
    }

    /// <summary>
    /// 设置服务ID（从 WebSocket URL 的 service_id 查询参数提取）
    /// <para>ProtoBuf Ping 帧需要包含 serviceId 字段</para>
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    public void SetServiceId(int serviceId)
    {
        _serviceId = serviceId;
        if (_options.EnableLogging)
            _logger.LogDebug("心跳管理器已设置 ServiceId={ServiceId}", serviceId);
    }

    /// <summary>
    /// 启动心跳循环（对齐 Python SDK <c>_ping_loop</c> 设计）
    /// <para>循环逻辑：等待间隔 → 发送 ProtoBuf Ping → 记录日志。失败时仅记录警告，不触发重连。</para>
    /// <para>连接断开由接收循环的异常或
    /// <c>KeepAliveInterval</c> 协议级 Ping 超时检测，此处不重复处理。</para>
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartHeartbeatAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("心跳管理器已启动，心跳间隔: {IntervalMs}ms", _options.HeartbeatIntervalMs);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 与 Python SDK 一致：先等待间隔，再发送 Ping
                await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken);

                try
                {
                    // 构建并发送 ProtoBuf 二进制 Ping 帧
                    var serviceId = _serviceId ?? 0;
                    var pingFrameData = FrameBuilder.BuildPingFrame(serviceId);
                    await _sendBinaryCallback(pingFrameData, cancellationToken);

                    if (_options.EnableLogging)
                        _logger.LogDebug("已发送 ProtoBuf 心跳 (ServiceId={ServiceId})", serviceId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // 对齐 Python SDK _ping_loop：Ping 失败仅记录警告，不触发重连。
                    // 连接是否断开由接收循环或 KeepAliveInterval 协议级 Ping 超时检测。
                    _logger.LogWarning("发送心跳时发生错误: {Message}", ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 通知收到Pong响应，应用服务端下发的 ClientConfig
    /// <para>飞书服务端通过 Pong 控制帧的 Payload 下发 ClientConfig（PingInterval、ReconnectInterval 等），
    /// 此方法解析并应用这些动态配置。</para>
    /// </summary>
    /// <param name="config">服务端通过 Pong 下发的客户端配置（可选）</param>
    public void OnPongReceived(ClientConfigInfo? config = null)
    {
        if (config != null)
        {
            ApplyClientConfig(config);
        }

        _logger.LogDebug("已收到 Pong 响应");
    }

    /// <summary>
    /// 应用服务端下发的 ClientConfig，动态更新心跳间隔等配置
    /// <para>ClientConfig 中的 PingInterval 单位为秒，需转换为毫秒</para>
    /// </summary>
    /// <param name="config">客户端配置信息</param>
    private void ApplyClientConfig(ClientConfigInfo config)
    {
        if (config == null)
            return;

        // PingInterval 单位为秒，转换为毫秒
        if (config.PingInterval > 0)
        {
            var newIntervalMs = config.PingInterval * 1000;
            if (newIntervalMs != _options.HeartbeatIntervalMs)
            {
                var oldIntervalMs = _options.HeartbeatIntervalMs;
                _options.HeartbeatIntervalMs = newIntervalMs;
                if (_options.EnableLogging)
                    _logger.LogInformation("心跳间隔已动态更新: {OldMs}ms → {NewMs}ms (服务端下发 PingInterval={PingInterval}s)",
                        oldIntervalMs, newIntervalMs, config.PingInterval);
            }
        }

        // ReconnectInterval 单位为秒，转换为毫秒
        if (config.ReconnectInterval > 0)
        {
            _options.ReconnectDelayMs = config.ReconnectInterval * 1000;
            if (_options.EnableLogging)
                _logger.LogDebug("重连间隔已更新: {Ms}ms", _options.ReconnectDelayMs);
        }

        // MaxReconnectAttempts: reconnectCount=-1 表示无限重连
        //  MaxReconnectAttempts=0 → 无限重连；>0 → 有限重连
        if (config.ReconnectCount >= -1)
        {
            // Java reconnectCount=-1 映射为 .NET MaxReconnectAttempts=0（无限重连）
            _options.MaxReconnectAttempts = config.ReconnectCount == -1 ? 0 : config.ReconnectCount;
            if (_options.EnableLogging)
                _logger.LogDebug("最大重连次数已更新: {Count}", _options.MaxReconnectAttempts);
        }
    }
}
