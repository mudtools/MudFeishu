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
/// 角色服务接口
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 获取角色列表
    /// </summary>
    Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有启用的角色
    /// </summary>
    Task<List<RoleDto>> GetAllEnabledRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据代码获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建角色
    /// </summary>
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds, int? assignedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    Task<bool> RemoveRolesFromUserAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    Task<List<RoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的用户列表
    /// </summary>
    Task<List<UserDto>> GetRoleUsersAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化默认角色
    /// </summary>
    Task InitializeDefaultRolesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色服务实现
/// </summary>
public class RoleService : IRoleService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<RoleService> _logger;

    public RoleService(TaskManageDbContext dbContext, ILogger<RoleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .AsQueryable();

        if (!string.IsNullOrEmpty(parameters.Keyword))
        {
            var keyword = parameters.Keyword.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(keyword) ||
                r.Code.ToLower().Contains(keyword) ||
                (r.Description != null && r.Description.ToLower().Contains(keyword)));
        }

        if (parameters.IsEnabled.HasValue)
        {
            query = query.Where(r => r.IsEnabled == parameters.IsEnabled.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var roleDtos = new List<RoleDto>();
        foreach (var role in roles)
        {
            var userCount = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == role.Id, cancellationToken);
            roleDtos.Add(MapToDto(role, userCount));
        }

        return (roleDtos, total);
    }

    public async Task<List<RoleDto>> GetAllEnabledRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var roleDtos = new List<RoleDto>();
        foreach (var role in roles)
        {
            var userCount = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == role.Id, cancellationToken);
            roleDtos.Add(MapToDto(role, userCount));
        }

        return roleDtos;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role == null) return null;

        var userCount = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == role.Id, cancellationToken);
        return MapToDto(role, userCount);
    }

    public async Task<RoleDto?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (role == null) return null;

        var userCount = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == role.Id, cancellationToken);
        return MapToDto(role, userCount);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var existingRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Code == request.Code, cancellationToken);

        if (existingRole != null)
        {
            throw new ArgumentException($"角色代码 {request.Code} 已存在");
        }

        var role = new Role
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsSystem = false,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.PermissionIds.Count > 0)
        {
            await AssignPermissionsToRoleAsync(role.Id, request.PermissionIds, cancellationToken);
        }

        _logger.LogInformation("创建角色: {RoleCode} - {RoleName}", role.Code, role.Name);

        return (await GetRoleByIdAsync(role.Id, cancellationToken))!;
    }

    public async Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FindAsync([id], cancellationToken);
        if (role == null) return null;

        if (role.IsSystem)
        {
            throw new InvalidOperationException("系统内置角色不能修改");
        }

        if (!string.IsNullOrEmpty(request.Name))
            role.Name = request.Name;

        if (request.Description != null)
            role.Description = request.Description;

        if (request.IsEnabled.HasValue)
            role.IsEnabled = request.IsEnabled.Value;

        if (request.SortOrder.HasValue)
            role.SortOrder = request.SortOrder.Value;

        role.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.PermissionIds != null)
        {
            await AssignPermissionsToRoleAsync(role.Id, request.PermissionIds, cancellationToken);
        }

        _logger.LogInformation("更新角色: {RoleId}", id);

        return await GetRoleByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FindAsync([id], cancellationToken);
        if (role == null) return false;

        if (role.IsSystem)
        {
            throw new InvalidOperationException("系统内置角色不能删除");
        }

        var hasUsers = await _dbContext.UserRoles.AnyAsync(ur => ur.RoleId == id, cancellationToken);
        if (hasUsers)
        {
            throw new InvalidOperationException("角色下还有用户，不能删除");
        }

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("删除角色: {RoleId} - {RoleCode}", id, role.Code);

        return true;
    }

    public async Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FindAsync([roleId], cancellationToken);
        if (role == null) return false;

        var existingPermissions = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existingPermissions);

        var validPermissionIds = await _dbContext.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in validPermissionIds)
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("为角色 {RoleId} 分配 {Count} 个权限", roleId, validPermissionIds.Count);

        return true;
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var permissions = await _dbContext.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Code = rp.Permission.Code,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Group = rp.Permission.Group,
                IsEnabled = rp.Permission.IsEnabled
            })
            .ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds, int? assignedBy = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null) return false;

        var validRoleIds = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id) && r.IsEnabled)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var existingRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var newRoleIds = validRoleIds.Except(existingRoleIds).ToList();

        foreach (var roleId in newRoleIds)
        {
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                CreatedBy = assignedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("为用户 {UserId} 分配 {Count} 个角色", userId, newRoleIds.Count);

        return true;
    }

    public async Task<bool> RemoveRolesFromUserAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default)
    {
        var userRoles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync(cancellationToken);

        _dbContext.UserRoles.RemoveRange(userRoles);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("移除用户 {UserId} 的 {Count} 个角色", userId, userRoles.Count);

        return true;
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .ToListAsync(cancellationToken);

        return roles.Select(r => MapToDto(r, 0)).ToList();
    }

    public async Task<List<UserDto>> GetRoleUsersAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => new UserDto
            {
                Id = ur.User.Id,
                FeishuId = ur.User.FeishuId,
                Name = ur.User.Name,
                Email = ur.User.Email,
                AvatarUrl = ur.User.AvatarUrl,
                Role = ur.User.Role ?? UserRoles.User,
                IsActive = ur.User.IsActive,
                CreatedAt = ur.User.CreatedAt,
                LastLoginAt = ur.User.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task InitializeDefaultRolesAsync(CancellationToken cancellationToken = default)
    {
        var existingRoles = await _dbContext.Roles
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);

        var defaultRoles = new List<(string Code, string Name, string? Description, int SortOrder, List<string> Permissions)>
        {
            (UserRoles.Admin, "系统管理员", "拥有系统所有权限", 1, new List<string>
            {
                Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate, Permissions.TaskDelete, Permissions.TaskAssign,
                Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate, Permissions.TaskListDelete,
                Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate, Permissions.TemplateDelete,
                Permissions.StatisticsView, Permissions.UserManage, Permissions.DepartmentManage
            }),
            (UserRoles.Manager, "部门经理", "可以管理任务和模板", 2, new List<string>
            {
                Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate, Permissions.TaskDelete, Permissions.TaskAssign,
                Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate, Permissions.TaskListDelete,
                Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate, Permissions.TemplateDelete,
                Permissions.StatisticsView
            }),
            (UserRoles.User, "普通用户", "基本任务操作权限", 3, new List<string>
            {
                Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate,
                Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate,
                Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate,
                Permissions.StatisticsView, Permissions.UserRead
            }),
            (UserRoles.DepartmentAdmin, "部门管理员", "管理部门内的用户和任务", 4, new List<string>
            {
                Permissions.TaskCreate, Permissions.TaskRead, Permissions.TaskUpdate, Permissions.TaskDelete, Permissions.TaskAssign,
                Permissions.TaskListCreate, Permissions.TaskListRead, Permissions.TaskListUpdate, Permissions.TaskListDelete,
                Permissions.TemplateCreate, Permissions.TemplateRead, Permissions.TemplateUpdate, Permissions.TemplateDelete,
                Permissions.StatisticsView, Permissions.UserRead, Permissions.DepartmentManage
            })
        };

        foreach (var (code, name, description, sortOrder, permissionCodes) in defaultRoles)
        {
            Role role;
            if (!existingRoles.Contains(code))
            {
                role = new Role
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    IsSystem = true,
                    IsEnabled = true,
                    SortOrder = sortOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.Roles.Add(role);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                role = await _dbContext.Roles.FirstAsync(r => r.Code == code, cancellationToken);
            }

            var permissionIds = await _dbContext.Permissions
                .Where(p => permissionCodes.Contains(p.Code))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            await AssignPermissionsToRoleAsync(role.Id, permissionIds, cancellationToken);
        }

        _logger.LogInformation("默认角色初始化完成");
    }

    private static RoleDto MapToDto(Role role, int userCount)
    {
        return new RoleDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            IsEnabled = role.IsEnabled,
            SortOrder = role.SortOrder,
            UserCount = userCount,
            CreatedAt = role.CreatedAt,
            Permissions = role.RolePermissions?
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
        };
    }
}
