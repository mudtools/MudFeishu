// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 飞书文件服务器控制器基类
/// 提供通用的辅助方法和响应封装
/// </summary>
[ApiController]
public abstract class FeishuControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// 获取当前登录用户的ID
    /// </summary>
    /// <returns>用户ID，如果未登录则返回null</returns>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// 获取当前登录用户的ID（必须已登录）
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="UnauthorizedAccessException">当用户未登录时抛出</exception>
    protected int GetRequiredUserId()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("未登录");
        }
        return userId.Value;
    }

    /// <summary>
    /// 返回成功响应（带数据）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">响应数据</param>
    /// <param name="message">响应消息</param>
    /// <returns>成功响应结果</returns>
    protected ActionResult<ApiResponse<T>> Success<T>(T? data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// 返回成功响应（无数据）
    /// </summary>
    /// <param name="message">响应消息</param>
    /// <returns>成功响应结果</returns>
    protected ActionResult<ApiResponse> Success(string? message = null)
    {
        return Ok(ApiResponse.Ok(message));
    }

    /// <summary>
    /// 返回错误请求响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>错误请求响应结果</returns>
    protected ActionResult<ApiResponse> BadRequestResult(string message)
    {
        return BadRequest(ApiResponse.Fail(message, 400));
    }

    /// <summary>
    /// 返回错误请求响应（带数据类型）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>错误请求响应结果</returns>
    protected ActionResult<ApiResponse<T>> BadRequestResult<T>(string message)
    {
        return BadRequest(ApiResponse<T>.Fail(message, 400));
    }

    /// <summary>
    /// 返回未授权响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>未授权响应结果</returns>
    protected ActionResult<ApiResponse> UnauthorizedResult(string message = "未登录")
    {
        return Unauthorized(ApiResponse.Fail(message, 401));
    }

    /// <summary>
    /// 返回未授权响应（带数据类型）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>未授权响应结果</returns>
    protected ActionResult<ApiResponse<T>> UnauthorizedResult<T>(string message = "未登录")
    {
        return Unauthorized(ApiResponse<T>.Fail(message, 401));
    }

    /// <summary>
    /// 返回资源不存在响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>资源不存在响应结果</returns>
    protected ActionResult<ApiResponse> NotFoundResult(string message = "资源不存在")
    {
        return NotFound(ApiResponse.Fail(message, 404));
    }

    /// <summary>
    /// 返回资源不存在响应（带数据类型）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>资源不存在响应结果</returns>
    protected ActionResult<ApiResponse<T>> NotFoundResult<T>(string message = "资源不存在")
    {
        return NotFound(ApiResponse<T>.Fail(message, 404));
    }

    /// <summary>
    /// 返回服务器错误响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>服务器错误响应结果</returns>
    protected IActionResult ServerErrorResult(string message)
    {
        return StatusCode(500, ApiResponse.Fail(message, 500));
    }

    /// <summary>
    /// 返回服务器错误响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>服务器错误响应结果</returns>
    protected ActionResult<ApiResponse> ServerError(string message)
    {
        return StatusCode(500, ApiResponse.Fail(message, 500));
    }

    /// <summary>
    /// 返回服务器错误响应（带数据类型）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>服务器错误响应结果</returns>
    protected ActionResult<ApiResponse<T>> ServerError<T>(string message)
    {
        return StatusCode(500, ApiResponse<T>.Fail(message, 500));
    }

    /// <summary>
    /// 返回已创建响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="actionName">操作名称</param>
    /// <param name="routeValues">路由值</param>
    /// <param name="data">响应数据</param>
    /// <returns>已创建响应结果</returns>
    protected ActionResult<ApiResponse<T>> CreatedResult<T>(string actionName, object routeValues, T? data)
    {
        return CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data));
    }

    /// <summary>
    /// 返回资源不存在错误
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>资源不存在响应</returns>
    protected IActionResult NotFoundError(string message = "资源不存在")
    {
        return NotFound(ApiResponse.Fail(message, 404));
    }

    /// <summary>
    /// 返回未授权错误
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>未授权响应</returns>
    protected IActionResult UnauthorizedError(string message = "未登录")
    {
        return Unauthorized(ApiResponse.Fail(message, 401));
    }

    /// <summary>
    /// 返回错误请求错误
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>错误请求响应</returns>
    protected IActionResult BadRequestError(string message)
    {
        return BadRequest(ApiResponse.Fail(message, 400));
    }
}
