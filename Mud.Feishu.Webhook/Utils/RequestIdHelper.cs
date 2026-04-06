// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Mud.Feishu.Webhook.Utils;

/// <summary>
/// 请求ID辅助工具类
/// 提供统一的 RequestId 生成、提取和管理功能
/// </summary>
public static class RequestIdHelper
{
    /// <summary>
    /// 常见的追踪请求头名称（按优先级排序）
    /// </summary>
    private static readonly string[] TraceHeaderNames = new[]
    {
        "X-Request-Id",           // 飞书请求ID
        "X-Trace-Id",             // 通用追踪ID
        "X-Correlation-Id",       // 关联ID
        "X-B3-TraceId",           // Zipkin B3追踪
        "X-Cloud-Trace-Context",  // Google Cloud Trace
        "Request-Id",             // 标准请求ID
        "Traceparent"             // W3C Trace Context
    };

    /// <summary>
    /// 请求ID存储在 HttpContext.Items 中的键名
    /// </summary>
    public const string RequestIdItemKey = "RequestId";

    /// <summary>
    /// 从请求中提取或生成 RequestId
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>RequestId</returns>
    public static string GetOrGenerateRequestId(HttpContext context)
    {
        // 1. 首先检查 HttpContext.Items 中是否已有 RequestId
        if (context.Items.TryGetValue(RequestIdItemKey, out var existingRequestId) &&
            existingRequestId is string existingId && !string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        // 2. 从请求头中提取追踪ID（按优先级）
        foreach (var headerName in TraceHeaderNames)
        {
            var headerValue = context.Request.Headers[headerName].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerValue))
            {
                var extractedId = ExtractTraceId(headerName, headerValue!);
                if (!string.IsNullOrEmpty(extractedId))
                {
                    context.Items[RequestIdItemKey] = extractedId;
                    return extractedId!;
                }
            }
        }

        // 3. 使用 ASP.NET Core 的 TraceIdentifier
        var traceId = context.TraceIdentifier;
        context.Items[RequestIdItemKey] = traceId;
        return traceId;
    }

    /// <summary>
    /// 从特定追踪头中提取追踪ID
    /// </summary>
    /// <param name="headerName">请求头名称</param>
    /// <param name="headerValue">请求头值</param>
    /// <returns>提取的追踪ID</returns>
    private static string? ExtractTraceId(string headerName, string headerValue)
    {
        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }

        switch (headerName)
        {
            case "X-Request-Id":
            case "X-Trace-Id":
            case "X-Correlation-Id":
            case "X-B3-TraceId":
            case "Request-Id":
                // 直接使用头值
                return ValidateRequestIdFormat(headerValue) ? headerValue : null;

            case "X-Cloud-Trace-Context":
                // 格式: TRACE_ID/span_id;o=trace_options
                // 示例: 105445aa7843bc8bf206b12000100000/123456789;o=1
                var parts = headerValue.Split('/');
                if (parts.Length > 0)
                {
                    return ValidateRequestIdFormat(parts[0]) ? parts[0] : null;
                }
                return null;

            case "Traceparent":
                // W3C Trace Context 格式: version-traceid-spanid-traceflags
                // 示例: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
                var traceParentParts = headerValue.Split('-');
                if (traceParentParts.Length >= 3)
                {
                    return ValidateRequestIdFormat(traceParentParts[1]) ? traceParentParts[1] : null;
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// 验证 RequestId 格式是否有效
    /// </summary>
    /// <param name="requestId">请求ID</param>
    /// <returns>是否有效</returns>
    private static bool ValidateRequestIdFormat(string requestId)
    {
        if (string.IsNullOrEmpty(requestId))
        {
            return false;
        }

        // 检查长度（防止过长的恶意输入）
        if (requestId.Length > 128)
        {
            return false;
        }

        // 检查是否只包含安全字符（字母、数字、连字符、下划线）
        return Regex.IsMatch(requestId, @"^[a-zA-Z0-9\-_]+$");
    }

    /// <summary>
    /// 生成新的 RequestId（GUID 格式）
    /// </summary>
    /// <returns>新的 RequestId</returns>
    public static string GenerateRequestId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// 为响应添加 RequestId 头
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    public static void AddRequestIdToResponse(HttpContext context)
    {
        var requestId = context.Items[RequestIdItemKey] as string;
        if (!string.IsNullOrEmpty(requestId) && !context.Response.HasStarted)
        {
            context.Response.Headers["X-Request-Id"] = requestId;
        }
    }

    /// <summary>
    /// 获取 RequestId 的来源类型
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>RequestId 来源描述</returns>
    public static string GetRequestIdSource(HttpContext context)
    {
        var requestId = context.Items[RequestIdItemKey] as string;
        if (string.IsNullOrEmpty(requestId))
        {
            return "None";
        }

        // 检查是否来自飞书
        if (context.Request.Headers["X-Request-Id"].FirstOrDefault() == requestId)
        {
            return "Feishu-X-Request-Id";
        }

        // 检查其他追踪头
        foreach (var headerName in TraceHeaderNames)
        {
            var headerValue = context.Request.Headers[headerName].FirstOrDefault();
            if (headerValue == requestId)
            {
                return headerName;
            }
        }

        // 检查是否为 ASP.NET Core TraceIdentifier
        if (requestId == context.TraceIdentifier)
        {
            return "ASPNETCORE-TraceIdentifier";
        }

        return "Unknown";
    }

    /// <summary>
    /// 为 Activity 设置 RequestId 标签
    /// </summary>
    /// <param name="activity">分布式追踪 Activity</param>
    /// <param name="context">HTTP 上下文</param>
    public static void SetActivityRequestId(Activity? activity, HttpContext context)
    {
        if (activity == null)
        {
            return;
        }

        var requestId = context.Items[RequestIdItemKey] as string;
        if (!string.IsNullOrEmpty(requestId))
        {
            activity.SetTag("request.id", requestId);
            activity.SetTag("request.id.source", GetRequestIdSource(context));
        }
    }
}
