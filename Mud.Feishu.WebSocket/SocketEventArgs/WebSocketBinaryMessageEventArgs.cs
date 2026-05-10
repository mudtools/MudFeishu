// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket.SocketEventArgs;

/// <summary>
/// WebSocket 二进制消息事件参数
/// </summary>
public class WebSocketBinaryMessageEventArgs : EventArgs
{
    /// <summary>
    /// 二进制消息数据
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 消息大小（字节）
    /// </summary>
    public int DataSize => Data.Length;

    /// <summary>
    /// 消息接收开始时间
    /// </summary>
    public DateTime ReceiveStartTime { get; set; }

    /// <summary>
    /// 消息接收完成时间
    /// </summary>
    public DateTime ReceiveEndTime { get; set; }

    /// <summary>
    /// 接收耗时（毫秒）
    /// </summary>
    public double ReceiveDurationMs => (ReceiveEndTime - ReceiveStartTime).TotalMilliseconds;

    /// <summary>
    /// 是否为完整的消息
    /// </summary>
    public bool IsCompleteMessage { get; set; } = true;

    /// <summary>
    /// 消息类型（如果可解析）
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// 解析后的 JSON 内容（如果适用）
    /// </summary>
    public string? JsonContent { get; set; }

    /// <summary>
    /// 解析错误信息（如果有）
    /// </summary>
    public string? ParseError { get; set; }

    /// <summary>
    /// 跳过处理的原因（如果因去重而跳过）
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// 是否成功解析
    /// </summary>
    public bool IsParseSuccess => string.IsNullOrEmpty(ParseError);

    /// <summary>
    /// 消息序列号（用于消息追踪）
    /// </summary>
    public long MessageSequence { get; set; }

    /// <summary>
    /// 队列中的消息数量
    /// </summary>
    public int QueueCount { get; set; }

    /// <summary>
    /// 处理任务（由事件处理器设置，用于异步等待处理完成）
    /// </summary>
    /// <remarks>
    /// 如果事件处理器需要异步处理，应设置此属性为一个表示处理完成的任务。
    /// BinaryMessageProcessor 会等待此任务完成后根据结果调用 MarkCompletedAsync 或 RollbackAsync。
    /// </remarks>
    public Task? ProcessingTask { get; set; }

    /// <summary>
    /// 处理是否成功（由事件处理器设置）
    /// </summary>
    /// <remarks>
    /// 如果设置为 false，BinaryMessageProcessor 会调用 RollbackAsync 回滚去重状态。
    /// 默认为 true。
    /// </remarks>
    public bool ProcessingSuccess { get; set; } = true;

    /// <summary>
    /// 处理异常（由事件处理器设置，如果处理失败）
    /// </summary>
    public Exception? ProcessingException { get; set; }
}