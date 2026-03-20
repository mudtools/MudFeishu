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

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 根据飞书用户ID获取用户信息
    /// </summary>
    Task<UserInfo?> GetUserByFeishuIdAsync(string feishuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步用户信息
    /// </summary>
    Task<User> SyncUserAsync(string feishuId, string name, string? avatarUrl, string? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    private static readonly Dictionary<string, List<string>> RolePermissions = new()
    {
        [UserRoles.Admin] = new List<string>
        {
            Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate, Permissions.TaskDelete, Permissions.TaskAssign,
            Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate, Permissions.TaskListDelete,
            Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate, Permissions.TemplateDelete,
            Permissions.StatisticsView, Permissions.UserManage, Permissions.DepartmentManage
        },
        [UserRoles.DepartmentAdmin] = new List<string>
        {
            Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate, Permissions.TaskDelete, Permissions.TaskAssign,
            Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate, Permissions.TaskListDelete,
            Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate, Permissions.TemplateDelete,
            Permissions.StatisticsView
        },
        [UserRoles.User] = new List<string>
        {
            Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate,
            Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate,
            Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate,
            Permissions.StatisticsView
        }
    };

    public AuthService(TaskManageDbContext dbContext, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserInfo?> GetUserByFeishuIdAsync(string feishuId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == feishuId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var permissions = await GetUserPermissionsAsync(user.Id.ToString(), cancellationToken);

        return new UserInfo
        {
            UserId = user.Id.ToString(),
            UserName = user.Name,
            FeishuId = user.FeishuId,
            DepartmentId = user.DepartmentId,
            Role = user.Role ?? UserRoles.User,
            Permissions = permissions
        };
    }

    public async Task<User> SyncUserAsync(
        string feishuId,
        string name,
        string? avatarUrl,
        string? departmentId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == feishuId, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                FeishuId = feishuId,
                Name = name,
                AvatarUrl = avatarUrl,
                DepartmentId = departmentId,
                Role = UserRoles.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
        }
        else
        {
            user.Name = name;
            user.AvatarUrl = avatarUrl;
            user.DepartmentId = departmentId;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var id))
        {
            return new List<string>();
        }

        var user = await _dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null)
        {
            return new List<string>();
        }

        var role = user.Role ?? UserRoles.User;
        return RolePermissions.TryGetValue(role, out var permissions)
            ? permissions
            : RolePermissions[UserRoles.User];
    }
}
