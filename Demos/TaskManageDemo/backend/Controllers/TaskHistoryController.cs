// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.History;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务历史控制器
/// </summary>
[ApiController]
[Route("api/tasks/{taskId}/[controller]")]
public class TaskHistoryController : BaseController
{
    private readonly ITaskHistoryService _historyService;
    private readonly ILogger<TaskHistoryController> _logger;

    /// <summary>
    /// 初始化任务历史控制器
    /// </summary>
    public TaskHistoryController(
        ITaskHistoryService historyService,
        ILogger<TaskHistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务历史记录
    /// </summary>
    [HttpGet]
    [RequirePermission("task:read")]
    public async Task<ActionResult<ApiResponse<List<TaskHistoryDto>>>> GetTaskHistory(
        int taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await _historyService.GetTaskHistoryAsync(taskId, cancellationToken);
            return Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务历史失败，TaskId: {TaskId}", taskId);
            return Fail<List<TaskHistoryDto>>($"获取任务历史失败: {ex.Message}");
        }
    }
}
