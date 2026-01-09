// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.Test.Controllers.Tasks;

/// <summary>
/// 飞书租户任务控制器
/// 用于测试租户模式下任务相关的API接口，包括创建、获取、更新、删除任务，以及管理任务成员、依赖、提醒等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TenantTaskController : ControllerBase
{
    private readonly IFeishuTenantV2Task _taskApi;

    /// <summary>
    /// 初始化TenantTaskController实例
    /// </summary>
    /// <param name="taskApi">飞书租户任务API接口</param>
    public TenantTaskController(IFeishuTenantV2Task taskApi)
    {
        _taskApi = taskApi;
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    /// <param name="createTaskRequest">创建任务请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTaskAsync(
        [FromBody] CreateTaskRequest createTaskRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.CreateTaskAsync(createTaskRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "创建任务成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "创建任务失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    /// <param name="task_guid">要更新的任务全局唯一ID</param>
    /// <param name="updateTaskRequest">更新任务请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{task_guid}")]
    public async Task<IActionResult> UpdateTaskAsync(
        [FromRoute] string task_guid,
        [FromBody] UpdateTaskRequest updateTaskRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.UpdateTaskAsync(task_guid, updateTaskRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "更新任务成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "更新任务失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>任务详情</returns>
    [HttpGet("{task_guid}")]
    public async Task<IActionResult> GetTaskByIdAsync(
        [FromRoute] string task_guid,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.GetTaskByIdAsync(task_guid, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取任务详情成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "获取任务详情失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    /// <param name="task_guid">要删除的任务全局唯一ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{task_guid}")]
    public async Task<IActionResult> DeleteTaskByIdAsync([FromRoute] string task_guid)
    {
        try
        {
            var result = await _taskApi.DeleteTaskByIdAsync(task_guid);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "删除任务成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "删除任务失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 添加任务成员
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="addMembersRequest">添加任务成员请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>添加结果</returns>
    [HttpPost("{task_guid}/add_members")]
    public async Task<IActionResult> AddMembersByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] AddMembersRequest addMembersRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.AddMembersByIdAsync(task_guid, addMembersRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加任务成员成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "添加任务成员失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 移除任务成员
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="removeMembersRequest">移除任务成员请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>移除结果</returns>
    [HttpPost("{task_guid}/remove_members")]
    public async Task<IActionResult> RemoveMembersByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] RemoveMembersRequest removeMembersRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.RemoveMembersByIdAsync(task_guid, removeMembersRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "移除任务成员成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "移除任务成员失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取任务所在的所有清单信息
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <returns>清单信息</returns>
    [HttpGet("{task_guid}/tasklists")]
    public async Task<IActionResult> GetTaskListsByIdAsync([FromRoute] string task_guid)
    {
        try
        {
            var result = await _taskApi.GetTaskListsByIdAsync(task_guid);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取任务清单信息成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "获取任务清单信息失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 将任务加入清单
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="addTasklistRequest">任务加入清单请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>添加结果</returns>
    [HttpPost("{task_guid}/add_tasklist")]
    public async Task<IActionResult> AddTaskListsByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] AddTasklistRequest addTasklistRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.AddTaskListsByIdAsync(task_guid, addTasklistRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "将任务加入清单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "将任务加入清单失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 将任务从清单中移出
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="removeTasklistRequest">移除任务清单请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>移除结果</returns>
    [HttpPost("{task_guid}/remove_tasklist")]
    public async Task<IActionResult> RemoveTaskListsByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] RemoveTasklistRequest removeTasklistRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.RemoveTaskListsByIdAsync(task_guid, removeTasklistRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "将任务从清单中移出成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "将任务从清单中移出失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 为任务添加提醒
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="addTaskReminderRequest">添加任务提醒请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>添加结果</returns>
    [HttpPost("{task_guid}/add_reminders")]
    public async Task<IActionResult> AddTaskReminderByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] AddTaskReminderRequest addTaskReminderRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.AddTaskReminderByIdAsync(task_guid, addTaskReminderRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加任务提醒成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "添加任务提醒失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 移除任务提醒
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="removeReminderRequest">移除任务提醒请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>移除结果</returns>
    [HttpPost("{task_guid}/remove_reminders")]
    public async Task<IActionResult> RemoveTaskReminderByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] RemoveReminderRequest removeReminderRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.RemoveTaskReminderByIdAsync(task_guid, removeReminderRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "移除任务提醒成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "移除任务提醒失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 为任务添加依赖
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="addTaskDependenciesRequest">添加任务依赖请求体</param>
    /// <returns>添加结果</returns>
    [HttpPost("{task_guid}/add_dependencies")]
    public async Task<IActionResult> AddTaskDependenciesByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] AddTaskDependenciesRequest addTaskDependenciesRequest)
    {
        try
        {
            var result = await _taskApi.AddTaskDependenciesByIdAsync(task_guid, addTaskDependenciesRequest);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加任务依赖成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "添加任务依赖失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 移除任务依赖
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="removeTaskDependenciesRequest">移除任务依赖请求体</param>
    /// <returns>移除结果</returns>
    [HttpPost("{task_guid}/remove_dependencies")]
    public async Task<IActionResult> RemoveTaskDependenciesByIdAsync(
        [FromRoute] string task_guid,
        [FromBody] RemoveTaskDependenciesRequest removeTaskDependenciesRequest)
    {
        try
        {
            var result = await _taskApi.RemoveTaskDependenciesByIdAsync(task_guid, removeTaskDependenciesRequest);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "移除任务依赖成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "移除任务依赖失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 创建子任务
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID（父任务）</param>
    /// <param name="createSubTaskRequest">创建子任务请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>创建结果</returns>
    [HttpPost("{task_guid}/subtasks")]
    public async Task<IActionResult> CreateSubTaskAsync(
        [FromRoute] string task_guid,
        [FromBody] CreateSubTaskRequest createSubTaskRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.CreateSubTaskAsync(task_guid, createSubTaskRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "创建子任务成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "创建子任务失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取任务的子任务列表
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID</param>
    /// <param name="page_size">分页大小</param>
    /// <param name="page_token">分页标记</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>子任务列表</returns>
    [HttpGet("subtasks/{task_guid}")]
    public async Task<IActionResult> GetSubTasksPageListByIdAsync(
        [FromRoute] string task_guid,
        [FromQuery] int page_size = Consts.PageSize,
        [FromQuery] string? page_token = null,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskApi.GetSubTasksPageListByIdAsync(task_guid, page_size, page_token, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取子任务列表成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "获取子任务列表失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }
}