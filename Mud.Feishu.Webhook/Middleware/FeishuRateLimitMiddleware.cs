// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Utils;
using System.Collections.Concurrent;

namespace Mud.Feishu.Webhook;

internal sealed class RateLimitCounter
{
    private int _count;

    public DateTime WindowStart { get; }

    public int Count => Volatile.Read(ref _count);

    public RateLimitCounter(DateTime windowStart)
    {
        WindowStart = windowStart;
        _count = 0;
    }

    public int Increment()
    {
        return Interlocked.Increment(ref _count);
    }
}

/// <summary>
/// 飞书 Webhook 请求频率限制中间件
/// </summary>
public class FeishuRateLimitMiddleware : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly ILogger<FeishuRateLimitMiddleware> _logger;

    private readonly ConcurrentDictionary<(string AppKey, string IP), RateLimitCounter> _requestCounts = new();

    private const int MaxIpEntries = 100000;

    // 定时清理的 Timer
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _optionsMonitor.CurrentValue;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuRateLimitMiddleware(
        RequestDelegate next,
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        ILogger<FeishuRateLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化定时清理任务，每分钟清理一次过期记录
        _cleanupTimer = new Timer(
            CleanupExpiredWindows,
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));

        _logger.LogInformation("飞书 Webhook 限流中间件已启动，定时清理间隔: 1分钟");
    }

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var options = Options;
        var rateLimitOptions = options.RateLimit;

        // 如果未启用限流，直接放行
        if (!rateLimitOptions.EnableRateLimit)
        {
            await _next(context);
            return;
        }

        // 检查是否为 Webhook 请求（使用动态前缀，与 MultiAppMiddleware 保持一致）
        if (!context.Request.Path.StartsWithSegments($"/{options.GlobalRoutePrefix}", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 提取应用键（使用动态前缀）
        var appKey = ExtractAppKeyFromPath(context.Request.Path.Value ?? string.Empty, options.GlobalRoutePrefix);

        // 获取客户端 IP
        var clientIp = GetClientIp(context);
        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogWarning("无法获取客户端 IP，拒绝请求");
            await WriteTooManyRequestsResponse(context, "无法识别客户端 IP", rateLimitOptions);
            return;
        }

        // 检查是否在白名单中
        if (!string.IsNullOrEmpty(clientIp) && rateLimitOptions.WhitelistIPs.Contains(clientIp!))
        {
            _logger.LogDebug("客户端 IP {ClientIP} 在白名单中，跳过限流", clientIp);
            await _next(context);
            return;
        }

        var now = DateTime.UtcNow;

        // 根据 EnableIpRateLimit 配置决定是否基于 IP 限流
        var rateLimitKey = rateLimitOptions.EnableIpRateLimit
            ? (appKey ?? "global", clientIp)
            : (appKey ?? "global", "global");

        if (_requestCounts.Count >= MaxIpEntries)
        {
            _logger.LogWarning("IP 条目数已达上限 {MaxIpEntries}，拒绝新 IP {ClientIP} 的请求", MaxIpEntries, clientIp);
            await WriteTooManyRequestsResponse(context, "服务繁忙，请稍后重试", rateLimitOptions);
            return;
        }

        var counter = _requestCounts.AddOrUpdate(
            rateLimitKey,
            _ => new RateLimitCounter(now),
            (_, existing) =>
            {
                if ((now - existing.WindowStart).TotalSeconds > rateLimitOptions.WindowSizeSeconds)
                    return new RateLimitCounter(now);
                return existing;
            });

        var currentCount = counter.Increment();

        if (currentCount > rateLimitOptions.MaxRequestsPerWindow)
        {
            _logger.LogWarning("客户端 IP {ClientIP}（应用: {AppKey}）请求频率超出限制：{Count}/{MaxRequests} 在 {WindowSize}秒内",
                clientIp, appKey ?? "global", currentCount, rateLimitOptions.MaxRequestsPerWindow, rateLimitOptions.WindowSizeSeconds);

            await WriteTooManyRequestsResponse(context,
                $"{rateLimitOptions.TooManyRequestsMessage}，请在 {rateLimitOptions.WindowSizeSeconds} 秒后重试", rateLimitOptions);
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 获取客户端真实 IP（ADR-3 零信任模型）
    /// 默认不信任任何转发头，仅在直连 IP 命中 TrustedProxies 时解析 X-Forwarded-For
    /// </summary>
    private string? GetClientIp(HttpContext context)
    {
        var rateLimitOptions = Options.RateLimit;
        var directIp = context.Connection.RemoteIpAddress?.ToString();

        // 零信任默认：无可信代理配置 / 转发头关闭 / 直连 IP 非可信代理 → 只认 RemoteIpAddress
        if (!rateLimitOptions.UseForwardedHeaders
            || rateLimitOptions.TrustedProxies.Count == 0
            || string.IsNullOrEmpty(directIp)
            || !IpAddressHelper.IsIpAllowed(directIp, rateLimitOptions.TrustedProxies))
        {
            return directIp;
        }

        // 可信代理后：从右向左取第一个非可信 IP（标准代理链算法，防止最左值被客户端伪造）
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
        {
            var hops = xff.ToString().Split(',');
            for (var i = hops.Length - 1; i >= 0; i--)
            {
                var hop = hops[i].Trim();
                if (string.IsNullOrEmpty(hop)) continue;
                if (!IpAddressHelper.IsIpAllowed(hop, rateLimitOptions.TrustedProxies))
                    return hop;
            }
        }

        return directIp;
    }

    private static string? ExtractAppKeyFromPath(string path, string globalRoutePrefix) => WebhookPathHelper.ExtractAppKeyFromPath(path, globalRoutePrefix);

    /// <summary>
    /// 清理过期的窗口记录（由定时器调用）
    /// </summary>
    private void CleanupExpiredWindows(object? state)
    {
        var rateLimitOptions = Options.RateLimit;
        var now = DateTime.UtcNow;
        var expiredKeys = _requestCounts
            .Where(kvp => (now - kvp.Value.WindowStart).TotalSeconds > rateLimitOptions.WindowSizeSeconds * 2)
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var key in expiredKeys)
        {
            if (_requestCounts.TryRemove(key, out _))
            {
                removedCount++;
            }
        }

        // 如果字典仍然过大,清理最旧的条目（LRU策略）
        if (_requestCounts.Count > MaxIpEntries)
        {
            var excessCount = _requestCounts.Count - MaxIpEntries;
            var oldestEntries = _requestCounts
                .OrderBy(kvp => kvp.Value.WindowStart)
                .Take(excessCount)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestEntries)
            {
                if (_requestCounts.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            _logger.LogWarning("IP 字典超过上限 ({MaxEntries}), 清理了 {ExcessCount} 个最旧条目", MaxIpEntries, excessCount);
        }

        if (removedCount > 0)
        {
            _logger.LogDebug("清理了 {Count} 个过期的限流记录", removedCount);
        }
    }

    /// <summary>
    /// 写入 429 响应
    /// </summary>
    private async Task WriteTooManyRequestsResponse(HttpContext context, string message, RateLimitOptions rateLimitOptions)
    {
        context.Response.StatusCode = rateLimitOptions.TooManyRequestsStatusCode;
        context.Response.ContentType = "application/json";

        // Retry-After 头（RFC 9110）：取当前窗口剩余秒数
        var retryAfter = Math.Max(1, rateLimitOptions.WindowSizeSeconds);
        context.Response.Headers["Retry-After"] = retryAfter.ToString();

        var errorResponse = new WebhookErrorResponse
        {
            Success = false,
            Error = new WebhookErrorDetail
            {
                Code = context.Response.StatusCode,
                Message = message
            }
        };

        await context.Response.WriteAsync(FeishuJsonAot.Serialize(errorResponse, FeishuJsonOptions.Serialize));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
