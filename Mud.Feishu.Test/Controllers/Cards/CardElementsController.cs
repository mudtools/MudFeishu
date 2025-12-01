// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.CardElements;

namespace Mud.Feishu.Test.Controllers.Cards;

/// <summary>
/// 飞书卡片元素控制器
/// 用于测试卡片元素相关的API接口，包括创建、更新、删除卡片组件以及流式更新文本等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CardElementsController : ControllerBase
{
    private readonly IFeishuTenantV1CardElements _cardElementsApi;

    /// <summary>
    /// 初始化CardElementsController实例
    /// </summary>
    /// <param name="cardElementsApi">飞书卡片元素API接口</param>
    public CardElementsController(IFeishuTenantV1CardElements cardElementsApi)
    {
        _cardElementsApi = cardElementsApi;
    }

    /// <summary>
    /// 创建卡片元素
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="cardElementRequest">创建卡片元素请求体</param>
    /// <returns>创建结果</returns>
    [HttpPost("{card_id}/elements")]
    public async Task<IActionResult> CreateCardElementAsync(
        [FromRoute] string card_id,
        [FromBody] CreateCardElementRequest cardElementRequest)
    {
        try
        {
            var result = await _cardElementsApi.CreateCardElementAsync(
                card_id,
                cardElementRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "创建卡片元素成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "创建卡片元素失败"
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
    /// 更新卡片元素
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="element_id">要更新的组件ID</param>
    /// <param name="cardElementRequest">更新卡片元素请求体</param>
    /// <returns>更新结果</returns>
    [HttpPut("{card_id}/elements/{element_id}")]
    public async Task<IActionResult> UpdateCardElementByIdAsync(
        [FromRoute] string card_id,
        [FromRoute] string element_id,
        [FromBody] UpdateCardElementRequest cardElementRequest)
    {
        try
        {
            var result = await _cardElementsApi.UpdateCardElementByIdAsync(
                card_id,
                element_id,
                cardElementRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "更新卡片元素成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新卡片元素失败"
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
    /// 更新卡片元素属性
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="element_id">要更新的组件ID</param>
    /// <param name="cardElementRequest">更新卡片元素属性请求体</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{card_id}/elements/{element_id}/attributes")]
    public async Task<IActionResult> UpdateCardElementAttributeByIdAsync(
        [FromRoute] string card_id,
        [FromRoute] string element_id,
        [FromBody] UpdateCardElementAttributeRequest cardElementRequest)
    {
        try
        {
            var result = await _cardElementsApi.UpdateCardElementAttributeByIdAsync(
                card_id,
                element_id,
                cardElementRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "更新卡片元素属性成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新卡片元素属性失败"
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
    /// 流式更新卡片文本
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="element_id">要更新的组件ID</param>
    /// <param name="cardElementRequest">流式更新文本请求体</param>
    /// <returns>更新结果</returns>
    [HttpPut("{card_id}/elements/{element_id}/content")]
    public async Task<IActionResult> StreamUpdateCardTextByIdAsync(
        [FromRoute] string card_id,
        [FromRoute] string element_id,
        [FromBody] StreamUpdateTextRequest cardElementRequest)
    {
        try
        {
            var result = await _cardElementsApi.StreamUpdateCardTextByIdAsync(
                card_id,
                element_id,
                cardElementRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "流式更新卡片文本成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "流式更新卡片文本失败"
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
    /// 删除卡片元素
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="element_id">要删除的组件ID</param>
    /// <param name="cardElementRequest">删除卡片元素请求体</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{card_id}/elements/{element_id}")]
    public async Task<IActionResult> DeleteCardElementByIdAsync(
        [FromRoute] string card_id,
        [FromRoute] string element_id,
        [FromBody] DeleteCardElementRequest cardElementRequest)
    {
        try
        {
            var result = await _cardElementsApi.DeleteCardElementByIdAsync(
                card_id,
                element_id,
                cardElementRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "删除卡片元素成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "删除卡片元素失败"
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