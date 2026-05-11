// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Mud.Feishu.Abstractions;

namespace FeishuWikiManager.Filters;

/// <summary>
/// 用户上下文增强过滤器
/// 负责从数据库获取用户ID并更新到用户上下文中
/// </summary>
/// <remarks>
/// 注意：基础的 open_id、union_id、name 已由 FeishuUserAuthenticationMiddleware 中间件设置。
/// 此过滤器仅负责查询数据库获取业务系统的用户ID。
/// </remarks>
public class SetUserContextFilter : IAsyncActionFilter
{
    private readonly IFeishuCurrentUserContext _userContext;
    private readonly IUserService _userService;
    private readonly ILogger<SetUserContextFilter> _logger;

    public SetUserContextFilter(
        IFeishuCurrentUserContext userContext,
        IUserService userService,
        ILogger<SetUserContextFilter> logger)
    {
        _userContext = userContext;
        _userService = userService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 中间件已设置 open_id、union_id、name
        // 此处仅从数据库获取用户ID并更新上下文
        if (_userContext.IsAuthenticated && !string.IsNullOrEmpty(_userContext.OpenId))
        {
            var openId = _userContext.OpenId;
            var dbUser = await _userService.GetUserByOpenIdAsync(openId);

            if (dbUser != null)
            {
                // 更新用户上下文，添加业务系统用户ID
                _userContext.SetUser(openId, _userContext.UnionId, dbUser.Id, _userContext.Name);
                _logger.LogDebug("用户上下文增强: OpenId={OpenId}, UserId={UserId}", openId, dbUser.Id);
            }
        }

        await next();
    }
}
