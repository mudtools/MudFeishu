// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.WsEndpoint;

/// <summary>
/// WebSocket客户端配置信息，用于管理连接重连机制和心跳检测的相关参数。
/// 该配置主要用于WebSocket连接的稳定性管理，包括断线重连策略和连接健康检查。
/// </summary>
public class ClientConfigInfo
{
    /// <summary>
    /// 重连次数限制，指定WebSocket连接断开后自动重连的最大尝试次数。
    /// <para>当达到此限制后，将停止自动重连，需要手动触发重新连接。</para>
    /// <para>默认值：3次</para>
    /// </summary>
    [JsonPropertyName("ReconnectCount")]
    public int ReconnectCount { get; set; }

    /// <summary>
    /// 重连间隔时间，单位：毫秒。
    /// <para>定义每次重连尝试之间的等待时间，用于避免频繁重连对服务器造成压力。</para>
    /// <para>建议设置在1000-5000毫秒之间，根据网络状况调整。</para>
    /// <para>默认值：3000毫秒</para>
    /// </summary>
    [JsonPropertyName("ReconnectInterval")]
    public int ReconnectInterval { get; set; }

    /// <summary>
    /// 重连随机数，用于在重连间隔基础上增加随机延迟。
    /// <para>通过添加随机因子，避免多个客户端同时重连造成的"惊群效应"。</para>
    /// <para>系统会在重连间隔基础上增加0到此值的随机毫秒数。</para>
    /// <para>默认值：1000毫秒</para>
    /// </summary>
    [JsonPropertyName("ReconnectNonce")]
    public int ReconnectNonce { get; set; }

    /// <summary>
    /// 心跳检测间隔时间，单位：毫秒。
    /// <para>用于定期发送ping消息以保持WebSocket连接活跃，检测连接状态。</para>
    /// <para>间隔过短可能增加网络开销，过长可能导致连接断开检测延迟。</para>
    /// <para>建议设置在30000-60000毫秒之间。</para>
    /// <para>默认值：30000毫秒</para>
    /// </summary>
    [JsonPropertyName("PingInterval")]
    public int PingInterval { get; set; }
}
