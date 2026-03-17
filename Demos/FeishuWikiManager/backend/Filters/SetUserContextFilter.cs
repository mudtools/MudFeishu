// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace FeishuWikiManager.Filters;

public class SetUserContextFilter : IAsyncActionFilter
{
    private readonly ICurrentUserContext _userContext;
    private readonly IUserService _userService;
    private readonly ILogger<SetUserContextFilter> _logger;

    public SetUserContextFilter(
        ICurrentUserContext userContext,
        IUserService userService,
        ILogger<SetUserContextFilter> logger)
    {
        _userContext = userContext;
        _userService = userService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var openId = user.FindFirst("open_id")?.Value;
            var unionId = user.FindFirst("union_id")?.Value;
            var name = user.FindFirst(ClaimTypes.Name)?.Value;

            if (!string.IsNullOrEmpty(openId))
            {
                var dbUser = await _userService.GetUserByOpenIdAsync(openId);
                var userId = dbUser?.Id;

                _userContext.SetUser(openId, unionId, userId, name);
                _logger.LogDebug("设置用户上下文: OpenId={OpenId}, UserId={UserId}", openId, userId);
            }
        }

        await next();

        _userContext.Clear();
    }
}
