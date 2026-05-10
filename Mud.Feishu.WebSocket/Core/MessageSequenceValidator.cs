// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息序号验证器 - 检测消息重放和丢失
/// </summary>
public class MessageSequenceValidator
{
    private readonly ILogger<MessageSequenceValidator> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly object _lock = new();
    private ulong? _lastProcessedSequenceNumber;
    private readonly SortedSet<ulong> _recentlyProcessedNumbers = new();
    private DateTime _lastResetTime = DateTime.UtcNow;

    /// <summary>
    /// 最近处理的序号数量（用于滑动窗口去重）
    /// </summary>
    private const int RecentNumbersWindow = 1000;

    /// <summary>
    /// 序号跳跃阈值，超过此值认为消息丢失
    /// </summary>
    private const int SequenceGapThreshold = 10;

    /// <summary>
    /// 清理旧数据的间隔（分钟）
    /// </summary>
    private const int CleanupIntervalMinutes = 30;

    /// <summary>
    /// 验证失败事件
    /// </summary>
    public event EventHandler<SequenceValidationEventArgs>? ValidationFailed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageSequenceValidator(
        ILogger<MessageSequenceValidator> logger,
        FeishuWebSocketOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
    }

    /// <summary>
    /// 验证消息序号
    /// </summary>
    /// <param name="sequenceNumber">消息序号</param>
    /// <param name="messageId">消息ID（可选）</param>
    /// <returns>验证结果</returns>
    public SequenceValidationResult ValidateSequence(ulong sequenceNumber, string? messageId = null)
    {
        lock (_lock)
        {
            // 定期清理旧数据
            CleanupOldData();

            // 首次接收消息
            if (!_lastProcessedSequenceNumber.HasValue)
            {
                _lastProcessedSequenceNumber = sequenceNumber;
                _recentlyProcessedNumbers.Add(sequenceNumber);

                if (_options.EnableLogging)
                    _logger.LogDebug("首次接收消息，序号: {SequenceNumber}", sequenceNumber);

                return SequenceValidationResult.Valid;
            }

            // 检查是否为重复消息（重放攻击）
            if (IsDuplicateMessage(sequenceNumber))
            {
                _logger.LogWarning("检测到重复消息: SequenceNumber={SequenceNumber}, MessageId={MessageId}",
                    sequenceNumber, messageId ?? "N/A");

                return SequenceValidationResult.Duplicate;
            }

            // 检查序号是否连续（检测消息丢失）
            if (sequenceNumber != _lastProcessedSequenceNumber + 1)
            {
                // 使用有符号整数比较来检测序号回退
                bool isRollback = sequenceNumber < _lastProcessedSequenceNumber.Value;

                // 如果序号回退，可能是重放攻击
                if (isRollback)
                {
                    long gap = (long)sequenceNumber - (long)_lastProcessedSequenceNumber.Value;
                    _logger.LogWarning("检测到序号回退（可能的攻击）: Last={LastSequence}, Current={CurrentSequence}, Gap={Gap}",
                        _lastProcessedSequenceNumber, sequenceNumber, gap);

                    ValidationFailed?.Invoke(this, new SequenceValidationEventArgs
                    {
                        SequenceNumber = sequenceNumber,
                        MessageType = SequenceValidationType.SequenceRollback,
                        Message = $"检测到序号回退: {sequenceNumber} (上一次: {_lastProcessedSequenceNumber})"
                    });

                    return SequenceValidationResult.Rollback;
                }

                // 计算序号跳跃的间隙
                ulong sequenceGap = sequenceNumber - _lastProcessedSequenceNumber.Value;

                // 如果序号跳跃较大，可能有消息丢失
                if (sequenceGap > SequenceGapThreshold)
                {
                    _logger.LogWarning("检测到消息丢失: Last={LastSequence}, Current={CurrentSequence}, LostCount={LostCount}",
                        _lastProcessedSequenceNumber, sequenceNumber, sequenceGap - 1);

                    ValidationFailed?.Invoke(this, new SequenceValidationEventArgs
                    {
                        SequenceNumber = sequenceNumber,
                        MessageType = SequenceValidationType.MessageLoss,
                        Message = $"可能丢失 {sequenceGap - 1} 条消息: 上一次序号 {_lastProcessedSequenceNumber}, 当前序号 {sequenceNumber}"
                    });
                }
                // 允许序号小范围跳跃（正常网络情况）
            }

            // 验证通过，更新状态
            _lastProcessedSequenceNumber = sequenceNumber;
            _recentlyProcessedNumbers.Add(sequenceNumber);

            return SequenceValidationResult.Valid;
        }
    }

    /// <summary>
    /// 检查是否为重复消息
    /// </summary>
    private bool IsDuplicateMessage(ulong sequenceNumber)
    {
        // 检查是否在最近处理的序号窗口内
        if (_recentlyProcessedNumbers.Contains(sequenceNumber))
            return true;

        // 注意：序号小于上一次处理的序号的情况，会在 ValidateSequence 方法中单独处理为 Rollback
        // 这里只检查是否完全相等（重复）
        if (_lastProcessedSequenceNumber.HasValue && sequenceNumber == _lastProcessedSequenceNumber.Value)
            return true;

        return false;
    }

    /// <summary>
    /// 清理旧数据
    /// </summary>
    private void CleanupOldData()
    {
        var timeSinceReset = DateTime.UtcNow - _lastResetTime;
        if (timeSinceReset.TotalMinutes > CleanupIntervalMinutes)
        {
            if (_options.EnableLogging)
                _logger.LogDebug("清理消息序号验证器的旧数据");

            _recentlyProcessedNumbers.Clear();
            _lastResetTime = DateTime.UtcNow;
        }
        else if (_recentlyProcessedNumbers.Count > RecentNumbersWindow)
        {
            // 使用 SortedSet 的 Min 属性（O(1) 操作）
            // 移除最旧的序号直到窗口大小
            while (_recentlyProcessedNumbers.Count > RecentNumbersWindow)
            {
                _recentlyProcessedNumbers.Remove(_recentlyProcessedNumbers.Min);
            }
        }
    }

    /// <summary>
    /// 重置验证器
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _lastProcessedSequenceNumber = null;
            _recentlyProcessedNumbers.Clear();
            _logger.LogInformation("消息序号验证器已重置");
        }
    }

    /// <summary>
    /// 获取验证器状态
    /// </summary>
    public ValidatorStatus GetStatus()
    {
        lock (_lock)
        {
            return new ValidatorStatus
            {
                LastProcessedSequenceNumber = _lastProcessedSequenceNumber,
                RecentlyProcessedCount = _recentlyProcessedNumbers.Count,
                LastResetTime = _lastResetTime
            };
        }
    }
}

/// <summary>
/// 序号验证结果
/// </summary>
public enum SequenceValidationResult
{
    /// <summary>
    /// 验证通过
    /// </summary>
    Valid,

    /// <summary>
    /// 重复消息
    /// </summary>
    Duplicate,

    /// <summary>
    /// 序号回退（可能的攻击）
    /// </summary>
    Rollback,

    /// <summary>
    /// 可能丢失消息
    /// </summary>
    MessageLoss
}

/// <summary>
/// 序号验证类型
/// </summary>
public enum SequenceValidationType
{
    /// <summary>
    /// 序号回退
    /// </summary>
    SequenceRollback,

    /// <summary>
    /// 消息丢失
    /// </summary>
    MessageLoss
}

/// <summary>
/// 序号验证事件参数
/// </summary>
public class SequenceValidationEventArgs : EventArgs
{
    /// <summary>
    /// 序号
    /// </summary>
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// 验证类型
    /// </summary>
    public SequenceValidationType MessageType { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 验证器状态
/// </summary>
public class ValidatorStatus
{
    /// <summary>
    /// 最后处理的序号
    /// </summary>
    public ulong? LastProcessedSequenceNumber { get; set; }

    /// <summary>
    /// 最近处理的序号数量
    /// </summary>
    public int RecentlyProcessedCount { get; set; }

    /// <summary>
    /// 最后重置时间
    /// </summary>
    public DateTime LastResetTime { get; set; }
}
