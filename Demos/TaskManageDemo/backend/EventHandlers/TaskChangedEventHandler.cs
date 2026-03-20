// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Services.Sync;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 任务变更事件处理器
/// </summary>
public class TaskChangedEventHandler : IFeishuEventHandler
{
    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public string SupportedEventType => "task.task.updated_v2";

    private readonly TaskManageDbContext _dbContext;
    private readonly ITaskSyncService _taskSyncService;
    private readonly IEventProcessService _eventProcessService;
    private readonly ILogger<TaskChangedEventHandler> _logger;

    /// <summary>
    /// 初始化任务变更事件处理器
    /// </summary>
    public TaskChangedEventHandler(
        TaskManageDbContext dbContext,
        ITaskSyncService taskSyncService,
        IEventProcessService eventProcessService,
        ILogger<TaskChangedEventHandler> logger)
    {
        _dbContext = dbContext;
        _taskSyncService = taskSyncService;
        _eventProcessService = eventProcessService;
        _logger = logger;
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理任务变更事件: {EventType}", eventData.EventType);

        var eventJson = JsonSerializer.Serialize(eventData.Event);
        var taskEvent = JsonSerializer.Deserialize<TaskChangedEvent>(eventJson);

        if (taskEvent?.Task == null || string.IsNullOrEmpty(taskEvent.Task.TaskId))
        {
            _logger.LogWarning("事件数据解析失败");
            return;
        }

        var taskGuid = taskEvent.Task.TaskId;
        var eventId = $"{eventData.EventType}_{taskGuid}_{eventData.EventId}";

        // 检查幂等性
        if (await _eventProcessService.IsProcessedAsync(eventId, cancellationToken))
        {
            _logger.LogWarning("任务变更事件已处理: {EventId}", eventId);
            return;
        }

        var record = await _eventProcessService.StartProcessAsync(eventId, eventData.EventType, cancellationToken);

        try
        {
            await _taskSyncService.SyncTaskAsync(taskGuid, cancellationToken);
            await _eventProcessService.MarkSuccessAsync(record.Id, cancellationToken);
            _logger.LogInformation("任务同步完成: {TaskGuid}", taskGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理任务变更事件失败: {EventId}", eventId);
            await _eventProcessService.MarkFailedAsync(record.Id, ex.Message, cancellationToken);
            throw;
        }
    }
}

/// <summary>
/// 任务变更事件数据
/// </summary>
public class TaskChangedEvent
{
    /// <summary>
    /// 任务信息
    /// </summary>
    public TaskEventInfo? Task { get; set; }
}

/// <summary>
/// 任务事件信息
/// </summary>
public class TaskEventInfo
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string? TaskId { get; set; }

    /// <summary>
    /// 任务标题
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool? Completed { get; set; }
}
