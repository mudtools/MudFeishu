// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.Exceptions;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 错误恢复策略 - 判断错误是否可恢复并提供恢复建议
/// </summary>
public class ErrorRecoveryStrategy
{
    private readonly ILogger<ErrorRecoveryStrategy> _logger;

    private static readonly Dictionary<WebSocketError, (bool IsRecoverable, string Recommendation, int DelaySeconds)> WebSocketErrorMap = new()
    {
        [WebSocketError.ConnectionClosedPrematurely] = (true, "立即重连", 1),
        [WebSocketError.Faulted] = (true, "立即重连", 1),
        [WebSocketError.InvalidState] = (true, "重新建立连接", 2),
        [WebSocketError.NotAWebSocket] = (false, "检查服务器配置", 0),
        [WebSocketError.UnsupportedVersion] = (false, "检查服务器配置", 0),
        [WebSocketError.UnsupportedProtocol] = (false, "检查服务器配置", 0),
        [WebSocketError.HeaderError] = (false, "检查请求头配置", 0),
    };

    private static readonly Dictionary<SocketError, (bool IsRecoverable, string Recommendation, int DelaySeconds)> SocketErrorMap = new()
    {
        [SocketError.ConnectionRefused] = (true, "网络连接问题，重试连接", 5),
        [SocketError.ConnectionReset] = (true, "网络连接问题，重试连接", 5),
        [SocketError.ConnectionAborted] = (true, "网络连接问题，重试连接", 5),
        [SocketError.TimedOut] = (true, "连接超时，重试连接", 3),
        [SocketError.NetworkUnreachable] = (true, "网络不可达，延迟重试", 30),
        [SocketError.HostUnreachable] = (true, "网络不可达，延迟重试", 30),
        [SocketError.AddressNotAvailable] = (false, "地址配置错误", 0),
        [SocketError.AddressFamilyNotSupported] = (false, "地址配置错误", 0),
    };

    private static readonly List<KeyValuePair<Type, Func<Exception, ErrorRecoveryResult>>> ExceptionAnalyzerMap = new();

    static ErrorRecoveryStrategy()
    {
        ExceptionAnalyzerMap.Add(new(typeof(FeishuAuthenticationException), ex =>
        {
            var authEx = (FeishuAuthenticationException)ex;
            return new ErrorRecoveryResult
            {
                ErrorType = "FeishuAuthenticationException",
                IsRecoverable = authEx.IsRecoverable,
                RecoveryRecommendation = authEx.IsRecoverable ? "认证失败，刷新令牌后重试" : "认证配置错误，检查应用凭据",
                SuggestedDelay = TimeSpan.FromSeconds(5)
            };
        }));
        ExceptionAnalyzerMap.Add(new(typeof(FeishuConnectionException), ex =>
        {
            var connEx = (FeishuConnectionException)ex;
            return new ErrorRecoveryResult
            {
                ErrorType = "FeishuConnectionException",
                IsRecoverable = connEx.IsRecoverable,
                RecoveryRecommendation = connEx.IsRecoverable ? "连接异常，重试连接" : "连接配置错误",
                SuggestedDelay = TimeSpan.FromSeconds(5)
            };
        }));
        ExceptionAnalyzerMap.Add(new(typeof(FeishuNetworkException), ex =>
        {
            var netEx = (FeishuNetworkException)ex;
            return new ErrorRecoveryResult
            {
                ErrorType = "FeishuNetworkException",
                IsRecoverable = netEx.IsRecoverable,
                RecoveryRecommendation = netEx.IsRecoverable ? "网络异常，重试连接" : "网络配置错误",
                SuggestedDelay = TimeSpan.FromSeconds(10)
            };
        }));
        ExceptionAnalyzerMap.Add(new(typeof(WebSocketException), ex => AnalyzeWebSocketException((WebSocketException)ex)));
        ExceptionAnalyzerMap.Add(new(typeof(SocketException), ex => AnalyzeSocketException((SocketException)ex)));
        ExceptionAnalyzerMap.Add(new(typeof(HttpRequestException), ex => AnalyzeHttpException((HttpRequestException)ex)));
        ExceptionAnalyzerMap.Add(new(typeof(TimeoutException), _ => new ErrorRecoveryResult
        {
            ErrorType = "TimeoutException",
            IsRecoverable = true,
            RecoveryRecommendation = "操作超时，重试连接",
            SuggestedDelay = TimeSpan.FromSeconds(3)
        }));
        ExceptionAnalyzerMap.Add(new(typeof(OperationCanceledException), _ => new ErrorRecoveryResult
        {
            ErrorType = "OperationCanceledException",
            IsRecoverable = false,
            RecoveryRecommendation = "操作被取消"
        }));
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ErrorRecoveryStrategy(ILogger<ErrorRecoveryStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 分析错误并确定恢复策略
    /// </summary>
    /// <param name="exception">发生的异常</param>
    /// <param name="context">错误上下文</param>
    /// <returns>错误恢复结果</returns>
    public ErrorRecoveryResult AnalyzeError(Exception exception, string context = "")
    {
        var result = AnalyzeCore(exception);
        result.Exception = exception;
        result.Context = context;
        result.Timestamp = DateTime.UtcNow;

        _logger.LogDebug("错误分析完成: {ErrorType}, 可恢复: {IsRecoverable}, 建议: {Recommendation}",
            result.ErrorType, result.IsRecoverable, result.RecoveryRecommendation);

        return result;
    }

    private static ErrorRecoveryResult AnalyzeCore(Exception exception)
    {
        var exType = exception.GetType();

        foreach (var kvp in ExceptionAnalyzerMap)
        {
            if (kvp.Key.IsAssignableFrom(exType))
            {
                return kvp.Value(exception);
            }
        }

        return new ErrorRecoveryResult
        {
            ErrorType = exType.Name,
            IsRecoverable = true,
            RecoveryRecommendation = "未知错误，尝试重连",
            SuggestedDelay = TimeSpan.FromSeconds(10)
        };
    }

    private static ErrorRecoveryResult AnalyzeWebSocketException(WebSocketException wsEx)
    {
        var result = new ErrorRecoveryResult
        {
            ErrorType = "WebSocketException",
            ErrorCode = wsEx.WebSocketErrorCode.ToString()
        };

        if (WebSocketErrorMap.TryGetValue(wsEx.WebSocketErrorCode, out var mapping))
        {
            result.IsRecoverable = mapping.IsRecoverable;
            result.RecoveryRecommendation = mapping.Recommendation;
            result.SuggestedDelay = TimeSpan.FromSeconds(mapping.DelaySeconds);
        }
        else
        {
            result.IsRecoverable = true;
            result.RecoveryRecommendation = "尝试重连";
            result.SuggestedDelay = TimeSpan.FromSeconds(5);
        }

        return result;
    }

    private static ErrorRecoveryResult AnalyzeSocketException(SocketException sockEx)
    {
        var result = new ErrorRecoveryResult
        {
            ErrorType = "SocketException",
            ErrorCode = sockEx.SocketErrorCode.ToString()
        };

        if (SocketErrorMap.TryGetValue(sockEx.SocketErrorCode, out var mapping))
        {
            result.IsRecoverable = mapping.IsRecoverable;
            result.RecoveryRecommendation = mapping.Recommendation;
            result.SuggestedDelay = TimeSpan.FromSeconds(mapping.DelaySeconds);
        }
        else
        {
            result.IsRecoverable = true;
            result.RecoveryRecommendation = "Socket错误，尝试重连";
            result.SuggestedDelay = TimeSpan.FromSeconds(10);
        }

        return result;
    }

    private static ErrorRecoveryResult AnalyzeHttpException(HttpRequestException httpEx)
    {
        var result = new ErrorRecoveryResult
        {
            ErrorType = "HttpRequestException",
            IsRecoverable = true,
            RecoveryRecommendation = "HTTP请求失败，重试连接",
            SuggestedDelay = TimeSpan.FromSeconds(5)
        };

        if (httpEx.Message.Contains("500") || httpEx.Message.Contains("502") || httpEx.Message.Contains("503"))
        {
            result.RecoveryRecommendation = "服务器错误，延迟重试";
            result.SuggestedDelay = TimeSpan.FromSeconds(15);
        }
        else if (httpEx.Message.Contains("401") || httpEx.Message.Contains("403"))
        {
            result.IsRecoverable = false;
            result.RecoveryRecommendation = "认证失败，检查凭据";
        }

        return result;
    }
}
