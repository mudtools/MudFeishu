// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket.SocketEventArgs;

/// <summary>
/// WebSocket 飞书事件参数
/// </summary>
public class WebSocketFeishuEventArgs : System.EventArgs
{
    /// <summary>
    /// 事件消息（v1.0版本）
    /// </summary>
    public EventMessage? EventMessage { get; set; }

    /// <summary>
    /// 是否为v2.0版本消息
    /// </summary>
    public bool IsV2Message { get; set; } = false;

    /// <summary>
    /// 事件数据（v1.0版本）
    /// </summary>
    public EventData? EventData => EventMessage?.Data;

    /// <summary>
    /// 事件类型（兼容v1.0和v2.0版本）
    /// </summary>
    public string? EventType => IsV2Message ? EventTypeV2 : EventData?.EventType;

    /// <summary>
    /// v2.0版本的事件类型
    /// </summary>
    public string? EventTypeV2 { get; set; }

    /// <summary>
    /// 事件ID（兼容v1.0和v2.0版本）
    /// </summary>
    public string? EventId => IsV2Message ? EventIdV2 : null; // v1.0版本的EventId需要从外部获取

    /// <summary>
    /// v2.0版本的事件ID
    /// </summary>
    public string? EventIdV2 { get; set; }

    /// <summary>
    /// 应用ID（v1.0版本）
    /// </summary>
    public string? AppId => EventData?.AppId;

    /// <summary>
    /// 租户ID（v1.0版本）
    /// </summary>
    public string? TenantKey => EventData?.TenantKey;

    /// <summary>
    /// 接收时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 事件内容（原始JSON）
    /// </summary>
    public string? RawEvent { get; set; }
}