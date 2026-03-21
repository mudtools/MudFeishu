// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.Search;

/// <summary>
/// 任务搜索服务实现
/// </summary>
public class TaskSearchService : ITaskSearchService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<TaskSearchService> _logger;

    /// <summary>
    /// 初始化任务搜索服务
    /// </summary>
    public TaskSearchService(TaskManageDbContext dbContext, ILogger<TaskSearchService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 搜索任务
    /// </summary>
    public async Task<PagedResponse<TaskDto>> SearchAsync(
        TaskSearchParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Keyword))
        {
            var keyword = parameters.Keyword.ToLower();
            query = query.Where(t =>
                t.Summary.ToLower().Contains(keyword) ||
                (t.Description != null && t.Description.ToLower().Contains(keyword)));
        }

        if (!parameters.IncludeCompleted)
        {
            query = query.Where(t => !t.IsCompleted);
        }

        if (!string.IsNullOrEmpty(parameters.AssigneeId))
        {
            query = query.Where(t => t.Members.Any(m =>
                m.User != null && m.User.FeishuId == parameters.AssigneeId && m.Role == TaskMemberRoles.Assignee));
        }

        if (parameters.Priorities != null && parameters.Priorities.Count > 0)
        {
            query = query.Where(t => parameters.Priorities.Contains(t.Priority));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueTime)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
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
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    public async Task<List<string>> GetSearchSuggestionsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new List<string>();
        }

        var keyword = query.ToLower();

        var suggestions = await _dbContext.Tasks
            .Where(t => t.Summary.ToLower().Contains(keyword))
            .Select(t => t.Summary)
            .Distinct()
            .Take(10)
            .ToListAsync(cancellationToken);

        return suggestions;
    }

    /// <summary>
    /// 高级筛选
    /// </summary>
    public async Task<PagedResponse<TaskDto>> AdvancedFilterAsync(
        AdvancedFilterParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Keyword))
        {
            var keyword = parameters.Keyword.ToLower();
            query = query.Where(t =>
                t.Summary.ToLower().Contains(keyword) ||
                (t.Description != null && t.Description.ToLower().Contains(keyword)));
        }

        if (parameters.Statuses != null && parameters.Statuses.Count > 0)
        {
            query = query.Where(t => parameters.Statuses.Contains(t.Status));
        }

        if (parameters.Priorities != null && parameters.Priorities.Count > 0)
        {
            query = query.Where(t => parameters.Priorities.Contains(t.Priority));
        }

        if (parameters.AssigneeIds != null && parameters.AssigneeIds.Count > 0)
        {
            query = query.Where(t => t.Members.Any(m =>
                m.User != null && parameters.AssigneeIds.Contains(m.User.FeishuId) && m.Role == TaskMemberRoles.Assignee));
        }

        if (parameters.TaskListGuids != null && parameters.TaskListGuids.Count > 0)
        {
            query = query.Where(t => t.TaskListGuid != null && parameters.TaskListGuids.Contains(t.TaskListGuid));
        }

        if (parameters.IsCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == parameters.IsCompleted.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.DueFrom.HasValue)
        {
            query = query.Where(t => t.DueTime.HasValue && t.DueTime.Value >= parameters.DueFrom.Value);
        }

        if (parameters.DueTo.HasValue)
        {
            query = query.Where(t => t.DueTime.HasValue && t.DueTime.Value <= parameters.DueTo.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, parameters.SortBy, parameters.IsDescending);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
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
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    private static IQueryable<Models.Entities.TaskSync> ApplySorting(
        IQueryable<Models.Entities.TaskSync> query,
        string? sortBy,
        bool isDescending)
    {
        return sortBy?.ToLower() switch
        {
            "summary" => isDescending
                ? query.OrderByDescending(t => t.Summary)
                : query.OrderBy(t => t.Summary),
            "priority" => isDescending
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "duetime" => isDescending
                ? query.OrderByDescending(t => t.DueTime)
                : query.OrderBy(t => t.DueTime),
            "createdat" => isDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            "completedtime" => isDescending
                ? query.OrderByDescending(t => t.CompletedTime)
                : query.OrderBy(t => t.CompletedTime),
            _ => isDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt)
        };
    }
}
