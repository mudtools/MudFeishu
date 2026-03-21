// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services;
using TaskManageDemo.Backend.Services.Search;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : BaseController
{
    private readonly ITaskService _taskService;
    private readonly ITaskSearchService _taskSearchService;
    private readonly ILogger<TasksController> _logger;

    /// <summary>
    /// 初始化任务控制器
    /// </summary>
    public TasksController(
        ITaskService taskService,
        ITaskSearchService taskSearchService,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _taskSearchService = taskSearchService;
        _logger = logger;
    }

    /// <summary>
    /// 搜索任务
    /// </summary>
    [HttpGet("search")]
    [RequirePermission("task:read")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TaskDto>>>> SearchTasks(
        [FromQuery] TaskSearchParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _taskSearchService.SearchAsync(parameters, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取任务列表
    /// </summary>
    [HttpGet]
    [RequirePermission("task:read")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TaskDto>>>> GetTasks(
        [FromQuery] TaskQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTasksAsync(parameters, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("task:read")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(int id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetTaskByIdAsync(id, cancellationToken);

        if (task == null)
        {
            return NotFoundResult<TaskDto>("任务不存在");
        }

        return Success(task);
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    [HttpPost]
    [RequirePermission("task:create")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.CreateTaskAsync(request, userId, cancellationToken);
            return Created(result, "任务创建成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "创建任务失败");
            return Fail<TaskDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建任务时发生未预期错误");
            return Fail<TaskDto>("创建任务失败，请稍后重试");
        }
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("task:update")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.UpdateTaskAsync(id, request, userId, cancellationToken);

            if (result == null)
            {
                return NotFoundResult<TaskDto>("任务不存在");
            }

            return Updated(result, "任务更新成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "更新任务失败，TaskId: {TaskId}", id);
            return Fail<TaskDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务时发生未预期错误，TaskId: {TaskId}", id);
            return Fail<TaskDto>("更新任务失败，请稍后重试");
        }
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("task:delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _taskService.DeleteTaskAsync(id, userId, cancellationToken);

            if (!success)
            {
                return NotFoundResult<bool>("任务不存在");
            }

            return Deleted("任务删除成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "删除任务失败，TaskId: {TaskId}", id);
            return Fail<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除任务时发生未预期错误，TaskId: {TaskId}", id);
            return Fail<bool>("删除任务失败，请稍后重试");
        }
    }

    /// <summary>
    /// 分配任务
    /// </summary>
    [HttpPost("{id}/assign")]
    [RequirePermission("task:update")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignTask(
        int id,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _taskService.AssignTaskAsync(id, request, cancellationToken);

            if (!success)
            {
                return NotFoundResult<bool>("任务不存在");
            }

            return Success(true, "任务分配成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "分配任务失败，TaskId: {TaskId}", id);
            return Fail<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配任务时发生未预期错误，TaskId: {TaskId}", id);
            return Fail<bool>("分配任务失败，请稍后重试");
        }
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    [HttpPut("{id}/status")]
    [RequirePermission("task:update")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateTaskStatus(
        int id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _taskService.UpdateTaskStatusAsync(id, request.IsCompleted, cancellationToken);

            if (!success)
            {
                return NotFoundResult<bool>("任务不存在");
            }

            return Success(true, "状态更新成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "更新任务状态失败，TaskId: {TaskId}", id);
            return Fail<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务状态时发生未预期错误，TaskId: {TaskId}", id);
            return Fail<bool>("更新状态失败，请稍后重试");
        }
    }
}
