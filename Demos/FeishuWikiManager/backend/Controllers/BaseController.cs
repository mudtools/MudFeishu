// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FeishuWikiManager.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    protected string? CurrentOpenId => User.FindFirst("open_id")?.Value;

    protected string? CurrentUnionId => User.FindFirst("union_id")?.Value;

    protected string? CurrentUserName => User.FindFirst(ClaimTypes.Name)?.Value;

    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    protected IActionResult Success<T>(T? data, string? message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    protected IActionResult Success(string? message = null)
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }

    protected IActionResult Fail<T>(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<T>
        {
            Success = false,
            Message = message
        });
    }

    protected IActionResult Fail(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message
        });
    }

    protected IActionResult BadRequestResult(string message)
    {
        return Fail<object>(message, StatusCodes.Status400BadRequest);
    }

    protected IActionResult UnauthorizedResult(string message = "未授权，请先登录")
    {
        return Fail<object>(message, StatusCodes.Status401Unauthorized);
    }

    protected IActionResult ForbiddenResult(string message = "没有权限访问该资源")
    {
        return Fail<object>(message, StatusCodes.Status403Forbidden);
    }

    protected IActionResult NotFoundResult(string message = "请求的资源不存在")
    {
        return Fail<object>(message, StatusCodes.Status404NotFound);
    }

    protected IActionResult ServerError(string message, Exception? ex = null)
    {
        var errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
        return Fail<object>(errorMessage, StatusCodes.Status500InternalServerError);
    }

    protected IActionResult PagedSuccess<T>(List<T> items, bool hasMore, string? pageToken = null)
    {
        return Ok(new PagedResponse<T>
        {
            Items = items,
            HasMore = hasMore,
            PageToken = pageToken
        });
    }

    protected void EnsureAuthenticated()
    {
        if (!IsAuthenticated || string.IsNullOrEmpty(CurrentOpenId))
        {
            throw new UnauthorizedAccessException("未授权，请先登录");
        }
    }

    protected string GetRequiredOpenId()
    {
        var openId = CurrentOpenId;
        if (string.IsNullOrEmpty(openId))
        {
            throw new UnauthorizedAccessException("未授权，请先登录");
        }
        return openId;
    }

    protected string? GetOptionalOpenId()
    {
        return CurrentOpenId;
    }
}
