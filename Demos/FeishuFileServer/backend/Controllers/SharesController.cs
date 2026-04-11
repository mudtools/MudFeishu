using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 分享控制器
/// 提供文件和文件夹的分享功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SharesController : FeishuControllerBase
{
    private readonly IShareService _shareService;
    private readonly ILogger<SharesController> _logger;

    /// <summary>
    /// 初始化分享控制器
    /// </summary>
    /// <param name="shareService">分享服务</param>
    /// <param name="logger">日志记录器</param>
    public SharesController(IShareService shareService, ILogger<SharesController> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    /// <summary>
    /// 创建分享链接
    /// </summary>
    /// <param name="request">创建分享请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享信息，包含分享码和链接</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ShareResponse>>> CreateShare(
        [FromBody] CreateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ShareResponse>("未登录");
        }

        try
        {
            var result = await _shareService.CreateShareAsync(request, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetShare), new { shareCode = result.ShareCode }, ApiResponse<ShareResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareResponse>(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<ShareResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建分享失败");
            return ServerError<ShareResponse>("创建分享失败");
        }
    }

    /// <summary>
    /// 访问分享（获取分享内容）
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="password">访问密码（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享内容信息</returns>
    [HttpGet("{shareCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShareContentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ShareContentResponse>>> AccessShare(
        string shareCode,
        [FromQuery] string? password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shareService.AccessShareAsync(shareCode, password, cancellationToken);
            return Success(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareContentResponse>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult<ShareContentResponse>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<ShareContentResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "访问分享失败: {ShareCode}", shareCode);
            return ServerError<ShareContentResponse>("访问分享失败");
        }
    }

    /// <summary>
    /// 获取当前用户创建的分享列表
    /// </summary>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享列表</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ShareListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ShareListResponse>>> GetMyShares(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ShareListResponse>("未登录");
        }

        var result = await _shareService.GetUserSharesAsync(userId.Value, page, pageSize, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取分享基本信息（无需密码）
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享基本信息</returns>
    [HttpGet("{shareCode}/info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShareResponse>>> GetShare(
        string shareCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shareService.AccessShareAsync(shareCode, null, cancellationToken);
            return Success(new ShareResponse
            {
                ResourceType = result.ResourceType,
                ResourceName = result.ResourceName,
                AllowDownload = result.AllowDownload
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分享信息失败: {ShareCode}", shareCode);
            return ServerError<ShareResponse>("获取分享信息失败");
        }
    }

    /// <summary>
    /// 下载分享的文件
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="password">访问密码（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
    [HttpGet("{shareCode}/download")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadSharedFile(
        string shareCode,
        [FromQuery] string? password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _shareService.DownloadSharedFileAsync(shareCode, password, cancellationToken);

            var shareContent = await _shareService.AccessShareAsync(shareCode, password, cancellationToken);
            var fileName = shareContent.ResourceName;

            return File(content, "application/octet-stream", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载分享文件失败: {ShareCode}", shareCode);
            return ServerErrorResult("下载分享文件失败");
        }
    }

    /// <summary>
    /// 更新分享设置
    /// </summary>
    /// <param name="shareId">分享ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的分享信息</returns>
    [HttpPut("{shareId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ShareResponse>>> UpdateShare(
        long shareId,
        [FromBody] UpdateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ShareResponse>("未登录");
        }

        try
        {
            var result = await _shareService.UpdateShareAsync(shareId, request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新分享失败: {ShareId}", shareId);
            return ServerError<ShareResponse>("更新分享失败");
        }
    }

    /// <summary>
    /// 删除分享
    /// </summary>
    /// <param name="shareId">分享ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
    [HttpDelete("{shareId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteShare(
        long shareId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedError("未登录");
        }

        try
        {
            await _shareService.DeleteShareAsync(shareId, userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除分享失败: {ShareId}", shareId);
            return ServerErrorResult("删除分享失败");
        }
    }
}
