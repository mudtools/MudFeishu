// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 会话管理器 - 负责管理 session_id 和会话状态
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private string? _currentSessionId;
    private DateTime _sessionStartTime = DateTime.MinValue;
    private readonly object _sessionLock = new();

    /// <summary>
    /// 会话更新事件
    /// </summary>
    public event EventHandler<SessionUpdatedEventArgs>? SessionUpdated;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SessionManager(
        ILogger<SessionManager> logger,
        FeishuWebSocketOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
    }

    /// <summary>
    /// 获取当前会话ID
    /// </summary>
    public string? CurrentSessionId
    {
        get
        {
            lock (_sessionLock)
            {
                return _currentSessionId;
            }
        }
    }

    /// <summary>
    /// 获取会话持续时间
    /// </summary>
    public TimeSpan SessionDuration
    {
        get
        {
            lock (_sessionLock)
            {
                return _sessionStartTime == DateTime.MinValue
                    ? TimeSpan.Zero
                    : DateTime.UtcNow - _sessionStartTime;
            }
        }
    }

    /// <summary>
    /// 是否有有效的会话
    /// </summary>
    public bool HasValidSession
    {
        get
        {
            lock (_sessionLock)
            {
                return !string.IsNullOrEmpty(_currentSessionId) &&
                       _sessionStartTime != DateTime.MinValue &&
                       SessionDuration < TimeSpan.FromHours(24); // 会话有效期为24小时
            }
        }
    }

    /// <summary>
    /// 设置会话ID
    /// </summary>
    public void SetSessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("会话ID不能为空", nameof(sessionId));

        lock (_sessionLock)
        {
            var isNewSession = _currentSessionId != sessionId;

            _currentSessionId = sessionId;
            _sessionStartTime = DateTime.UtcNow;

            if (isNewSession)
            {
                _logger.LogInformation("会话已更新: {SessionId}", sessionId);

                SessionUpdated?.Invoke(this, new SessionUpdatedEventArgs
                {
                    SessionId = sessionId,
                    SessionStartTime = _sessionStartTime,
                    IsNewSession = true
                });
            }
        }
    }

    /// <summary>
    /// 获取会话ID用于重连
    /// </summary>
    public string? GetSessionIdForReconnect()
    {
        lock (_sessionLock)
        {
            if (!HasValidSession)
            {
                if (_options.EnableLogging)
                    _logger.LogDebug("当前无有效会话，无法恢复");
                return null;
            }

            if (_options.EnableLogging)
                _logger.LogDebug("获取会话ID用于重连: {SessionId} (会话时长: {Duration})",
                    _currentSessionId, SessionDuration);

            return _currentSessionId;
        }
    }

    /// <summary>
    /// 重置会话
    /// </summary>
    public void ResetSession()
    {
        lock (_sessionLock)
        {
            var oldSessionId = _currentSessionId;

            _currentSessionId = null;
            _sessionStartTime = DateTime.MinValue;

            if (!string.IsNullOrEmpty(oldSessionId))
            {
                _logger.LogInformation("会话已重置: {OldSessionId}", oldSessionId);

                SessionUpdated?.Invoke(this, new SessionUpdatedEventArgs
                {
                    SessionId = null,
                    SessionStartTime = DateTime.MinValue,
                    IsNewSession = false
                });
            }
        }
    }
}

/// <summary>
/// 会话更新事件参数
/// </summary>
public class SessionUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 会话开始时间
    /// </summary>
    public DateTime SessionStartTime { get; set; }

    /// <summary>
    /// 是否为新会话
    /// </summary>
    public bool IsNewSession { get; set; }
}
