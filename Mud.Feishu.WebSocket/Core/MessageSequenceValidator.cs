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
    /// <summary>
    /// 业务失败后回滚的序号：允许同序号重发通过一次（one-shot）。
    /// 仅移除窗口记录不够——若游标已推进到该序号或更大值，
    /// <see cref="IsDuplicateMessage"/> 的游标相等/回退分支仍会吞掉重发。
    /// </summary>
    private readonly HashSet<ulong> _reprocessAllowed = new();
    private DateTime _lastResetTime = DateTime.UtcNow;

    /// <summary>
    /// 最近处理的序号数量（用于滑动窗口去重）
    /// </summary>
    private const int RecentNumbersWindow = 1000;

    /// <summary>
    /// 序号跳跃阈值，超过此值认为消息丢失。
    /// 从 FeishuWebSocketOptions.SequenceGapThreshold 读取，
    /// 默认为 0（禁用跳跃检测），因为飞书 SeqID 是全局计数器。
    /// </summary>
    private readonly ulong _sequenceGapThreshold;

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
        _sequenceGapThreshold = _options.SequenceGapThreshold;
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

            // P0-1：业务失败回滚后的同序号重发——一次性放行，不再落入 Duplicate/Rollback
            if (_reprocessAllowed.Remove(sequenceNumber))
            {
                _lastProcessedSequenceNumber = sequenceNumber;
                _recentlyProcessedNumbers.Add(sequenceNumber);
                _logger.LogDebug("业务失败回滚后的重发放行: SeqId={SequenceNumber}", sequenceNumber);
                return SequenceValidationResult.Valid;
            }

            // 首次接收消息
            if (!_lastProcessedSequenceNumber.HasValue)
            {
                _lastProcessedSequenceNumber = sequenceNumber;
                _recentlyProcessedNumbers.Add(sequenceNumber);

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
                // 当 _sequenceGapThreshold 为 0 时，禁用跳跃检测（飞书 SeqID 是全局计数器，跳跃属正常现象）
                if (_sequenceGapThreshold > 0 && sequenceGap > _sequenceGapThreshold)
                {
                    _logger.LogWarning("检测到消息丢失: Last={LastSequence}, Current={CurrentSequence}, LostCount={LostCount}",
                        _lastProcessedSequenceNumber, sequenceNumber, sequenceGap - 1);

                    ValidationFailed?.Invoke(this, new SequenceValidationEventArgs
                    {
                        SequenceNumber = sequenceNumber,
                        MessageType = SequenceValidationType.MessageLoss,
                        Message = $"可能丢失 {sequenceGap - 1} 条消息: 上一次序号 {_lastProcessedSequenceNumber}, 当前序号 {sequenceNumber}"
                    });

                    _lastProcessedSequenceNumber = sequenceNumber;
                    _recentlyProcessedNumbers.Add(sequenceNumber);

                    return SequenceValidationResult.MessageLoss;
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
            _logger.LogDebug("清理消息序号验证器的旧数据");

            _recentlyProcessedNumbers.Clear();
            _reprocessAllowed.Clear();
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

            // 放行集合同步限容，避免失败风暴导致无界增长
            while (_reprocessAllowed.Count > RecentNumbersWindow)
            {
                var any = System.Linq.Enumerable.First(_reprocessAllowed);
                _reprocessAllowed.Remove(any);
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
            _reprocessAllowed.Clear();
            _logger.LogInformation("消息序号验证器已重置");
        }
    }

    /// <summary>
    /// 回滚指定序号的幂等检测状态（业务处理失败后调用），
    /// 使服务端重发同序号帧时不再被判定为 Duplicate/Rollback。
    /// </summary>
    /// <remarks>
    /// 实现：从窗口移除 + 写入一次性放行集合。游标本身不主动回退到更小值
    /// （避免破坏后续新帧的判定），重发放行由 <c>_reprocessAllowed</c> 保证——
    /// 因为 <see cref="IsDuplicateMessage"/> 在窗口未命中时仍会因
    ///「游标相等 / 序号回退」吞掉重发。
    /// </remarks>
    /// <param name="sequenceNumber">要回滚的序号</param>
    /// <returns>是否确实登记了回滚（窗口记录存在，或游标等于该序号）</returns>
    public bool Remove(ulong sequenceNumber)
    {
        lock (_lock)
        {
            var removedFromWindow = _recentlyProcessedNumbers.Remove(sequenceNumber);
            var isCursorMatch = _lastProcessedSequenceNumber == sequenceNumber;

            if (!removedFromWindow && !isCursorMatch)
            {
                // 未在窗口且非游标：可能是并发路径下已被清理/从未处理，仍登记放行以便安全重试
                _logger.LogDebug("回滚序号 {SequenceNumber}：窗口无记录，登记一次性放行", sequenceNumber);
            }

            _reprocessAllowed.Add(sequenceNumber);
            _logger.LogDebug("已回滚序号 {SequenceNumber} 的重复检测状态", sequenceNumber);
            return true;
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
