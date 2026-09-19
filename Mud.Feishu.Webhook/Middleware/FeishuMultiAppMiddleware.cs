// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;
// 使用类型别名而非命名空间 using：Mud.Feishu.Abstractions.Utilities 与 Mud.Feishu.Webhook.Serialization
// 都存在 FeishuJsonContext，直接引入命名空间会造成 CS0104 二义性。
using FeishuJsonAot = Mud.Feishu.Abstractions.Utilities.FeishuJsonAot;
using Mud.Feishu.Abstractions.Observability;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Exceptions;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Serialization;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utils;
using System.Diagnostics;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书多应用 Webhook 中间件
/// </summary>
public class FeishuMultiAppMiddleware : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeishuMultiAppMiddleware> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry;
    private readonly IDisposable? _onChangeSubscription;
    private bool _disposed;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _options.CurrentValue;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuMultiAppMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<FeishuMultiAppMiddleware> logger,
        IOptionsMonitor<FeishuWebhookOptions> options,
        FeishuWebhookHandlerRegistry handlerRegistry)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
        _handlerRegistry = handlerRegistry;

        // 监听配置变更（WHF-13：持有订阅句柄，宿主关停时释放，防止句柄泄漏）
        _onChangeSubscription = _options.OnChange((newOptions, name) =>
        {
            var oldOptions = Options;
            var changes = new List<string>();

            // 检测关键配置项的变更
            if (oldOptions.GlobalRoutePrefix != newOptions.GlobalRoutePrefix)
            {
                changes.Add($"GlobalRoutePrefix: {oldOptions.GlobalRoutePrefix} → {newOptions.GlobalRoutePrefix}");
            }

            if (oldOptions.AutoRegisterEndpoint != newOptions.AutoRegisterEndpoint)
            {
                changes.Add($"AutoRegisterEndpoint: {oldOptions.AutoRegisterEndpoint} → {newOptions.AutoRegisterEndpoint}");
            }

            if (oldOptions.MaxRequestBodySize != newOptions.MaxRequestBodySize)
            {
                changes.Add($"MaxRequestBodySize: {oldOptions.MaxRequestBodySize} bytes → {newOptions.MaxRequestBodySize} bytes");
            }

            if (oldOptions.AllowedHttpMethods.Count != newOptions.AllowedHttpMethods.Count ||
                !oldOptions.AllowedHttpMethods.SetEquals(newOptions.AllowedHttpMethods))
            {
                changes.Add($"AllowedHttpMethods: [{string.Join(", ", oldOptions.AllowedHttpMethods)}] → [{string.Join(", ", newOptions.AllowedHttpMethods)}]");
            }

            // 检测 IP 白名单的变更
            if (!oldOptions.AllowedSourceIPs.SetEquals(newOptions.AllowedSourceIPs))
            {
                changes.Add($"AllowedSourceIPs: [{string.Join(", ", oldOptions.AllowedSourceIPs)}] → [{string.Join(", ", newOptions.AllowedSourceIPs)}]");
            }

            // 检测限流配置的变更
            if (oldOptions.RateLimit.EnableRateLimit != newOptions.RateLimit.EnableRateLimit)
            {
                changes.Add($"RateLimit.EnableRateLimit: {oldOptions.RateLimit.EnableRateLimit} → {newOptions.RateLimit.EnableRateLimit}");
            }

            if (oldOptions.RateLimit.WindowSizeSeconds != newOptions.RateLimit.WindowSizeSeconds)
            {
                changes.Add($"RateLimit.WindowSizeSeconds: {oldOptions.RateLimit.WindowSizeSeconds}s → {newOptions.RateLimit.WindowSizeSeconds}s");
            }

            if (oldOptions.RateLimit.MaxRequestsPerWindow != newOptions.RateLimit.MaxRequestsPerWindow)
            {
                changes.Add($"RateLimit.MaxRequestsPerWindow: {oldOptions.RateLimit.MaxRequestsPerWindow} → {newOptions.RateLimit.MaxRequestsPerWindow}");
            }

            if (changes.Count > 0)
            {
                _logger.LogInformation("飞书多应用 Webhook 配置已更新，来源: {ChangeSource}，变更内容:\n{Changes}", name, string.Join("\n  - ", changes));
            }
            else
            {
                _logger.LogDebug("飞书多应用 Webhook 配置已更新，来源: {ChangeSource}（无关键配置变更）", name);
            }
        });
    }

    /// <summary>
    /// 释放资源（WHF-13：释放 IOptionsMonitor.OnChange 订阅，宿主关停时由 WebHost 调用）
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _onChangeSubscription?.Dispose();
    }

    /// <summary>
    /// 飞书多应用 Webhook 中间件
    /// 支持根据路径中的 AppKey 动态路由到不同应用的 Webhook 处理
    /// </summary>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;

        _logger.LogDebug("当前请求路径: {Path}", path);

        // 尝试从路径中提取 AppKey
        var appKey = ExtractAppKeyFromPath(path);
        _logger.LogDebug("当前应用键 AppKey: {AppKey}", appKey);
        if (string.IsNullOrEmpty(appKey))
        {
            await _next(context);
            return;
        }

        // 验证应用是否存在
        if (!Options.Apps.ContainsKey(appKey ?? string.Empty))
        {
            _logger.LogWarning("未知的应用键: {AppKey}", appKey);
            await _next(context);
            return;
        }

        // 获取应用配置
        var requestId = RequestIdHelper.GetOrGenerateRequestId(context);
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        using var activity = FeishuActivitySource.Instance.StartActivity(
            FeishuActivitySource.ActivityNameWebhookRequest,
            ActivityKind.Server);
        activity?.SetTag(FeishuActivitySource.Tags.AppKey, appKey);
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.path", path);
        activity?.SetTag("request.client_ip", clientIp);

        // 生成 CorrelationId，贯穿日志与 Trace
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        activity?.SetTag(FeishuActivitySource.Tags.CorrelationId, correlationId);

        // 使用日志作用域自动注入 AppKey、RequestId 和 CorrelationId
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["AppKey"] = appKey ?? "unknown",
            ["RequestId"] = requestId,
            ["ClientIp"] = clientIp,
            ["Path"] = path
        });

        // P0-1 修复：记录 Webhook 请求指标（计数 + 耗时），使用实际 AppKey 作为维度
        using var webhookMetrics = FeishuMetricsHelper.RecordWebhookRequest(appKey ?? "unknown");

        try
        {
            // 验证客户端 IP（如果配置了白名单）
            if (Options.AllowedSourceIPs.Count > 0)
            {
                if (!IpAddressHelper.IsIpAllowed(clientIp, Options.AllowedSourceIPs))
                {
                    _logger.LogWarning("客户端 IP {ClientIP} 不在白名单中，拒绝请求, AppKey: {AppKey}",
                        clientIp, appKey ?? "null");
                    await WriteErrorResponse(context, 403, "Forbidden: IP not allowed", requestId);
                    return;
                }
            }

            // 验证 HTTP 方法
            if (!Options.AllowedHttpMethods.Contains(context.Request.Method))
            {
                await WriteErrorResponse(context, 405, "Method Not Allowed", requestId);
                return;
            }

            // 验证 Content-Type
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) || !contentType.ToLowerInvariant().Contains("application/json"))
            {
                await WriteErrorResponse(context, 415, "Unsupported Media Type", requestId);
                return;
            }

            // 读取请求体
            var requestBody = await ReadRequestBodyAsync(context.Request);
            if (string.IsNullOrEmpty(requestBody))
            {
                // WHF-17：错误文案收敛（阶段差异仅记录在日志）
                _logger.LogWarning("请求体为空, AppKey: {AppKey}", appKey ?? "unknown");
                await WriteErrorResponse(context, 400, "Bad Request", requestId);
                return;
            }

            _logger.LogInformation("收到应用的 Webhook 请求");

            // 处理请求
            await ProcessWebhookRequestAsync(
                context,
                requestBody,
                requestId,
                appKey ?? string.Empty);
        }
        catch (FeishuWebhookValidationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning("请求体验证失败: {Message}, AppKey: {AppKey}", ex.Message, appKey ?? "unknown");
            await WriteErrorResponse(context, 413, "Request Entity Too Large", requestId);
        }
        catch (FeishuRedisException ex) when (ex.FailureKind == FeishuRedisFailureKind.Server)
        {
            // WHF-02：去重服务致命故障（Server）——返回 503 让飞书按重推策略稍后重试，
            // 而非伪装成 403（攻击面）或 500（会被误认为业务失败）
            activity?.SetStatus(ActivityStatusCode.Error, "Deduplication server failure");
            _logger.LogError(ex, "去重服务致命故障（Server），返回 503 以便飞书重推, AppKey: {AppKey}", appKey ?? "unknown");
            await WriteErrorResponse(context, 503, "Service Unavailable", requestId);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // WHF-16：客户端已断开——记日志后直接返回，禁止再向已中止连接写响应
            activity?.SetStatus(ActivityStatusCode.Error, "Request aborted");
            _logger.LogWarning("Webhook 请求处理期间客户端断开连接, AppKey: {AppKey}", appKey ?? "unknown");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "处理应用的 Webhook 请求时发生错误");
            await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);
            _logger.LogInformation("请求处理完成, 耗时: {DurationMs}ms, AppKey: {AppKey}", stopwatch.ElapsedMilliseconds, appKey ?? "unknown");
        }
    }

    /// <summary>
    /// 从路径中提取 AppKey
    /// </summary>
    private string? ExtractAppKeyFromPath(string path) => WebhookPathHelper.ExtractAppKeyFromPath(path, Options.GlobalRoutePrefix);


    /// <summary>
    /// 处理 Webhook 请求
    /// </summary>
    private async Task ProcessWebhookRequestAsync(
        HttpContext context,
        string requestBody,
        string requestId,
        string appKey)
    {
        using var scope = _scopeFactory.CreateScope();
        var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();

        // 设置当前应用键以支持多应用场景
        webhookService.SetCurrentAppKey(appKey);

        try
        {
            // WHF-17：单次 JsonDocument 解析——明文验证探测与 encrypt 提取一次完成
            // （原实现每个请求完整反序列化两次：EventVerificationRequest + FeishuWebhookRequest）
            FeishuWebhookRequest eventRequest;
            using (var doc = JsonDocument.Parse(requestBody))
            {
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("请求体根元素不是 JSON 对象, AppKey: {AppKey}", appKey);
                    await WriteErrorResponse(context, 400, "Bad Request", requestId);
                    return;
                }

                // 明文 URL 验证探测：明文验证在强制 EncryptKey 策略下一律拒绝（加密验证走加密链路）
                if (root.TryGetProperty("type", out var typeElement) &&
                    typeElement.ValueKind == JsonValueKind.String &&
                    typeElement.GetString() == "url_verification")
                {
                    _logger.LogWarning("明文验证在强制 EncryptKey 策略下一律拒绝，AppKey: {AppKey}", appKey);
                    await WriteErrorResponse(context, 403, "Forbidden: Plaintext verification not allowed, use encrypted verification", requestId);
                    return;
                }

                // encrypt 字段提取（缺失或非字符串 → 400）
                if (!root.TryGetProperty("encrypt", out var encryptElement) ||
                    encryptElement.ValueKind != JsonValueKind.String)
                {
                    _logger.LogWarning("请求缺少 encrypt 字段, AppKey: {AppKey}", appKey);
                    await WriteErrorResponse(context, 400, "Bad Request", requestId);
                    return;
                }

                eventRequest = new FeishuWebhookRequest
                {
                    Encrypt = encryptElement.GetString() ?? string.Empty
                };
            }

            // 从请求头提取签名相关信息
            eventRequest.Signature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault() ?? string.Empty;
            eventRequest.Nonce = context.Request.Headers["X-Lark-Request-Nonce"].FirstOrDefault() ?? string.Empty;
            eventRequest.Timestamp = long.TryParse(context.Request.Headers["X-Lark-Request-Timestamp"].FirstOrDefault(), out var ts) ? ts : 0;

            // 获取应用配置的加密密钥
            var appConfig = Options.GetAppConfig(appKey);
            if (appConfig == null)
            {
                _logger.LogError("未找到应用配置, AppKey: {AppKey}", appKey);
                await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
                return;
            }

            // 先验证请求签名，再解密（安全原则：先验签后解密）
            // WHF-16：透传 RequestAborted，客户端断开时尽早取消验签链路
            if (!await webhookService.HandleEventAsync(eventRequest, requestBody, context.RequestAborted))
            {
                _logger.LogWarning("签名验证失败 - Timestamp: {Timestamp}, Nonce: {Nonce}, SignaturePrefix: {SignaturePrefix}, AppKey: {AppKey}",
                    eventRequest.Timestamp,
                    eventRequest.Nonce,
                    eventRequest.Signature?.Length > 8 ? eventRequest.Signature.Substring(0, 8) + "..." : eventRequest.Signature ?? "(null)",
                    appKey);
                await WriteErrorResponse(context, 403, "Forbidden", requestId);
                return;
            }

            // 签名验证通过后再解密（验证请求使用 1 秒超时，确保飞书要求）
            using var decryptionCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var decryptedData = await webhookService.DecryptEventAsync(eventRequest.Encrypt!, decryptionCts.Token);

            if (decryptedData == null)
            {
                // WHF-17：错误文案收敛（解密失败细节仅在服务端日志）
                _logger.LogError("解密失败, AppKey: {AppKey}", appKey);
                await WriteErrorResponse(context, 400, "Bad Request", requestId);
                return;
            }

            _logger.LogInformation("解密成功 - EventType: {EventType}, EventId: {EventId}, DecryptedAppId: {DecryptedAppId}",
                decryptedData.EventType ?? "(null)",
                decryptedData.EventId ?? "(null)",
                decryptedData.AppId ?? "(null)");

            // 检查是否为加密验证请求
            if (decryptedData.EventType == "url_verification")
            {
                await HandleEncryptedVerificationAsync(context, decryptedData, appConfig, requestId);
                return;
            }

            // 检查事件数据是否有效
            if (string.IsNullOrEmpty(decryptedData.EventType) && string.IsNullOrEmpty(decryptedData.EventId))
            {
                _logger.LogError("事件数据无效：EventType 和 EventId 均为空");
                await WriteErrorResponse(context, 400, "Bad Request", requestId);
                return;
            }

            // WHF-05：空 EventId 无法去重（每次都会按新事件处理，幂等性失效）——fail-closed
            if (Options.RejectEmptyIdentifiers && string.IsNullOrEmpty(decryptedData.EventId))
            {
                _logger.LogWarning("事件 EventId 为空，RejectEmptyIdentifiers=true 拒绝处理, EventType: {EventType}, AppKey: {AppKey}",
                    decryptedData.EventType, appKey);
                await WriteErrorResponse(context, 400, "Bad Request", requestId);
                return;
            }

            // 使用已解密的数据直接处理事件
            // WHF-16：分发路径透传 RequestAborted（客户端断开即取消）；
            // 去重标记路径使用 CancellationToken.None（标记必须完成），由 HandleEventWithInterceptorsAsync 内部保证
            var result = await webhookService.HandleEventAsync(decryptedData, context.RequestAborted);

            // 检查事件处理结果
            if (!result.Success)
            {
                _logger.LogError("事件处理失败: {Reason}", result.ErrorReason ?? "未知错误");
                await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
                return;
            }

            _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                decryptedData.EventType ?? "(null)",
                decryptedData.EventId ?? "(null)",
                appKey);

            await WriteJsonResponse(context, 200, new WebhookEmptyResponse());
        }
        catch (JsonException ex)
        {
            // WHF-17：错误文案收敛——阶段差异仅写入服务端日志，响应统一为 "Bad Request"
            _logger.LogError(ex, "解析请求体失败（非法 JSON）, RequestId: {RequestId}", requestId);
            await WriteErrorResponse(context, 400, "Bad Request", requestId);
        }
    }

    /// <summary>
    /// 处理加密的 URL 验证请求
    /// 验证解密后数据中的 token 字段，确保请求来源合法
    /// </summary>
    private async Task HandleEncryptedVerificationAsync(
        HttpContext context,
        EventData decryptedData,
        FeishuAppWebhookOptions appConfig,
        string requestId)
    {
        string? challenge = null;
        string? token = null;

        if (decryptedData.Event is string eventJson)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(eventJson);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("challenge", out var challengeElement))
                {
                    challenge = challengeElement.GetString();
                }
                if (root.TryGetProperty("token", out var tokenElement))
                {
                    token = tokenElement.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析验证请求数据时发生错误");
            }
        }
        else if (decryptedData.Event is JsonElement eventElement)
        {
            if (eventElement.TryGetProperty("challenge", out var challengeElement))
            {
                challenge = challengeElement.GetString();
            }
            if (eventElement.TryGetProperty("token", out var tokenElement))
            {
                token = tokenElement.GetString();
            }
        }

        if (string.IsNullOrEmpty(appConfig.VerificationToken))
        {
            _logger.LogWarning("应用未配置 VerificationToken，拒绝加密验证请求（安全边界），AppKey: {AppKey}", appConfig.AppKey);
            await WriteErrorResponse(context, 403, "Forbidden: VerificationToken not configured", requestId);
            return;
        }

        // 使用固定时间比较防止计时攻击
        var tokenBytes = Encoding.UTF8.GetBytes(token ?? string.Empty);
        var expectedTokenBytes = Encoding.UTF8.GetBytes(appConfig.VerificationToken ?? string.Empty);
        if (!SignatureValidator.FixedTimeEquals(tokenBytes, expectedTokenBytes))
        {
            var actualTokenPrefix = token?.Length > 4 ? token.Substring(0, 4) + "***" : "***";
            _logger.LogWarning("加密验证请求 Token 不匹配: 实际 {ActualToken}, AppKey: {AppKey}",
                actualTokenPrefix, appConfig.AppKey);
            await WriteErrorResponse(context, 403, "Forbidden: Token mismatch", requestId);
            return;
        }

        var verificationResponse = new EventVerificationResponse
        {
            Challenge = challenge ?? string.Empty
        };

        _logger.LogInformation("加密验证成功，返回挑战码: {Challenge}", challenge);
        await WriteJsonResponse(context, 200, verificationResponse);
    }

    /// <summary>
    /// 读取请求体（带字节大小限制检查）
    /// </summary>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        var maxSize = Options.MaxRequestBodySize;

        // 检查 Content-Length 头（快速拒绝）
        if (request.ContentLength.HasValue && request.ContentLength.Value > maxSize)
        {
            throw new FeishuWebhookValidationException(
                $"请求体大小 {request.ContentLength.Value} 超过限制 {maxSize} 字节");
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        // 逐块读取，按字节计数（T2-4: 修复按字符数计量导致多字节内容可超限约3倍的问题）
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        long totalBytes = 0;
        int read;

        while ((read = await request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            totalBytes += read;
            if (totalBytes > maxSize)
            {
                throw new FeishuWebhookValidationException(
                    $"请求体大小超过限制 {maxSize} 字节");
            }
            ms.Write(buffer, 0, read);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>
    /// 写入 JSON 响应
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, int statusCode, T data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        RequestIdHelper.AddRequestIdToResponse(context);

        var json = FeishuJsonAot.Serialize(data, FeishuJsonOptions.Serialize);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// 写入错误响应
    /// </summary>
    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string? requestId = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        RequestIdHelper.AddRequestIdToResponse(context);

        var errorResponse = new WebhookErrorResponse
        {
            Success = false,
            RequestId = requestId,
            Error = new WebhookErrorDetail
            {
                Code = statusCode,
                Message = message
            }
        };

        var json = FeishuJsonAot.Serialize(errorResponse, FeishuJsonOptions.Serialize);
        await context.Response.WriteAsync(json);
    }
}
