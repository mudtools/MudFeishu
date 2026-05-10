// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.QueryBuilder;

/// <summary>
/// 任务查询构建器
/// 统一任务查询条件的构建逻辑
/// </summary>
public class TaskQueryBuilder
{
    private IQueryable<TaskSync> _query;

    public TaskQueryBuilder(IQueryable<TaskSync> baseQuery)
    {
        _query = baseQuery;
    }

    /// <summary>
    /// 按关键词过滤
    /// </summary>
    public TaskQueryBuilder WithKeyword(string? keyword)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            _query = _query.Where(t =>
                t.Summary.Contains(keyword) ||
                (t.Description != null && t.Description.Contains(keyword)));
        }
        return this;
    }

    /// <summary>
    /// 按状态过滤
    /// </summary>
    public TaskQueryBuilder WithStatus(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            _query = status.ToLower() switch
            {
                "completed" => _query.Where(t => t.IsCompleted),
                "pending" => _query.Where(t => !t.IsCompleted),
                _ => _query
            };
        }
        return this;
    }

    /// <summary>
    /// 按优先级过滤
    /// </summary>
    public TaskQueryBuilder WithPriority(int? priority)
    {
        if (priority.HasValue)
        {
            _query = _query.Where(t => t.Priority == priority.Value);
        }
        return this;
    }

    /// <summary>
    /// 按负责人过滤
    /// </summary>
    public TaskQueryBuilder WithAssignee(int? assigneeId)
    {
        if (assigneeId.HasValue)
        {
            _query = _query.Where(t =>
                t.Members.Any(m => m.UserId == assigneeId.Value && m.Role == TaskMemberRoles.Assignee));
        }
        return this;
    }

    /// <summary>
    /// 按截止日期范围过滤
    /// </summary>
    public TaskQueryBuilder WithDueDateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue)
        {
            _query = _query.Where(t => t.DueTime >= from.Value);
        }
        if (to.HasValue)
        {
            _query = _query.Where(t => t.DueTime <= to.Value);
        }
        return this;
    }

    /// <summary>
    /// 按是否逾期过滤
    /// </summary>
    public TaskQueryBuilder WithOverdue(bool? overdue)
    {
        if (overdue == true)
        {
            var now = DateTime.UtcNow;
            _query = _query.Where(t =>
                !t.IsCompleted &&
                t.DueTime != null &&
                t.DueTime < now);
        }
        return this;
    }

    /// <summary>
    /// 包含已完成的任务
    /// </summary>
    public TaskQueryBuilder IncludeCompleted(bool include)
    {
        if (!include)
        {
            _query = _query.Where(t => !t.IsCompleted);
        }
        return this;
    }

    /// <summary>
    /// 包含成员信息
    /// </summary>
    public TaskQueryBuilder WithMembers()
    {
        _query = _query.Include(t => t.Members).ThenInclude(m => m.User);
        return this;
    }

    /// <summary>
    /// 按排序条件排序
    /// </summary>
    public TaskQueryBuilder WithSorting(string? sortBy, bool sortDescending = false)
    {
        _query = sortBy?.ToLower() switch
        {
            "created" => sortDescending
                ? _query.OrderByDescending(t => t.CreatedAt)
                : _query.OrderBy(t => t.CreatedAt),
            "due" => sortDescending
                ? _query.OrderByDescending(t => t.DueTime)
                : _query.OrderBy(t => t.DueTime),
            "priority" => sortDescending
                ? _query.OrderByDescending(t => t.Priority)
                : _query.OrderBy(t => t.Priority),
            "updated" => sortDescending
                ? _query.OrderByDescending(t => t.UpdatedAt)
                : _query.OrderBy(t => t.UpdatedAt),
            _ => sortDescending
                ? _query.OrderByDescending(t => t.CreatedAt)
                : _query.OrderBy(t => t.CreatedAt)
        };
        return this;
    }

    /// <summary>
    /// 应用分页
    /// </summary>
    public async Task<(List<TaskSync> Items, int Total)> WithPagingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var total = await _query.CountAsync(cancellationToken);
        var items = await _query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <summary>
    /// 构建最终查询
    /// </summary>
    public IQueryable<TaskSync> Build()
    {
        return _query;
    }

    /// <summary>
    /// 获取总数
    /// </summary>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _query.CountAsync(cancellationToken);
    }
}

/// <summary>
/// 查询构建器扩展方法
/// </summary>
public static class TaskQueryBuilderExtensions
{
    /// <summary>
    /// 创建任务查询构建器
    /// </summary>
    public static TaskQueryBuilder AsQueryBuilder(this DbSet<TaskSync> tasks)
    {
        return new TaskQueryBuilder(tasks.AsQueryable());
    }
}
