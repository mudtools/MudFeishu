// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Mud.Feishu.Abstractions.Metrics;

/// <summary>
/// 飞书 SDK 性能指标源。
/// 仅包含 Feishu 特有指标（事件处理、WebSocket、Webhook）。
/// HTTP 请求指标由 Mud.HttpUtils.MudHttpMeter 自动采集（mud.http.requests / mud.http.request.duration）。
/// Token 刷新指标由 Mud.HttpUtils.TokenManagerBase 自动采集（mud.token.refresh / mud.token.refresh.duration）。
/// </summary>
public static class FeishuMetrics
{
    /// <summary>
    /// Meter 名称，遵循 OTel 命名约定。
    /// </summary>
    public const string MeterName = "Mud.Feishu";

    /// <summary>
    /// Meter 版本。
    /// </summary>
    public const string Version = "3.0.0";

    /// <summary>
    /// 静态 Meter 实例。
    /// </summary>
    public static readonly Meter Instance = new(MeterName, Version);

    // ── 事件处理指标 ──

    /// <summary>
    /// 事件处理总次数（维度：app_key, event_type, handler_type, outcome）。
    /// </summary>
    public static readonly Counter<long> EventHandlingCount = Instance.CreateCounter<long>(
        "feishu.event.handling",
        unit: "{event}",
        description: "飞书事件处理总次数");

    /// <summary>
    /// 事件处理耗时直方图（毫秒，维度：app_key, event_type, handler_type）。
    /// </summary>
    public static readonly Histogram<double> EventHandlingDuration = Instance.CreateHistogram<double>(
        "feishu.event.handling.duration",
        unit: "ms",
        description: "飞书事件处理耗时分布");

    /// <summary>
    /// 事件去重命中计数（维度：app_key, dedup_type, outcome）。
    /// </summary>
    public static readonly Counter<long> EventDeduplicationCount = Instance.CreateCounter<long>(
        "feishu.event.deduplication",
        unit: "{operation}",
        description: "飞书事件去重命中/未命中计数");

    // ── WebSocket 指标 ──

    /// <summary>
    /// WebSocket 活跃连接数（维度：app_key）。
    /// </summary>
    public static readonly ObservableGauge<int> WebSocketConnectionGauge;

    /// <summary>
    /// WebSocket 连接数提供器（按 app_key 分组）。
    /// </summary>
    public static Func<IEnumerable<Measurement<int>>>? WebSocketConnectionObserver { get; set; }

    /// <summary>
    /// WebSocket 消息处理耗时直方图（毫秒，维度：app_key, message_type）。
    /// </summary>
    public static readonly Histogram<double> WebSocketMessageDuration = Instance.CreateHistogram<double>(
        "feishu.websocket.message.duration",
        unit: "ms",
        description: "WebSocket 消息处理耗时分布");

    /// <summary>
    /// WebSocket 重连次数计数（维度：app_key, outcome）。
    /// </summary>
    public static readonly Counter<long> WebSocketReconnectCount = Instance.CreateCounter<long>(
        "feishu.websocket.reconnect",
        unit: "{reconnect}",
        description: "WebSocket 重连次数");

    /// <summary>
    /// WebSocket 待处理消息积压数（维度：app_key）。
    /// </summary>
    public static readonly ObservableGauge<int> WebSocketBacklogGauge;

    /// <summary>
    /// WebSocket 消息积压数提供器（按 app_key 分组）。
    /// </summary>
    public static Func<IEnumerable<Measurement<int>>>? WebSocketBacklogObserver { get; set; }

    // ── Webhook 指标 ──

    /// <summary>
    /// Webhook 请求计数（维度：app_key, outcome）。
    /// </summary>
    public static readonly Counter<long> WebhookRequestCount = Instance.CreateCounter<long>(
        "feishu.webhook.request",
        unit: "{request}",
        description: "Webhook 入站请求计数");

    /// <summary>
    /// Webhook 请求处理耗时直方图（毫秒，维度：app_key, outcome）。
    /// </summary>
    public static readonly Histogram<double> WebhookRequestDuration = Instance.CreateHistogram<double>(
        "feishu.webhook.request.duration",
        unit: "ms",
        description: "Webhook 请求处理耗时分布");

    static FeishuMetrics()
    {
        WebSocketConnectionGauge = Instance.CreateObservableGauge<int>(
            "feishu.websocket.connections",
            observeValues: () => WebSocketConnectionObserver?.Invoke() ?? [],
            unit: "{connection}",
            description: "WebSocket 活跃连接数");

        WebSocketBacklogGauge = Instance.CreateObservableGauge<int>(
            "feishu.websocket.backlog",
            observeValues: () => WebSocketBacklogObserver?.Invoke() ?? [],
            unit: "{message}",
            description: "WebSocket 待处理消息积压数");
    }

    /// <summary>
    /// OTel 语义约定与 Feishu 自定义标签的常量集合。
    /// </summary>
    public static class Tags
    {
        /// <summary>飞书应用 AppKey（多应用区分维度）</summary>
        public const string AppKey = "feishu.app_key";

        /// <summary>飞书应用 AppId</summary>
        public const string AppId = "feishu.app_id";

        /// <summary>事件类型</summary>
        public const string EventType = "feishu.event.type";

        /// <summary>事件处理器类型名</summary>
        public const string HandlerType = "feishu.event.handler_type";

        /// <summary>去重类型（redis/memory/seqid）</summary>
        public const string DedupType = "feishu.dedup.type";

        /// <summary>操作结果（success/failure/deduplicated）</summary>
        public const string Outcome = "outcome";

        /// <summary>错误类型名</summary>
        public const string ErrorType = "error.type";

        /// <summary>WebSocket 消息类型</summary>
        public const string MessageType = "feishu.websocket.message_type";

        /// <summary>事件 ID</summary>
        public const string EventId = "feishu.event.id";

        /// <summary>租户 Key</summary>
        public const string TenantKey = "feishu.tenant_key";
    }
}
