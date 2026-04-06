// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 降级告警事件类型
/// </summary>
public enum FallbackAlertType
{
    /// <summary>
    /// Redis 连接失败
    /// </summary>
    RedisConnectionFailed,

    /// <summary>
    /// Redis 操作超时
    /// </summary>
    RedisTimeout,

    /// <summary>
    /// Redis 连续失败触发降级
    /// </summary>
    RedisFallbackActivated,

    /// <summary>
    /// Redis 恢复正常
    /// </summary>
    RedisRecovered,

    /// <summary>
    /// 内存去重缓存容量告警
    /// </summary>
    MemoryCacheCapacityWarning,

    /// <summary>
    /// 处理中超时恢复
    /// </summary>
    ProcessingTimeoutRecovery
}

/// <summary>
/// 降级告警事件参数
/// </summary>
public class FallbackAlertEventArgs : EventArgs
{
    /// <summary>
    /// 告警类型
    /// </summary>
    public FallbackAlertType AlertType { get; set; }

    /// <summary>
    /// 告警消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息（可选）
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 连续失败次数
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// 是否已降级
    /// </summary>
    public bool IsFallbackActive { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 附加数据
    /// </summary>
    public Dictionary<string, object?> AdditionalData { get; set; } = new();
}

/// <summary>
/// 降级告警服务接口
/// 用于在 Redis 失败时发送告警通知
/// </summary>
public interface IFallbackAlertService
{
    /// <summary>
    /// 触发告警
    /// </summary>
    /// <param name="alertType">告警类型</param>
    /// <param name="message">告警消息</param>
    /// <param name="exception">异常信息（可选）</param>
    /// <param name="additionalData">附加数据（可选）</param>
    Task RaiseAlertAsync(FallbackAlertType alertType, string message, Exception? exception = null, Dictionary<string, object?>? additionalData = null);

    /// <summary>
    /// 告警事件（可用于订阅告警）
    /// </summary>
    event EventHandler<FallbackAlertEventArgs>? AlertRaised;
}
