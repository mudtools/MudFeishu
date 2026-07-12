// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mud.Feishu.Abstractions.Metrics;

/// <summary>
/// 飞书 Metrics 辅助类，提供便捷的指标记录方法。
/// 所有方法均以 app_key 为首要维度，确保多应用场景指标可区分。
/// </summary>
public static class FeishuMetricsHelper
{
    /// <summary>
    /// 记录事件处理指标，返回 IDisposable 用于标记耗时。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="handlerType">处理器类型名（可选）</param>
    /// <returns>可释放的耗时记录器</returns>
    public static IDisposable RecordEventHandling(string appKey, string eventType, string? handlerType = null)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.AppKey, appKey },
            { FeishuMetrics.Tags.EventType, eventType },
        };

        if (handlerType != null)
            tags.Add(new(FeishuMetrics.Tags.HandlerType, handlerType));

        FeishuMetrics.EventHandlingCount.Add(1, tags);
        return FeishuMetrics.EventHandlingDuration.RecordDuration(tags);
    }

    /// <summary>
    /// 记录事件处理结果。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="success">是否成功</param>
    /// <param name="errorType">错误类型名（可选，仅失败时填充）</param>
    public static void RecordEventOutcome(string appKey, string eventType, bool success, string? errorType = null)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.AppKey, appKey },
            { FeishuMetrics.Tags.EventType, eventType },
            { FeishuMetrics.Tags.Outcome, success ? "success" : "failure" },
        };

        if (!success && errorType != null)
            tags.Add(new(FeishuMetrics.Tags.ErrorType, errorType));

        FeishuMetrics.EventHandlingCount.Add(1, tags);
    }

    /// <summary>
    /// 记录事件去重命中。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey</param>
    /// <param name="dedupType">去重类型</param>
    /// <param name="hit">是否命中去重</param>
    public static void RecordEventDeduplication(string appKey, string dedupType, bool hit)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.AppKey, appKey },
            { FeishuMetrics.Tags.DedupType, dedupType },
            { FeishuMetrics.Tags.Outcome, hit ? "deduplicated" : "passed" },
        };

        FeishuMetrics.EventDeduplicationCount.Add(1, tags);
    }

    /// <summary>
    /// 记录 WebSocket 消息处理耗时。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey</param>
    /// <param name="messageType">消息类型（可选）</param>
    /// <returns>可释放的耗时记录器</returns>
    public static IDisposable RecordWebSocketMessageProcessing(string appKey, string? messageType = null)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.AppKey, appKey },
        };

        if (messageType != null)
            tags.Add(new(FeishuMetrics.Tags.MessageType, messageType));

        return FeishuMetrics.WebSocketMessageDuration.RecordDuration(tags);
    }

    /// <summary>
    /// 记录 WebSocket 重连。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey</param>
    /// <param name="success">重连是否成功</param>
    public static void RecordWebSocketReconnect(string appKey, bool success)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.AppKey, appKey },
            { FeishuMetrics.Tags.Outcome, success ? "success" : "failure" },
        };

        FeishuMetrics.WebSocketReconnectCount.Add(1, tags);
    }

    /// <summary>
    /// 记录 Webhook 请求。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey</param>
    /// <returns>可释放的耗时记录器</returns>
    public static IDisposable RecordWebhookRequest(string appKey)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.AppKey, appKey },
        };

        FeishuMetrics.WebhookRequestCount.Add(1, tags);
        return FeishuMetrics.WebhookRequestDuration.RecordDuration(tags);
    }
}
