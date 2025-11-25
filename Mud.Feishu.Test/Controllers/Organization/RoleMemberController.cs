// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.RoleMembers;

namespace Mud.Feishu.Test.Controllers.Messages;

/// <summary>
/// 飞书角色成员管理控制器
/// 用于测试角色成员相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoleMemberController : ControllerBase
{
    private readonly IFeishuTenantV3RoleMember _roleMemberApi;

    public RoleMemberController(IFeishuTenantV3RoleMember roleMemberApi)
    {
        _roleMemberApi = roleMemberApi;
    }

    /// <summary>
    /// 批量添加角色成员
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="membersRequest">角色成员用户ID列表请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpPost("{roleId}/batch-add-member")]
    public async Task<IActionResult> BatchAddMember(
        string roleId,
        [FromBody] RoleMembersRequest membersRequest,
        [FromQuery] string? userIdType = "open_id")
    {
        try
        {
            var result = await _roleMemberApi.BatchAddMemberAsync(roleId, membersRequest, userIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量设置角色成员管理范围
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="scopeRequest">角色成员管理范围请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("{roleId}/set-member-scopes")]
    public async Task<IActionResult> BatchAddMembersSopes(
        string roleId,
        [FromBody] RoleMembersScopeRequest scopeRequest,
        [FromQuery] string? userIdType = "open_id",
        [FromQuery] string? departmentIdType = "open_department_id")
    {
        try
        {
            var result = await _roleMemberApi.BatchAddMembersSopesAsync(roleId, scopeRequest, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取角色成员的管理范围
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="memberId">角色成员的用户ID</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{roleId}/members/{memberId}/scopes")]
    public async Task<IActionResult> GetMemberSopes(
        string roleId,
        string memberId,
        [FromQuery] string? userIdType = "open_id",
        [FromQuery] string? departmentIdType = "open_department_id")
    {
        try
        {
            var result = await _roleMemberApi.GetMembersSopesAsync(roleId, memberId, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取角色成员列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{roleId}/members")]
    public async Task<IActionResult> GetMembers(
        string roleId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? userIdType = "open_id",
        [FromQuery] string? departmentIdType = "open_department_id")
    {
        try
        {
            var result = await _roleMemberApi.GetMembersAsync(roleId, pageSize, pageToken, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量删除角色成员
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="membersRequest">需删除的角色成员用户ID列表请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpDelete("{roleId}/batch-delete-member")]
    public async Task<IActionResult> DeleteMembers(
        string roleId,
        [FromBody] RoleMembersRequest membersRequest,
        [FromQuery] string? userIdType = "open_id")
    {
        try
        {
            var result = await _roleMemberApi.DeleteMembersByRoleIdAsync(roleId, membersRequest, userIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}