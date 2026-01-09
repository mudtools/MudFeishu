// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
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
    private bool _isAuthenticated = false;
    private readonly FeishuWebSocketOptions _options;

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
    public AuthenticationManager(
        ILogger<AuthenticationManager> logger,
        FeishuWebSocketOptions options,
        Func<string, Task> sendMessageCallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sendMessageCallback = sendMessageCallback ?? throw new ArgumentNullException(nameof(sendMessageCallback));
        _options = options;
    }

    /// <summary>
    /// 发送认证消息
    /// </summary>
    public async Task AuthenticateAsync(string appAccessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appAccessToken))
            throw new ArgumentException("应用访问令牌不能为空", nameof(appAccessToken));

        try
        {
           _logger.LogInformation("正在进行WebSocket认证...");
            _isAuthenticated = false; // 重置认证状态

            // 创建认证消息
            var authMessage = new AuthMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data = new AuthData
                {
                    AppAccessToken = appAccessToken
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
           _logger.LogError(ex, "WebSocket认证失败");

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"WebSocket认证失败: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);
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
                Authenticated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _isAuthenticated = false;
                _logger.LogError("WebSocket认证失败: {Code} - {Message}", authResponse?.Code, authResponse?.Message);
                

                var errorArgs = new WebSocketErrorEventArgs
                {
                    ErrorMessage = $"WebSocket认证失败: {authResponse?.Code} - {authResponse?.Message}",
                    IsAuthError = true
                };

                AuthenticationFailed?.Invoke(this, errorArgs);
            }
        }
        catch (JsonException ex)
        {
            _isAuthenticated = false;
            _logger.LogError(ex, "解析认证响应失败: {Message}", responseMessage);

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
       _logger.LogDebug("已重置认证状态");
    }
}