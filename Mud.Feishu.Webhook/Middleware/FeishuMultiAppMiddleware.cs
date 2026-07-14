// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Observability;
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
public class FeishuMultiAppMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeishuMultiAppMiddleware> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry;

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

        // 监听配置变更
        _options.OnChange((newOptions, name) =>
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
            if (Options.EnableRequestLogging)
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
                await WriteErrorResponse(context, 400, "Bad Request: Empty body", requestId);
                return;
            }

            if (Options.EnableRequestLogging)
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
            if (Options.EnableRequestLogging)
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
            // 尝试处理明文 URL 验证请求（仅在未配置 EncryptKey 时允许）
            if (await TryHandlePlaintextVerificationAsync(context, requestBody, webhookService, appKey, requestId))
            {
                return;
            }

            // 解析加密请求
            var eventRequest = JsonSerializer.Deserialize(
                requestBody,
                FeishuJsonContext.Default.FeishuWebhookRequest);

            if (eventRequest == null || string.IsNullOrEmpty(eventRequest.Encrypt))
            {
                await WriteErrorResponse(context, 400, "Bad Request: Missing encrypt field", requestId);
                return;
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
            if (!await webhookService.HandleEventAsync(eventRequest, requestBody))
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
                _logger.LogError("解密失败");
                await WriteErrorResponse(context, 400, "Bad Request: Decryption failed", requestId);
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
                await WriteErrorResponse(context, 400, "Bad Request: Invalid event data", requestId);
                return;
            }

            // 使用已解密的数据直接处理事件
            var result = await webhookService.HandleEventAsync(decryptedData);

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
            _logger.LogError(ex, "反序列化请求体时发生错误, RequestId: {RequestId}", requestId);
            await WriteErrorResponse(context, 400, "Bad Request: Invalid JSON", requestId);
        }
    }

    /// <summary>
    /// 尝试处理明文 URL 验证请求
    /// 当应用配置了 EncryptKey 时，拒绝明文验证请求（安全边界）
    /// </summary>
    private async Task<bool> TryHandlePlaintextVerificationAsync(
        HttpContext context,
        string requestBody,
        IFeishuWebhookService webhookService,
        string appKey,
        string requestId)
    {
        var verificationRequest = JsonSerializer.Deserialize(
            requestBody,
            FeishuJsonContext.Default.EventVerificationRequest);

        if (verificationRequest?.Type == "url_verification")
        {
            _logger.LogDebug("检测到明文 URL 验证请求");

            var appConfig = Options.GetAppConfig(appKey);
            if (appConfig != null && !string.IsNullOrEmpty(appConfig.EncryptKey))
            {
                _logger.LogWarning("应用已配置 EncryptKey，拒绝明文验证请求（安全边界），AppKey: {AppKey}", appKey);
                await WriteErrorResponse(context, 403, "Forbidden: Plaintext verification not allowed when EncryptKey is configured", requestId);
                return true;
            }

            using var verificationCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var verificationResponse = await webhookService.VerifyEventSubscriptionAsync(verificationRequest, verificationCts.Token);

            if (verificationResponse == null)
            {
                _logger.LogWarning("验证令牌不匹配或验证失败");
                return false;
            }

            _logger.LogInformation("明文验证成功，返回挑战码");
            await WriteJsonResponse(context, 200, verificationResponse);
            return true;
        }

        return false;
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
    /// 读取请求体（带大小限制检查）
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

        // 逐块读取，防止无 Content-Length 的攻击
        var sb = new StringBuilder();
        var buffer = new char[4096];
        long totalRead = 0;

        using var reader = new StreamReader(
            request.Body, Encoding.UTF8, true, bufferSize: 1024, leaveOpen: true);

        int read;
        while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += read;
            if (totalRead > maxSize)
            {
                throw new FeishuWebhookValidationException(
                    $"请求体大小超过限制 {maxSize} 字节");
            }
            sb.Append(buffer, 0, read);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 写入 JSON 响应
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, int statusCode, T data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        RequestIdHelper.AddRequestIdToResponse(context);

        var json = JsonSerializer.Serialize(data, FeishuJsonOptions.Serialize);
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

        var json = JsonSerializer.Serialize(errorResponse, FeishuJsonOptions.Serialize);
        await context.Response.WriteAsync(json);
    }
}
