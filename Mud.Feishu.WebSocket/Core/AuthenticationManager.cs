// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 认证管理器 - 处理WebSocket认证相关逻辑
/// </summary>
public class AuthenticationManager
{
    private readonly ILogger<AuthenticationManager> _logger;
    private readonly Func<string, Task> _sendMessageCallback;
    private readonly SessionManager? _sessionManager;
    private bool _isAuthenticated = false;
    private readonly FeishuWebSocketOptions _options;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private int _authRetryCount = 0;
    private int _totalAuthFailures = 0;
    private DateTime _lastAuthFailureTime = DateTime.MinValue;

    // 认证失败冷却期管理
    private readonly Dictionary<string, DateTime> _authFailureCooldowns = new();
    private readonly object _cooldownLock = new();
    private const int MaxAuthFailuresBeforeCooldown = 3;
    private static readonly TimeSpan AuthFailureCooldownPeriod = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 认证成功事件
    /// </summary>
    public event EventHandler<EventArgs>? Authenticated;

    /// <summary>
    /// 认证失败事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? AuthenticationFailed;

    /// <summary>
    /// 获取当前认证状态
    /// </summary>
    /// <returns>如果已认证返回true，否则返回false</returns>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// 初始化认证管理器实例
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="sendMessageCallback">发送消息回调函数</param>
    /// <param name="sessionManager">会话管理器（可选）</param>
    public AuthenticationManager(
        ILogger<AuthenticationManager> logger,
        FeishuWebSocketOptions options,
        Func<string, Task> sendMessageCallback,
        SessionManager? sessionManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sendMessageCallback = sendMessageCallback ?? throw new ArgumentNullException(nameof(sendMessageCallback));
        _sessionManager = sessionManager;
        _options = options;
    }

    /// <summary>
    /// 发送认证消息（带重试机制和冷却期检查）
    /// </summary>
    public async Task AuthenticateAsync(string appAccessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appAccessToken))
            throw new ArgumentException("应用访问令牌不能为空", nameof(appAccessToken));

        // 检查认证冷却期
        if (IsInAuthCooldown(appAccessToken))
        {
            var cooldownRemaining = GetAuthCooldownRemaining(appAccessToken);
            _logger.LogWarning("认证失败过多，处于冷却期中，剩余时间: {CooldownRemaining}", cooldownRemaining);
            throw new InvalidOperationException($"认证失败过多，请在 {cooldownRemaining:mm\\:ss} 后重试");
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            // 如果已认证，直接返回
            if (_isAuthenticated)
            {
                _logger.LogDebug("WebSocket已认证，跳过重复认证");
                return;
            }

            // 使用指数退避策略重试认证
            var maxRetries = _options.MaxReconnectAttempts;
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _authRetryCount = attempt;
                    await AuthenticateInternalAsync(appAccessToken, cancellationToken);
                    // 认证成功，清除冷却期
                    ClearAuthCooldown(appAccessToken);
                    break;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "WebSocket认证失败（第 {Attempt} 次尝试），准备重试...", attempt + 1);

                    // 记录认证失败
                    RecordAuthFailure(appAccessToken);

                    // 计算退避延迟时间：baseDelay * (2^attempt)，最大不超过 MaxReconnectDelayMs
                    var baseDelay = TimeSpan.FromMilliseconds(_options.ReconnectDelayMs);
                    var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    var maxDelay = TimeSpan.FromMilliseconds(_options.MaxReconnectDelayMs);

                    // 添加随机抖动，避免多个客户端同时重试造成雪崩
                    var random = new Random();
                    var jitter = random.Next(0, 1000); // 0-1000ms 的随机抖动
                    var delay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter);

                    _logger.LogInformation("等待 {Delay}ms 后进行第 {NextAttempt} 次认证尝试（含 {Jitter}ms 抖动）",
                        delay.TotalMilliseconds, attempt + 2, jitter);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// 内部认证实现
    /// </summary>
    private async Task AuthenticateInternalAsync(string appAccessToken, CancellationToken cancellationToken)
    {
        try
        {
            if (_authRetryCount > 0)
            {
                _logger.LogInformation("正在进行WebSocket认证（重试第 {RetryCount} 次）...", _authRetryCount);
            }
            else
            {
                _logger.LogInformation("正在进行WebSocket认证...");
            }

            _isAuthenticated = false; // 重置认证状态

            // 创建认证消息
            var authMessage = new AuthMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data = new AuthData
                {
                    AppAccessToken = appAccessToken,
                    // 尝试使用缓存的 session_id 进行会话恢复
                    SessionId = _sessionManager?.GetSessionIdForReconnect()
                }
            };

            var authJson = JsonSerializer.Serialize(authMessage, JsonOptions.Default);
            await _sendMessageCallback(authJson);

            if (_options.EnableLogging)
            {
                _logger.LogInformation("已发送认证消息，等待响应...");
            }
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;
            _logger.LogError(ex, "WebSocket认证失败（第 {Attempt} 次尝试）", _authRetryCount + 1);

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"WebSocket认证失败: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);

            // 如果是最后一次尝试，抛出异常；否则由外层重试
            if (_authRetryCount >= _options.MaxReconnectAttempts)
            {
                throw new InvalidOperationException($"WebSocket认证失败，已达到最大重试次数 {_options.MaxReconnectAttempts}", ex);
            }

            throw;
        }
    }

    /// <summary>
    /// 处理认证响应
    /// </summary>
    public void HandleAuthResponse(string responseMessage)
    {
        try
        {
            var authResponse = JsonSerializer.Deserialize<AuthResponseMessage>(responseMessage);

            if (authResponse?.Code == 0)
            {
                _isAuthenticated = true;
                _logger.LogInformation("WebSocket认证成功: {Message}", authResponse.Message);

                // 记录认证成功指标
                FeishuMetricsHelper.RecordEventHandlingSuccess("auth");

                // 如果响应中包含 session_id，保存到会话管理器
                if (!string.IsNullOrEmpty(authResponse.SessionId) && _sessionManager != null)
                {
                    _sessionManager.SetSessionId(authResponse.SessionId);
                }

                Authenticated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _isAuthenticated = false;
                _totalAuthFailures++;
                _lastAuthFailureTime = DateTime.UtcNow;

                // 记录认证失败指标
                var errorType = authResponse?.Code.ToString() ?? "unknown";
                FeishuMetricsHelper.RecordEventHandlingFailure("auth", errorType);

                _logger.LogError("WebSocket认证失败: {Code} - {Message}, 总失败次数: {TotalFailures}",
                    authResponse?.Code, authResponse?.Message, _totalAuthFailures);

                // 认证失败时重置会话
                _sessionManager?.ResetSession();

                // 根据不同的错误码记录详细信息
                var errorCode = authResponse?.Code;
                var errorMessage = authResponse?.Message;
                LogDetailedAuthError(errorCode, errorMessage);

                var errorArgs = new WebSocketErrorEventArgs
                {
                    ErrorMessage = $"WebSocket认证失败: {errorCode} - {errorMessage}",
                    IsAuthError = true,
                    ErrorType = $"AuthError_{errorCode}",
                    Exception = new InvalidOperationException($"认证失败: {errorCode} - {errorMessage}")
                };

                AuthenticationFailed?.Invoke(this, errorArgs);
            }
        }
        catch (JsonException ex)
        {
            _isAuthenticated = false;
            _logger.LogError(ex, "解析认证响应失败: {Message}", responseMessage);

            // 记录认证失败指标
            FeishuMetricsHelper.RecordEventHandlingFailure("auth", "json_parse_error");

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"解析认证响应失败: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;
            _logger.LogError(ex, "处理认证响应时发生错误");

            // 记录认证失败指标
            FeishuMetricsHelper.RecordEventHandlingFailure("auth", "unknown_error");

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"处理认证响应时发生错误: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);
        }
    }

    /// <summary>
    /// 重置认证状态
    /// </summary>
    public void ResetAuthentication()
    {
        _isAuthenticated = false;
        _authRetryCount = 0;
        _logger.LogDebug("已重置认证状态");
    }

    /// <summary>
    /// 获取认证重试次数（当前认证流程）
    /// </summary>
    public int AuthRetryCount => _authRetryCount;

    /// <summary>
    /// 获取总认证失败次数
    /// </summary>
    public int TotalAuthFailures => _totalAuthFailures;

    /// <summary>
    /// 获取最近一次认证失败时间
    /// </summary>
    public DateTime LastAuthFailureTime => _lastAuthFailureTime;

    /// <summary>
    /// 检查是否处于认证冷却期
    /// </summary>
    /// <param name="appAccessToken">应用访问令牌（用作标识）</param>
    /// <returns>是否处于冷却期</returns>
    private bool IsInAuthCooldown(string appAccessToken)
    {
        lock (_cooldownLock)
        {
            var tokenHash = GetTokenHash(appAccessToken);
            if (_authFailureCooldowns.TryGetValue(tokenHash, out var cooldownEnd))
            {
                if (DateTime.UtcNow < cooldownEnd)
                {
                    return true;
                }
                else
                {
                    // 冷却期已过，移除记录
                    _authFailureCooldowns.Remove(tokenHash);
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 获取认证冷却期剩余时间
    /// </summary>
    /// <param name="appAccessToken">应用访问令牌</param>
    /// <returns>剩余冷却时间</returns>
    private TimeSpan GetAuthCooldownRemaining(string appAccessToken)
    {
        lock (_cooldownLock)
        {
            var tokenHash = GetTokenHash(appAccessToken);
            if (_authFailureCooldowns.TryGetValue(tokenHash, out var cooldownEnd))
            {
                var remaining = cooldownEnd - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// 记录认证失败
    /// </summary>
    /// <param name="appAccessToken">应用访问令牌</param>
    private void RecordAuthFailure(string appAccessToken)
    {
        lock (_cooldownLock)
        {
            _totalAuthFailures++;
            _lastAuthFailureTime = DateTime.UtcNow;

            // 如果连续失败次数达到阈值，设置冷却期
            if (_totalAuthFailures >= MaxAuthFailuresBeforeCooldown)
            {
                var tokenHash = GetTokenHash(appAccessToken);
                var cooldownEnd = DateTime.UtcNow.Add(AuthFailureCooldownPeriod);
                _authFailureCooldowns[tokenHash] = cooldownEnd;

                _logger.LogWarning("认证失败次数达到阈值 {MaxFailures}，设置冷却期至 {CooldownEnd}",
                    MaxAuthFailuresBeforeCooldown, cooldownEnd);
            }
        }
    }

    /// <summary>
    /// 清除认证冷却期
    /// </summary>
    /// <param name="appAccessToken">应用访问令牌</param>
    private void ClearAuthCooldown(string appAccessToken)
    {
        lock (_cooldownLock)
        {
            var tokenHash = GetTokenHash(appAccessToken);
            if (_authFailureCooldowns.Remove(tokenHash))
            {
                _logger.LogDebug("已清除认证冷却期");
            }
            // 认证成功后重置失败计数
            _totalAuthFailures = 0;
        }
    }

    /// <summary>
    /// 获取令牌哈希（用于冷却期标识，避免在内存中存储完整令牌）
    /// </summary>
    /// <param name="appAccessToken">应用访问令牌</param>
    /// <returns>令牌哈希</returns>
    private static string GetTokenHash(string appAccessToken)
    {
        // 使用令牌的哈希值作为标识，避免在内存中存储完整令牌
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(appAccessToken));
        var base64Hash = Convert.ToBase64String(hashBytes);
        // 兼容netstandard2.0，使用Substring代替Range
        return base64Hash.Length > 16 ? base64Hash.Substring(0, 16) : base64Hash;
    }

    /// <summary>
    /// 记录详细的认证错误信息
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMessage">错误消息</param>
    private void LogDetailedAuthError(int? errorCode, string? errorMessage)
    {
        if (!errorCode.HasValue)
        {
            _logger.LogWarning("认证响应缺少错误码: {Message}", errorMessage ?? "未知错误");
            return;
        }

        switch (errorCode.Value)
        {
            // ===== 认证相关错误码 =====
            case 10009: // Token 过期
                _logger.LogWarning("应用访问令牌已过期，请更新令牌");
                break;
            case 10010: // Token 无效
                _logger.LogError("应用访问令牌无效，请检查 App ID 和 App Secret 配置");
                break;
            case 10011: // 权限不足
                _logger.LogError("应用权限不足，请检查应用权限配置");
                break;
            case 10012: // 参数错误
                _logger.LogError("认证参数错误: {Message}", errorMessage);
                break;
            case 10013: // 系统繁忙
                _logger.LogWarning("飞书系统繁忙，建议稍后重试");
                break;
            case 10014: // 版本不支持
                _logger.LogError("WebSocket 版本不支持，请更新 SDK");
                break;
            case 10015: // Session ID 无效
                _logger.LogWarning("Session ID 无效，将重新建立会话");
                break;

            // ===== 飞书常见业务错误码 =====
            case 99991663: // 机器人被禁用
                _logger.LogError("机器人已被禁用，请检查飞书应用状态");
                break;
            case 99991664: // 机器人不在群聊中
                _logger.LogWarning("机器人不在目标群聊中");
                break;
            case 99991665: // 机器人被移除
                _logger.LogWarning("机器人已被移出群聊");
                break;

            // ===== 服务端错误码 =====
            case 12340001: // 请求参数错误
                _logger.LogError("请求参数错误: {Message}", errorMessage);
                break;
            case 12340002: // 请求体过大
                _logger.LogError("请求体过大，请减少消息内容");
                break;
            case 12340003: // 文件上传失败
                _logger.LogError("文件上传失败: {Message}", errorMessage);
                break;

            // ===== 服务端内部错误 =====
            case 12350001: // 服务端内部错误
                _logger.LogError("飞书服务端内部错误，建议稍后重试");
                break;
            case 12350002: // 服务端超时
                _logger.LogWarning("飞书服务端响应超时，建议稍后重试");
                break;
            case 12350003: // 服务端限流
                _logger.LogWarning("触发飞书服务端限流，建议降低请求频率");
                break;

            // ===== 网络相关错误码 =====
            case 12360001: // 网络连接失败
                _logger.LogError("网络连接失败，请检查网络");
                break;
            case 12360002: // 服务不可用
                _logger.LogWarning("飞书服务暂时不可用，建议稍后重试");
                break;

            default:
                // 根据错误码范围判断错误类型
                if (errorCode.Value >= 10000 && errorCode.Value < 20000)
                {
                    _logger.LogWarning("认证/授权相关错误码: {Code}, 消息: {Message}", errorCode.Value, errorMessage);
                }
                else if (errorCode.Value >= 99990000 && errorCode.Value < 100000000)
                {
                    _logger.LogWarning("机器人相关错误码: {Code}, 消息: {Message}", errorCode.Value, errorMessage);
                }
                else
                {
                    _logger.LogWarning("未知的认证错误码: {Code}, 消息: {Message}", errorCode.Value, errorMessage);
                }
                break;
        }

        // 如果连续失败次数过多，记录警告
        if (_totalAuthFailures > 5)
        {
            _logger.LogWarning("认证已连续失败 {Count} 次，请检查网络连接和配置", _totalAuthFailures);
        }

        // 记录最近失败时间间隔（如果有）
        if (_lastAuthFailureTime != DateTime.MinValue)
        {
            var timeSinceLastFailure = DateTime.UtcNow - _lastAuthFailureTime;
            _logger.LogInformation("距上次认证失败时间: {Minutes}分钟", timeSinceLastFailure.TotalMinutes);
        }
    }
}