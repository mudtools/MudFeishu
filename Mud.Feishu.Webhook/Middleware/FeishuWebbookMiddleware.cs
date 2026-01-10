// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using System.Diagnostics;

namespace Mud.Feishu.Webhook.Middleware;

/// <summary>
/// 飞书 Webhook 中间件
/// </summary>
public class FeishuWebhookMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<FeishuWebhookMiddleware> logger,
    IOptions<FeishuWebhookOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<FeishuWebhookMiddleware> _logger = logger;
    private readonly FeishuWebhookOptions _options = options.Value;

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 检查请求路径是否匹配
            if (!IsTargetRequest(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // 检查 HTTP 方法
            if (!_options.AllowedHttpMethods.Contains(context.Request.Method))
            {
                await WriteErrorResponse(context, 405, "Method Not Allowed");
                return;
            }

            // 检查请求体大小
            if (context.Request.ContentLength > _options.MaxRequestBodySize)
            {
                await WriteErrorResponse(context, 413, "Request Entity Too Large");
                return;
            }

            // 验证来源 IP（如果启用）
            if (_options.ValidateSourceIP && !ValidateSourceIP(context.Connection.RemoteIpAddress?.ToString()))
            {
                await WriteErrorResponse(context, 403, "Forbidden");
                return;
            }

            // 读取请求体
            var requestBody = await ReadRequestBodyAsync(context.Request);
            if (string.IsNullOrEmpty(requestBody))
            {
                await WriteErrorResponse(context, 400, "Bad Request: Empty request body");
                return;
            }

            // 记录请求日志
            if (_options.EnableRequestLogging)
            {
                _logger.LogInformation("收到飞书 Webhook 请求，路径: {Path}, 方法: {Method}, 内容长度: {ContentLength}",
                    context.Request.Path, context.Request.Method, requestBody.Length);
            }

            // 处理请求
            await ProcessWebhookRequest(context, requestBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书 Webhook 请求时发生错误");

            if (_options.EnableExceptionHandling)
            {
                await WriteErrorResponse(context, 500, "Internal Server Error");
            }
            else
            {
                throw;
            }
        }
        finally
        {
            stopwatch.Stop();

            if (_options.EnablePerformanceMonitoring)
            {
                _logger.LogDebug("飞书 Webhook 请求处理完成，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// 检查是否为目标请求
    /// </summary>
    private bool IsTargetRequest(string path)
    {
        return path.StartsWith($"/{_options.RoutePrefix}", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证来源 IP
    /// </summary>
    private bool ValidateSourceIP(string? remoteIpAddress)
    {
        if (string.IsNullOrEmpty(remoteIpAddress) || _options.AllowedSourceIPs.Count == 0)
        {
            return true;
        }

        return _options.AllowedSourceIPs.Contains(remoteIpAddress!);
    }

    /// <summary>
    /// 验证请求头签名
    /// </summary>
    private bool ValidateHeaderSignature(HttpContext context, string requestBody, FeishuWebhookRequest eventRequest, IFeishuEventValidator validator)
    {
        // 获取 X-Lark-Signature 请求头
        var headerSignature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault();

        // 验证请求头签名
        return validator.ValidateHeaderSignature(
            eventRequest.Timestamp,
            eventRequest.Nonce,
            requestBody,
            headerSignature,
            _options.EncryptKey);
    }

    /// <summary>
    /// 读取请求体
    /// </summary>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

#if NETSTANDARD2_0
        using var reader = new StreamReader(request.Body, Encoding.UTF8, true);
#else
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
#endif
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        return body;
    }

    /// <summary>
    /// 处理 Webhook 请求
    /// </summary>
    private async Task ProcessWebhookRequest(HttpContext context, string requestBody)
    {
        using var scope = _scopeFactory.CreateScope();
        var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();
        var validator = scope.ServiceProvider.GetRequiredService<IFeishuEventValidator>();

        try
        {
            // 尝试反序列化为验证请求
            var verificationRequest = JsonSerializer.Deserialize<EventVerificationRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (verificationRequest?.Type == "url_verification")
            {
                // 处理验证请求
                var verificationResponse = await webhookService.VerifyEventSubscriptionAsync(verificationRequest);
                if (verificationResponse != null)
                {
                    await WriteJsonResponse(context, 200, verificationResponse);
                    return;
                }
            }

            // 尝试反序列化为事件请求
            var eventRequest = JsonSerializer.Deserialize<FeishuWebhookRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (eventRequest != null)
            {
                // 验证 X-Lark-Signature 请求头签名
                if (!ValidateHeaderSignature(context, requestBody, eventRequest, validator))
                {
                    await WriteErrorResponse(context, 401, "Unauthorized: Invalid X-Lark-Signature");
                    return;
                }

                // 处理事件请求
                var (success, errorReason) = await webhookService.HandleEventAsync(eventRequest);
                if (success)
                {
                    await WriteJsonResponse(context, 200, new { });
                    return;
                }

                // 根据错误原因返回不同的状态码
                if (errorReason == "Signature validation failed")
                {
                    await WriteErrorResponse(context, 401, "Unauthorized: Invalid signature");
                    return;
                }

                // 其他错误返回 400
                await WriteErrorResponse(context, 400, $"Bad Request: {errorReason}");
                return;
            }

            await WriteErrorResponse(context, 400, "Bad Request: Invalid request format");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "反序列化请求体时发生错误");
            await WriteErrorResponse(context, 400, "Bad Request: Invalid JSON format");
        }
    }

    /// <summary>
    /// 写入 JSON 响应
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, int statusCode, T data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// 写入错误响应
    /// </summary>
    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            success = false,
            error = new
            {
                code = statusCode,
                message
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}