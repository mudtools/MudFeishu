// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 权限服务接口
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 获取用户的所有权限（包含角色权限和用户特定权限）
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予用户权限
    /// </summary>
    Task GrantPermissionAsync(int userId, string permission, int? grantedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户权限
    /// </summary>
    Task RevokePermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    List<string> GetRolePermissions(string role);

    /// <summary>
    /// 检查用户是否可以访问指定任务
    /// </summary>
    Task<bool> CanAccessTaskAsync(int userId, int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否可以修改指定任务
    /// </summary>
    Task<bool> CanModifyTaskAsync(int userId, int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化权限数据
    /// </summary>
    Task InitializePermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 为新用户初始化默认权限
    /// </summary>
    Task InitializeDefaultPermissionsAsync(int userId, CancellationToken cancellationToken = default);
}
