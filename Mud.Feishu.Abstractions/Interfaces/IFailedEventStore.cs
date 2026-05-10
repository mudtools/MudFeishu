// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 失败事件存储接口
/// 用于持久化后台处理失败的事件，支持重试
/// </summary>
public interface IFailedEventStore
{
    /// <summary>
    /// 存储失败的事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="exception">异常信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task StoreFailedEventAsync(EventData eventData, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取需要重试的失败事件列表
    /// </summary>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败事件列表</returns>
    Task<IEnumerable<FailedEventInfo>> GetFailedEventsForRetryAsync(int maxRetryCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待重试的失败事件（基于时间）
    /// </summary>
    /// <param name="beforeTime">时间点，获取此时间之前需要重试的事件</param>
    /// <param name="maxCount">最大返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败事件列表</returns>
    Task<List<FailedEventInfo>> GetPendingRetryEventsAsync(DateTimeOffset beforeTime, int maxCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新失败事件的重试次数
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="retryCount">新的重试次数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task UpdateRetryCountAsync(string eventId, int retryCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新失败事件信息
    /// </summary>
    /// <param name="eventInfo">事件信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task UpdateFailedEventAsync(FailedEventInfo eventInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除已成功处理的失败事件记录
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task RemoveFailedEventAsync(string eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 失败事件信息
/// </summary>
public class FailedEventInfo
{
    /// <summary>
    /// 事件ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的事件数据
    /// </summary>
    public string SerializedEventData { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息
    /// </summary>
    public string ExceptionMessage { get; set; } = string.Empty;

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string ExceptionStackTrace { get; set; } = string.Empty;

    /// <summary>
    /// 失败时间
    /// </summary>
    public DateTime FailedAt { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }
}
