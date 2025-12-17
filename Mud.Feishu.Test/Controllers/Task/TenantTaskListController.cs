// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.TaskList;

namespace Mud.Feishu.Test.Controllers.Task;

/// <summary>
/// 飞书租户任务清单控制器
/// 用于测试租户模式下任务清单相关的API接口，包括创建、获取、更新、删除任务清单，以及管理清单成员等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TenantTaskListController : ControllerBase
{
    private readonly IFeishuTenantV2TaskList _taskListApi;

    /// <summary>
    /// 初始化TenantTaskListController实例
    /// </summary>
    /// <param name="taskListApi">飞书租户任务清单API接口</param>
    public TenantTaskListController(IFeishuTenantV2TaskList taskListApi)
    {
        _taskListApi = taskListApi;
    }

    /// <summary>
    /// 创建任务清单
    /// </summary>
    /// <param name="createTaskListRequest">创建任务清单请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTaskListAsync(
        [FromBody] CreateTaskListRequest createTaskListRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskListApi.CreateTaskListAsync(createTaskListRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "创建任务清单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "创建任务清单失败"
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
    /// 根据ID获取任务清单详情
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>任务清单详情</returns>
    [HttpGet("{tasklist_guid}")]
    public async Task<IActionResult> GetTaskListByIdAsync(
        [FromRoute] string tasklist_guid,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskListApi.GetTaskListByIdAsync(tasklist_guid, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取任务清单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "获取任务清单失败"
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
    /// 更新任务清单
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID</param>
    /// <param name="updateTaskListRequest">更新任务清单请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{tasklist_guid}")]
    public async Task<IActionResult> UpdateTaskListByIdAsync(
        [FromRoute] string tasklist_guid,
        [FromBody] UpdateTaskListRequest updateTaskListRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskListApi.UpdateTaskListByIdAsync(tasklist_guid, updateTaskListRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "更新任务清单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "更新任务清单失败"
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
    /// 删除任务清单
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{tasklist_guid}")]
    public async Task<IActionResult> DeleteTaskListByIdAsync([FromRoute] string tasklist_guid)
    {
        try
        {
            var result = await _taskListApi.DeleteTaskListByIdAsync(tasklist_guid);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "删除任务清单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "删除任务清单失败"
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
    /// 向任务清单添加成员
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID</param>
    /// <param name="addTaskListMemberRequest">添加清单成员请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>添加结果</returns>
    [HttpPost("{tasklist_guid}/add_members")]
    public async Task<IActionResult> AddTaskListMemberByIdAsync(
        [FromRoute] string tasklist_guid,
        [FromBody] AddTaskListMemberRequest addTaskListMemberRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskListApi.AddTaskListMemberByIdAsync(tasklist_guid, addTaskListMemberRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加清单成员成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "添加清单成员失败"
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
    /// 移除任务清单成员
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID</param>
    /// <param name="removeTaskListMemberRequest">移除清单成员请求体</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>移除结果</returns>
    [HttpPost("{tasklist_guid}/remove_members")]
    public async Task<IActionResult> RemoveTaskListMemberByIdAsync(
        [FromRoute] string tasklist_guid,
        [FromBody] RemoveTaskListMemberRequest removeTaskListMemberRequest,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskListApi.RemoveTaskListMemberByIdAsync(tasklist_guid, removeTaskListMemberRequest, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "移除清单成员成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "移除清单成员失败"
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
    /// 分页获取任务清单中的任务列表
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID</param>
    /// <param name="page_size">分页大小</param>
    /// <param name="page_token">分页标记</param>
    /// <param name="completed">任务完成状态</param>
    /// <param name="created_from">创建起始时间</param>
    /// <param name="created_to">创建结束时间</param>
    /// <param name="user_id_type">用户ID类型</param>
    /// <returns>任务列表</returns>
    [HttpGet("{tasklist_guid}/tasks")]
    public async Task<IActionResult> GetTaskListPageListByIdAsync(
        [FromRoute] string tasklist_guid,
        [FromQuery] int page_size = 10,
        [FromQuery] string? page_token = null,
        [FromQuery] bool? completed = null,
        [FromQuery] string? created_from = null,
        [FromQuery] string? created_to = null,
        [FromQuery] string user_id_type = "open_id")
    {
        try
        {
            var result = await _taskListApi.GetTaskListPageListByIdAsync(
                tasklist_guid, page_size, page_token, completed, created_from, created_to, user_id_type);

            if (result?.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取任务列表成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result?.Code,
                    message = result?.Msg ?? "获取任务列表失败"
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