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
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户列表
    /// </summary>
    Task<(List<UserDto> users, int total)> GetUsersAsync(UserQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据飞书ID获取用户
    /// </summary>
    Task<UserDto?> GetUserByFeishuIdAsync(string feishuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户（软删除）
    /// </summary>
    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 激活用户
    /// </summary>
    Task<bool> ActivateUserAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用用户
    /// </summary>
    Task<bool> DeactivateUserAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    Task<CurrentUserInfo?> GetCurrentUserAsync(string feishuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 OpenId 获取用户
    /// </summary>
    Task<UserDto?> GetUserByOpenIdAsync(string openId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除用户令牌
    /// </summary>
    Task ClearUserTokenAsync(string openId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步飞书用户信息到本地
    /// </summary>
    Task<UserDto?> SyncFeishuUserAsync(string feishuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户统计数据
    /// </summary>
    Task<UserStatisticsDto> GetUserStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        TaskManageDbContext dbContext,
        IPermissionService permissionService,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<(List<UserDto> users, int total)> GetUsersAsync(UserQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();

        // 关键词搜索
        if (!string.IsNullOrEmpty(parameters.Keyword))
        {
            var keyword = parameters.Keyword.ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(keyword) ||
                (u.Email != null && u.Email.ToLower().Contains(keyword)) ||
                (u.Mobile != null && u.Mobile.Contains(keyword)));
        }

        // 角色筛选
        if (!string.IsNullOrEmpty(parameters.Role))
        {
            query = query.Where(u => u.Role == parameters.Role);
        }

        // 部门筛选
        if (!string.IsNullOrEmpty(parameters.DepartmentId))
        {
            query = query.Where(u => u.DepartmentId == parameters.DepartmentId);
        }

        // 状态筛选
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == parameters.IsActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FeishuId = u.FeishuId,
                OpenId = u.OpenId,
                UnionId = u.UnionId,
                Name = u.Name,
                EnglishName = u.EnglishName,
                Email = u.Email,
                Mobile = u.Mobile,
                AvatarUrl = u.AvatarUrl,
                DepartmentId = u.DepartmentId,
                Position = u.Position,
                Role = u.Role ?? UserRoles.User,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        return (users, total);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByFeishuIdAsync(string feishuId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == feishuId, cancellationToken);
        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null) return null;

        if (!string.IsNullOrEmpty(request.Name))
            user.Name = request.Name;

        if (!string.IsNullOrEmpty(request.EnglishName))
            user.EnglishName = request.EnglishName;

        if (!string.IsNullOrEmpty(request.Mobile))
            user.Mobile = request.Mobile;

        if (!string.IsNullOrEmpty(request.Role))
        {
            // 验证角色是否有效
            if (request.Role != UserRoles.Admin && request.Role != UserRoles.Manager && request.Role != UserRoles.User)
            {
                throw new ArgumentException("无效的角色");
            }
            user.Role = request.Role;
        }

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("更新用户信息: {UserId}", id);

        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null) return false;

        // 软删除：禁用用户
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("禁用用户: {UserId}", id);
        return true;
    }

    public async Task<bool> ActivateUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null) return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("激活用户: {UserId}", id);
        return true;
    }

    public async Task<bool> DeactivateUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("禁用用户: {UserId}", id);
        return true;
    }

    public async Task<CurrentUserInfo?> GetCurrentUserAsync(string feishuId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == feishuId || u.OpenId == feishuId, cancellationToken);

        if (user == null) return null;

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        return new CurrentUserInfo
        {
            Id = user.Id,
            FeishuId = user.FeishuId,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role ?? UserRoles.User,
            Permissions = permissions,
            DepartmentName = user.DepartmentId, // 这里可以从部门表获取名称
            Position = user.Position
        };
    }

    public async Task<UserDto?> GetUserByOpenIdAsync(string openId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.OpenId == openId, cancellationToken);
        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task ClearUserTokenAsync(string openId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId, cancellationToken);
        if (user != null)
        {
            user.FeishuAccessToken = null;
            user.FeishuRefreshToken = null;
            user.TokenExpiresAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("清除用户Token: {UserId}", user.Id);
        }
    }

    public async Task<UserDto?> SyncFeishuUserAsync(string feishuId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == feishuId, cancellationToken);

        if (user == null) return null;

        // 标记需要同步
        user.LastSyncedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("同步飞书用户信息: {UserId}", user.Id);

        return MapToDto(user);
    }

    public async Task<UserStatisticsDto> GetUserStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive, cancellationToken);
        var adminUsers = await _dbContext.Users.CountAsync(u => u.Role == UserRoles.Admin, cancellationToken);
        var newUsersThisMonth = await _dbContext.Users
            .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1), cancellationToken);

        return new UserStatisticsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            AdminUsers = adminUsers,
            NewUsersThisMonth = newUsersThisMonth
        };
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FeishuId = user.FeishuId,
            OpenId = user.OpenId,
            UnionId = user.UnionId,
            Name = user.Name,
            EnglishName = user.EnglishName,
            Email = user.Email,
            Mobile = user.Mobile,
            AvatarUrl = user.AvatarUrl,
            DepartmentId = user.DepartmentId,
            Position = user.Position,
            Role = user.Role ?? UserRoles.User,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}

/// <summary>
/// 用户统计数据
/// </summary>
public class UserStatisticsDto
{
    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// 活跃用户数
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// 管理员数量
    /// </summary>
    public int AdminUsers { get; set; }

    /// <summary>
    /// 本月新增用户数
    /// </summary>
    public int NewUsersThisMonth { get; set; }
}
