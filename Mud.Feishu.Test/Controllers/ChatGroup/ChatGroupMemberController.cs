// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.ChatGroupMember;

namespace Mud.Feishu.Test.Controllers.ChatGroup;

/// <summary>
/// 飞书群成员管理控制器
/// 用于测试群成员管理相关的API接口，包括添加成员、移除成员、设置管理员等
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatGroupMemberController : ControllerBase
{
    private readonly IFeishuTenantV1ChatGroupMember _chatGroupMemberApi;

    public ChatGroupMemberController(IFeishuTenantV1ChatGroupMember chatGroupMemberApi)
    {
        _chatGroupMemberApi = chatGroupMemberApi;
    }

    /// <summary>
    /// 添加群管理员
    /// 指定群组，将群内指定的用户或者机器人设置为群管理员
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="addGroupManagerRequest">添加群管理员请求体</param>
    /// <returns>添加结果</returns>
    [HttpPost("{chatId}/managers/add")]
    public async Task<IActionResult> AddManagersAsync(
        [FromRoute] string chatId,
        [FromBody] GroupManagerRequest addGroupManagerRequest)
    {
        try
        {
            var result = await _chatGroupMemberApi.AddManagersAsync(
                chatId,
                addGroupManagerRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加群管理员成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 删除群管理员
    /// 指定群组，删除群组内指定的管理员，包括用户类型的管理员和机器人类型的管理员
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="deleteGroupManagerRequest">删除群管理员请求体</param>
    /// <returns>删除结果</returns>
    [HttpPost("{chatId}/managers/delete")]
    public async Task<IActionResult> DeleteManagersAsync(
        [FromRoute] string chatId,
        [FromBody] GroupManagerRequest deleteGroupManagerRequest)
    {
        try
        {
            var result = await _chatGroupMemberApi.DeleteManagersAsync(
                chatId,
                deleteGroupManagerRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "删除群管理员成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 添加群成员
    /// 把指定的用户或机器人拉入指定群聊内
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="addMemberRequest">添加成员请求体</param>
    /// <returns>添加结果</returns>
    [HttpPost("{chatId}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string chatId,
        [FromBody] MembersRequest addMemberRequest)
    {
        try
        {
            var result = await _chatGroupMemberApi.AddMemberAsync(
                chatId,
                addMemberRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加群成员成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 当前用户加入群聊
    /// 将当前调用接口的操作者（用户或机器人）加入指定群聊
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <returns>加入结果</returns>
    [HttpPatch("{chatId}/members/me_join")]
    public async Task<IActionResult> MeJoinChatGroupAsync([FromRoute] string chatId)
    {
        try
        {
            var result = await _chatGroupMemberApi.MeJoinChatGroupAsync(chatId);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "加入群聊成功" });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 移除群成员
    /// 将指定的用户或机器人从群聊中移出
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="membersRequest">移除成员请求体</param>
    /// <returns>移除结果</returns>
    [HttpDelete("{chatId}/members")]
    public async Task<IActionResult> RemoveMemberAsync(
        [FromRoute] string chatId,
        [FromBody] MembersRequest membersRequest)
    {
        try
        {
            var result = await _chatGroupMemberApi.RemoveMemberAsync(
                chatId,
                membersRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "移除群成员成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取群成员列表
    /// 分页获取指定群组的成员信息，包括成员名字与ID
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="pageSize">分页大小，默认为10</param>
    /// <param name="pageToken">分页标记，第一次请求不填</param>
    /// <returns>成员列表</returns>
    [HttpGet("{chatId}/members")]
    public async Task<IActionResult> GetMemberPageListAsync(
        [FromRoute] string chatId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _chatGroupMemberApi.GetMemberPageListByIdAsync(
                chatId,
               page_size: pageSize,
              page_token: pageToken);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pageToken = result.Data.PageToken,
                    hasMore = result.Data.HasMore,
                    message = "获取群成员列表成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 检查用户是否在群中
    /// 根据使用的access_token判断对应的用户或者机器人是否在指定的群里
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <returns>检查结果</returns>
    [HttpGet("{chatId}/members/is_in_chat")]
    public async Task<IActionResult> GetMemberInChatAsync([FromRoute] string chatId)
    {
        try
        {
            var result = await _chatGroupMemberApi.GetMemberInChatByIdAsync(chatId);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "检查用户在群中状态成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}