// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 任务评论 DTO
/// </summary>
public record TaskCommentDto
{
    /// <summary>
    /// 评论 ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 任务 ID
    /// </summary>
    public int TaskId { get; init; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public int UserId { get; init; }

    /// <summary>
    /// 用户名称
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// 用户头像
    /// </summary>
    public string? UserAvatar { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 父评论 ID
    /// </summary>
    public int? ParentCommentId { get; init; }

    /// <summary>
    /// 回复列表
    /// </summary>
    public List<TaskCommentDto> Replies { get; init; } = [];

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// 创建评论请求
/// </summary>
public record CreateCommentRequest
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "评论内容长度必须在 1-2000 个字符之间")]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 父评论 ID（回复评论时使用）
    /// </summary>
    public int? ParentCommentId { get; init; }
}

/// <summary>
/// 更新评论请求
/// </summary>
public record UpdateCommentRequest
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "评论内容长度必须在 1-2000 个字符之间")]
    public string Content { get; init; } = string.Empty;
}
