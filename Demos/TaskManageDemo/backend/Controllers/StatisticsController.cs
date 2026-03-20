// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Statistics;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 统计控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : BaseController
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsService statisticsService,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务统计概览
    /// </summary>
    [HttpGet("tasks")]
    [RequirePermission("statistics:read")]
    public async Task<ActionResult<ApiResponse<TaskStatisticsDto>>> GetTaskStatistics(
        CancellationToken cancellationToken)
    {
        var statistics = await _statisticsService.GetTaskStatisticsAsync(cancellationToken);
        return Success(statistics);
    }

    /// <summary>
    /// 获取用户工作量统计
    /// </summary>
    [HttpGet("user-workload")]
    [RequirePermission("statistics:read")]
    public async Task<ActionResult<ApiResponse<List<UserWorkloadDto>>>> GetUserWorkload(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var workload = await _statisticsService.GetUserWorkloadAsync(
            startDate,
            endDate,
            cancellationToken);
        return Success(workload);
    }

    /// <summary>
    /// 获取任务完成趋势
    /// </summary>
    [HttpGet("task-trend")]
    [RequirePermission("statistics:read")]
    public async Task<ActionResult<ApiResponse<List<TaskTrendDto>>>> GetTaskTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var trend = await _statisticsService.GetTaskTrendAsync(start, end, cancellationToken);
        return Success(trend);
    }
}
