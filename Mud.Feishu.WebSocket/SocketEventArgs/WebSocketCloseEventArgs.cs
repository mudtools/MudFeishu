// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.SocketEventArgs;

/// <summary>
/// WebSocket连接关闭事件参数
/// </summary>
public class WebSocketCloseEventArgs : EventArgs
{
    /// <summary>
    /// 关闭状态码
    /// </summary>
    public WebSocketCloseStatus? CloseStatus { get; set; }

    /// <summary>
    /// 关闭描述
    /// </summary>
    public string? CloseStatusDescription { get; set; }

    /// <summary>
    /// 关闭时的时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 是否为服务器主动关闭
    /// </summary>
    public bool IsServerInitiated { get; set; }

    /// <summary>
    /// 连接持续时间（如果有的话）
    /// </summary>
    public TimeSpan? ConnectionDuration { get; set; }
}