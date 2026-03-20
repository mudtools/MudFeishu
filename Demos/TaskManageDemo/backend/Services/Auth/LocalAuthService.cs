// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 本地认证服务接口
/// </summary>
public interface ILocalAuthService
{
    /// <summary>
    /// 用户名密码登录
    /// </summary>
    Task<LoginResponse?> PasswordLoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册新用户
    /// </summary>
    Task<LoginResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 绑定飞书账号
    /// </summary>
    Task<BindFeishuResponse> BindFeishuAsync(int userId, string code, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查飞书授权状态
    /// </summary>
    Task<FeishuAuthCheckResponse> CheckFeishuAuthAsync(string code, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化管理员账号
    /// </summary>
    Task InitializeAdminAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证密码
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// 哈希密码
    /// </summary>
    string HashPassword(string password);
}

/// <summary>
/// 本地认证服务实现
/// </summary>
public class LocalAuthService : ILocalAuthService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IFeishuAuthService _feishuAuthService;
    private readonly IPermissionService _permissionService;
    private readonly IStateStorageService _stateStorageService;
    private readonly ILogger<LocalAuthService> _logger;

    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;

    public LocalAuthService(
        TaskManageDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IFeishuAuthService feishuAuthService,
        IPermissionService permissionService,
        IStateStorageService stateStorageService,
        ILogger<LocalAuthService> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _feishuAuthService = feishuAuthService;
        _permissionService = permissionService;
        _stateStorageService = stateStorageService;
        _logger = logger;
    }

    public async Task<LoginResponse?> PasswordLoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("用户不存在: {Username}", username);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("用户已被禁用: {Username}", username);
            return null;
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("密码验证失败: {Username}", username);
            return null;
        }

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        var token = _jwtTokenService.GenerateToken(
            user.Id.ToString(),
            user.Username ?? user.Name,
            user.OpenId ?? string.Empty,
            user.Role ?? UserRoles.User,
            permissions);

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = 3600,
            User = MapToUserDto(user, permissions),
            IsFirstLogin = user.IsFirstLogin
        };
    }

    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            _logger.LogWarning("密码确认不匹配");
            return null;
        }

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("用户名已存在: {Username}", request.Username);
            return null;
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            Name = request.Username,
            Role = UserRoles.User,
            IsActive = true,
            IsFirstLogin = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(request.FeishuCode) && !string.IsNullOrEmpty(request.FeishuState))
        {
            if (_stateStorageService.ValidateState(request.FeishuState))
            {
                _stateStorageService.RemoveState(request.FeishuState);

                try
                {
                    var feishuUser = await _feishuAuthService.GetUserInfoByCodeAsync(request.FeishuCode, cancellationToken);
                    if (feishuUser != null)
                    {
                        user.FeishuId = feishuUser.FeishuId;
                        user.OpenId = feishuUser.OpenId;
                        user.UnionId = feishuUser.UnionId;
                        user.Name = feishuUser.Name;
                        user.EnglishName = feishuUser.EnglishName;
                        user.AvatarUrl = feishuUser.AvatarUrl;
                        user.Email = feishuUser.Email;
                        user.Mobile = feishuUser.Mobile;
                        user.DepartmentId = feishuUser.DepartmentId;
                        user.IsFeishuBound = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取飞书用户信息失败");
                }
            }
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        var token = _jwtTokenService.GenerateToken(
            user.Id.ToString(),
            user.Username,
            user.OpenId ?? string.Empty,
            user.Role ?? UserRoles.User,
            permissions);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = 3600,
            User = MapToUserDto(user, permissions),
            IsFirstLogin = false
        };
    }

    public async Task<BindFeishuResponse> BindFeishuAsync(int userId, string code, string state, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return new BindFeishuResponse
            {
                Success = false,
                Message = "用户不存在"
            };
        }

        if (user.IsFeishuBound)
        {
            return new BindFeishuResponse
            {
                Success = true,
                FeishuName = user.Name,
                FeishuAvatar = user.AvatarUrl,
                Message = "已绑定飞书账号"
            };
        }

        if (!_stateStorageService.ValidateState(state))
        {
            return new BindFeishuResponse
            {
                Success = false,
                Message = "State验证失败"
            };
        }

        _stateStorageService.RemoveState(state);

        try
        {
            var feishuUser = await _feishuAuthService.GetUserInfoByCodeAsync(code, cancellationToken);
            if (feishuUser == null)
            {
                return new BindFeishuResponse
                {
                    Success = false,
                    Message = "获取飞书用户信息失败"
                };
            }

            var existingBinding = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.FeishuId == feishuUser.FeishuId && u.Id != userId, cancellationToken);

            if (existingBinding != null)
            {
                return new BindFeishuResponse
                {
                    Success = false,
                    Message = "该飞书账号已被其他用户绑定"
                };
            }

            user.FeishuId = feishuUser.FeishuId;
            user.OpenId = feishuUser.OpenId;
            user.UnionId = feishuUser.UnionId;
            user.Name = feishuUser.Name;
            user.EnglishName = feishuUser.EnglishName;
            user.AvatarUrl = feishuUser.AvatarUrl;
            user.Email = feishuUser.Email;
            user.Mobile = feishuUser.Mobile;
            user.DepartmentId = feishuUser.DepartmentId;
            user.IsFeishuBound = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new BindFeishuResponse
            {
                Success = true,
                FeishuName = feishuUser.Name,
                FeishuAvatar = feishuUser.AvatarUrl,
                Message = "绑定成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "绑定飞书账号失败");
            return new BindFeishuResponse
            {
                Success = false,
                Message = "绑定飞书账号失败"
            };
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return false;
        }

        if (!VerifyPassword(oldPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = HashPassword(newPassword);
        user.IsFirstLogin = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FeishuAuthCheckResponse> CheckFeishuAuthAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        if (!_stateStorageService.ValidateState(state))
        {
            return new FeishuAuthCheckResponse
            {
                UserExists = false,
                IsFeishuBound = false
            };
        }

        _stateStorageService.RemoveState(state);

        try
        {
            var feishuUser = await _feishuAuthService.GetUserInfoByCodeAsync(code, cancellationToken);
            if (feishuUser == null)
            {
                return new FeishuAuthCheckResponse
                {
                    UserExists = false,
                    IsFeishuBound = false
                };
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.FeishuId == feishuUser.FeishuId, cancellationToken);

            if (existingUser != null)
            {
                var permissions = await _permissionService.GetUserPermissionsAsync(existingUser.Id, cancellationToken);
                var token = _jwtTokenService.GenerateToken(
                    existingUser.Id.ToString(),
                    existingUser.Username ?? existingUser.Name,
                    existingUser.OpenId ?? string.Empty,
                    existingUser.Role ?? UserRoles.User,
                    permissions);

                existingUser.LastLoginAt = DateTime.UtcNow;
                existingUser.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new FeishuAuthCheckResponse
                {
                    UserExists = true,
                    IsFeishuBound = true,
                    FeishuUser = new FeishuUserInfo
                    {
                        FeishuId = feishuUser.FeishuId,
                        OpenId = feishuUser.OpenId,
                        Name = feishuUser.Name,
                        EnglishName = feishuUser.EnglishName,
                        AvatarUrl = feishuUser.AvatarUrl,
                        Email = feishuUser.Email,
                        Mobile = feishuUser.Mobile,
                        DepartmentId = feishuUser.DepartmentId
                    },
                    TempToken = token
                };
            }

            var tempToken = _jwtTokenService.GenerateTempToken(feishuUser.FeishuId, feishuUser.Name);

            return new FeishuAuthCheckResponse
            {
                UserExists = false,
                IsFeishuBound = false,
                FeishuUser = new FeishuUserInfo
                {
                    FeishuId = feishuUser.FeishuId,
                    OpenId = feishuUser.OpenId,
                    Name = feishuUser.Name,
                    EnglishName = feishuUser.EnglishName,
                    AvatarUrl = feishuUser.AvatarUrl,
                    Email = feishuUser.Email,
                    Mobile = feishuUser.Mobile,
                    DepartmentId = feishuUser.DepartmentId
                },
                TempToken = tempToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查飞书授权状态失败");
            return new FeishuAuthCheckResponse
            {
                UserExists = false,
                IsFeishuBound = false
            };
        }
    }

    public async Task InitializeAdminAccountAsync(CancellationToken cancellationToken = default)
    {
        var adminUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);

        if (adminUser == null)
        {
            adminUser = new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                Name = "系统管理员",
                Role = UserRoles.Admin,
                IsActive = true,
                IsFirstLogin = true,
                FeishuId = $"local_admin_{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(adminUser);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Code == "admin", cancellationToken);
            if (adminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.UserRoles.Add(userRole);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("管理员账号初始化完成");
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var key = Convert.FromBase64String(parts[2]);

        using var algorithm = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var keyToCheck = algorithm.GetBytes(KeySize);

        return keyToCheck.SequenceEqual(key);
    }

    public string HashPassword(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256);
        var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        var salt = Convert.ToBase64String(algorithm.Salt);

        return $"{Iterations}.{salt}.{key}";
    }

    private static UserDto MapToUserDto(User user, List<string> permissions)
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
            Role = user.Role ?? UserRoles.User,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
