// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu;
using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.DataModels.TasksList;

namespace TaskManageDemo.Backend.Services.Feishu;

/// <summary>
/// 飞书任务清单服务接口
/// </summary>
public interface IFeishuTaskListService
{
    /// <summary>
    /// 创建任务清单
    /// </summary>
    Task<string?> CreateTaskListAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务清单详情
    /// </summary>
    Task<TaskList?> GetTaskListByIdAsync(string taskListGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务清单
    /// </summary>
    Task<bool> UpdateTaskListAsync(
        string taskListGuid,
        string? name,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务清单
    /// </summary>
    Task<bool> DeleteTaskListAsync(string taskListGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加清单成员
    /// </summary>
    Task<bool> AddMembersAsync(
        string taskListGuid,
        List<string> memberIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除清单成员
    /// </summary>
    Task<bool> RemoveMembersAsync(
        string taskListGuid,
        List<string> memberIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 飞书任务清单服务实现
/// </summary>
public class FeishuTaskListService : IFeishuTaskListService
{
    private readonly IFeishuTenantV2TaskList _taskListApi;
    private readonly ILogger<FeishuTaskListService> _logger;

    /// <summary>
    /// 初始化飞书任务清单服务
    /// </summary>
    public FeishuTaskListService(IFeishuTenantV2TaskList taskListApi, ILogger<FeishuTaskListService> logger)
    {
        _taskListApi = taskListApi;
        _logger = logger;
    }

    /// <summary>
    /// 创建任务清单
    /// </summary>
    public async Task<string?> CreateTaskListAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateTaskListRequest
        {
            Name = name
        };

        var result = await _taskListApi.CreateTaskListAsync(request, cancellationToken: cancellationToken);

        if (result?.Data?.Tasklist?.Guid != null)
        {
            _logger.LogInformation("任务清单创建成功: {TaskListGuid}", result.Data.Tasklist.Guid);
            return result.Data.Tasklist.Guid;
        }

        _logger.LogWarning("任务清单创建失败");
        return null;
    }

    /// <summary>
    /// 获取任务清单详情
    /// </summary>
    public async Task<TaskList?> GetTaskListByIdAsync(string taskListGuid, CancellationToken cancellationToken = default)
    {
        var result = await _taskListApi.GetTaskListByIdAsync(taskListGuid, cancellationToken: cancellationToken);

        if (result?.Data?.Tasklist != null)
        {
            var taskList = result.Data.Tasklist;
            return new TaskList
            {
                TaskListGuid = taskList.Guid ?? taskListGuid,
                Name = taskList.Name ?? string.Empty,
                LastSyncedAt = DateTime.UtcNow
            };
        }

        return null;
    }

    /// <summary>
    /// 更新任务清单
    /// </summary>
    public async Task<bool> UpdateTaskListAsync(
        string taskListGuid,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateTaskListRequest
        {
            Tasklist = new TaskListData
            {
                Name = name ?? string.Empty
            },
            UpdateFields = name != null ? ["name"] : []
        };

        var result = await _taskListApi.UpdateTaskListByIdAsync(taskListGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("任务清单更新成功: {TaskListGuid}", taskListGuid);
            return true;
        }

        _logger.LogWarning("任务清单更新失败: {TaskListGuid}", taskListGuid);
        return false;
    }

    /// <summary>
    /// 删除任务清单
    /// </summary>
    public async Task<bool> DeleteTaskListAsync(string taskListGuid, CancellationToken cancellationToken = default)
    {
        var result = await _taskListApi.DeleteTaskListByIdAsync(taskListGuid, cancellationToken);

        if (result != null && result.Code == 0)
        {
            _logger.LogInformation("任务清单删除成功: {TaskListGuid}", taskListGuid);
            return true;
        }

        _logger.LogWarning("任务清单删除失败: {TaskListGuid}", taskListGuid);
        return false;
    }

    /// <summary>
    /// 添加清单成员
    /// </summary>
    public async Task<bool> AddMembersAsync(
        string taskListGuid,
        List<string> memberIds,
        CancellationToken cancellationToken = default)
    {
        var request = new AddTaskListMemberRequest
        {
            Members = memberIds.Select(id => new TaskMember
            {
                Id = id,
                Type = "user"
            }).ToArray()
        };

        var result = await _taskListApi.AddTaskListMemberByIdAsync(taskListGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("清单成员添加成功: {TaskListGuid}", taskListGuid);
            return true;
        }

        _logger.LogWarning("清单成员添加失败: {TaskListGuid}", taskListGuid);
        return false;
    }

    /// <summary>
    /// 移除清单成员
    /// </summary>
    public async Task<bool> RemoveMembersAsync(
        string taskListGuid,
        List<string> memberIds,
        CancellationToken cancellationToken = default)
    {
        var request = new RemoveTaskListMemberRequest
        {
            Members = memberIds.Select(id => new TaskMember
            {
                Id = id,
                Type = "user"
            }).ToArray()
        };

        var result = await _taskListApi.RemoveTaskListMemberByIdAsync(taskListGuid, request, cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("清单成员移除成功: {TaskListGuid}", taskListGuid);
            return true;
        }

        _logger.LogWarning("清单成员移除失败: {TaskListGuid}", taskListGuid);
        return false;
    }
}
