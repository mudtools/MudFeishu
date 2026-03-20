// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace TaskManageDemo.Backend.Filters;

/// <summary>
/// 全局异常过滤器
/// 统一处理所有未捕获的异常
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var requestId = context.HttpContext.TraceIdentifier;

        // 记录异常日志
        LogException(exception, requestId, context);

        // 构建错误响应
        var errorResponse = new ErrorResponse
        {
            RequestId = requestId,
            Message = GetUserFriendlyMessage(exception),
            Timestamp = DateTime.UtcNow
        };

        // 开发环境包含详细错误信息
        if (_environment.IsDevelopment())
        {
            errorResponse.Detail = exception.Message;
            errorResponse.StackTrace = exception.StackTrace;
        }

        // 根据异常类型设置 HTTP 状态码
        var statusCode = GetStatusCode(exception);
        
        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/json" }
        };

        context.ExceptionHandled = true;
    }

    /// <summary>
    /// 记录异常日志
    /// </summary>
    private void LogException(Exception exception, string requestId, ExceptionContext context)
    {
        var request = context.HttpContext.Request;
        
        _logger.LogError(
            exception,
            "未处理的异常: RequestId={RequestId}, Method={Method}, Path={Path}, Message={Message}",
            requestId,
            request.Method,
            request.Path,
            exception.Message);
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "请求参数不能为空",
            ArgumentException => "请求参数无效",
            ValidationException => "数据验证失败",
            UnauthorizedAccessException => "没有权限执行此操作",
            KeyNotFoundException => "请求的资源不存在",
            TimeoutException => "操作超时，请稍后重试",
            InvalidOperationException => "操作无效，请检查当前状态",
            JsonException => "数据格式错误",
            _ => "服务器内部错误，请稍后重试"
        };
    }

    /// <summary>
    /// 获取 HTTP 状态码
    /// </summary>
    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

/// <summary>
/// 错误响应模型
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 请求 ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细错误信息（仅开发环境）
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// 堆栈跟踪（仅开发环境）
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 全局异常过滤器扩展
/// </summary>
public static class GlobalExceptionFilterExtensions
{
    /// <summary>
    /// 添加全局异常过滤器
    /// </summary>
    public static void AddGlobalExceptionFilter(this MvcOptions options)
    {
        options.Filters.Add<GlobalExceptionFilter>();
    }
}
