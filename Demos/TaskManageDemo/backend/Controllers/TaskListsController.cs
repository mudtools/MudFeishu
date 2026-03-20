// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Feishu;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务清单控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TaskListsController : ControllerBase
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskListService _taskListService;
    private readonly ILogger<TaskListsController> _logger;

    /// <summary>
    /// 初始化任务清单控制器
    /// </summary>
    public TaskListsController(
        TaskManageDbContext dbContext,
        IFeishuTaskListService taskListService,
        ILogger<TaskListsController> logger)
    {
        _dbContext = dbContext;
        _taskListService = taskListService;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务清单列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<TaskListDto>>>> GetTaskLists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaskLists
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskListDto
            {
                Id = t.Id,
                TaskListGuid = t.TaskListGuid,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Members = t.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
                {
                    FeishuId = m.User!.FeishuId,
                    Name = m.User.Name,
                    AvatarUrl = m.User.AvatarUrl,
                    Role = "member"
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<TaskListDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ApiResponse<PagedResponse<TaskListDto>>.Ok(response);
    }

    /// <summary>
    /// 获取任务清单详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TaskListDto>>> GetTaskList(int id, CancellationToken cancellationToken)
    {
        var taskList = await _dbContext.TaskLists
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (taskList == null)
        {
            return ApiResponse<TaskListDto>.Fail("任务清单不存在");
        }

        var dto = new TaskListDto
        {
            Id = taskList.Id,
            TaskListGuid = taskList.TaskListGuid,
            Name = taskList.Name,
            Description = taskList.Description,
            CreatedAt = taskList.CreatedAt,
            Members = taskList.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
            {
                FeishuId = m.User!.FeishuId,
                Name = m.User.Name,
                AvatarUrl = m.User.AvatarUrl,
                Role = "member"
            }).ToList()
        };

        return ApiResponse<TaskListDto>.Ok(dto);
    }

    /// <summary>
    /// 创建任务清单
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskListDto>>> CreateTaskList(
        [FromBody] CreateTaskListRequest request,
        CancellationToken cancellationToken)
    {
        var taskListGuid = await _taskListService.CreateTaskListAsync(
            request.Name,
            request.Description,
            cancellationToken);

        if (string.IsNullOrEmpty(taskListGuid))
        {
            return ApiResponse<TaskListDto>.Fail("创建任务清单失败");
        }

        var taskList = new Models.Entities.TaskList
        {
            TaskListGuid = taskListGuid,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };

        _dbContext.TaskLists.Add(taskList);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new TaskListDto
        {
            Id = taskList.Id,
            TaskListGuid = taskList.TaskListGuid,
            Name = taskList.Name,
            Description = taskList.Description,
            CreatedAt = taskList.CreatedAt
        };

        return ApiResponse<TaskListDto>.Ok(dto, "任务清单创建成功");
    }

    /// <summary>
    /// 更新任务清单
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TaskListDto>>> UpdateTaskList(
        int id,
        [FromBody] UpdateTaskListRequest request,
        CancellationToken cancellationToken)
    {
        var taskList = await _dbContext.TaskLists.FindAsync([id], cancellationToken);
        if (taskList == null)
        {
            return ApiResponse<TaskListDto>.Fail("任务清单不存在");
        }

        var success = await _taskListService.UpdateTaskListAsync(
            taskList.TaskListGuid,
            request.Name,
            request.Description,
            cancellationToken);

        if (!success)
        {
            return ApiResponse<TaskListDto>.Fail("更新任务清单失败");
        }

        taskList.Name = request.Name ?? taskList.Name;
        taskList.Description = request.Description ?? taskList.Description;
        taskList.LastSyncedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new TaskListDto
        {
            Id = taskList.Id,
            TaskListGuid = taskList.TaskListGuid,
            Name = taskList.Name,
            Description = taskList.Description,
            CreatedAt = taskList.CreatedAt
        };

        return ApiResponse<TaskListDto>.Ok(dto, "任务清单更新成功");
    }

    /// <summary>
    /// 删除任务清单
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTaskList(int id, CancellationToken cancellationToken)
    {
        var taskList = await _dbContext.TaskLists.FindAsync([id], cancellationToken);
        if (taskList == null)
        {
            return ApiResponse<bool>.Fail("任务清单不存在");
        }

        var success = await _taskListService.DeleteTaskListAsync(taskList.TaskListGuid, cancellationToken);
        if (!success)
        {
            return ApiResponse<bool>.Fail("删除任务清单失败");
        }

        _dbContext.TaskLists.Remove(taskList);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "任务清单删除成功");
    }

    /// <summary>
    /// 添加清单成员
    /// </summary>
    [HttpPost("{id}/members")]
    public async Task<ActionResult<ApiResponse<bool>>> AddMembers(
        int id,
        [FromBody] AddTaskListMembersRequest request,
        CancellationToken cancellationToken)
    {
        var taskList = await _dbContext.TaskLists.FindAsync([id], cancellationToken);
        if (taskList == null)
        {
            return ApiResponse<bool>.Fail("任务清单不存在");
        }

        var success = await _taskListService.AddMembersAsync(
            taskList.TaskListGuid,
            request.MemberIds,
            cancellationToken);

        if (!success)
        {
            return ApiResponse<bool>.Fail("添加成员失败");
        }

        return ApiResponse<bool>.Ok(true, "成员添加成功");
    }

    /// <summary>
    /// 移除清单成员
    /// </summary>
    [HttpDelete("{id}/members")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveMembers(
        int id,
        [FromBody] RemoveTaskListMembersRequest request,
        CancellationToken cancellationToken)
    {
        var taskList = await _dbContext.TaskLists.FindAsync([id], cancellationToken);
        if (taskList == null)
        {
            return ApiResponse<bool>.Fail("任务清单不存在");
        }

        var success = await _taskListService.RemoveMembersAsync(
            taskList.TaskListGuid,
            request.MemberIds,
            cancellationToken);

        if (!success)
        {
            return ApiResponse<bool>.Fail("移除成员失败");
        }

        return ApiResponse<bool>.Ok(true, "成员移除成功");
    }

    /// <summary>
    /// 获取清单内的任务
    /// </summary>
    [HttpGet("{id}/tasks")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TaskDto>>>> GetTaskListTasks(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var taskList = await _dbContext.TaskLists.FindAsync([id], cancellationToken);
        if (taskList == null)
        {
            return ApiResponse<PagedResponse<TaskDto>>.Fail("任务清单不存在");
        }

        var query = _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .Where(t => t.TaskListGuid == taskList.TaskListGuid);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        var response = new PagedResponse<TaskDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ApiResponse<PagedResponse<TaskDto>>.Ok(response);
    }
}
