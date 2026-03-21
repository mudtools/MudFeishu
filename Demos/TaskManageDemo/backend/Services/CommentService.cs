// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services;

/// <summary>
/// 评论服务实现
/// </summary>
public class CommentService : ICommentService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<CommentService> _logger;

    public CommentService(TaskManageDbContext dbContext, ILogger<CommentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<TaskCommentDto>> GetTaskCommentsAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var comments = await _dbContext.TaskComments
            .Include(c => c.User)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Where(c => c.TaskId == taskId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(MapToDto).ToList();
    }

    public async Task<TaskCommentDto?> GetCommentByIdAsync(int commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.TaskComments
            .Include(c => c.User)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        return comment is null ? null : MapToDto(comment);
    }

    public async Task<TaskCommentDto> CreateCommentAsync(int taskId, int userId, CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        var taskExists = await _dbContext.Tasks.AnyAsync(t => t.Id == taskId, cancellationToken);
        if (!taskExists)
        {
            throw new InvalidOperationException($"任务不存在: {taskId}");
        }

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _dbContext.TaskComments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value && c.TaskId == taskId, cancellationToken);
            if (!parentExists)
            {
                throw new InvalidOperationException($"父评论不存在或不属于该任务: {request.ParentCommentId}");
            }
        }

        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("创建评论: TaskId={TaskId}, UserId={UserId}, CommentId={CommentId}",
            taskId, userId, comment.Id);

        var createdComment = await _dbContext.TaskComments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id, cancellationToken);

        return MapToDto(createdComment);
    }

    public async Task<TaskCommentDto?> UpdateCommentAsync(int commentId, int userId, UpdateCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.TaskComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return null;
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("只能修改自己的评论");
        }

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("更新评论: CommentId={CommentId}, UserId={UserId}", commentId, userId);

        return MapToDto(comment);
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.TaskComments
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return false;
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("只能删除自己的评论");
        }

        if (comment.Replies.Count > 0)
        {
            comment.Content = "[评论已删除]";
            comment.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.TaskComments.Remove(comment);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("删除评论: CommentId={CommentId}, UserId={UserId}", commentId, userId);

        return true;
    }

    private static TaskCommentDto MapToDto(TaskComment comment)
    {
        return new TaskCommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = comment.User?.Name ?? "未知用户",
            UserAvatar = comment.User?.AvatarUrl,
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            Replies = comment.Replies?.Select(MapToDto).ToList() ?? [],
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
