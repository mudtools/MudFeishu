using System.Collections.Concurrent;

namespace FeishuFileServer.Middleware;

/// <summary>
/// API速率限制中间件
/// 限制每个IP地址在一定时间内的请求次数
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _maxRequestsPerMinute;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimitEntries;

    /// <summary>
    /// 初始化速率限制中间件实例
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRequestsPerMinute">每分钟最大请求数</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, int maxRequestsPerMinute = 100)
    {
        _next = next;
        _logger = logger;
        _maxRequestsPerMinute = maxRequestsPerMinute;
        _rateLimitEntries = new ConcurrentDictionary<string, RateLimitEntry>();
    }

    /// <summary>
    /// 处理HTTP请求
    /// 检查请求频率是否超过限制
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var now = DateTime.UtcNow;

        var entry = _rateLimitEntries.GetOrAdd(ipAddress, _ => new RateLimitEntry
        {
            RequestCount = 0,
            WindowStart = now
        });

        bool shouldThrottle = false;
        lock (entry)
        {
            if (now - entry.WindowStart > TimeSpan.FromMinutes(1))
            {
                entry.RequestCount = 0;
                entry.WindowStart = now;
            }

            entry.RequestCount++;

            if (entry.RequestCount > _maxRequestsPerMinute)
            {
                shouldThrottle = true;
            }
        }

        if (shouldThrottle)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode = 429,
                message = "请求过于频繁，请稍后再试",
                retryAfter = 60
            };

            context.Response.Headers["Retry-After"] = "60";
            var json = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 获取客户端IP地址
    /// 支持代理服务器场景
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 速率限制条目
    /// </summary>
    private class RateLimitEntry
    {
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

/// <summary>
/// 速率限制中间件扩展方法
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// 注册速率限制中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <param name="maxRequestsPerMinute">每分钟最大请求数</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseRateLimiter(this IApplicationBuilder builder, int maxRequestsPerMinute = 100)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(maxRequestsPerMinute);
    }
}
