// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu;
using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.DataModels.TasksList;
using System.Text.Json;

namespace TaskManageDemo.Backend.Services.Feishu;

/// <summary>
/// 飞书任务服务实现
/// </summary>
public class FeishuTaskService : IFeishuTaskService
{
    private readonly IFeishuTenantV2Task _taskApi;
    private readonly IFeishuTenantV2TaskList _taskListApi;
    private readonly ILogger<FeishuTaskService> _logger;

    /// <summary>
    /// 初始化飞书任务服务
    /// </summary>
    public FeishuTaskService(
        IFeishuTenantV2Task taskApi,
        IFeishuTenantV2TaskList taskListApi,
        ILogger<FeishuTaskService> logger)
    {
        _taskApi = taskApi;
        _taskListApi = taskListApi;
        _logger = logger;
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    public async Task<string?> CreateTaskAsync(
        string summary,
        string? description,
        List<string>? assignees,
        DateTime? dueTime,
        DateTime? startTime = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateTaskRequest
        {
            Summary = summary,
            Description = description,
            Due = dueTime.HasValue
                ? new TaskTime { Timestamp = ((DateTimeOffset)dueTime.Value).ToUnixTimeMilliseconds().ToString() }
                : null,
            Start = startTime.HasValue
                ? new TasksStartTime { Timestamp = ((DateTimeOffset)startTime.Value).ToUnixTimeMilliseconds().ToString() }
                : null,
            Members = assignees?.Select(a => new TaskMemberInfo
            {
                Id = a,
                Type = "user",
                Role = "assignee"
            }).ToArray()
        };

        var result = await _taskApi.CreateTaskAsync(request, cancellationToken: cancellationToken);

        if (result?.Data?.Task?.Guid != null)
        {
            _logger.LogInformation("任务创建成功: {TaskGuid}", result.Data.Task.Guid);
            return result.Data.Task.Guid;
        }

        _logger.LogWarning("任务创建失败: {Result}", JsonSerializer.Serialize(result));
        return null;
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    public async Task<bool> UpdateTaskAsync(
        string taskGuid,
        string? summary,
        string? description,
        bool? isCompleted,
        DateTime? dueTime,
        CancellationToken cancellationToken = default)
    {
        var updateFields = new List<string>();
        var taskData = new UpdateTaskData();

        if (summary != null)
        {
            taskData.Summary = summary;
            updateFields.Add("summary");
        }

        if (description != null)
        {
            taskData.Description = description;
            updateFields.Add("description");
        }

        if (isCompleted.HasValue)
        {
            taskData.CompletedAt = isCompleted.Value
                ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                : "0";
            updateFields.Add("completed_at");
        }

        if (dueTime.HasValue)
        {
            taskData.Due = new TaskTime { Timestamp = ((DateTimeOffset)dueTime.Value).ToUnixTimeMilliseconds().ToString() };
            updateFields.Add("due");
        }

        if (updateFields.Count == 0)
        {
            return true;
        }

        var request = new UpdateTaskRequest
        {
            Task = taskData,
            UpdateFields = updateFields.ToArray()
        };

        var result = await _taskApi.UpdateTaskAsync(taskGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("任务更新成功: {TaskGuid}", taskGuid);
            return true;
        }

        _logger.LogWarning("任务更新失败: {TaskGuid}", taskGuid);
        return false;
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    public async Task<TaskSync?> GetTaskByIdAsync(string taskGuid, CancellationToken cancellationToken = default)
    {
        var result = await _taskApi.GetTaskByIdAsync(taskGuid, cancellationToken: cancellationToken);

        if (result?.Data?.Task != null)
        {
            var task = result.Data.Task;
            var taskSync = new TaskSync
            {
                TaskGuid = task.Guid ?? taskGuid,
                Summary = task.Summary ?? string.Empty,
                Description = task.Description,
                IsCompleted = !string.IsNullOrEmpty(task.CompletedAt) && task.CompletedAt != "0",
                StartTime = task.Start?.Timestamp != null
                    ? DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(task.Start.Timestamp)).DateTime
                    : null,
                DueTime = task.Due?.Timestamp != null
                    ? DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(task.Due.Timestamp)).DateTime
                    : null,
                CompletedTime = task.CompletedAt != null && long.TryParse(task.CompletedAt, out var completedTs) && completedTs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(completedTs).DateTime
                    : null,
                CreatorId = task.Creator?.Id,
                LastSyncedAt = DateTime.UtcNow
            };

            if (task.Members != null && task.Members.Length > 0)
            {
                taskSync.Members = task.Members.Select(m => new TaskMemberEntity
                {
                    FeishuUserId = m.Id,
                    Role = m.Role ?? TaskMemberRoles.Assignee,
                    JoinedAt = DateTime.UtcNow
                }).ToList();
            }

            return taskSync;
        }

        return null;
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    public async Task<bool> DeleteTaskAsync(string taskGuid, CancellationToken cancellationToken = default)
    {
        var result = await _taskApi.DeleteTaskByIdAsync(taskGuid, cancellationToken);

        if (result != null && result.Code == 0)
        {
            _logger.LogInformation("任务删除成功: {TaskGuid}", taskGuid);
            return true;
        }

        _logger.LogWarning("任务删除失败: {TaskGuid}", taskGuid);
        return false;
    }

    /// <summary>
    /// 添加任务成员
    /// </summary>
    public async Task<bool> AddMembersAsync(
        string taskGuid,
        List<string> assigneeIds,
        List<string> followerIds,
        CancellationToken cancellationToken = default)
    {
        var members = new List<TaskMemberInfo>();

        members.AddRange(assigneeIds.Select(id => new TaskMemberInfo
        {
            Id = id,
            Type = "user",
            Role = "assignee"
        }));

        members.AddRange(followerIds.Select(id => new TaskMemberInfo
        {
            Id = id,
            Type = "user",
            Role = "follower"
        }));

        if (members.Count == 0)
        {
            return true;
        }

        var request = new AddMembersRequest
        {
            Members = members.ToArray()
        };

        var result = await _taskApi.AddMembersByIdAsync(taskGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("任务成员添加成功: {TaskGuid}", taskGuid);
            return true;
        }

        _logger.LogWarning("任务成员添加失败: {TaskGuid}", taskGuid);
        return false;
    }

    /// <summary>
    /// 移除任务成员
    /// </summary>
    public async Task<bool> RemoveMembersAsync(
        string taskGuid,
        List<string> memberIds,
        CancellationToken cancellationToken = default)
    {
        var request = new RemoveMembersRequest
        {
            Members = memberIds.Select(id => new TaskMemberInfo
            {
                Id = id,
                Type = "user"
            }).ToArray()
        };

        var result = await _taskApi.RemoveMembersByIdAsync(taskGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("任务成员移除成功: {TaskGuid}", taskGuid);
            return true;
        }

        _logger.LogWarning("任务成员移除失败: {TaskGuid}", taskGuid);
        return false;
    }

    /// <summary>
    /// 创建子任务
    /// </summary>
    public async Task<string?> CreateSubTaskAsync(
        string parentTaskGuid,
        string summary,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateSubTaskRequest
        {
            Summary = summary,
            Description = description
        };

        var result = await _taskApi.CreateSubTaskAsync(parentTaskGuid, request, cancellationToken: cancellationToken);

        if (result?.Data?.Subtask?.Guid != null)
        {
            _logger.LogInformation("子任务创建成功: {TaskGuid}", result.Data.Subtask.Guid);
            return result.Data.Subtask.Guid;
        }

        _logger.LogWarning("子任务创建失败");
        return null;
    }

    /// <summary>
    /// 添加任务提醒
    /// </summary>
    public async Task<bool> AddTaskReminderAsync(
        string taskGuid,
        int relativeFireMinute,
        CancellationToken cancellationToken = default)
    {
        var request = new AddTaskReminderRequest
        {
            Reminders = new[]
            {
                new AddTaskReminder
                {
                    RelativeFireMinute = relativeFireMinute
                }
            }
        };

        var result = await _taskApi.AddTaskReminderByIdAsync(taskGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("任务提醒添加成功: {TaskGuid}", taskGuid);
            return true;
        }

        _logger.LogWarning("任务提醒添加失败: {TaskGuid}", taskGuid);
        return false;
    }

    /// <summary>
    /// 获取清单下的所有任务
    /// </summary>
    public async Task<List<TaskSummary>> GetTaskListTasksAsync(
        string taskListGuid,
        bool? completed = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<TaskSummary>();
        string? pageToken = null;

        do
        {
            var result = await _taskListApi.GetTaskListPageListByIdAsync(
                taskListGuid,
                page_size: 50,
                page_token: pageToken,
                completed: completed,
                cancellationToken: cancellationToken);

            if (result?.Data?.Items != null)
            {
                tasks.AddRange(result.Data.Items);
            }

            pageToken = result?.Data?.PageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        _logger.LogInformation("获取清单任务列表成功: {TaskListGuid}, 数量: {Count}", taskListGuid, tasks.Count);
        return tasks;
    }
}
