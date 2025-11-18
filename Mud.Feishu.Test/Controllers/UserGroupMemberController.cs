using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.UserGroupMember;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书用户组成员管理控制器
/// 用于测试用户组成员相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserGroupMemberController : ControllerBase
{
    private readonly IFeishuUserGroupMember _userGroupMemberApi;

    public UserGroupMemberController(IFeishuUserGroupMember userGroupMemberApi)
    {
        _userGroupMemberApi = userGroupMemberApi;
    }

    /// <summary>
    /// 添加用户组成员
    /// </summary>
    /// <param name="groupId">用户组ID</param>
    /// <param name="memberRequest">添加成员请求体</param>
    /// <returns></returns>
    [HttpPost("{groupId}/add-member")]
    public async Task<IActionResult> AddMember(string groupId, [FromBody] UserGroupMemberRequest memberRequest)
    {
        try
        {
            var result = await _userGroupMemberApi.AddMemberAsync(groupId, memberRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量添加用户组成员
    /// </summary>
    /// <param name="groupId">用户组ID</param>
    /// <param name="batchRequest">批量添加成员请求体</param>
    /// <returns></returns>
    [HttpPost("{groupId}/batch-add-member")]
    public async Task<IActionResult> BatchAddMember(string groupId, [FromBody] BatchMembersRequest batchRequest)
    {
        try
        {
            var result = await _userGroupMemberApi.BatchAddMemberAsync(groupId, batchRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取用户组成员列表
    /// </summary>
    /// <param name="groupId">用户组ID</param>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="memberIdType">成员ID类型</param>
    /// <param name="memberType">成员类型</param>
    /// <returns></returns>
    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetMemberList(
        string groupId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? memberIdType = "open_id",
        [FromQuery] string? memberType = "user")
    {
        try
        {
            var result = await _userGroupMemberApi.GetMemberListByGroupIdAsync(
                groupId, pageSize, pageToken, memberIdType, memberType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 移除用户组成员
    /// </summary>
    /// <param name="groupId">用户组ID</param>
    /// <param name="memberRequest">移除成员请求体</param>
    /// <returns></returns>
    [HttpPost("{groupId}/remove-member")]
    public async Task<IActionResult> RemoveMember(string groupId, [FromBody] UserGroupMemberRequest memberRequest)
    {
        try
        {
            var result = await _userGroupMemberApi.RemoveMemberAsync(groupId, memberRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量移除用户组成员
    /// </summary>
    /// <param name="groupId">用户组ID</param>
    /// <param name="batchRequest">批量移除成员请求体</param>
    /// <returns></returns>
    [HttpPost("{groupId}/batch-remove-member")]
    public async Task<IActionResult> BatchRemoveMember(string groupId, [FromBody] BatchMembersRequest batchRequest)
    {
        try
        {
            var result = await _userGroupMemberApi.BatchRemoveMemberAsync(groupId, batchRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}