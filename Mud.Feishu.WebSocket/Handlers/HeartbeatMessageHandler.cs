// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using System.Diagnostics.CodeAnalysis;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 心跳消息处理器
/// </summary>
public class HeartbeatMessageHandler : JsonMessageHandler
{
    /// <summary>
    /// 初始化心跳消息处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <remarks>
    /// WS2-12：删除 <c>FeishuWebSocketOptions options</c> 参数——它的唯一去向是一个从不被读取的
    /// 私有字段。应用层心跳间隔由 <see cref="HeartbeatManager"/>（负责构造并发送 ProtoBuf Ping 帧）
    /// 持有，本处理器只负责把收到的 JSON <c>heartbeat</c> 消息记入日志，与配置无关。
    /// 模块未发布，无源兼容包袱。
    /// </remarks>
    public HeartbeatMessageHandler(ILogger<HeartbeatMessageHandler> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override bool CanHandle(string messageType)
    {
        return messageType.ToLowerInvariant() == "heartbeat";
    }
    /// <inheritdoc/>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public override Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var heartbeatMessage = SafeDeserialize<HeartbeatMessage>(message);

        _logger.LogDebug("收到心跳消息，时间戳: {Timestamp}, 状态: {Status}",
                heartbeatMessage?.Data?.Timestamp, heartbeatMessage?.Data?.Status);

        return Task.CompletedTask;
    }
}