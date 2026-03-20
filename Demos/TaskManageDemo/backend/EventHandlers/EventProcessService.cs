// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu.Abstractions;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 事件处理记录实体
/// </summary>
public class EventProcessRecord
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 事件ID（用于幂等性检查）
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 处理状态
    /// </summary>
    public EventProcessStatus Status { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后处理时间
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// 下次重试时间
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// 事件处理状态
/// </summary>
public enum EventProcessStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 处理中
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 处理成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 处理失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已重试超过最大次数
    /// </summary>
    MaxRetryExceeded = 4
}

/// <summary>
/// 事件处理服务接口
/// </summary>
public interface IEventProcessService
{
    /// <summary>
    /// 检查事件是否已处理（幂等性检查）
    /// </summary>
    Task<bool> IsProcessedAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录事件开始处理
    /// </summary>
    Task<EventProcessRecord> StartProcessAsync(string eventId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录事件处理成功
    /// </summary>
    Task MarkSuccessAsync(int recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录事件处理失败
    /// </summary>
    Task MarkFailedAsync(int recordId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待重试的事件
    /// </summary>
    Task<List<EventProcessRecord>> GetPendingRetryEventsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 事件处理服务实现
/// </summary>
public class EventProcessService : IEventProcessService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<EventProcessService> _logger;

    /// <summary>
    /// 初始化事件处理服务
    /// </summary>
    public EventProcessService(TaskManageDbContext dbContext, ILogger<EventProcessService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 检查事件是否已处理
    /// </summary>
    public async Task<bool> IsProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<EventProcessRecord>()
            .FirstOrDefaultAsync(r => r.EventId == eventId, cancellationToken);

        return record?.Status == EventProcessStatus.Success;
    }

    /// <summary>
    /// 记录事件开始处理
    /// </summary>
    public async Task<EventProcessRecord> StartProcessAsync(string eventId, string eventType, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<EventProcessRecord>()
            .FirstOrDefaultAsync(r => r.EventId == eventId, cancellationToken);

        if (record == null)
        {
            record = new EventProcessRecord
            {
                EventId = eventId,
                EventType = eventType,
                Status = EventProcessStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Set<EventProcessRecord>().Add(record);
        }
        else
        {
            record.Status = EventProcessStatus.Processing;
            record.LastProcessedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    /// <summary>
    /// 记录事件处理成功
    /// </summary>
    public async Task MarkSuccessAsync(int recordId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<EventProcessRecord>().FindAsync([recordId], cancellationToken);
        if (record != null)
        {
            record.Status = EventProcessStatus.Success;
            record.LastProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 记录事件处理失败
    /// </summary>
    public async Task MarkFailedAsync(int recordId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<EventProcessRecord>().FindAsync([recordId], cancellationToken);
        if (record != null)
        {
            record.RetryCount++;
            record.ErrorMessage = errorMessage;
            record.LastProcessedAt = DateTime.UtcNow;

            if (record.RetryCount >= record.MaxRetryCount)
            {
                record.Status = EventProcessStatus.MaxRetryExceeded;
                _logger.LogError("事件处理超过最大重试次数: {EventId}, 错误: {Error}", record.EventId, errorMessage);
            }
            else
            {
                record.Status = EventProcessStatus.Failed;
                record.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, record.RetryCount));
                _logger.LogWarning("事件处理失败，将在 {NextRetry} 重试: {EventId}, 错误: {Error}",
                    record.NextRetryAt, record.EventId, errorMessage);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 获取待重试的事件
    /// </summary>
    public async Task<List<EventProcessRecord>> GetPendingRetryEventsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Set<EventProcessRecord>()
            .Where(r => r.Status == EventProcessStatus.Failed &&
                        r.NextRetryAt.HasValue &&
                        r.NextRetryAt.Value <= now)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 支持重试的事件处理器基类
/// </summary>
public abstract class RetryableEventHandler : IFeishuEventHandler
{
    private readonly IEventProcessService _eventProcessService;
    private readonly ILogger _logger;

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public abstract string SupportedEventType { get; }

    /// <summary>
    /// 初始化事件处理器
    /// </summary>
    protected RetryableEventHandler(IEventProcessService eventProcessService, ILogger logger)
    {
        _eventProcessService = eventProcessService;
        _logger = logger;
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        var eventId = GetEventId(eventData);
        if (string.IsNullOrEmpty(eventId))
        {
            _logger.LogWarning("无法获取事件ID，跳过处理");
            return;
        }

        if (await _eventProcessService.IsProcessedAsync(eventId, cancellationToken))
        {
            _logger.LogInformation("事件已处理，跳过: {EventId}", eventId);
            return;
        }

        var record = await _eventProcessService.StartProcessAsync(eventId, SupportedEventType, cancellationToken);

        try
        {
            await ProcessEventAsync(eventData, cancellationToken);
            await _eventProcessService.MarkSuccessAsync(record.Id, cancellationToken);
            _logger.LogInformation("事件处理成功: {EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理失败: {EventId}", eventId);
            await _eventProcessService.MarkFailedAsync(record.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 获取事件ID（用于幂等性检查）
    /// </summary>
    protected virtual string? GetEventId(EventData eventData)
    {
        if (eventData.Event == null) return null;
        var eventJson = JsonSerializer.Serialize(eventData.Event);
        using var doc = JsonDocument.Parse(eventJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("event_id", out var eventIdProp))
        {
            return eventIdProp.GetString();
        }

        return eventData.EventType + "_" + eventData.Event.GetHashCode();
    }

    /// <summary>
    /// 实际处理事件的逻辑
    /// </summary>
    protected abstract Task ProcessEventAsync(EventData eventData, CancellationToken cancellationToken = default);
}
