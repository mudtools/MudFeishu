// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 批量操作控制器
/// 提供文件的批量删除、移动、复制、下载等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BatchController : FeishuControllerBase
{
    private readonly IBatchService _batchService;
    private readonly IFileService _fileService;
    private readonly ILogger<BatchController> _logger;

    /// <summary>
    /// 初始化批量操作控制器
    /// </summary>
    /// <param name="batchService">批量操作服务</param>
    /// <param name="fileService">文件服务</param>
    /// <param name="logger">日志记录器</param>
    public BatchController(IBatchService batchService, IFileService fileService, ILogger<BatchController> logger)
    {
        _batchService = batchService;
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// 批量删除文件和文件夹
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作结果</returns>
    [HttpPost("delete")]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BatchOperationResponse>>> BatchDelete(
        [FromBody] BatchDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<BatchOperationResponse>("未登录");
        }

        try
        {
            var result = await _batchService.BatchDeleteAsync(request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除失败");
            return ServerError<BatchOperationResponse>("批量删除失败");
        }
    }

    /// <summary>
    /// 批量移动文件和文件夹
    /// </summary>
    /// <param name="request">批量移动请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作结果</returns>
    [HttpPost("move")]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BatchOperationResponse>>> BatchMove(
        [FromBody] BatchMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<BatchOperationResponse>("未登录");
        }

        try
        {
            var result = await _batchService.BatchMoveAsync(request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量移动失败");
            return ServerError<BatchOperationResponse>("批量移动失败");
        }
    }

    /// <summary>
    /// 批量复制文件和文件夹
    /// </summary>
    /// <param name="request">批量复制请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作结果</returns>
    [HttpPost("copy")]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BatchOperationResponse>>> BatchCopy(
        [FromBody] BatchCopyRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<BatchOperationResponse>("未登录");
        }

        try
        {
            var result = await _batchService.BatchCopyAsync(request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量复制失败");
            return ServerError<BatchOperationResponse>("批量复制失败");
        }
    }

    /// <summary>
    /// 批量恢复文件和文件夹（从回收站）
    /// </summary>
    /// <param name="request">批量恢复请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作结果</returns>
    [HttpPost("restore")]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BatchOperationResponse>>> BatchRestore(
        [FromBody] BatchRestoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<BatchOperationResponse>("未登录");
        }

        try
        {
            var result = await _batchService.BatchRestoreAsync(request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量恢复失败");
            return ServerError<BatchOperationResponse>("批量恢复失败");
        }
    }

    /// <summary>
    /// 批量下载文件（打包为ZIP）
    /// </summary>
    /// <param name="request">批量下载请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ZIP文件内容</returns>
    [HttpPost("download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BatchDownload(
        [FromBody] BatchDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedError("未登录");
        }

        try
        {
            var (zipContent, zipFileName) = await _batchService.BatchDownloadAsync(request, userId.Value, cancellationToken);
            return File(zipContent, "application/zip", zipFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量下载失败");
            return ServerErrorResult("批量下载失败");
        }
    }
}
