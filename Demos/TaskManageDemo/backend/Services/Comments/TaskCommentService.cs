// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.Comments;

/// <summary>
/// 任务评论服务实现
/// </summary>
public class TaskCommentService : ITaskCommentService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<TaskCommentService> _logger;

    public TaskCommentService(
        TaskManageDbContext dbContext,
        ILogger<TaskCommentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务的评论列表
    /// </summary>
    public async Task<List<TaskComment>> GetCommentsByTaskIdAsync(
        int taskId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        return await _dbContext.Set<TaskComment>()
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId && !c.IsDeleted && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 创建评论
    /// </summary>
    public async Task<TaskComment> CreateCommentAsync(
        int taskId,
        int userId,
        string content,
        int? parentCommentId = null,
        CancellationToken cancellationToken = default)
    {
        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = content,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Set<TaskComment>().Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("创建评论: CommentId={CommentId}, TaskId={TaskId}", 
            comment.Id, taskId);

        return comment;
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    public async Task<TaskComment?> UpdateCommentAsync(
        int commentId,
        int userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.Set<TaskComment>()
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment == null || comment.UserId != userId)
        {
            return null;
        }

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("更新评论: CommentId={CommentId}", commentId);

        return comment;
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    public async Task<bool> DeleteCommentAsync(
        int commentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.Set<TaskComment>()
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment == null || comment.UserId != userId)
        {
            return false;
        }

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("删除评论: CommentId={CommentId}", commentId);

        return true;
    }

    /// <summary>
    /// 获取评论数量
    /// </summary>
    public async Task<int> GetCommentCountAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TaskComment>()
            .CountAsync(c => c.TaskId == taskId && !c.IsDeleted, cancellationToken);
    }
}

