// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.Entities;

/// <summary>
/// 用户实体 - 存储从飞书同步的用户数据
/// </summary>
public class User
{
    /// <summary>
    /// 本地数据库ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户名（用于本地登录）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码哈希（用于本地登录）
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// 飞书用户ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 用户Open ID
    /// </summary>
    public string? OpenId { get; set; }

    /// <summary>
    /// 用户Union ID
    /// </summary>
    public string? UnionId { get; set; }

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 英文名
    /// </summary>
    public string? EnglishName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 所属部门ID
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 是否已绑定飞书
    /// </summary>
    public bool IsFeishuBound { get; set; }

    /// <summary>
    /// 是否首次登录（需要修改密码或绑定飞书）
    /// </summary>
    public bool IsFirstLogin { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后同步时间
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 飞书访问令牌
    /// </summary>
    public string? FeishuAccessToken { get; set; }

    /// <summary>
    /// 飞书刷新令牌
    /// </summary>
    public string? FeishuRefreshToken { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// 租户密钥
    /// </summary>
    public string? TenantKey { get; set; }

    /// <summary>
    /// 用户参与的任务
    /// </summary>
    public ICollection<TaskMemberEntity> TaskMembers { get; set; } = new List<TaskMemberEntity>();

    /// <summary>
    /// 用户角色关联
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
