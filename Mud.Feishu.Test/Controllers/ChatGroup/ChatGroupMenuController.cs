// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.ChatGroupMenu;

namespace Mud.Feishu.Test.Controllers.ChatGroup;

/// <summary>
/// 飞书群菜单管理控制器
/// 用于测试群菜单相关的API接口，包括添加、删除、修改、查询以及排序群菜单等操作
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatGroupMenuController : ControllerBase
{
    private readonly IFeishuTenantV1ChatGroupMenu _chatGroupMenuApi;

    /// <summary>
    /// 初始化ChatGroupMenuController实例
    /// </summary>
    /// <param name="chatGroupMenuApi">飞书群菜单API接口</param>
    public ChatGroupMenuController(IFeishuTenantV1ChatGroupMenu chatGroupMenuApi)
    {
        _chatGroupMenuApi = chatGroupMenuApi;
    }

    /// <summary>
    /// 在指定群组中添加一个或多个群菜单
    /// </summary>
    /// <param name="chat_id">群ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="addChatGroupMenuRequest">添加群菜单请求体</param>
    /// <returns>添加结果</returns>
    [HttpPost("{chat_id}/menu")]
    public async Task<IActionResult> AddMenuByIdAsync(
        [FromRoute] string chat_id,
        [FromBody] AddChatGroupMenuRequest addChatGroupMenuRequest)
    {
        try
        {
            var result = await _chatGroupMenuApi.AddMenuByIdAsync(
                chat_id,
                addChatGroupMenuRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "添加群菜单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "添加群菜单失败"
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
    /// 修改指定群组内的某个一级菜单或者二级菜单的元信息
    /// </summary>
    /// <param name="chat_id">群ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="menu_item_id">一级菜单或者二级菜单的ID，示例值："7156553273518882844"</param>
    /// <param name="updateChatMenuItemRequest">更新群菜单请求体</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{chat_id}/menu/{menu_item_id}")]
    public async Task<IActionResult> UpdateMenuByIdAsync(
        [FromRoute] string chat_id,
        [FromRoute] string menu_item_id,
        [FromBody] UpdateChatMenuItemRequest updateChatMenuItemRequest)
    {
        try
        {
            var result = await _chatGroupMenuApi.UpdateMenuByIdAsync(
                chat_id,
                menu_item_id,
                updateChatMenuItemRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "更新群菜单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "更新群菜单失败"
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
    /// 删除指定群内的一级菜单
    /// </summary>
    /// <param name="chat_id">群ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="deleteMenuIdsRequest">删除群内的一级菜单请求体</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{chat_id}/menu")]
    public async Task<IActionResult> DeleteMenuByIdAsync(
        [FromRoute] string chat_id,
        [FromBody] ChartMenuIdsRequest deleteMenuIdsRequest)
    {
        try
        {
            var result = await _chatGroupMenuApi.DeleteMenuByIdAsync(
                chat_id,
                deleteMenuIdsRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "删除群菜单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "删除群菜单失败"
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
    /// 调整指定群组内的群菜单排列顺序
    /// </summary>
    /// <param name="chat_id">群ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="sortMenuRequest">菜单项目排序请求体</param>
    /// <returns>排序结果</returns>
    [HttpPost("{chat_id}/menu/sort")]
    public async Task<IActionResult> SortMenuByIdAsync(
        [FromRoute] string chat_id,
        [FromBody] ChartMenuIdsRequest sortMenuRequest)
    {
        try
        {
            var result = await _chatGroupMenuApi.SortMenuByIdAsync(
                chat_id,
                sortMenuRequest);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "排序群菜单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "排序群菜单失败"
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
    /// 获取指定群组内的群菜单信息
    /// </summary>
    /// <param name="chat_id">群ID，示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken">取消操作令牌</param>
    /// <returns>群菜单信息</returns>
    [HttpGet("{chat_id}/menu")]
    public async Task<IActionResult> GetMenuByIdAsync(
        [FromRoute] string chat_id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _chatGroupMenuApi.GetMenuByIdAsync(
                chat_id,
                cancellationToken);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "获取群菜单成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Msg ?? "获取群菜单失败"
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