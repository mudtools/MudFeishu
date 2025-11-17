using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Mud.Feishu.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFeishuUser _userApi;

    public UserController(IFeishuUser userApi)
    {
        _userApi = userApi;
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId, [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _userApi.GetUserByIdAsync(userId, userIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentId">部门ID</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery][Required] string departmentId,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? pageToken = null,
        [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _userApi.GetUserByDepartmentIdAsync(departmentId, pageSize, pageToken, userIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}