// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
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
    /// 记录令牌获取指标
    /// </summary>
    public static IDisposable RecordTokenFetch(string tokenType, bool fromCache)
    {
        FeishuMetrics.TokenFetchCount.Add(1, new TagList { { "token_type", tokenType } });

        if (fromCache)
        {
            FeishuMetrics.TokenCacheHitCount.Add(1, new TagList { { "token_type", tokenType } });
        }
        else
        {
            FeishuMetrics.TokenCacheMissCount.Add(1, new TagList { { "token_type", tokenType } });
        }

        return FeishuMetrics.TokenFetchDuration.RecordDuration();
    }

    /// <summary>
    /// 记录令牌刷新指标
    /// </summary>
    public static void RecordTokenRefresh(string tokenType)
    {
        FeishuMetrics.TokenRefreshCount.Add(1, new TagList { { "token_type", tokenType } });
    }

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
    /// 记录 HTTP 请求成功
    /// </summary>
    public static void RecordHttpRequestSuccess(string method, int statusCode)
    {
        FeishuMetrics.HttpRequestSuccessCount.Add(1, new TagList { { "method", method }, { "status_code", statusCode.ToString() } });
    }

    /// <summary>
    /// 记录 HTTP 请求失败
    /// </summary>
    public static void RecordHttpRequestFailure(string method, int statusCode, string? errorType = null)
    {
        var tags = new TagList { { "method", method }, { "status_code", statusCode.ToString() } };

        if (errorType != null)
        {
            tags.Add(new("error_type", errorType));
        }

        FeishuMetrics.HttpRequestFailureCount.Add(1, tags);
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
    /// 记录 WebSocket 消息发送
    /// </summary>
    public static void RecordWebSocketMessageSent(long bytes = 0)
    {
        FeishuMetrics.WebSocketMessageSentCount.Add(1);
        if (bytes > 0)
        {
            FeishuMetrics.WebSocketBytesSentCount.Add(bytes);
        }
    }

    /// <summary>
    /// 记录 WebSocket 消息接收
    /// </summary>
    public static void RecordWebSocketMessageReceived(long bytes = 0)
    {
        FeishuMetrics.WebSocketMessageReceivedCount.Add(1);
        if (bytes > 0)
        {
            FeishuMetrics.WebSocketBytesReceivedCount.Add(bytes);
        }
    }

    /// <summary>
    /// 记录 WebSocket 连接错误
    /// </summary>
    public static void RecordWebSocketConnectionError()
    {
        FeishuMetrics.WebSocketConnectionErrorCount.Add(1);
    }

    /// <summary>
    /// 记录 WebSocket 认证错误
    /// </summary>
    public static void RecordWebSocketAuthenticationError()
    {
        FeishuMetrics.WebSocketAuthenticationErrorCount.Add(1);
    }

    /// <summary>
    /// 记录 WebSocket 消息处理持续时间
    /// </summary>
    public static IDisposable RecordWebSocketMessageProcessing()
    {
        return FeishuMetrics.WebSocketMessageProcessingDuration.RecordDuration();
    }
}
