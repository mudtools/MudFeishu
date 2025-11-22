// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Roles;

namespace Mud.Feishu.Test.Controllers.Messages;

/// <summary>
/// 飞书角色管理控制器
/// 用于测试角色相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly IFeishuV3RoleService _roleApi;

    public RoleController(IFeishuV3RoleService roleApi)
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