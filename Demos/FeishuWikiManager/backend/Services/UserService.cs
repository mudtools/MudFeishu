using FeishuWikiManager.Data;
using FeishuWikiManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FeishuWikiManager.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
}
