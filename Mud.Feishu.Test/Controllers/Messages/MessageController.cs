// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书消息管理控制器
/// 用于测试消息相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IFeishuV1MessageService _messageApi;

    public MessageController(IFeishuV1MessageService messageApi)
    {
        _messageApi = messageApi;
    }

    /// <summary>
    /// 发送消息
    /// 支持发送文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件以及表情包等消息类型
    /// </summary>
    /// <param name="sendMessageRequest">发送消息请求体</param>
    /// <param name="receiveIdType">接收者ID类型，默认为open_id</param>
    /// <returns>发送结果</returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessageAsync(
        [FromBody] SendMessageRequest sendMessageRequest,
        [FromQuery] string receiveIdType = "open_id")
    {
        try
        {
            var result = await _messageApi.SendMessageAsync(
                sendMessageRequest,
                receiveIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    messageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "消息发送成功"
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
    /// 回复消息
    /// 支持回复文本、富文本、卡片、群名片、个人名片、图片、视频、文件等多种类型的消息
    /// </summary>
    /// <param name="messageId">待回复的消息ID</param>
    /// <param name="replyMessageRequest">回复消息请求体</param>
    /// <returns>回复结果</returns>
    [HttpPost("{messageId}/reply")]
    public async Task<IActionResult> ReplyMessageAsync(
        [FromRoute] string messageId,
        [FromBody] ReplyMessageRequest replyMessageRequest)
    {
        try
        {
            var result = await _messageApi.ReplyMessageAsync(
                messageId,
                replyMessageRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    messageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "消息回复成功"
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
    /// 编辑消息
    /// 支持编辑文本、富文本消息。如需编辑卡片消息，请使用更新应用发送的消息卡片接口
    /// </summary>
    /// <param name="messageId">待编辑的消息ID</param>
    /// <param name="editMessageRequest">编辑消息请求体</param>
    /// <returns>编辑结果</returns>
    [HttpPut("{messageId}")]
    public async Task<IActionResult> EditMessageAsync(
        [FromRoute] string messageId,
        [FromBody] EditMessageRequest editMessageRequest)
    {
        try
        {
            var result = await _messageApi.EditMessageAsync(
                messageId,
                editMessageRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    messageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "消息编辑成功"
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
    /// 转发消息
    /// 将一条指定的消息转发给用户、群聊或话题
    /// </summary>
    /// <param name="messageId">待转发的消息ID</param>
    /// <param name="receiveMessageRequest">转发消息请求体</param>
    /// <param name="receiveIdType">消息接收者ID类型</param>
    /// <param name="uuid">自定义设置的唯一字符串序列，用于在转发消息时请求去重</param>
    /// <returns>转发结果</returns>
    [HttpPost("{messageId}/forward")]
    public async Task<IActionResult> ForwardMessageAsync(
        [FromRoute] string messageId,
        [FromBody] ReceiveMessageRequest receiveMessageRequest,
        [FromQuery] string receiveIdType = "open_id",
        [FromQuery] string? uuid = null)
    {
        try
        {
            var result = await _messageApi.ReceiveMessageAsync(
                messageId,
                receiveMessageRequest,
                receiveIdType,
                uuid);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    result = result.Data,
                    message = "消息转发成功"
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
    /// 合并转发消息
    /// 将来自同一个会话内的多条消息，合并转发给指定的用户、群聊或话题
    /// </summary>
    /// <param name="mergeReceiveMessageRequest">合并转发消息请求体</param>
    /// <param name="receiveIdType">消息接收者ID类型</param>
    /// <param name="uuid">自定义设置的唯一字符串序列，用于在转发消息时请求去重</param>
    /// <returns>合并转发结果</returns>
    [HttpPost("merge_forward")]
    public async Task<IActionResult> MergeForwardMessageAsync(
        [FromBody] MergeReceiveMessageRequest mergeReceiveMessageRequest,
        [FromQuery] string receiveIdType = "open_id",
        [FromQuery] string? uuid = null)
    {
        try
        {
            var result = await _messageApi.MergeReceiveMessageAsync(
                mergeReceiveMessageRequest,
                receiveIdType,
                uuid);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    result = result.Data,
                    message = "合并转发消息成功"
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
    /// 转发话题
    /// 将话题转发至指定的用户、群聊或话题
    /// </summary>
    /// <param name="threadId">要转发的话题ID</param>
    /// <param name="receiveMessageRequest">转发消息请求体</param>
    /// <param name="receiveIdType">消息接收者ID类型</param>
    /// <param name="uuid">自定义设置的唯一字符串序列，用于在转发消息时请求去重</param>
    /// <returns>转发结果</returns>
    [HttpPost("threads/{threadId}/forward")]
    public async Task<IActionResult> ForwardThreadAsync(
        [FromRoute] string threadId,
        [FromBody] ReceiveMessageRequest receiveMessageRequest,
        [FromQuery] string receiveIdType = "open_id",
        [FromQuery] string? uuid = null)
    {
        try
        {
            var result = await _messageApi.ReceiveThreadsAsync(
                threadId,
                receiveMessageRequest,
                receiveIdType,
                uuid);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    result = result.Data,
                    message = "话题转发成功"
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
    /// 撤回消息
    /// 机器人可以撤回该机器人自己发送的消息；群聊的群主可以撤回群内指定的消息
    /// </summary>
    /// <param name="messageId">待撤回的消息ID</param>
    /// <returns>撤回结果</returns>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> RevokeMessageAsync([FromRoute] string messageId)
    {
        try
        {
            var result = await _messageApi.RevokeMessage_Tenant_Async(messageId);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "消息撤回成功" });
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
    /// 创建跟随气泡
    /// 在最新一条消息下方添加气泡样式的内容，当消息接收者点击气泡或者新消息到达后，气泡消失
    /// </summary>
    /// <param name="messageId">机器人发送的消息ID</param>
    /// <param name="messageFollowUpRequest">跟随气泡请求体</param>
    /// <returns>创建结果</returns>
    [HttpPost("{messageId}/follow_up")]
    public async Task<IActionResult> CreateMessageFollowUpAsync(
        [FromRoute] string messageId,
        [FromBody] MessageFollowUpRequest messageFollowUpRequest)
    {
        try
        {
            var result = await _messageApi.CreateMessageFollowUpAsync(
                messageId,
                messageFollowUpRequest);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "跟随气泡创建成功" });
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
    /// 查询消息已读用户
    /// 查询指定消息是否已读。接口只返回已读用户的信息，不返回未读用户的信息
    /// </summary>
    /// <param name="messageId">待查询的消息ID</param>
    /// <param name="pageSize">分页大小，默认值：10</param>
    /// <param name="pageToken">分页标记，第一次请求不填</param>
    /// <param name="userIdType">用户ID类型，默认值：open_id</param>
    /// <returns>已读用户列表</returns>
    [HttpGet("{messageId}/read_users")]
    public async Task<IActionResult> GetMessageReadUsersAsync(
        [FromRoute] string messageId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _messageApi.GetMessageReadUsesAsync(
                messageId,
                pageSize,
                pageToken,
                userIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pageToken = result.Data.PageToken,
                    hasMore = result.Data.HasMore,
                    message = "查询成功"
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
    /// 获取历史消息
    /// 获取指定会话（包括单聊、群组）内的历史消息（即聊天记录）
    /// </summary>
    /// <param name="containerIdType">容器类型：chat（单聊和群聊）或thread（话题）</param>
    /// <param name="containerId">容器ID</param>
    /// <param name="startTime">待查询历史信息的起始时间，秒级时间戳</param>
    /// <param name="endTime">待查询历史信息的结束时间，秒级时间戳</param>
    /// <param name="sortType">消息排序方式：ByCreateTimeAsc（升序）或ByCreateTimeDesc（降序）</param>
    /// <param name="pageSize">分页大小，默认值：10</param>
    /// <param name="pageToken">分页标记，第一次请求不填</param>
    /// <returns>历史消息列表</returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistoryMessageAsync(
        [FromQuery] string containerIdType,
        [FromQuery] string containerId,
        [FromQuery] string? startTime = null,
        [FromQuery] string? endTime = null,
        [FromQuery] string sortType = "ByCreateTimeAsc",
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _messageApi.GetHistoryMessageAsync(
                containerIdType,
                containerId,
                startTime,
                endTime,
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
                    message = "获取历史消息成功"
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
    /// 获取消息内容
    /// 通过消息的message_id查询指定消息的内容
    /// </summary>
    /// <param name="messageId">待查询的消息ID</param>
    /// <param name="userIdType">用户ID类型，默认值：open_id</param>
    /// <returns>消息内容</returns>
    [HttpGet("{messageId}/content")]
    public async Task<IActionResult> GetMessageContentAsync(
        [FromRoute] string messageId,
        [FromQuery] string userIdType = "open_id")
    {
        try
        {
            var result = await _messageApi.GetContentListByMessageIdAsync(
                messageId,
                userIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取消息内容成功"
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
    /// 获取消息资源文件（小文件）
    /// 获取指定消息内包含的资源文件，包括音频、视频、图片和文件
    /// 注意：该函数适用于获取小文件
    /// </summary>
    /// <param name="messageId">待查询的消息ID</param>
    /// <param name="fileKey">待查询资源的Key</param>
    /// <param name="type">资源类型：image（图片）或file（文件、音频、视频）</param>
    /// <returns>文件二进制数据</returns>
    [HttpGet("{messageId}/resources/{fileKey}")]
    public async Task<IActionResult> GetMessageFileAsync(
        [FromRoute] string messageId,
        [FromRoute] string fileKey,
        [FromQuery] string type)
    {
        try
        {
            var fileData = await _messageApi.GetMessageFile(messageId, fileKey, type);

            // 根据文件类型设置Content-Type
            var contentType = type.ToLower() switch
            {
                "image" => "image/jpeg",
                "file" => "application/octet-stream",
                _ => "application/octet-stream"
            };

            return File(fileData, contentType);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取消息资源文件（大文件）
    /// 获取指定消息内包含的资源文件，包括音频、视频、图片和文件
    /// 注意：该函数适用于获取大文件，将文件保存到本地路径
    /// </summary>
    /// <param name="messageId">待查询的消息ID</param>
    /// <param name="fileKey">待查询资源的Key</param>
    /// <param name="type">资源类型：image（图片）或file（文件、音频、视频）</param>
    /// <returns>下载结果</returns>
    [HttpPost("{messageId}/resources/{fileKey}/download")]
    public async Task<IActionResult> DownloadMessageLargeFileAsync(
        [FromRoute] string messageId,
        [FromRoute] string fileKey,
        [FromQuery] string type)
    {
        try
        {
            string localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString() + ".tmp");

            await _messageApi.GetMessageLargeFile(messageId, fileKey, type, localFilePath);

            return Ok(new
            {
                success = true,
                filePath = localFilePath,
                message = "大文件下载成功"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}