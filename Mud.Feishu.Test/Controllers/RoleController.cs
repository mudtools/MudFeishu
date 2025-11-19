using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Roles;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书角色管理控制器
/// 用于测试角色相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly IFeishuV3Role _roleApi;

    public RoleController(IFeishuV3Role roleApi)
    {
        _roleApi = roleApi;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="roleRequest">角色创建请求体</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleRequest roleRequest)
    {
        try
        {
            var result = await _roleApi.CreateRoleAsync(roleRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新角色名称
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="roleRequest">角色更新请求体</param>
    /// <returns></returns>
    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] RoleRequest roleRequest)
    {
        try
        {
            var result = await _roleApi.UpdateRoleAsync(roleId, roleRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        try
        {
            var result = await _roleApi.DeleteRoleByIdAsync(roleId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}