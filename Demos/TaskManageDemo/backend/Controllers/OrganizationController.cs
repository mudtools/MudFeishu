// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Sync;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 组织架构控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrganizationController : BaseController
{
    private readonly IDepartmentSyncService _departmentSyncService;
    private readonly ILogger<OrganizationController> _logger;

    /// <summary>
    /// 初始化组织架构控制器
    /// </summary>
    public OrganizationController(
        IDepartmentSyncService departmentSyncService,
        ILogger<OrganizationController> logger)
    {
        _departmentSyncService = departmentSyncService;
        _logger = logger;
    }

    /// <summary>
    /// 全量同步组织架构
    /// </summary>
    [HttpPost("sync")]
    [RequirePermission("department:manage")]
    public async Task<ActionResult<ApiResponse<OrganizationSyncResult>>> SyncOrganization(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始全量同步组织架构");
            var result = await _departmentSyncService.SyncOrganizationAsync(cancellationToken);

            return Success(result, "组织架构同步完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "组织架构同步失败");
            return Fail<OrganizationSyncResult>($"组织架构同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 同步所有部门
    /// </summary>
    [HttpPost("sync/departments")]
    [RequirePermission("department:manage")]
    public async Task<ActionResult<ApiResponse<int>>> SyncDepartments(
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await _departmentSyncService.SyncAllDepartmentsAsync(cancellationToken);
            return Success(count, $"成功同步 {count} 个部门");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "部门同步失败");
            return Fail<int>($"部门同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 同步部门用户
    /// </summary>
    [HttpPost("sync/departments/{departmentId}/users")]
    [RequirePermission("department:manage")]
    public async Task<ActionResult<ApiResponse<int>>> SyncDepartmentUsers(
        string departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await _departmentSyncService.SyncDepartmentUsersAsync(departmentId, cancellationToken);
            return Success(count, $"成功同步 {count} 个用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "部门用户同步失败，DepartmentId: {DepartmentId}", departmentId);
            return Fail<int>($"部门用户同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取部门树
    /// </summary>
    [HttpGet("departments/tree")]
    public async Task<ActionResult<ApiResponse<List<DepartmentTreeNode>>>> GetDepartmentTree(
        CancellationToken cancellationToken)
    {
        try
        {
            var tree = await _departmentSyncService.GetDepartmentTreeAsync(cancellationToken);
            return Success(tree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门树失败");
            return Fail<List<DepartmentTreeNode>>($"获取部门树失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取部门用户
    /// </summary>
    [HttpGet("departments/{departmentId}/users")]
    public async Task<ActionResult<ApiResponse<List<User>>>> GetDepartmentUsers(
        string departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await _departmentSyncService.GetDepartmentUsersAsync(departmentId, cancellationToken);
            return Success(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门用户失败，DepartmentId: {DepartmentId}", departmentId);
            return Fail<List<User>>($"获取部门用户失败: {ex.Message}");
        }
    }
}
