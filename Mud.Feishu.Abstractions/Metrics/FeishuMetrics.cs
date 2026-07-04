// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Mud.Feishu.Abstractions.Metrics;

/// <summary>
/// 飞书 SDK 性能指标收集器
/// </summary>
public class FeishuMetrics
{
    private static readonly Meter Meter = new("Mud.Feishu", "3.0.0");

    /// <summary>
    /// 记录事件处理次数
    /// </summary>
    public static readonly Counter<long> EventHandlingCount = Meter.CreateCounter<long>(
        "feishu_event_handling_total",
        description: "事件处理总次数");

    /// <summary>
    /// 记录事件处理成功次数
    /// </summary>
    public static readonly Counter<long> EventHandlingSuccessCount = Meter.CreateCounter<long>(
        "feishu_event_handling_success_total",
        description: "事件处理成功次数");

    /// <summary>
    /// 记录事件处理失败次数
    /// </summary>
    public static readonly Counter<long> EventHandlingFailureCount = Meter.CreateCounter<long>(
        "feishu_event_handling_failure_total",
        description: "事件处理失败次数");

    /// <summary>
    /// 记录事件去重命中次数
    /// </summary>
    public static readonly Counter<long> EventDeduplicationHitCount = Meter.CreateCounter<long>(
        "feishu_event_deduplication_hit_total",
        description: "事件去重命中次数");

    /// <summary>
    /// 记录 HTTP 请求次数
    /// </summary>
    public static readonly Counter<long> HttpRequestCount = Meter.CreateCounter<long>(
        "feishu_http_request_total",
        description: "HTTP 请求总次数");

    /// <summary>
    /// 记录 HTTP 请求持续时间（毫秒）
    /// </summary>
    public static readonly Histogram<double> HttpRequestDuration = Meter.CreateHistogram<double>(
        "feishu_http_request_duration_ms",
        unit: "ms",
        description: "HTTP 请求持续时间（毫秒）");

    /// <summary>
    /// 记录事件处理持续时间（毫秒）
    /// </summary>
    public static readonly Histogram<double> EventHandlingDuration = Meter.CreateHistogram<double>(
        "feishu_event_handling_duration_ms",
        unit: "ms",
        description: "事件处理持续时间（毫秒）");

    /// <summary>
    /// 记录 WebSocket 连接数
    /// </summary>
    public static readonly ObservableGauge<int> WebSocketConnectionCount;

    /// <summary>
    /// WebSocket 连接数提供器
    /// </summary>
    public static Func<int> WebSocketConnectionCountProvider { get; set; } = () => 0;

    static FeishuMetrics()
    {
        WebSocketConnectionCount = Meter.CreateObservableGauge<int>(
            "feishu_websocket_connections",
            observeValue: () => WebSocketConnectionCountProvider(),
            description: "WebSocket 连接数");
    }

    /// <summary>
    /// 记录 WebSocket 消息处理持续时间（毫秒）
    /// </summary>
    public static readonly Histogram<double> WebSocketMessageProcessingDuration = Meter.CreateHistogram<double>(
        "feishu_websocket_message_processing_duration_ms",
        unit: "ms",
        description: "WebSocket 消息处理持续时间（毫秒）");
}
