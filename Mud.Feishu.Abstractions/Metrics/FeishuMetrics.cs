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
    /// WebSocket 连接数指标源注册表（P2-3 修复）。
    /// </summary>
    /// <remarks>
    /// 此前为<b>单个静态可写属性</b>（<c>WebSocketConnectionObserver</c>），存在两个缺陷：
    /// ① 同一进程内注册多个应用（多个 ServiceProvider / 多个 HostedService）时互相覆盖，只保留最后一个；
    /// ② 静态属性长期持有服务实例，HostedService 释放后仍无法回收。
    /// <para>现在改为"按注册实例独立登记 + 观测时聚合"，并用 <see cref="IDisposable"/> 令牌在释放时移除。</para>
    /// </remarks>
    private static readonly List<WebSocketMetricsSource> WebSocketSources = new();

    private static readonly object WebSocketSourcesLock = new();

    /// <summary>
    /// 单个 WebSocket 指标源的取值委托集合。
    /// </summary>
    private sealed class WebSocketMetricsSource
    {
        public WebSocketMetricsSource(
            Func<string> appKeyProvider,
            Func<int> activeConnectionsProvider,
            Func<int> pendingMessagesProvider)
        {
            AppKeyProvider = appKeyProvider;
            ActiveConnectionsProvider = activeConnectionsProvider;
            PendingMessagesProvider = pendingMessagesProvider;
        }

        public Func<string> AppKeyProvider { get; }

        public Func<int> ActiveConnectionsProvider { get; }

        public Func<int> PendingMessagesProvider { get; }
    }

    /// <summary>
    /// 注销令牌：释放时把对应指标源从观测结果中移除（幂等）。
    /// </summary>
    private sealed class WebSocketMetricsRegistration : IDisposable
    {
        private readonly WebSocketMetricsSource _source;
        private int _disposed;

        public WebSocketMetricsRegistration(WebSocketMetricsSource source) => _source = source;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            lock (WebSocketSourcesLock)
            {
                WebSocketSources.Remove(_source);
            }
        }
    }

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
    /// 注册一个 WebSocket 指标源（P2-3 修复；替代原 <c>WebSocketConnectionObserver</c> / <c>WebSocketBacklogObserver</c> 静态属性）。
    /// </summary>
    /// <param name="appKeyProvider">
    /// AppKey 提供器。<b>每次采集时调用</b>，因此支持配置热更新（AppKey 变更无需重新注册）。
    /// </param>
    /// <param name="activeConnectionsProvider">活跃连接数提供器（当前实例为 1 或 0）。</param>
    /// <param name="pendingMessagesProvider">待处理（在途处理中）消息数提供器。</param>
    /// <returns>注销令牌；释放后该源立即不再参与采集，重复释放安全。</returns>
    /// <exception cref="ArgumentNullException">任一提供器为 <c>null</c> 时抛出。</exception>
    /// <remarks>
    /// 每个注册实例独立登记，互不覆盖：同一进程内多个应用会各自产生一条带
    /// <see cref="Tags.AppKey"/> 维度的 Measurement，由 ObservableGauge 聚合上报。
    /// <para>
    /// <b>约束</b>：同一 AppKey 不应重复注册（会产生重复序列）。HostedService 应在构造期注册、
    /// 在 <c>Dispose</c> 中释放令牌。
    /// </para>
    /// </remarks>
    public static IDisposable RegisterWebSocketMetricsSource(
        Func<string> appKeyProvider,
        Func<int> activeConnectionsProvider,
        Func<int> pendingMessagesProvider)
    {
        if (appKeyProvider == null)
            throw new ArgumentNullException(nameof(appKeyProvider));
        if (activeConnectionsProvider == null)
            throw new ArgumentNullException(nameof(activeConnectionsProvider));
        if (pendingMessagesProvider == null)
            throw new ArgumentNullException(nameof(pendingMessagesProvider));

        var source = new WebSocketMetricsSource(appKeyProvider, activeConnectionsProvider, pendingMessagesProvider);

        lock (WebSocketSourcesLock)
        {
            WebSocketSources.Add(source);
        }

        return new WebSocketMetricsRegistration(source);
    }

    /// <summary>
    /// 采集全部已注册 WebSocket 指标源（连接数）。
    /// </summary>
    /// <returns>按 app_key 分组的测量值集合</returns>
    internal static IEnumerable<Measurement<int>> ObserveWebSocketConnections()
        => Observe(kind: 0);

    /// <summary>
    /// 采集全部已注册 WebSocket 指标源（在途积压数）。
    /// </summary>
    /// <returns>按 app_key 分组的测量值集合</returns>
    internal static IEnumerable<Measurement<int>> ObserveWebSocketBacklog()
        => Observe(kind: 1);

    /// <summary>
    /// 按 <paramref name="kind"/> 采集所有已注册指标源（0=连接数，1=积压数）。
    /// </summary>
    /// <remarks>
    /// 先在锁内取快照，再在锁外调用用户提供器：避免提供器内再次注册/注销造成重入等待，
    /// 也避免提供器阻塞采集线程时长时间持锁。
    /// </remarks>
    private static IEnumerable<Measurement<int>> Observe(int kind)
    {
        WebSocketMetricsSource[] snapshot;
        lock (WebSocketSourcesLock)
        {
            if (WebSocketSources.Count == 0)
            {
                return Array.Empty<Measurement<int>>();
            }

            snapshot = WebSocketSources.ToArray();
        }

        var results = new List<Measurement<int>>(snapshot.Length);
        foreach (var source in snapshot)
        {
            try
            {
                var appKey = source.AppKeyProvider();
                var value = kind == 0 ? source.ActiveConnectionsProvider() : source.PendingMessagesProvider();
                results.Add(new Measurement<int>(
                    value,
                    new KeyValuePair<string, object?>(Tags.AppKey, appKey)));
            }
            catch (Exception)
            {
                // 指标采集不得因单个源故障而中断（也不得把异常抛进 OTel 采集回调）
            }
        }

        return results;
    }

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
            observeValues: ObserveWebSocketConnections,
            unit: "{connection}",
            description: "WebSocket 活跃连接数");

        WebSocketBacklogGauge = Instance.CreateObservableGauge<int>(
            "feishu.websocket.backlog",
            observeValues: ObserveWebSocketBacklog,
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
