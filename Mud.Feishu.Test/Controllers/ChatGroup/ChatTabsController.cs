// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.ChatTabs;

namespace Mud.Feishu.Test.Controllers.ChatGroup;

/// <summary>
/// 飞书会话标签页管理控制器
/// 用于测试会话标签页相关的API接口，包括添加、删除、更新以及获取会话标签页等操作
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatTabsController : ControllerBase
{
    private readonly IFeishuTenantV1ChatTabs _chatTabsApi;

    /// <summary>
    /// 初始化ChatTabsController实例
    /// </summary>
    /// <param name="chatTabsApi">飞书会话标签页API接口</param>
    public ChatTabsController(IFeishuTenantV1ChatTabs chatTabsApi)
    {
        _chatTabsApi = chatTabsApi;
    }

    /// <summary>
    /// 在指定会话中添加一个或多个标签页
    /// </summary>
    /// <param name="chat_id">会话ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="createChatTabsRequest">创建会话标签页请求体</param>
    /// <param name="user_id_type">用户ID类型，默认为open_id</param>
    /// <param name="set_bot_manager">是否设置创建群的机器人为管理员</param>
    /// <param name="uuid">创建群组请求去重的唯一字符串序列</param>
    /// <returns>添加结果</returns>
    [HttpPost("{chat_id}/tabs")]
    public async Task<IActionResult> CreateChatTabsAsync(
        [FromRoute] string chat_id,
        [FromBody] CreateChatTabsRequest createChatTabsRequest,
        [FromQuery] string user_id_type = "open_id",
        [FromQuery] bool set_bot_manager = false,
        [FromQuery] string? uuid = null)
    {
        try
        {
            var result = await _chatTabsApi.CreateChatTabsByIdAsync(
                chat_id,
                createChatTabsRequest,
                user_id_type,
                set_bot_manager,
                uuid);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加标签页成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "添加标签页失败"
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
    /// 删除指定会话中的一个或多个标签页
    /// </summary>
    /// <param name="chat_id">会话ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="deleteChatTabsRequest">删除会话标签页请求体</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{chat_id}/tabs")]
    public async Task<IActionResult> DeleteChatTabsAsync(
        [FromRoute] string chat_id,
        [FromBody] ChatTabsIdsRequest deleteChatTabsRequest)
    {
        try
        {
            var result = await _chatTabsApi.DeleteChatTabsByIdAsync(
                chat_id,
                deleteChatTabsRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "删除标签页成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "删除标签页失败"
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
    /// 更新指定会话中的标签页信息
    /// </summary>
    /// <param name="chat_id">会话ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="updateChatTabsRequest">更新会话标签页请求体</param>
    /// <returns>更新结果</returns>
    [HttpPut("{chat_id}/tabs")]
    public async Task<IActionResult> UpdateChatTabsAsync(
        [FromRoute] string chat_id,
        [FromBody] UpdateChatTabsRequest updateChatTabsRequest)
    {
        try
        {
            var result = await _chatTabsApi.UpdateChatTabsByIdAsync(
                chat_id,
                updateChatTabsRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "更新标签页成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新标签页失败"
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
    /// 获取指定会话中的所有标签页信息
    /// </summary>
    /// <param name="chat_id">会话ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <returns>标签页列表</returns>
    [HttpGet("{chat_id}/tabs")]
    public async Task<IActionResult> GetChatTabsAsync(
        [FromRoute] string chat_id)
    {
        try
        {
            var result = await _chatTabsApi.GetChatTabsListByIdAsync(chat_id);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取标签页成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "获取标签页失败"
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
    /// 调整指定会话内的多个会话标签页排列顺序
    /// </summary>
    /// <param name="chat_id">会话ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="chatTabsSortRequest">排序会话标签页请求体</param>
    /// <returns>排序结果</returns>
    [HttpPost("{chat_id}/tabs/sort")]
    public async Task<IActionResult> SortChatTabsAsync(
        [FromRoute] string chat_id,
        [FromBody] ChatTabsIdsRequest chatTabsSortRequest)
    {
        try
        {
            var result = await _chatTabsApi.ChatTabsSortByIdAsync(
                chat_id,
                chatTabsSortRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "排序标签页成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "排序标签页失败"
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