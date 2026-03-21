// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

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
