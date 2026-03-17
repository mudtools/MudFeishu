using FeishuWikiManager.Models;

namespace FeishuWikiManager.Services;

public interface IUserService
{
    Task<string> GetOrCreateUserAsync(string openId, string unionId, string name, string? avatar, string? email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByOpenIdAsync(string openId);
    Task UpdateUserTokenAsync(string userId, string? accessToken, string? refreshToken, DateTime? expiresAt);
    Task ClearUserTokenAsync(string openId);
    Task<UserPreference?> GetPreferenceAsync(string userId, string key);
    Task SetPreferenceAsync(string userId, string key, string? value);
}
