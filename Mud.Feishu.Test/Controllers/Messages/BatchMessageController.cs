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
/// 飞书批量消息管理控制器
/// 用于测试批量消息相关的API接口，支持给多个用户或多个部门成员发送消息
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BatchMessageController : ControllerBase
{
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;

    public BatchMessageController(IFeishuTenantV1BatchMessage batchMessageApi)
    {
        _batchMessageApi = batchMessageApi;
    }

    /// <summary>
    /// 批量发送文本消息
    /// 给多个用户或者多个部门中的成员发送文本消息
    /// </summary>
    /// <param name="sendMessageRequest">批量发送文本消息请求体</param>
    /// <returns>发送结果</returns>
    [HttpPost("send/text")]
    public async Task<IActionResult> BatchSendTextMessageAsync(
        [FromBody] BatchSenderTextMessageRequest sendMessageRequest)
    {
        try
        {
            var result = await _batchMessageApi.BatchSendTextMessageAsync(sendMessageRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    batchMessageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "批量文本消息发送成功"
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
    /// 批量发送富文本消息
    /// 给多个用户或者多个部门中的成员发送富文本消息
    /// </summary>
    /// <param name="sendMessageRequest">批量发送富文本消息请求体</param>
    /// <returns>发送结果</returns>
    [HttpPost("send/richtext")]
    public async Task<IActionResult> BatchSendRichTextMessageAsync(
        [FromBody] BatchSenderRichTextMessageRequest sendMessageRequest)
    {
        try
        {
            var result = await _batchMessageApi.BatchSendRichTextMessageAsync(sendMessageRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    batchMessageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "批量富文本消息发送成功"
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
    /// 批量发送图片消息
    /// 给多个用户或者多个部门中的成员发送图片消息
    /// </summary>
    /// <param name="sendMessageRequest">批量发送图片消息请求体</param>
    /// <returns>发送结果</returns>
    [HttpPost("send/image")]
    public async Task<IActionResult> BatchSendImageMessageAsync(
        [FromBody] BatchSenderMessageImageRequest sendMessageRequest)
    {
        try
        {
            var result = await _batchMessageApi.BatchSendImageMessageAsync(sendMessageRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    batchMessageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "批量图片消息发送成功"
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
    /// 批量发送群分享消息
    /// 给多个用户或者多个部门中的成员发群分享消息
    /// </summary>
    /// <param name="sendMessageRequest">批量发送群分享消息请求体</param>
    /// <returns>发送结果</returns>
    [HttpPost("send/group_share")]
    public async Task<IActionResult> BatchSendGroupShareMessageAsync(
        [FromBody] BatchSenderMessageGroupShareRequest sendMessageRequest)
    {
        try
        {
            var result = await _batchMessageApi.BatchSendGroupShareMessageAsync(sendMessageRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    batchMessageId = result.Data?.MessageId,
                    result = result.Data,
                    message = "批量群分享消息发送成功"
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
    /// 撤回批量消息
    /// 用于撤回通过批量发送消息接口发送的消息
    /// </summary>
    /// <param name="batchMessageId">待撤回的批量消息任务ID</param>
    /// <returns>撤回结果</returns>
    [HttpDelete("{batchMessageId}")]
    public async Task<IActionResult> RevokeBatchMessageAsync([FromRoute] string batchMessageId)
    {
        try
        {
            var result = await _batchMessageApi.RevokeMessageAsync(batchMessageId);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "批量消息撤回成功" });
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
    /// 查询批量消息已读状态
    /// 批量发送消息后，可通过该接口查询消息推送的总人数以及消息已读人数
    /// </summary>
    /// <param name="batchMessageId">待查询的批量消息任务ID</param>
    /// <returns>已读状态信息</returns>
    [HttpGet("{batchMessageId}/read_status")]
    public async Task<IActionResult> GetBatchMessageReadStatusAsync([FromRoute] string batchMessageId)
    {
        try
        {
            var result = await _batchMessageApi.GetUserReadMessageInfosAsync(batchMessageId);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "查询批量消息已读状态成功"
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
    /// 查询批量消息进度
    /// 批量发送消息或者批量撤回消息后，可通过该接口查询消息的发送进度和撤回进度
    /// </summary>
    /// <param name="batchMessageId">待查询的批量消息任务ID</param>
    /// <returns>进度信息</returns>
    [HttpGet("{batchMessageId}/progress")]
    public async Task<IActionResult> GetBatchMessageProgressAsync([FromRoute] string batchMessageId)
    {
        try
        {
            var result = await _batchMessageApi.GetBatchMessageProgressAsync(batchMessageId);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "查询批量消息进度成功"
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