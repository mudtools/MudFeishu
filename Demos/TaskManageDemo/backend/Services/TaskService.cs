// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Caching;
using TaskManageDemo.Backend.Services.Feishu;

namespace TaskManageDemo.Backend.Services;

/// <summary>
/// 任务服务实现
/// </summary>
public class TaskService : ITaskService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskService _feishuTaskService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        TaskManageDbContext dbContext,
        IFeishuTaskService feishuTaskService,
        ILogger<TaskService> logger)
    {
        _dbContext = dbContext;
        _feishuTaskService = feishuTaskService;
        _logger = logger;
    }

    public async Task<PagedResponse<TaskDto>> GetTasksAsync(
        TaskSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        // 应用搜索条件
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(t =>
                t.Summary.Contains(request.Keyword) ||
                (t.Description != null && t.Description.Contains(request.Keyword)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = request.Status.ToLower() switch
            {
                "completed" => query.Where(t => t.IsCompleted),
                "pending" => query.Where(t => !t.IsCompleted),
                _ => query
            };
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        if (request.AssigneeId.HasValue)
        {
            query = query.Where(t =>
                t.Members.Any(m => m.UserId == request.AssigneeId.Value && m.Role == "assignee"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                TaskGuid = t.TaskGuid,
                Summary = t.Summary,
                Description = t.Description,
                Status = t.Status,
                IsCompleted = t.IsCompleted,
                Priority = t.Priority,
                StartTime = t.StartTime,
                DueTime = t.DueTime,
                CompletedTime = t.CompletedTime,
                CreatedAt = t.CreatedAt,
                CreatorId = t.CreatorId,
                TaskListGuid = t.TaskListGuid,
                Members = t.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
                {
                    FeishuId = m.User!.FeishuId,
                    Name = m.User.Name,
                    AvatarUrl = m.User.AvatarUrl,
                    Role = m.Role
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskDto>
        {
            Items = items,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<TaskDto?> GetTaskByIdAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
        {
            return null;
        }

        return new TaskDto
        {
            Id = task.Id,
            TaskGuid = task.TaskGuid,
            Summary = task.Summary,
            Description = task.Description,
            Status = task.Status,
            IsCompleted = task.IsCompleted,
            Priority = task.Priority,
            StartTime = task.StartTime,
            DueTime = task.DueTime,
            CompletedTime = task.CompletedTime,
            CreatedAt = task.CreatedAt,
            CreatorId = task.CreatorId,
            TaskListGuid = task.TaskListGuid,
            Members = task.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
            {
                FeishuId = m.User!.FeishuId,
                Name = m.User.Name,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role
            }).ToList()
        };
    }

    public async Task<TaskDto> CreateTaskAsync(
        CreateTaskRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // 在飞书创建任务
        var taskGuid = await _feishuTaskService.CreateTaskAsync(
            request.Summary,
            request.Description,
            request.AssigneeIds,
            request.DueTime,
            cancellationToken);

        if (string.IsNullOrEmpty(taskGuid))
        {
            throw new InvalidOperationException("创建任务失败：飞书 API 返回空值");
        }

        // 同步到本地数据库
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.TaskGuid == taskGuid, cancellationToken);

        if (task == null)
        {
            throw new InvalidOperationException("任务同步到本地数据库失败");
        }

        return new TaskDto
        {
            Id = task.Id,
            TaskGuid = task.TaskGuid,
            Summary = task.Summary,
            Description = task.Description,
            Status = task.Status,
            IsCompleted = task.IsCompleted,
            Priority = task.Priority,
            StartTime = task.StartTime,
            DueTime = task.DueTime,
            CompletedTime = task.CompletedTime,
            CreatedAt = task.CreatedAt,
            CreatorId = task.CreatorId,
            TaskListGuid = task.TaskListGuid,
            Members = task.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
            {
                FeishuId = m.User!.FeishuId,
                Name = m.User.Name,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role
            }).ToList()
        };
    }

    public async Task<TaskDto?> UpdateTaskAsync(
        int taskId,
        UpdateTaskRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks.FindAsync([taskId], cancellationToken);
        if (task == null)
        {
            return null;
        }

        // 在飞书更新任务
        var success = await _feishuTaskService.UpdateTaskAsync(
            task.TaskGuid,
            request.Summary,
            request.Description,
            request.IsCompleted,
            request.DueTime,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException("更新任务失败：飞书 API 返回失败");
        }

        // 重新获取更新后的任务
        var updatedTask = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (updatedTask == null)
        {
            return null;
        }

        return new TaskDto
        {
            Id = updatedTask.Id,
            TaskGuid = updatedTask.TaskGuid,
            Summary = updatedTask.Summary,
            Description = updatedTask.Description,
            Status = updatedTask.Status,
            IsCompleted = updatedTask.IsCompleted,
            Priority = updatedTask.Priority,
            StartTime = updatedTask.StartTime,
            DueTime = updatedTask.DueTime,
            CompletedTime = updatedTask.CompletedTime,
            CreatedAt = updatedTask.CreatedAt,
            CreatorId = updatedTask.CreatorId,
            TaskListGuid = updatedTask.TaskListGuid,
            Members = updatedTask.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
            {
                FeishuId = m.User!.FeishuId,
                Name = m.User.Name,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role
            }).ToList()
        };
    }

    public async Task<bool> DeleteTaskAsync(
        int taskId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks.FindAsync([taskId], cancellationToken);
        if (task == null)
        {
            return false;
        }

        // 在飞书删除任务
        var success = await _feishuTaskService.DeleteTaskAsync(task.TaskGuid, cancellationToken);
        if (!success)
        {
            return false;
        }

        // 删除本地记录
        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
