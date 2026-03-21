// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        TaskManageDbContext dbContext,
        IPermissionService permissionService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
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

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

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
        if (!int.TryParse(userId, out var id))
        {
            return false;
        }

        return await _permissionService.HasPermissionAsync(id, permission, cancellationToken);
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var id))
        {
            return new List<string>();
        }

        return await _permissionService.GetUserPermissionsAsync(id, cancellationToken);
    }
}
