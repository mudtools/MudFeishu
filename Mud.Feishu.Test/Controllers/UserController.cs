// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Users;
using System.ComponentModel.DataAnnotations;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书用户管理控制器
/// 用于测试用户相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFeishuV3UserService _userApi;

    public UserController(IFeishuV3UserService userApi)
    {
        _userApi = userApi;
    }

    /// <summary>
    /// 创建用户（员工入职）
    /// </summary>
    /// <param name="userModel">创建的用户请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <param name="clientToken">用于幂等判断的客户端令牌</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest userModel,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null,
        [FromQuery] string? clientToken = null)
    {
        try
        {
            var result = await _userApi.CreateUserAsync(userModel, userIdType, departmentIdType, clientToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userModel">用于更新的用户请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(
        string userId,
        [FromBody] UpdateUserRequest userModel,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _userApi.UpdateUser_Tenant_Async(userId, userModel, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户ID
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="updateUserId">更新用户ID请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpPatch("{userId}/update-user-id")]
    public async Task<IActionResult> UpdateUserId(
        string userId,
        [FromBody] UpdateUserIdRequest updateUserId,
        [FromQuery] string? userIdType = null)
    {
        try
        {
            var result = await _userApi.UpdateUserIdAsync(userId, updateUserId, userIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(
        string userId,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _userApi.GetUserInfoById_Tenant_Async(userId, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量获取用户信息
    /// </summary>
    /// <param name="userIds">用户ID数组</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("batch")]
    public async Task<IActionResult> GetUsersByIds(
        [FromBody][Required] string[] userIds,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _userApi.GetUserByIds_Tenant_Async(userIds, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 根据部门ID获取用户列表
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("by-department/{departmentId}")]
    public async Task<IActionResult> GetUsersByDepartment(
        string departmentId,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _userApi.GetUserByDepartmentId_Tenant_Async(departmentId, pageSize, pageToken, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 通过手机号或邮箱获取用户ID
    /// </summary>
    /// <param name="queryRequest">查询参数请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpPost("batch-get-id")]
    public async Task<IActionResult> GetBatchUsers(
        [FromBody] UserQueryRequest queryRequest,
        [FromQuery] string? userIdType = null)
    {
        try
        {
            var result = await _userApi.GetBatchUsersAsync(queryRequest, userIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 通过关键词搜索用户
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery][Required] string query,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _userApi.GetUsersByKeywordAsync(query, pageSize, pageToken);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除用户（员工离职）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deleteSettingsRequest">用户删除参数请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(
        string userId,
        [FromBody] DeleteSettingsRequest deleteSettingsRequest,
        [FromQuery] string? userIdType = null)
    {
        try
        {
            var result = await _userApi.DeleteUserByIdAsync(userId, deleteSettingsRequest, userIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 恢复已删除用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resurrectUserRequest">恢复用户请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("{userId}/resurrect")]
    public async Task<IActionResult> ResurrectUser(
        string userId,
        [FromBody] ResurrectUserRequest resurrectUserRequest,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _userApi.ResurrectUserByIdAsync(userId, resurrectUserRequest, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var result = await _userApi.GetUserInfoAsync();
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}