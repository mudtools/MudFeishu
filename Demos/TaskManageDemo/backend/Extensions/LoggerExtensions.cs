// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using TaskManageDemo.Backend.Utils;

namespace TaskManageDemo.Backend.Extensions;

/// <summary>
/// 日志扩展方法
/// 提供结构化日志记录和敏感数据脱敏功能
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// 记录 API 请求日志（自动脱敏敏感数据）
    /// </summary>
    public static void LogApiRequest<T>(
        this ILogger logger,
        string method,
        string path,
        T? requestData,
        string? userId = null)
    {
        var sanitizedData = requestData != null
            ? SensitiveDataMasker.MaskJson(System.Text.Json.JsonSerializer.Serialize(requestData))
            : null;

        logger.LogInformation(
            "API请求: Method={Method}, Path={Path}, UserId={UserId}, Data={Data}",
            method,
            path,
            userId ?? "anonymous",
            sanitizedData ?? "{}");
    }

    /// <summary>
    /// 记录 API 响应日志
    /// </summary>
    public static void LogApiResponse(
        this ILogger logger,
        string method,
        string path,
        int statusCode,
        long durationMs)
    {
        logger.LogInformation(
            "API响应: Method={Method}, Path={Path}, StatusCode={StatusCode}, Duration={Duration}ms",
            method,
            path,
            statusCode,
            durationMs);
    }

    /// <summary>
    /// 记录数据库操作日志
    /// </summary>
    public static void LogDatabaseOperation(
        this ILogger logger,
        string operation,
        string entity,
        int? entityId = null,
        string? userId = null)
    {
        logger.LogInformation(
            "数据库操作: Operation={Operation}, Entity={Entity}, EntityId={EntityId}, UserId={UserId}",
            operation,
            entity,
            entityId,
            userId ?? "system");
    }

    /// <summary>
    /// 记录安全相关日志
    /// </summary>
    public static void LogSecurityEvent(
        this ILogger logger,
        string eventType,
        string description,
        string? userId = null,
        string? ipAddress = null)
    {
        logger.LogWarning(
            "安全事件: Type={EventType}, Description={Description}, UserId={UserId}, IP={IpAddress}",
            eventType,
            description,
            userId ?? "unknown",
            ipAddress ?? "unknown");
    }

    /// <summary>
    /// 记录飞书 API 调用日志
    /// </summary>
    public static void LogFeishuApiCall(
        this ILogger logger,
        string apiName,
        bool success,
        string? errorCode = null,
        string? errorMessage = null)
    {
        if (success)
        {
            logger.LogInformation(
                "飞书API调用成功: Api={ApiName}",
                apiName);
        }
        else
        {
            logger.LogError(
                "飞书API调用失败: Api={ApiName}, ErrorCode={ErrorCode}, Message={Message}",
                apiName,
                errorCode,
                errorMessage);
        }
    }

    /// <summary>
    /// 记录任务同步日志
    /// </summary>
    public static void LogTaskSync(
        this ILogger logger,
        string action,
        string taskGuid,
        bool success,
        string? error = null)
    {
        if (success)
        {
            logger.LogInformation(
                "任务同步: Action={Action}, TaskGuid={TaskGuid}, Success=true",
                action,
                taskGuid);
        }
        else
        {
            logger.LogError(
                "任务同步失败: Action={Action}, TaskGuid={TaskGuid}, Error={Error}",
                action,
                taskGuid,
                error);
        }
    }

    /// <summary>
    /// 记录认证日志（自动脱敏 Token）
    /// </summary>
    public static void LogAuthentication(
        this ILogger logger,
        string action,
        string userId,
        bool success,
        string? token = null)
    {
        var maskedToken = !string.IsNullOrEmpty(token)
            ? SensitiveDataMasker.MaskToken(token)
            : null;

        logger.LogInformation(
            "认证事件: Action={Action}, UserId={UserId}, Success={Success}, Token={Token}",
            action,
            userId,
            success,
            maskedToken ?? "none");
    }

    /// <summary>
    /// 记录性能警告
    /// </summary>
    public static void LogPerformanceWarning(
        this ILogger logger,
        string operation,
        long durationMs,
        long thresholdMs)
    {
        logger.LogWarning(
            "性能警告: Operation={Operation}, Duration={Duration}ms, Threshold={Threshold}ms",
            operation,
            durationMs,
            thresholdMs);
    }

    /// <summary>
    /// 记录业务逻辑错误（不抛出异常但需要关注）
    /// </summary>
    public static void LogBusinessError(
        this ILogger logger,
        string errorCode,
        string message,
        object? context = null)
    {
        logger.LogError(
            "业务错误: Code={Code}, Message={Message}, Context={Context}",
            errorCode,
            message,
            context != null ? System.Text.Json.JsonSerializer.Serialize(context) : "none");
    }
}

/// <summary>
/// 日志级别辅助类
/// </summary>
public static class LogLevelHelper
{
    /// <summary>
    /// 根据 HTTP 状态码确定日志级别
    /// </summary>
    public static LogLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            >= 300 => LogLevel.Information,
            _ => LogLevel.Debug
        };
    }

    /// <summary>
    /// 根据异常类型确定日志级别
    /// </summary>
    public static LogLevel GetLogLevelForException(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => LogLevel.Information,
            TimeoutException => LogLevel.Warning,
            ArgumentException => LogLevel.Warning,
            UnauthorizedAccessException => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }
}
