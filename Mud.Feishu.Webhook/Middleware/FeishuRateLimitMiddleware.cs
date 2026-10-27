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
/// <remarks>
/// <para>
/// <b>实例生命周期</b>：由 <c>UseMiddleware</c> 经 <c>ActivatorUtilities</c> 创建，与 DI 容器无关，
/// 因此<b>不得</b>在容器中注册本类型（会多出一个持有独立计数表的常驻实例）。
/// </para>
/// <para>
/// <b>为什么没有定时器</b>（W-02/F-3）：此前用 <c>System.Threading.Timer</c> 每分钟清理过期窗口，
/// 而定时器回调抛出的异常会落到线程池并<b>终止进程</b>（回调内还要读
/// <c>IOptionsMonitor.CurrentValue</c>，热更成非法配置时必抛）。现改为
/// <b>请求路径上的机会式清扫</b>：每 <c>WindowSizeSeconds</c> 最多清扫一次（<c>Interlocked</c> 节流）。
/// 好处有三：① 无定时器 ⇒ 无回调异常面、无 <c>IDisposable</c>、无孤儿资源；
/// ② 清扫频率自适应窗口配置（热更即生效）；③ 无请求时集合本来就不会增长（上限由 <c>MaxIpEntries</c> 准入兜底）。
/// </para>
/// </remarks>
public class FeishuRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly ILogger<FeishuRateLimitMiddleware> _logger;

    private readonly ConcurrentDictionary<(string AppKey, string IP), RateLimitCounter> _requestCounts = new();

    private const int MaxIpEntries = 100000;

    /// <summary>
    /// 上次清扫时间（UTC Tick，<see cref="Interlocked"/> 节流；热更重放安全）。
    /// </summary>
    private long _lastSweepTicks;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _optionsMonitor.CurrentValue;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">管道中的下一个中间件</param>
    /// <param name="optionsMonitor">Webhook 配置监视器（支持热更新）</param>
    /// <param name="logger">日志</param>
    public FeishuRateLimitMiddleware(
        RequestDelegate next,
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        ILogger<FeishuRateLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _lastSweepTicks = DateTime.UtcNow.Ticks;
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

        // 提取应用键——同时也是"是否本模块路径"的判定（与 FeishuMultiAppMiddleware 共用
        // WebhookPathHelper 单一实现；W-04 起 GlobalRoutePrefix 已在 PostConfigure 归一化，
        // 不再需要在此各自拼接 "/{prefix}"，消除两套拼接逻辑的漂移）
        var appKey = ExtractAppKeyFromPath(context.Request.Path.Value ?? string.Empty, options.GlobalRoutePrefix);

        // 非本模块路径（含 AppKey 段非法/缺失）：直接放行，交由路由 404
        if (appKey is null)
        {
            await _next(context);
            return;
        }

        // 获取客户端 IP
        var clientIp = GetClientIp(context);
        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogWarning("无法获取客户端 IP，拒绝请求");
            await WriteTooManyRequestsResponse(context, "无法识别客户端 IP", rateLimitOptions, DateTime.UtcNow);
            return;
        }

        // 检查是否在白名单中（WHF-R2/C1：复用 IpAddressHelper 归一化，支持 IPv4-mapped 与 CIDR）
        if (!string.IsNullOrEmpty(clientIp) && IpAddressHelper.IsIpAllowed(clientIp, rateLimitOptions.WhitelistIPs))
        {
            _logger.LogDebug("客户端 IP {ClientIP} 在白名单中，跳过限流", clientIp);
            await _next(context);
            return;
        }

        var now = DateTime.UtcNow;

        // 机会式清扫（替代原 Timer）：每窗口最多一次，热更窗口大小后自动生效
        TrySweepExpiredWindows(now, rateLimitOptions);

        // 根据 EnableIpRateLimit 配置决定是否基于 IP 限流
        // clientIp 已在上方 IsNullOrEmpty 守卫中确保非空，此处 ! 消除元组与字典键 (string, string) 的可空性差异。
        var rateLimitKey = rateLimitOptions.EnableIpRateLimit
            ? (appKey, clientIp!)
            : (appKey, "global");

        // 满员时仅拒绝「导致新键插入」的请求——已跟踪键（正常客户端）不受
        // 攻击者用伪造 IP 撑满字典的影响，仍可正常计数与限流。
        // 已存在键的窗口滚动（过期重建）由下方 AddOrUpdate 的 update 路径保留。
        var existed = _requestCounts.TryGetValue(rateLimitKey, out _);
        if (!existed && _requestCounts.Count >= MaxIpEntries)
        {
            _logger.LogWarning("IP 条目数已达上限 {MaxIpEntries}，拒绝新 IP {ClientIP} 的请求", MaxIpEntries, clientIp);
            await WriteTooManyRequestsResponse(context, "服务繁忙，请稍后重试", rateLimitOptions, DateTime.UtcNow);
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
                clientIp, appKey, currentCount, rateLimitOptions.MaxRequestsPerWindow, rateLimitOptions.WindowSizeSeconds);

            // WHF-R2/C2：Retry-After 返回窗口剩余秒数（RFC 9110 语义）
            await WriteTooManyRequestsResponse(context,
                $"{rateLimitOptions.TooManyRequestsMessage}，请在 {rateLimitOptions.WindowSizeSeconds} 秒后重试", rateLimitOptions, counter.WindowStart);
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
        // WHF-R2/A3：下沉到 ClientIpResolver 共享实现
        var rateLimitOptions = Options.RateLimit;
        return ClientIpResolver.GetClientIp(
            context,
            rateLimitOptions.UseForwardedHeaders,
            rateLimitOptions.TrustedProxies);
    }

    private static string? ExtractAppKeyFromPath(string path, string globalRoutePrefix) => WebhookPathHelper.ExtractAppKeyFromPath(path, globalRoutePrefix);

    /// <summary>
    /// 机会式清理过期窗口：每 <see cref="RateLimitOptions.WindowSizeSeconds"/> 最多执行一次。
    /// </summary>
    /// <remarks>
    /// 用 <see cref="Interlocked.CompareExchange(ref long, long, long)"/> 做无锁节流——并发请求
    /// 同时进入时只有一个执行清扫，其余直接返回。节流窗口取配置值 ⇒ 配置热更即生效。
    /// </remarks>
    private void TrySweepExpiredWindows(DateTime now, RateLimitOptions rateLimitOptions)
    {
        var intervalTicks = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.WindowSizeSeconds)).Ticks;
        var last = Interlocked.Read(ref _lastSweepTicks);

        if (now.Ticks - last < intervalTicks)
            return;

        // 抢到节流权的那个调用者执行清扫；抢不到说明别人刚清过
        if (Interlocked.CompareExchange(ref _lastSweepTicks, now.Ticks, last) != last)
            return;

        CleanupExpiredWindows(now, rateLimitOptions);
    }

    /// <summary>
    /// 清理过期的窗口记录
    /// </summary>
    private void CleanupExpiredWindows(DateTime now, RateLimitOptions rateLimitOptions)
    {
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
    private async Task WriteTooManyRequestsResponse(HttpContext context, string message, RateLimitOptions rateLimitOptions, DateTime windowStart)
    {
        context.Response.StatusCode = rateLimitOptions.TooManyRequestsStatusCode;
        context.Response.ContentType = "application/json";

        // Retry-After 头（RFC 9110）：取当前窗口剩余秒数（WHF-R2/C2）
        var remaining = Math.Max(1, (int)(rateLimitOptions.WindowSizeSeconds - (DateTime.UtcNow - windowStart).TotalSeconds));
        context.Response.Headers["Retry-After"] = remaining.ToString();

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
}
