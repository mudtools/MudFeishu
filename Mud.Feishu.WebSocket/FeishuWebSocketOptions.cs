
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端配置选项
/// </summary>
public class FeishuWebSocketOptions
{
    /// <summary>
    /// 自动重连，默认为true
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 最大重连次数，默认为5次
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>
    /// 重连延迟时间（毫秒），默认为5000毫秒
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 5000;

    /// <summary>
    /// 接收缓冲区大小（字节），默认为4KB
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// 心跳间隔时间（毫秒），默认为30000毫秒
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 30000;

    /// <summary>
    /// 连接超时时间（毫秒），默认为10000毫秒
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// 是否启用日志记录，默认为true
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 最大消息大小（字符数），默认为1MB
    /// </summary>
    public int MaxMessageSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// 启用多处理器模式，默认为false
    /// </summary>
    public bool EnableMultiHandlerMode { get; set; } = false;

    /// <summary>
    /// 多处理器模式下是否并行执行，默认为true
    /// </summary>
    public bool ParallelMultiHandlers { get; set; } = true;

    /// <summary>
    /// 是否启用消息队列处理，默认为true
    /// </summary>
    public bool EnableMessageQueue { get; set; } = true;

    /// <summary>
    /// 消息队列最大容量，默认为1000条
    /// </summary>
    public int MessageQueueCapacity { get; set; } = 1000;
 
}
