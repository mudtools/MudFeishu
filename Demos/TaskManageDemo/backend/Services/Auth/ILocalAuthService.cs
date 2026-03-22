// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

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

    /// <summary>
    /// 飞书登录后完成本地账户绑定
    /// </summary>
    Task<LoginResponse?> RegisterWithFeishuAsync(string tempToken, string username, string password, CancellationToken cancellationToken = default);
}
