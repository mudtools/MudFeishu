// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Middleware;

/// <summary>
/// 请求限流中间件
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;

    private static readonly ConcurrentDictionary<string, RateLimitCounter> Counters = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly object CleanupLock = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options ?? new RateLimitOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        TryCleanupExpiredCounters();

        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointKey(context);

        var key = $"{clientId}:{endpoint}";

        var counter = Counters.GetOrAdd(key, _ => new RateLimitCounter
        {
            WindowStart = DateTime.UtcNow
        });

        lock (counter)
        {
            var now = DateTime.UtcNow;
            var windowEnd = counter.WindowStart.Add(_options.Window);

            if (now > windowEnd)
            {
                counter.RequestCount = 0;
                counter.WindowStart = now;
            }

            counter.RequestCount++;

            var remainingRequests = Math.Max(0, _options.MaxRequests - counter.RequestCount);
            var resetTime = windowEnd;

            context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remainingRequests.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)resetTime).ToUnixTimeSeconds().ToString();

            if (counter.RequestCount > _options.MaxRequests)
            {
                _logger.LogWarning("请求限流触发: Client={ClientId}, Endpoint={Endpoint}, Count={Count}",
                    clientId, endpoint, counter.RequestCount);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ((int)(resetTime - now).TotalSeconds).ToString();

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = "请求过于频繁，请稍后再试"
                };

                context.Response.ContentType = "application/json; charset=utf-8";
                return;
            }
        }

        await _next(context);
    }

    private void TryCleanupExpiredCounters()
    {
        var now = DateTime.UtcNow;
        var cleanupInterval = _options.Window.Add(TimeSpan.FromMinutes(5));

        if (now - _lastCleanup < cleanupInterval)
        {
            return;
        }

        lock (CleanupLock)
        {
            if (now - _lastCleanup < cleanupInterval)
            {
                return;
            }

            var expiredKeys = Counters
                .Where(kvp => now > kvp.Value.WindowStart.Add(_options.Window))
                .Select(kvp => kvp.Key)
                .ToList();

            var removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (Counters.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogDebug("清理过期限流计数器: {Count} 个", removedCount);
            }

            _lastCleanup = now;
        }
    }

    private string GetClientIdentifier(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.ToString();
        }

        var connection = context.Connection;
        return connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            var controller = segments.Length > 1 ? segments[1] : "unknown";
            return $"{method}:{controller}";
        }

        return $"{method}:{path}";
    }
}

/// <summary>
/// 限流选项
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// 时间窗口
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 最大请求数
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 限流计数器
/// </summary>
public class RateLimitCounter
{
    /// <summary>
    /// 请求计数
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// 窗口开始时间
    /// </summary>
    public DateTime WindowStart { get; set; }
}

/// <summary>
/// 限流中间件扩展
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// 使用请求限流
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitOptions? options = null)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(options ?? new RateLimitOptions());
    }
}
