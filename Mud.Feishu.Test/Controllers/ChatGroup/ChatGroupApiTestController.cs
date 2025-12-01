// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.ChatGroupMenu;
using Mud.Feishu.DataModels.ChatTabs;

namespace Mud.Feishu.Test.Controllers.ChatGroup;

/// <summary>
/// 飞书群组API综合测试控制器
/// 提供群组相关API的综合测试示例，包括标签页和菜单的组合操作
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatGroupApiTestController : ControllerBase
{
    private readonly IFeishuTenantV1ChatTabs _chatTabsApi;
    private readonly IFeishuTenantV1ChatGroupMenu _chatGroupMenuApi;

    /// <summary>
    /// 初始化ChatGroupApiTestController实例
    /// </summary>
    /// <param name="chatTabsApi">飞书会话标签页API接口</param>
    /// <param name="chatGroupMenuApi">飞书群菜单API接口</param>
    public ChatGroupApiTestController(
        IFeishuTenantV1ChatTabs chatTabsApi,
        IFeishuTenantV1ChatGroupMenu chatGroupMenuApi)
    {
        _chatTabsApi = chatTabsApi;
        _chatGroupMenuApi = chatGroupMenuApi;
    }

    /// <summary>
    /// 获取群组的完整信息（标签页和菜单）
    /// </summary>
    /// <param name="chat_id">群ID</param>
    /// <returns>群组完整信息</returns>
    [HttpGet("{chat_id}/full-info")]
    public async Task<IActionResult> GetGroupFullInfoAsync(
        [FromRoute] string chat_id)
    {
        try
        {
            // 并行获取标签页和菜单信息
            var tabsTask = _chatTabsApi.GetChatTabsListByIdAsync(chat_id);
            var menuTask = _chatGroupMenuApi.GetMenuByIdAsync(chat_id);

            await Task.WhenAll(tabsTask, menuTask);

            var tabsResult = await tabsTask;
            var menuResult = await menuTask;

            return Ok(new
            {
                success = true,
                data = new
                {
                    chat_id,
                    tabs = tabsResult.Code == 0 ? tabsResult.Data : null,
                    menu = menuResult.Code == 0 ? menuResult.Data : null,
                    errors = new[]
                    {
                        tabsResult.Code != 0 ? new { type = "tabs", code = tabsResult.Code, message = tabsResult.Msg } : null,
                        menuResult.Code != 0 ? new { type = "menu", code = menuResult.Code, message = menuResult.Msg } : null
                    }.Where(x => x != null).ToArray()
                },
                message = "获取群组完整信息成功"
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
    /// 批量操作：添加标签页和菜单
    /// </summary>
    /// <param name="chat_id">群ID</param>
    /// <param name="createTabsRequest">创建标签页请求</param>
    /// <param name="addMenuRequest">添加菜单请求</param>
    /// <returns>批量操作结果</returns>
    [HttpPost("{chat_id}/batch-create")]
    public async Task<IActionResult> BatchCreateAsync(
        [FromRoute] string chat_id,
        [FromBody] CreateChatTabsRequest createTabsRequest,
        [FromBody] AddChatGroupMenuRequest addMenuRequest)
    {
        try
        {
            var tabsResult = await _chatTabsApi.CreateChatTabsByIdAsync(chat_id, createTabsRequest);
            var menuResult = await _chatGroupMenuApi.AddMenuByIdAsync(chat_id, addMenuRequest);


            return Ok(new
            {
                success = true,
                data = new
                {
                    chat_id,
                    tabs = tabsResult.Code == 0 ? tabsResult.Data : null,
                    menu = menuResult.Code == 0 ? menuResult.Data : null
                },
                operations = new[]
                {
                    new { type = "tabs", success = tabsResult.Code == 0, code = tabsResult.Code, message = tabsResult.Msg },
                    new { type = "menu", success = menuResult.Code == 0, code = menuResult.Code, message = menuResult.Msg }
                },
                message = "批量创建操作完成"
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