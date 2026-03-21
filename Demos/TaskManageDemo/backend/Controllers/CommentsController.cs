// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务评论控制器
/// </summary>
[ApiController]
[Route("api/tasks/{taskId}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务的评论列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TaskCommentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TaskCommentDto>>>> GetComments(
        int taskId,
        CancellationToken cancellationToken)
    {
        var comments = await _commentService.GetTaskCommentsAsync(taskId, cancellationToken);
        return Ok(new ApiResponse<List<TaskCommentDto>>
        {
            Success = true,
            Data = comments
        });
    }

    /// <summary>
    /// 获取评论详情
    /// </summary>
    [HttpGet("{commentId}")]
    [ProducesResponseType(typeof(ApiResponse<TaskCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskCommentDto>>> GetComment(
        int taskId,
        int commentId,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.GetCommentByIdAsync(commentId, cancellationToken);
        if (comment is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "评论不存在"
            });
        }

        return Ok(new ApiResponse<TaskCommentDto>
        {
            Success = true,
            Data = comment
        });
    }

    /// <summary>
    /// 创建评论
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskCommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TaskCommentDto>>> CreateComment(
        int taskId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "用户未认证"
            });
        }

        try
        {
            var comment = await _commentService.CreateCommentAsync(taskId, userId.Value, request, cancellationToken);
            return CreatedAtAction(
                nameof(GetComment),
                new { taskId, commentId = comment.Id },
                new ApiResponse<TaskCommentDto>
                {
                    Success = true,
                    Data = comment,
                    Message = "评论创建成功"
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    [HttpPut("{commentId}")]
    [ProducesResponseType(typeof(ApiResponse<TaskCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TaskCommentDto>>> UpdateComment(
        int taskId,
        int commentId,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "用户未认证"
            });
        }

        try
        {
            var comment = await _commentService.UpdateCommentAsync(commentId, userId.Value, request, cancellationToken);
            if (comment is null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "评论不存在"
                });
            }

            return Ok(new ApiResponse<TaskCommentDto>
            {
                Success = true,
                Data = comment,
                Message = "评论更新成功"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpDelete("{commentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(
        int taskId,
        int commentId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _commentService.DeleteCommentAsync(commentId, userId.Value, cancellationToken);
            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "评论不存在"
                });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id");
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
