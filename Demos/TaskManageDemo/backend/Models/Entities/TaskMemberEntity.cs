// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.Entities;

/// <summary>
/// 任务成员角色常量
/// </summary>
public static class TaskMemberRoles
{
    /// <summary>
    /// 创建者
    /// </summary>
    public const string Creator = "creator";

    /// <summary>
    /// 负责人
    /// </summary>
    public const string Assignee = "assignee";

    /// <summary>
    /// 关注人
    /// </summary>
    public const string Follower = "follower";
}

/// <summary>
/// 任务成员实体 - 存储任务的负责人和关注人
/// </summary>
public class TaskMemberEntity
{
    /// <summary>
    /// 本地数据库ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的任务ID
    /// </summary>
    public int TaskSyncId { get; set; }

    /// <summary>
    /// 关联的任务
    /// </summary>
    public TaskSync? Task { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// 成员角色 (assignee: 负责人, follower: 关注人, creator: 创建者)
    /// </summary>
    public string Role { get; set; } = TaskMemberRoles.Assignee;

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
