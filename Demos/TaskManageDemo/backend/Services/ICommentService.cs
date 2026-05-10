// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services;

/// <summary>
/// 评论服务接口
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// 获取任务的评论列表
    /// </summary>
    Task<List<TaskCommentDto>> GetTaskCommentsAsync(int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论详情
    /// </summary>
    Task<TaskCommentDto?> GetCommentByIdAsync(int commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建评论
    /// </summary>
    Task<TaskCommentDto> CreateCommentAsync(int taskId, int userId, CreateCommentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新评论
    /// </summary>
    Task<TaskCommentDto?> UpdateCommentAsync(int commentId, int userId, UpdateCommentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除评论
    /// </summary>
    Task<bool> DeleteCommentAsync(int commentId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除评论（支持管理员删除）
    /// </summary>
    Task<bool> DeleteCommentAsync(int commentId, int userId, bool isAdmin, CancellationToken cancellationToken = default);
}
