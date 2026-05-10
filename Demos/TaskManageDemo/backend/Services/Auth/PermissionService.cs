// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 权限服务实现
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<PermissionService> _logger;

    private static readonly Dictionary<string, List<string>> RolePermissions = new()
    {
        [UserRoles.Admin] = new List<string>
        {
            Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate, Permissions.TaskDelete, Permissions.TaskAssign,
            Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate, Permissions.TaskListDelete,
            Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate, Permissions.TemplateDelete,
            Permissions.StatisticsView, Permissions.UserManage, Permissions.DepartmentManage
        },
        [UserRoles.Manager] = new List<string>
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
            Permissions.StatisticsView, Permissions.UserRead
        }
    };

    public PermissionService(TaskManageDbContext dbContext, ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            return new List<string>();
        }

        // 1. 获取角色基础权限
        var role = user.Role ?? UserRoles.User;
        var rolePermissions = GetRolePermissions(role);

        // 2. 获取用户特定权限覆盖
        var userPermissions = await _dbContext.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync(cancellationToken);

        // 3. 合并权限
        var permissions = new HashSet<string>(rolePermissions);

        foreach (var userPermission in userPermissions)
        {
            if (userPermission.IsGranted)
            {
                permissions.Add(userPermission.PermissionCode);
            }
            else
            {
                permissions.Remove(userPermission.PermissionCode);
            }
        }

        return permissions.ToList();
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }

    public async Task GrantPermissionAsync(int userId, string permission, int? grantedBy = null, CancellationToken cancellationToken = default)
    {
        var existingPermission = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionCode == permission, cancellationToken);

        if (existingPermission != null)
        {
            existingPermission.IsGranted = true;
        }
        else
        {
            _dbContext.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionCode = permission,
                IsGranted = true,
                CreatedBy = grantedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("已授予用户 {UserId} 权限 {Permission}", userId, permission);
    }

    public async Task RevokePermissionAsync(int userId, string permission, CancellationToken cancellationToken = default)
    {
        var existingPermission = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionCode == permission, cancellationToken);

        if (existingPermission != null)
        {
            existingPermission.IsGranted = false;
        }
        else
        {
            _dbContext.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionCode = permission,
                IsGranted = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("已撤销用户 {UserId} 权限 {Permission}", userId, permission);
    }

    public List<string> GetRolePermissions(string role)
    {
        return RolePermissions.TryGetValue(role, out var permissions)
            ? permissions
            : RolePermissions[UserRoles.User];
    }

    public async Task<bool> CanAccessTaskAsync(int userId, int taskId, CancellationToken cancellationToken = default)
    {
        // 1. 检查用户是否有任务读取权限
        if (!await HasPermissionAsync(userId, Permissions.TaskRead, cancellationToken))
        {
            return false;
        }

        // 2. 检查用户是否是任务的成员
        var isMember = await _dbContext.TaskMembers
            .AnyAsync(tm => tm.TaskSyncId == taskId && tm.UserId == userId, cancellationToken);

        if (isMember)
        {
            return true;
        }

        // 3. 检查用户是否有管理权限
        if (await HasPermissionAsync(userId, Permissions.TaskDelete, cancellationToken))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CanModifyTaskAsync(int userId, int taskId, CancellationToken cancellationToken = default)
    {
        // 1. 检查用户是否有任务更新权限
        if (!await HasPermissionAsync(userId, Permissions.TaskUpdate, cancellationToken))
        {
            return false;
        }

        // 2. 检查用户是否是任务的创建者或负责人
        var taskMember = await _dbContext.TaskMembers
            .FirstOrDefaultAsync(tm => tm.TaskSyncId == taskId && tm.UserId == userId, cancellationToken);

        if (taskMember != null && (taskMember.Role == TaskMemberRoles.Creator || taskMember.Role == TaskMemberRoles.Assignee))
        {
            return true;
        }

        // 3. 检查用户是否有管理权限
        if (await HasPermissionAsync(userId, Permissions.TaskDelete, cancellationToken))
        {
            return true;
        }

        return false;
    }

    public async Task InitializePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var existingPermissions = await _dbContext.Permissions
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var allPermissions = new[]
        {
            new Permission { Code = Permissions.TaskCreate, Name = "创建任务", Group = "任务管理", Description = "创建新任务的权限" },
            new Permission { Code = Permissions.TaskRead, Name = "查看任务", Group = "任务管理", Description = "查看任务详情的权限" },
            new Permission { Code = Permissions.TaskUpdate, Name = "更新任务", Group = "任务管理", Description = "更新任务信息的权限" },
            new Permission { Code = Permissions.TaskDelete, Name = "删除任务", Group = "任务管理", Description = "删除任务的权限" },
            new Permission { Code = Permissions.TaskAssign, Name = "分配任务", Group = "任务管理", Description = "分配任务给其他人的权限" },

            new Permission { Code = Permissions.TaskListCreate, Name = "创建任务清单", Group = "任务清单", Description = "创建新任务清单的权限" },
            new Permission { Code = Permissions.TaskListRead, Name = "查看任务清单", Group = "任务清单", Description = "查看任务清单的权限" },
            new Permission { Code = Permissions.TaskListUpdate, Name = "更新任务清单", Group = "任务清单", Description = "更新任务清单的权限" },
            new Permission { Code = Permissions.TaskListDelete, Name = "删除任务清单", Group = "任务清单", Description = "删除任务清单的权限" },

            new Permission { Code = Permissions.TemplateCreate, Name = "创建模板", Group = "模板管理", Description = "创建任务模板的权限" },
            new Permission { Code = Permissions.TemplateRead, Name = "查看模板", Group = "模板管理", Description = "查看任务模板的权限" },
            new Permission { Code = Permissions.TemplateUpdate, Name = "更新模板", Group = "模板管理", Description = "更新任务模板的权限" },
            new Permission { Code = Permissions.TemplateDelete, Name = "删除模板", Group = "模板管理", Description = "删除任务模板的权限" },

            new Permission { Code = Permissions.StatisticsView, Name = "查看统计", Group = "统计分析", Description = "查看统计数据的权限" },
            new Permission { Code = Permissions.UserManage, Name = "用户管理", Group = "系统管理", Description = "管理用户的权限" },
            new Permission { Code = Permissions.DepartmentManage, Name = "部门管理", Group = "系统管理", Description = "管理部门的权限" }
        };

        foreach (var permission in allPermissions)
        {
            if (!existingPermissions.Contains(permission.Code))
            {
                _dbContext.Permissions.Add(permission);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("权限数据初始化完成");
    }

    /// <summary>
    /// 为新用户初始化默认权限
    /// </summary>
    public async Task InitializeDefaultPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // 新用户默认使用 User 角色的权限，不需要额外添加数据库记录
        // 这里可以添加一些特定的新用户初始化逻辑，如欢迎通知等
        _logger.LogInformation("为用户 {UserId} 初始化默认权限", userId);
        await Task.CompletedTask;
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _dbContext.Permissions
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Id)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Group = p.Group,
                IsEnabled = p.IsEnabled
            })
            .ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<List<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await GetAllPermissionsAsync(cancellationToken);

        var groups = permissions
            .GroupBy(p => p.Group)
            .Select(g => new PermissionGroupDto
            {
                Group = g.Key,
                Permissions = g.ToList()
            })
            .ToList();

        return groups;
    }

    public async Task<UserPermissionDetailDto?> GetUserPermissionDetailAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.TaskMembers)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return null;

        var userRoles = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        var userPermissions = await _dbContext.UserPermissions
            .Include(up => up.User)
            .Where(up => up.UserId == userId)
            .ToListAsync(cancellationToken);

        var effectivePermissions = await GetUserPermissionsAsync(userId, cancellationToken);

        var grantedPermissions = userPermissions
            .Where(up => up.IsGranted)
            .Select(up => new PermissionDto
            {
                Code = up.PermissionCode,
                Name = up.PermissionCode
            })
            .ToList();

        var revokedPermissions = userPermissions
            .Where(up => !up.IsGranted)
            .Select(up => new PermissionDto
            {
                Code = up.PermissionCode,
                Name = up.PermissionCode
            })
            .ToList();

        return new UserPermissionDetailDto
        {
            UserId = userId,
            UserName = user.Name,
            Roles = userRoles.Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Code = ur.Role.Code,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                IsSystem = ur.Role.IsSystem,
                IsEnabled = ur.Role.IsEnabled,
                Permissions = ur.Role.RolePermissions?
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Code = rp.Permission.Code,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                        Group = rp.Permission.Group,
                        IsEnabled = rp.Permission.IsEnabled
                    })
                    .ToList() ?? new List<PermissionDto>()
            }).ToList(),
            GrantedPermissions = grantedPermissions,
            RevokedPermissions = revokedPermissions,
            EffectivePermissions = effectivePermissions
        };
    }
}
