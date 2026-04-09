// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
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
        var result = new ErrorRecoveryResult
        {
            Exception = exception,
            Context = context,
            Timestamp = DateTime.UtcNow
        };

        // 分析异常类型和可恢复性
        switch (exception)
        {
            case WebSocketException wsEx:
                result = AnalyzeWebSocketException(wsEx, result);
                break;

            case SocketException sockEx:
                result = AnalyzeSocketException(sockEx, result);
                break;

            case HttpRequestException httpEx:
                result = AnalyzeHttpException(httpEx, result);
                break;

            case TimeoutException timeoutEx:
                result = AnalyzeTimeoutException(timeoutEx, result);
                break;

            case OperationCanceledException cancelEx:
                result = AnalyzeCancellationException(cancelEx, result);
                break;

            case FeishuAuthenticationException authEx:
                result = AnalyzeAuthenticationException(authEx, result);
                break;

            case FeishuConnectionException connEx:
                result = AnalyzeConnectionException(connEx, result);
                break;

            case FeishuNetworkException netEx:
                result = AnalyzeNetworkException(netEx, result);
                break;

            default:
                result = AnalyzeGenericException(exception, result);
                break;
        }

        _logger.LogDebug("错误分析完成: {ErrorType}, 可恢复: {IsRecoverable}, 建议: {Recommendation}",
            result.ErrorType, result.IsRecoverable, result.RecoveryRecommendation);

        return result;
    }

    /// <summary>
    /// 分析WebSocket异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeWebSocketException(WebSocketException wsEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "WebSocketException";
        result.ErrorCode = wsEx.WebSocketErrorCode.ToString();

        switch (wsEx.WebSocketErrorCode)
        {
            case WebSocketError.ConnectionClosedPrematurely:
            case WebSocketError.Faulted:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "立即重连";
                result.SuggestedDelay = TimeSpan.FromSeconds(1);
                break;

            case WebSocketError.InvalidState:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "重新建立连接";
                result.SuggestedDelay = TimeSpan.FromSeconds(2);
                break;

            case WebSocketError.NotAWebSocket:
            case WebSocketError.UnsupportedVersion:
            case WebSocketError.UnsupportedProtocol:
                result.IsRecoverable = false;
                result.RecoveryRecommendation = "检查服务器配置";
                break;

            case WebSocketError.HeaderError:
                result.IsRecoverable = false;
                result.RecoveryRecommendation = "检查请求头配置";
                break;

            default:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "尝试重连";
                result.SuggestedDelay = TimeSpan.FromSeconds(5);
                break;
        }

        return result;
    }

    /// <summary>
    /// 分析Socket异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeSocketException(SocketException sockEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "SocketException";
        result.ErrorCode = sockEx.SocketErrorCode.ToString();

        switch (sockEx.SocketErrorCode)
        {
            case SocketError.ConnectionRefused:
            case SocketError.ConnectionReset:
            case SocketError.ConnectionAborted:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "网络连接问题，重试连接";
                result.SuggestedDelay = TimeSpan.FromSeconds(5);
                break;

            case SocketError.TimedOut:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "连接超时，重试连接";
                result.SuggestedDelay = TimeSpan.FromSeconds(3);
                break;

            case SocketError.NetworkUnreachable:
            case SocketError.HostUnreachable:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "网络不可达，延迟重试";
                result.SuggestedDelay = TimeSpan.FromSeconds(30);
                break;

            case SocketError.AddressNotAvailable:
            case SocketError.AddressFamilyNotSupported:
                result.IsRecoverable = false;
                result.RecoveryRecommendation = "地址配置错误";
                break;

            default:
                result.IsRecoverable = true;
                result.RecoveryRecommendation = "Socket错误，尝试重连";
                result.SuggestedDelay = TimeSpan.FromSeconds(10);
                break;
        }

        return result;
    }

    /// <summary>
    /// 分析HTTP异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeHttpException(HttpRequestException httpEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "HttpRequestException";
        result.IsRecoverable = true;
        result.RecoveryRecommendation = "HTTP请求失败，重试连接";
        result.SuggestedDelay = TimeSpan.FromSeconds(5);

        // 检查是否包含特定的HTTP状态码信息
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

    /// <summary>
    /// 分析超时异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeTimeoutException(TimeoutException timeoutEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "TimeoutException";
        result.IsRecoverable = true;
        result.RecoveryRecommendation = "操作超时，重试连接";
        result.SuggestedDelay = TimeSpan.FromSeconds(3);
        return result;
    }

    /// <summary>
    /// 分析取消异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeCancellationException(OperationCanceledException cancelEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "OperationCanceledException";
        result.IsRecoverable = false; // 取消操作通常不需要恢复
        result.RecoveryRecommendation = "操作被取消";
        return result;
    }

    /// <summary>
    /// 分析认证异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeAuthenticationException(FeishuAuthenticationException authEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "FeishuAuthenticationException";
        result.IsRecoverable = authEx.IsRecoverable;

        if (authEx.IsRecoverable)
        {
            result.RecoveryRecommendation = "认证失败，刷新令牌后重试";
            result.SuggestedDelay = TimeSpan.FromSeconds(5);
        }
        else
        {
            result.RecoveryRecommendation = "认证配置错误，检查应用凭据";
        }

        return result;
    }

    /// <summary>
    /// 分析连接异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeConnectionException(FeishuConnectionException connEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "FeishuConnectionException";
        result.IsRecoverable = connEx.IsRecoverable;
        result.RecoveryRecommendation = connEx.IsRecoverable ? "连接异常，重试连接" : "连接配置错误";
        result.SuggestedDelay = TimeSpan.FromSeconds(5);
        return result;
    }

    /// <summary>
    /// 分析网络异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeNetworkException(FeishuNetworkException netEx, ErrorRecoveryResult result)
    {
        result.ErrorType = "FeishuNetworkException";
        result.IsRecoverable = netEx.IsRecoverable;
        result.RecoveryRecommendation = netEx.IsRecoverable ? "网络异常，重试连接" : "网络配置错误";
        result.SuggestedDelay = TimeSpan.FromSeconds(10);
        return result;
    }

    /// <summary>
    /// 分析通用异常
    /// </summary>
    private ErrorRecoveryResult AnalyzeGenericException(Exception ex, ErrorRecoveryResult result)
    {
        result.ErrorType = ex.GetType().Name;
        result.IsRecoverable = true; // 默认认为可恢复
        result.RecoveryRecommendation = "未知错误，尝试重连";
        result.SuggestedDelay = TimeSpan.FromSeconds(10);
        return result;
    }
}