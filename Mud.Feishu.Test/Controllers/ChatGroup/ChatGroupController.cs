// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.ChatGroup;

namespace Mud.Feishu.Test.Controllers.ChatGroup;

/// <summary>
/// 飞书群组管理控制器
/// 用于测试群组管理相关的API接口，包括创建群、解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatGroupController : ControllerBase
{
    private readonly IFeishuTenantV1ChatGroup _chatGroupApi;

    public ChatGroupController(IFeishuTenantV1ChatGroup chatGroupApi)
    {
        _chatGroupApi = chatGroupApi;
    }

    /// <summary>
    /// 创建群聊
    /// 创建时支持设置群头像、群名称、群主以及群类型等配置，同时支持邀请群成员、群机器人入群
    /// </summary>
    /// <param name="createChatRequest">创建群聊请求体</param>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <param name="setBotManager">是否设置创建群的机器人为管理员</param>
    /// <param name="uuid">创建群组请求去重的唯一字符串序列</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateChatGroupAsync(
        [FromBody] CreateChatRequest createChatRequest,
        [FromQuery] string userIdType = "open_id",
        [FromQuery] bool setBotManager = false,
        [FromQuery] string? uuid = null)
    {
        try
        {
            var result = await _chatGroupApi.CreateChatGroupAsync(
                createChatRequest,
                userIdType,
                setBotManager,
                uuid);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    chatId = result.Data?.ChatId,
                    result = result.Data,
                    message = "群聊创建成功"
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
    /// 更新群信息
    /// 更新指定群的信息，包括群头像、群名称、群描述、群配置以及群主等
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="updateChatRequest">更新群聊请求体</param>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <returns>更新结果</returns>
    [HttpPut("{chatId}")]
    public async Task<IActionResult> UpdateChatGroupAsync(
        [FromRoute] string chatId,
        [FromBody] UpdateChatRequest updateChatRequest,
        [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _chatGroupApi.UpdateChatGroupByIdAsync(
                chatId,
                updateChatRequest,
                userIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    chatId = result.Data?.ChatId,
                    result = result.Data,
                    message = "群信息更新成功"
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
    /// 解散群组
    /// 通过chat_id解散指定群组。通过API解散群组后，群聊天记录将不会保存
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <returns>解散结果</returns>
    [HttpDelete("{chatId}")]
    public async Task<IActionResult> DeleteChatGroupAsync([FromRoute] string chatId)
    {
        try
        {
            var result = await _chatGroupApi.DeleteChatGroupAsync(chatId);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "群组解散成功" });
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
    /// 更新群发言权限
    /// 更新指定群组的发言权限，可设置为所有群成员可发言、仅群主或管理员可发言、指定群成员可发言
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="updateChatModerationRequest">更新群发言权限请求体</param>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <returns>更新结果</returns>
    [HttpPut("{chatId}/moderation")]
    public async Task<IActionResult> UpdateChatModerationAsync(
        [FromRoute] string chatId,
        [FromBody] UpdateChatModerationRequest updateChatModerationRequest,
        [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _chatGroupApi.UpdateChatModerationAsync(
                chatId,
                updateChatModerationRequest,
                userIdType);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "群发言权限更新成功" });
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
    /// 获取群信息
    /// 获取指定群的基本信息，包括群名称、群描述、群头像、群主ID以及群权限配置等
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <returns>群信息</returns>
    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetChatGroupInfoAsync(
        [FromRoute] string chatId,
        [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _chatGroupApi.GetChatGroupInoByIdAsync(chatId, userIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取群信息成功"
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
    /// 设置群置顶
    /// 更新群组中的群置顶信息，可以将群中的某一条消息，或群公告置顶展示
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="chatTopNoticeRequest">群置顶操作请求体</param>
    /// <returns>设置结果</returns>
    [HttpPost("{chatId}/top_notice")]
    public async Task<IActionResult> PutChatGroupTopNoticeAsync(
        [FromRoute] string chatId,
        [FromBody] ChatTopNoticeRequest chatTopNoticeRequest)
    {
        try
        {
            var result = await _chatGroupApi.PutChatGroupTopNoticeAsync(chatId, chatTopNoticeRequest);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "群置顶设置成功" });
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
    /// 取消群置顶
    /// 撤销指定群组中的置顶消息或群公告
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <returns>取消结果</returns>
    [HttpDelete("{chatId}/top_notice")]
    public async Task<IActionResult> DeleteChatGroupTopNoticeAsync([FromRoute] string chatId)
    {
        try
        {
            var result = await _chatGroupApi.DeleteChatGroupTopNoticeAsync(chatId);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "群置顶取消成功" });
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
    /// 获取群列表
    /// 分页获取当前access_token所代表的用户或者机器人所在的群列表
    /// </summary>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <param name="sortType">群组排序方式：ByCreateTimeAsc或ByActiveTimeDesc</param>
    /// <param name="pageSize">分页大小，默认为10</param>
    /// <param name="pageToken">分页标记，第一次请求不填</param>
    /// <returns>群列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetChatGroupPageListAsync(
        [FromQuery] string userIdType = "open_id",
        [FromQuery] string sortType = "ByCreateTimeAsc",
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _chatGroupApi.GetChatGroupPageListAsync(
                userIdType,
                sortType,
                pageSize,
                pageToken);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pageToken = result.Data.PageToken,
                    hasMore = result.Data.HasMore,
                    message = "获取群列表成功"
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
    /// 搜索群列表
    /// 分页获取当前身份（用户或机器人）可见的群列表，包括当前身份所在的群、对当前身份公开的群。支持关键词搜索、分页搜索
    /// </summary>
    /// <param name="query">关键词</param>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <param name="sortType">群组排序方式：ByCreateTimeAsc或ByActiveTimeDesc</param>
    /// <param name="pageSize">分页大小，默认为10</param>
    /// <param name="pageToken">分页标记，第一次请求不填</param>
    /// <returns>群列表</returns>
    [HttpGet("search")]
    public async Task<IActionResult> GetChatGroupPageListByKeywordAsync(
        [FromQuery] string query = "",
        [FromQuery] string userIdType = "open_id",
        [FromQuery] string sortType = "ByCreateTimeAsc",
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _chatGroupApi.GetChatGroupPageListByKeywordAsync(
                query,
                userIdType,
                sortType,
                pageSize,
                pageToken);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pageToken = result.Data.PageToken,
                    hasMore = result.Data.HasMore,
                    message = "搜索群列表成功"
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
    /// 获取群发言权限信息
    /// 分页获取指定群组的发言模式、可发言用户名单等信息
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="userIdType">用户ID类型，默认为open_id</param>
    /// <param name="pageSize">分页大小，默认为10</param>
    /// <param name="pageToken">分页标记，第一次请求不填</param>
    /// <returns>发言权限信息</returns>
    [HttpGet("{chatId}/moderation")]
    public async Task<IActionResult> GetChatGroupModeratorPageListAsync(
        [FromRoute] string chatId,
        [FromQuery] string userIdType = "open_id",
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _chatGroupApi.GetChatGroupModeratorPageListByIdAsync(
                chatId,
                userIdType,
                pageSize,
                pageToken);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pageToken = result.Data.PageToken,
                    hasMore = result.Data.HasMore,
                    message = "获取群发言权限信息成功"
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
    /// 获取群分享链接
    /// 获取指定群的分享链接，他人点击分享链接后可加入群组
    /// </summary>
    /// <param name="chatId">群ID</param>
    /// <param name="shareLinkRequest">获取群分享链接请求体</param>
    /// <returns>分享链接</returns>
    [HttpPost("{chatId}/link")]
    public async Task<IActionResult> GetChatGroupShareLinkAsync(
        [FromRoute] string chatId,
        [FromBody] ShareLinkRequest shareLinkRequest)
    {
        try
        {
            var result = await _chatGroupApi.GetChatGroupShareLinkByIdAsync(chatId, shareLinkRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取群分享链接成功"
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