// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Task;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using TaskManageDemo.Backend.Services.Sync;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 任务变更事件处理器
/// <para>处理飞书任务变更事件</para>
/// </summary>
public class FeishuTaskUpdatedEventHandler : global::Mud.Feishu.Abstractions.EventHandlers.TaskUpdatedEventHandler
{
    private readonly ITaskSyncService _taskSyncService;

    public FeishuTaskUpdatedEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<FeishuTaskUpdatedEventHandler> logger,
        ITaskSyncService taskSyncService)
        : base(businessDeduplicator, logger)
    {
        _taskSyncService = taskSyncService ?? throw new ArgumentNullException(nameof(taskSyncService));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        TaskUpdatedResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity == null || string.IsNullOrEmpty(eventEntity.TaskId))
        {
            _logger.LogWarning("任务变更事件实体为空或任务ID缺失，跳过处理");
            return;
        }

        var taskGuid = eventEntity.TaskId;
        var objType = eventEntity.ObjType;

        _logger.LogInformation("处理任务变更事件: TaskId={TaskId}, ObjType={ObjType}",
            taskGuid, objType);

        var objTypeDescription = objType switch
        {
            1 => "任务详情发生变化",
            2 => "任务协作者发生变化",
            3 => "任务关注者发生变化",
            4 => "任务提醒时间发生变化",
            5 => "任务完成",
            6 => "任务取消完成",
            7 => "任务删除",
            _ => "未知变更类型"
        };

        _logger.LogDebug("任务变更类型: {ObjTypeDescription}", objTypeDescription);

        await _taskSyncService.SyncTaskAsync(taskGuid, cancellationToken);

        _logger.LogInformation("任务同步完成: TaskId={TaskId}", taskGuid);
    }
}
