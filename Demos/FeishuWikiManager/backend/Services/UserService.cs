// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Data;
using FeishuWikiManager.Models;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels;

namespace FeishuWikiManager.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserService> _logger;
    private readonly IFeishuUserV3User _feishuUserApi;
    private readonly IFeishuAppManager _feishuAppManager;

    public UserService(AppDbContext dbContext, ILogger<UserService> logger, IFeishuUserV3User feishuUserApi, IFeishuAppManager feishuAppManager)
    {
        _dbContext = dbContext;
        _logger = logger;
        _feishuUserApi = feishuUserApi;
        _feishuAppManager = feishuAppManager;
    }

    public async Task<string> GetOrCreateUserAsync(string openId, string unionId, string name, string? avatar, string? email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId);

        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.Name = name;
            user.Avatar = avatar;
            user.Email = email;
            user.UnionId = unionId;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("用户登录更新: {UserId}, {Name}", user.Id, user.Name);
            return user.Id;
        }

        user = new User
        {
            OpenId = openId,
            UnionId = unionId,
            Name = name,
            Avatar = avatar,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("创建新用户: {UserId}, {Name}", user.Id, user.Name);
        return user.Id;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByOpenIdAsync(string openId)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId);
    }

    public async Task UpdateUserTokenAsync(string userId, string? accessToken, string? refreshToken, DateTime? expiresAt)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.FeishuAccessToken = accessToken;
            user.FeishuRefreshToken = refreshToken;
            user.TokenExpiresAt = expiresAt;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("更新用户Token: {UserId}", userId);
        }
    }

    public async Task ClearUserTokenAsync(string openId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId);
        if (user != null)
        {
            user.FeishuAccessToken = null;
            user.FeishuRefreshToken = null;
            user.TokenExpiresAt = null;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("清除用户Token: {UserId}", user.Id);
        }
    }

    public async Task<UserPreference?> GetPreferenceAsync(string userId, string key)
    {
        return await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Key == key);
    }

    public async Task SetPreferenceAsync(string userId, string key, string? value)
    {
        var preference = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Key == key);

        if (preference != null)
        {
            preference.Value = value;
            preference.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            preference = new UserPreference
            {
                UserId = userId,
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.UserPreferences.Add(preference);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<GetUserDataResult?> GetDetailedUserInfoFromFeishuAsync(string openId)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {OpenId}", openId);
                return null;
            }

            // 检查 Token 是否有效
            if (string.IsNullOrEmpty(user.FeishuAccessToken))
            {
                _logger.LogWarning("用户Token为空: {OpenId}", openId);
                return null;
            }

            // 调用飞书 API 获取详细用户信息
            var result = await _feishuUserApi.GetUserInfoAsync();

            if (result?.Code != 0 || result.Data == null)
            {
                _logger.LogError("从飞书获取用户信息失败: {Message}", result?.Msg ?? "未知错误");
                return null;
            }

            _logger.LogInformation("成功获取用户详细信息: {OpenId}", openId);
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取详细用户信息失败: {OpenId}", openId);
            return null;
        }
    }
}
