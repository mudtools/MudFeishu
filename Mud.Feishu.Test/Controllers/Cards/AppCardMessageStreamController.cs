// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.CardMessageStream;

namespace Mud.Feishu.Test.Controllers.Cards;

/// <summary>
/// 飞书应用消息流卡片控制器
/// 用于测试应用消息流卡片相关的API接口，包括创建、更新、删除消息流卡片以及即时提醒等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AppCardMessageStreamController : ControllerBase
{
    private readonly IFeishuTenantV2AppCardMessageStream _appCardMessageStreamApi;

    /// <summary>
    /// 初始化AppCardMessageStreamController实例
    /// </summary>
    /// <param name="appCardMessageStreamApi">飞书应用消息流卡片API接口</param>
    public AppCardMessageStreamController(IFeishuTenantV2AppCardMessageStream appCardMessageStreamApi)
    {
        _appCardMessageStreamApi = appCardMessageStreamApi;
    }

    /// <summary>
    /// 创建应用消息流卡片
    /// </summary>
    /// <param name="appCardMessageStreamRequest">创建应用消息流卡片请求体</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCardMessageStreamAsync([FromBody] CreateAppCardMessageStreamRequest appCardMessageStreamRequest)
    {
        try
        {
            var result = await _appCardMessageStreamApi.CreateCardMessageStreamAsync(appCardMessageStreamRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "创建应用消息流卡片成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "创建应用消息流卡片失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 更新应用消息流卡片
    /// </summary>
    /// <param name="appCardMessageStreamRequest">更新应用消息流卡片请求体</param>
    /// <returns>更新结果</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateCardMessageStreamAsync([FromBody] UpdateAppCardMessageStreamRequest appCardMessageStreamRequest)
    {
        try
        {
            var result = await _appCardMessageStreamApi.UpdateCardMessageStreamAsync(appCardMessageStreamRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "更新应用消息流卡片成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新应用消息流卡片失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 删除应用消息流卡片
    /// </summary>
    /// <param name="appCardMessageStreamRequest">删除应用消息流卡片请求体</param>
    /// <returns>删除结果</returns>
    [HttpDelete]
    public async Task<IActionResult> DeleteCardMessageStreamAsync([FromBody] DeleteAppCardMessageStreamRequest appCardMessageStreamRequest)
    {
        try
        {
            var result = await _appCardMessageStreamApi.DeleteCardMessageStreamAsync(appCardMessageStreamRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "删除应用消息流卡片成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "删除应用消息流卡片失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 设置机器人单聊即时提醒
    /// </summary>
    /// <param name="timeSentiveRequest">机器人单聊即时提醒请求体</param>
    /// <returns>设置结果</returns>
    [HttpPost("bot-time-sentive")]
    public async Task<IActionResult> BotTimeSentiveAsync(
        [FromBody] BotTimeSentiveRequest timeSentiveRequest)
    {
        try
        {
            var result = await _appCardMessageStreamApi.BotTimeSentiveAsync(timeSentiveRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "设置机器人单聊即时提醒成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "设置机器人单聊即时提醒失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 更新消息流卡片按钮
    /// </summary>
    /// <param name="updateCardMessageStreamButtonRequest">更新消息流卡片按钮请求体</param>
    /// <returns>更新结果</returns>
    [HttpPut("buttons")]
    public async Task<IActionResult> UpdateCardMessageStreamButtonAsync([FromBody] UpdateCardMessageStreamButtonRequest updateCardMessageStreamButtonRequest)
    {
        try
        {
            var result = await _appCardMessageStreamApi.UpdateCardMessageStreamButtonAsync(
                updateCardMessageStreamButtonRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "更新消息流卡片按钮成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新消息流卡片按钮失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 设置即时提醒
    /// </summary>
    /// <param name="feed_card_id">消息流卡片ID</param>
    /// <param name="feedCardsByFeedCardIdRequest">即时提醒请求体</param>
    /// <returns>设置结果</returns>
    [HttpPatch("{feed_card_id}/feed-cards")]
    public async Task<IActionResult> FeedCardsByFeedCardIdAsync(
        [FromRoute] string feed_card_id,
        [FromBody] FeedCardsByFeedCardIdRequest feedCardsByFeedCardIdRequest)
    {
        try
        {
            var result = await _appCardMessageStreamApi.FeedCardsByFeedCardIdAsync(
                feed_card_id,
                feedCardsByFeedCardIdRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "设置即时提醒成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "设置即时提醒失败"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }
}