// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Middleware;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// 初始化异常处理中间件
    /// </summary>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "请求处理发生异常: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json; charset=utf-8";

        var (statusCode, message) = exception switch
        {
            ArgumentNullException argEx => (HttpStatusCode.BadRequest, $"参数不能为空: {argEx.ParamName}"),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "未授权访问"),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message ?? "资源不存在"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "请求超时"),
            OperationCanceledException => (HttpStatusCode.BadRequest, "请求已取消"),
            _ => (HttpStatusCode.InternalServerError, "服务器内部错误")
        };

        response.StatusCode = (int)statusCode;

        object? errorData = null;
        if (_environment.IsDevelopment())
        {
            errorData = new
            {
                ExceptionType = exception.GetType().Name,
                Message = exception.Message ?? string.Empty,
                StackTrace = exception.StackTrace ?? string.Empty,
                InnerException = exception.InnerException?.Message ?? string.Empty
            };
        }

        var errorResponse = new ApiResponse<object?>
        {
            Success = false,
            Message = message ?? "服务器内部错误",
            Data = errorData
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await response.WriteAsync(json);
    }
}

/// <summary>
/// 请求日志中间件
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// 初始化请求日志中间件
    /// </summary>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var request = context.Request;

        _logger.LogInformation("请求开始: {Method} {Path}{QueryString}",
            request.Method, request.Path, request.QueryString);

        await _next(context);

        var elapsed = DateTime.UtcNow - startTime;
        var response = context.Response;

        _logger.LogInformation("请求完成: {Method} {Path} - {StatusCode} - {Elapsed}ms",
            request.Method, request.Path, response.StatusCode, elapsed.TotalMilliseconds);
    }
}

/// <summary>
/// 请求验证中间件
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    /// <summary>
    /// 初始化请求验证中间件
    /// </summary>
    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        if (request.ContentLength.HasValue && request.ContentLength.Value > 10 * 1024 * 1024)
        {
            _logger.LogWarning("请求体过大: {Size} bytes", request.ContentLength);
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Success = false,
                Message = "请求体大小超过限制（最大10MB）"
            });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// 使用全局异常处理
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// 使用请求日志
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// 使用请求验证
    /// </summary>
    public static IApplicationBuilder UseRequestValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestValidationMiddleware>();
    }
}
