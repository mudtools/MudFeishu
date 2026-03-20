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

namespace TaskManageDemo.Backend.Services.Statistics;

/// <summary>
/// 统计服务接口
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// 获取任务统计概览
    /// </summary>
    Task<TaskStatisticsDto> GetTaskStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户工作量统计
    /// </summary>
    Task<List<UserWorkloadDto>> GetUserWorkloadAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务完成趋势
    /// </summary>
    Task<List<TaskTrendDto>> GetTaskTrendAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 统计服务实现
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(TaskManageDbContext dbContext, ILogger<StatisticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var totalTasks = await _dbContext.Tasks.CountAsync(cancellationToken);
        var completedTasks = await _dbContext.Tasks
            .Where(t => t.IsCompleted)
            .CountAsync(cancellationToken);
        var pendingTasks = totalTasks - completedTasks;
        var overdueTasks = await _dbContext.Tasks
            .Where(t => !t.IsCompleted && t.DueTime < now)
            .CountAsync(cancellationToken);

        var todayCreated = await _dbContext.Tasks
            .Where(t => t.CreatedAt >= todayStart)
            .CountAsync(cancellationToken);
        var todayCompleted = await _dbContext.Tasks
            .Where(t => t.IsCompleted && t.CompletedTime >= todayStart)
            .CountAsync(cancellationToken);

        var weekCreated = await _dbContext.Tasks
            .Where(t => t.CreatedAt >= weekStart)
            .CountAsync(cancellationToken);
        var weekCompleted = await _dbContext.Tasks
            .Where(t => t.IsCompleted && t.CompletedTime >= weekStart)
            .CountAsync(cancellationToken);

        var monthCreated = await _dbContext.Tasks
            .Where(t => t.CreatedAt >= monthStart)
            .CountAsync(cancellationToken);
        var monthCompleted = await _dbContext.Tasks
            .Where(t => t.IsCompleted && t.CompletedTime >= monthStart)
            .CountAsync(cancellationToken);

        var priorityGroups = await _dbContext.Tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var priorityDistribution = priorityGroups.Select(g => new PriorityDistributionDto
        {
            Priority = g.Priority,
            PriorityName = GetPriorityName(g.Priority),
            Count = g.Count,
            Percentage = totalTasks > 0 ? Math.Round((double)g.Count / totalTasks * 100, 2) : 0
        }).ToList();

        var statusDistribution = new List<StatusDistributionDto>
        {
            new() { Status = "已完成", Count = completedTasks, Percentage = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 2) : 0 },
            new() { Status = "进行中", Count = pendingTasks - overdueTasks, Percentage = totalTasks > 0 ? Math.Round((double)(pendingTasks - overdueTasks) / totalTasks * 100, 2) : 0 },
            new() { Status = "已逾期", Count = overdueTasks, Percentage = totalTasks > 0 ? Math.Round((double)overdueTasks / totalTasks * 100, 2) : 0 }
        };

        var completionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 2) : 0;

        return new TaskStatisticsDto
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            PendingTasks = pendingTasks,
            OverdueTasks = overdueTasks,
            CompletionRate = completionRate,
            PriorityDistribution = priorityDistribution,
            StatusDistribution = statusDistribution
        };
    }

    private static string GetPriorityName(int priority) => priority switch
    {
        1 => "低",
        2 => "中",
        3 => "高",
        4 => "紧急",
        _ => "无"
    };

    public async Task<List<UserWorkloadDto>> GetUserWorkloadAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaskMembers
            .Include(m => m.User)
            .Include(m => m.Task)
            .Where(m => m.Role == TaskMemberRoles.Assignee);

        if (startDate.HasValue)
        {
            query = query.Where(m => m.Task!.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.Task!.CreatedAt <= endDate.Value);
        }

        var userTasks = await query
            .GroupBy(m => m.User)
            .Select(g => new UserWorkloadDto
            {
                UserId = g.Key!.Id,
                FeishuId = g.Key.FeishuId,
                UserName = g.Key.Name,
                TotalAssigned = g.Count(),
                CompletedCount = g.Count(m => m.Task!.IsCompleted),
                PendingCount = g.Count(m => !m.Task!.IsCompleted),
                OverdueCount = g.Count(m => !m.Task!.IsCompleted && m.Task.DueTime < DateTime.UtcNow)
            })
            .ToListAsync(cancellationToken);

        foreach (var item in userTasks)
        {
            item.CompletionRate = item.TotalAssigned > 0
                ? Math.Round((double)item.CompletedCount / item.TotalAssigned * 100, 2)
                : 0;
        }

        return userTasks.OrderByDescending(u => u.TotalAssigned).Take(20).ToList();
    }

    public async Task<List<TaskTrendDto>> GetTaskTrendAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var createdTrend = await _dbContext.Tasks
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Created = g.Count() })
            .ToListAsync(cancellationToken);

        var completedTrend = await _dbContext.Tasks
            .Where(t => t.IsCompleted && t.CompletedTime >= startDate && t.CompletedTime <= endDate)
            .GroupBy(t => t.CompletedTime!.Value.Date)
            .Select(g => new { Date = g.Key, Completed = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new List<TaskTrendDto>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var created = createdTrend.FirstOrDefault(t => t.Date == date)?.Created ?? 0;
            var completed = completedTrend.FirstOrDefault(t => t.Date == date)?.Completed ?? 0;
            result.Add(new TaskTrendDto
            {
                Date = date,
                CreatedCount = created,
                CompletedCount = completed
            });
        }

        return result;
    }
}
