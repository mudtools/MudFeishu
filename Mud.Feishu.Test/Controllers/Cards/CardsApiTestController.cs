// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.CardElements;
using Mud.Feishu.DataModels.CardMessageStream;
using Mud.Feishu.DataModels.Cards;

namespace Mud.Feishu.Test.Controllers.Cards;

/// <summary>
/// 飞书卡片API综合测试控制器
/// 提供卡片相关API的综合测试示例，包括卡片创建、元素操作、消息流卡片等的组合操作
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CardsApiTestController : ControllerBase
{
    private readonly IFeishuTenantV1Card _cardApi;
    private readonly IFeishuTenantV1CardElements _cardElementsApi;
    private readonly IFeishuTenantV2AppCardMessageStream _appCardMessageStreamApi;

    /// <summary>
    /// 初始化CardsApiTestController实例
    /// </summary>
    /// <param name="cardApi">飞书卡片API接口</param>
    /// <param name="cardElementsApi">飞书卡片元素API接口</param>
    /// <param name="appCardMessageStreamApi">飞书应用消息流卡片API接口</param>
    public CardsApiTestController(
        IFeishuTenantV1Card cardApi,
        IFeishuTenantV1CardElements cardElementsApi,
        IFeishuTenantV2AppCardMessageStream appCardMessageStreamApi)
    {
        _cardApi = cardApi;
        _cardElementsApi = cardElementsApi;
        _appCardMessageStreamApi = appCardMessageStreamApi;
    }

    /// <summary>
    /// 完整的卡片创建和配置流程
    /// </summary>
    /// <param name="createCardRequest">创建卡片请求</param>
    /// <param name="cardElementRequest">添加元素请求</param>
    /// <returns>完整流程结果</returns>
    [HttpPost("complete-card-creation")]
    public async Task<IActionResult> CompleteCardCreationAsync(
        [FromBody] CreateCardRequest createCardRequest,
        [FromBody] CreateCardElementRequest cardElementRequest
        )
    {
        try
        {
            // 第一步：创建卡片
            var createCardResult = await _cardApi.CreateCardAsync(createCardRequest);

            if (createCardResult.Code != 0)
            {
                return BadRequest(new
                {
                    success = false,
                    step = "create_card",
                    code = createCardResult.Code,
                    message = createCardResult.Msg ?? "创建卡片失败"
                });
            }

            var cardId = createCardResult.Data?.CardId;
            if (string.IsNullOrEmpty(cardId))
            {
                return BadRequest(new
                {
                    success = false,
                    step = "create_card",
                    message = "创建卡片成功但未返回卡片ID"
                });
            }

            // 第二步：添加元素
            var addElementResult = await _cardElementsApi.CreateCardElementAsync(
                cardId,
                cardElementRequest
                );

            var result = new
            {
                success = true,
                message = "完整卡片创建流程执行完成",
                steps = new object[]
                {
                    new { step = "create_card", success = createCardResult.Code == 0, data = createCardResult.Data },
                    new { step = "add_element", success = addElementResult.Code == 0, data = addElementResult.Data }
                },
                card_id = cardId
            };

            return Ok(result);
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
    /// 批量操作：创建卡片并发送消息流
    /// </summary>
    /// <param name="createCardRequest">创建卡片请求</param>
    /// <param name="appCardMessageStreamRequest">创建消息流卡片请求</param>
    /// <returns>批量操作结果</returns>
    [HttpPost("batch-create-with-stream")]
    public async Task<IActionResult> BatchCreateWithStreamAsync(
        [FromBody] CreateCardRequest createCardRequest,
        [FromBody] CreateAppCardMessageStreamRequest appCardMessageStreamRequest
        )
    {
        try
        {
            // 并行执行创建卡片和创建消息流
            var cardTask = _cardApi.CreateCardAsync(createCardRequest);
            var streamTask = _appCardMessageStreamApi.CreateCardMessageStreamAsync(
                appCardMessageStreamRequest
                );

            await Task.WhenAll(cardTask, streamTask);

            var cardResult = await cardTask;
            var streamResult = await streamTask;

            return Ok(new
            {
                success = true,
                message = "批量创建操作完成",
                results = new
                {
                    card = new
                    {
                        success = cardResult.Code == 0,
                        code = cardResult.Code,
                        message = cardResult.Msg,
                        data = cardResult.Data
                    },
                    message_stream = new
                    {
                        success = streamResult.Code == 0,
                        code = streamResult.Code,
                        message = streamResult.Msg,
                        data = streamResult.Data
                    }
                }
            });
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
    /// 卡片完整生命周期管理
    /// </summary>
    /// <param name="card_id">卡片ID</param>
    /// <param name="element_id">元素ID</param>
    /// <param name="updateElementRequest">更新元素请求</param>
    /// <param name="updateCardRequest">更新卡片请求</param>
    /// <returns>生命周期管理结果</returns>
    [HttpPost("{card_id}/lifecycle/{element_id}")]
    public async Task<IActionResult> CardLifecycleAsync(
        [FromRoute] string card_id,
        [FromRoute] string element_id,
        [FromBody] UpdateCardElementRequest updateElementRequest,
        [FromBody] UpdateCardRequest updateCardRequest
        )
    {
        try
        {
            var operations = new List<object>();

            // 操作1：更新元素
            var updateElementResult = await _cardElementsApi.UpdateCardElementByIdAsync(
                card_id, element_id, updateElementRequest);

            operations.Add(new
            {
                operation = "update_element",
                success = updateElementResult.Code == 0,
                code = updateElementResult.Code,
                message = updateElementResult.Msg
            });

            // 操作2：局部更新卡片
            var partialUpdateRequest = new PartialUpdateCardRequest(); // 实际使用时需要根据业务需求构建
            var partialUpdateResult = await _cardApi.PartialUpdateCardByIdAsync(
                card_id, partialUpdateRequest);

            operations.Add(new
            {
                operation = "partial_update_card",
                success = partialUpdateResult.Code == 0,
                code = partialUpdateResult.Code,
                message = partialUpdateResult.Msg
            });

            // 操作3：流式更新文本
            var streamUpdateRequest = new StreamUpdateTextRequest { Content = "这是更新后的文本内容。将以打字机式的效果输出", Sequence = 1 }; // 实际使用时需要根据业务需求构建
            var streamUpdateResult = await _cardElementsApi.StreamUpdateCardTextByIdAsync(
                card_id, element_id, streamUpdateRequest);

            operations.Add(new
            {
                operation = "stream_update_text",
                success = streamUpdateResult.Code == 0,
                code = streamUpdateResult.Code,
                message = streamUpdateResult.Msg
            });

            return Ok(new
            {
                success = true,
                message = "卡片生命周期管理操作完成",
                card_id,
                element_id,
                operations
            });
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
    /// 卡片状态检查和诊断
    /// </summary>
    /// <param name="card_id">卡片ID</param>
    /// <returns>诊断结果</returns>
    [HttpGet("{card_id}/diagnosis")]
    public async Task<IActionResult> CardDiagnosisAsync([FromRoute] string card_id)
    {
        try
        {
            // 这里可以添加各种诊断逻辑
            // 例如：检查卡片是否存在、元素的完整性等

            return Ok(new
            {
                success = true,
                message = "卡片诊断完成",
                card_id,
                diagnosis = new
                {
                    card_id_valid = !string.IsNullOrEmpty(card_id),
                    timestamp = DateTimeOffset.UtcNow,
                    status = "healthy"
                }
            });
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