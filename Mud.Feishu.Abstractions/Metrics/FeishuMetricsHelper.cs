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
/// 飞书 Metrics 辅助类，提供便捷的指标记录方法
/// </summary>
public static class FeishuMetricsHelper
{
    /// <summary>
    /// 记录事件处理指标
    /// </summary>
    public static IDisposable RecordEventHandling(string eventType, string? handlerType = null)
    {
        var tags = new TagList { { "event_type", eventType } };

        if (handlerType != null)
        {
            tags.Add(new("handler_type", handlerType));
        }

        FeishuMetrics.EventHandlingCount.Add(1, tags);

        return FeishuMetrics.EventHandlingDuration.RecordDuration(tags);
    }

    /// <summary>
    /// 记录事件处理成功
    /// </summary>
    public static void RecordEventHandlingSuccess(string eventType)
    {
        FeishuMetrics.EventHandlingSuccessCount.Add(1, new TagList { { "event_type", eventType } });
    }

    /// <summary>
    /// 记录事件处理失败
    /// </summary>
    public static void RecordEventHandlingFailure(string eventType, string? errorType = null)
    {
        var tags = new TagList { { "event_type", eventType } };

        if (errorType != null)
        {
            tags.Add(new("error_type", errorType));
        }

        FeishuMetrics.EventHandlingFailureCount.Add(1, tags);
    }

    /// <summary>
    /// 记录事件去重命中
    /// </summary>
    public static void RecordEventDeduplicationHit(string dedupType)
    {
        FeishuMetrics.EventDeduplicationHitCount.Add(1, new TagList { { "dedup_type", dedupType } });
    }

    /// <summary>
    /// 记录 HTTP 请求指标
    /// </summary>
    public static IDisposable RecordHttpRequest(string method, string url)
    {
        var tags = new TagList
        {
            { "method", method },
            { "url", TruncateUrl(url, 50) }
        };

        FeishuMetrics.HttpRequestCount.Add(1, tags);

        return FeishuMetrics.HttpRequestDuration.RecordDuration(tags);
    }

    /// <summary>
    /// 截断 URL 以避免标签过长
    /// </summary>
    private static string TruncateUrl(string url, int maxLength)
    {
        if (url.Length <= maxLength)
            return url;

        return string.Concat(url.Substring(0, maxLength), "...");
    }

    /// <summary>
    /// 记录 WebSocket 消息处理持续时间
    /// </summary>
    public static IDisposable RecordWebSocketMessageProcessing()
    {
        return FeishuMetrics.WebSocketMessageProcessingDuration.RecordDuration();
    }
}
