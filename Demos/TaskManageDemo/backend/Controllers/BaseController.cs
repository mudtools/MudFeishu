// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 控制器基类，提供通用响应方法
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// 返回成功响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
    {
        return ApiResponse<T>.Ok(data, message);
    }

    /// <summary>
    /// 返回失败响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> Fail<T>(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, ApiResponse<T>.Fail(message));
    }

    /// <summary>
    /// 返回分页响应
    /// </summary>
    protected ActionResult<ApiResponse<PagedResponse<T>>> Paged<T>(
        List<T> items,
        int total,
        int page,
        int pageSize,
        string message = "获取成功")
    {
        var response = new PagedResponse<T>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
        return ApiResponse<PagedResponse<T>>.Ok(response, message);
    }

    /// <summary>
    /// 返回无数据成功响应
    /// </summary>
    protected ActionResult<ApiResponse<bool>> Success(string message = "操作成功")
    {
        return ApiResponse<bool>.Ok(true, message);
    }

    /// <summary>
    /// 返回创建成功响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> Created<T>(T data, string message = "创建成功")
    {
        return StatusCode(201, ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// 返回更新成功响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> Updated<T>(T data, string message = "更新成功")
    {
        return ApiResponse<T>.Ok(data, message);
    }

    /// <summary>
    /// 返回删除成功响应
    /// </summary>
    protected ActionResult<ApiResponse<bool>> Deleted(string message = "删除成功")
    {
        return NoContent();
    }

    /// <summary>
    /// 返回未找到响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFoundResult<T>(string message = "资源不存在")
    {
        return NotFound(ApiResponse<T>.Fail(message));
    }

    /// <summary>
    /// 返回参数错误响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> BadRequestResult<T>(string message = "参数错误")
    {
        return BadRequest(ApiResponse<T>.Fail(message));
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    protected string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("FeishuId")?.Value
            ?? "system";
    }
}
