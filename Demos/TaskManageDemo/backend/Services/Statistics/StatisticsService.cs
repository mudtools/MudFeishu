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
            Count = g.Count
        }).ToList();

        return new TaskStatisticsDto
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            PendingTasks = pendingTasks,
            OverdueTasks = overdueTasks,
            TodayCreated = todayCreated,
            TodayCompleted = todayCompleted,
            WeekCreated = weekCreated,
            WeekCompleted = weekCompleted,
            MonthCreated = monthCreated,
            MonthCompleted = monthCompleted,
            PriorityDistribution = priorityDistribution
        };
    }

    public async Task<List<UserWorkloadDto>> GetUserWorkloadAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .Where(u => u.IsActive)
            .AsQueryable();

        var users = await query.ToListAsync(cancellationToken);
        var result = new List<UserWorkloadDto>();

        foreach (var user in users)
        {
            var taskQuery = _dbContext.TaskMembers
                .Where(m => m.UserId == user.Id && m.Role == TaskMemberRoles.Assignee)
                .Join(_dbContext.Tasks,
                    m => m.TaskSyncId,
                    t => t.Id,
                    (m, t) => t)
                .AsQueryable();

            if (startDate.HasValue)
            {
                taskQuery = taskQuery.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                taskQuery = taskQuery.Where(t => t.CreatedAt <= endDate.Value);
            }

            var totalTasks = await taskQuery.CountAsync(cancellationToken);
            var completedTasks = await taskQuery
                .Where(t => t.IsCompleted)
                .CountAsync(cancellationToken);
            var overdueTasks = await taskQuery
                .Where(t => !t.IsCompleted && t.DueTime < DateTime.UtcNow)
                .CountAsync(cancellationToken);

            result.Add(new UserWorkloadDto
            {
                UserId = user.Id.ToString(),
                UserName = user.Name,
                AvatarUrl = user.AvatarUrl,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks,
                CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0
            });
        }

        return result.OrderByDescending(r => r.TotalTasks).ToList();
    }

    public async Task<List<TaskTrendDto>> GetTaskTrendAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var result = new List<TaskTrendDto>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var nextDate = currentDate.AddDays(1);

            var created = await _dbContext.Tasks
                .Where(t => t.CreatedAt >= currentDate && t.CreatedAt < nextDate)
                .CountAsync(cancellationToken);

            var completed = await _dbContext.Tasks
                .Where(t => t.IsCompleted && t.CompletedTime >= currentDate && t.CompletedTime < nextDate)
                .CountAsync(cancellationToken);

            result.Add(new TaskTrendDto
            {
                Date = currentDate,
                CreatedCount = created,
                CompletedCount = completed
            });

            currentDate = nextDate;
        }

        return result;
    }
}
