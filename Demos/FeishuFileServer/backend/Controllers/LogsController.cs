using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 操作日志控制器
/// 提供用户操作日志和资源操作日志的查询功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : FeishuControllerBase
{
    private readonly IOperationLogService _logService;
    private readonly ILogger<LogsController> _logger;

    /// <summary>
    /// 初始化日志控制器
    /// </summary>
    /// <param name="logService">操作日志服务</param>
    /// <param name="logger">日志记录器</param>
    public LogsController(IOperationLogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的操作日志
    /// </summary>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认50</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户操作日志列表</returns>
    [HttpGet("user")]
    [ProducesResponseType(typeof(ApiResponse<UserLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserLogsResponse>>> GetUserLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<UserLogsResponse>("未登录");
        }

        var logs = await _logService.GetUserLogsAsync(userId.Value, page, pageSize, cancellationToken);
        return Success(new UserLogsResponse { Logs = logs, TotalCount = logs.Count });
    }

    /// <summary>
    /// 获取指定资源的操作日志
    /// </summary>
    /// <param name="resourceToken">资源Token（文件或文件夹）</param>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认50</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源操作日志列表</returns>
    [HttpGet("resource/{resourceToken}")]
    [ProducesResponseType(typeof(ApiResponse<UserLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserLogsResponse>>> GetResourceLogs(
        string resourceToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var logs = await _logService.GetResourceLogsAsync(resourceToken, page, pageSize, cancellationToken);
        return Success(new UserLogsResponse { Logs = logs, TotalCount = logs.Count });
    }
}

/// <summary>
/// 用户日志响应
/// </summary>
public class UserLogsResponse
{
    /// <summary>
    /// 日志列表
    /// </summary>
    public List<FeishuFileServer.Models.OperationLog> Logs { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }
}
