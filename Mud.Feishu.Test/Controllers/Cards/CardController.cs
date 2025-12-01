// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Cards;

namespace Mud.Feishu.Test.Controllers.Cards;

/// <summary>
/// 飞书卡片控制器
/// 用于测试卡片相关的API接口，包括创建、更新、局部更新卡片等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CardController : ControllerBase
{
    private readonly IFeishuTenantV1Card _cardApi;

    /// <summary>
    /// 初始化CardController实例
    /// </summary>
    /// <param name="cardApi">飞书卡片API接口</param>
    public CardController(IFeishuTenantV1Card cardApi)
    {
        _cardApi = cardApi;
    }

    /// <summary>
    /// 创建卡片
    /// </summary>
    /// <param name="createCardRequest">创建卡片请求体</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCardAsync([FromBody] CreateCardRequest createCardRequest)
    {
        try
        {
            var result = await _cardApi.CreateCardAsync(createCardRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "创建卡片成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "创建卡片失败"
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
    /// 更新卡片设置
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="updateCardRequest">更新卡片设置请求体</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{card_id}/settings")]
    public async Task<IActionResult> UpdateCardSettingsByIdAsync(
        [FromRoute] string card_id,
        [FromBody] UpdateCardSettingsRequest updateCardRequest)
    {
        try
        {
            var result = await _cardApi.UpdateCardSettingsByIdAsync(card_id, updateCardRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "更新卡片设置成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新卡片设置失败"
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
    /// 局部更新卡片
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="partialUpdateCardRequest">局部更新卡片请求体</param>
    /// <returns>更新结果</returns>
    [HttpPost("{card_id}/batch-update")]
    public async Task<IActionResult> PartialUpdateCardByIdAsync(
        [FromRoute] string card_id,
        [FromBody] PartialUpdateCardRequest partialUpdateCardRequest)
    {
        try
        {
            var result = await _cardApi.PartialUpdateCardByIdAsync(
                card_id,
                partialUpdateCardRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "局部更新卡片成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "局部更新卡片失败"
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
    /// 全量更新卡片
    /// </summary>
    /// <param name="card_id">卡片实体ID</param>
    /// <param name="updateCardRequest">全量更新卡片请求体</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{card_id}")]
    public async Task<IActionResult> UpdateCardByIdAsync(
        [FromRoute] string card_id,
        [FromBody] UpdateCardRequest updateCardRequest)
    {
        try
        {
            var result = await _cardApi.UpdateCardByIdAsync(
                card_id,
                updateCardRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "全量更新卡片成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "全量更新卡片失败"
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