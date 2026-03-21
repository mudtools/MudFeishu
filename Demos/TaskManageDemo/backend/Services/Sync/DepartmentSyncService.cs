// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.Sync;

/// <summary>
/// 部门同步服务实现
/// </summary>
public class DepartmentSyncService : IDepartmentSyncService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuDepartmentApi _departmentApi;
    private readonly ILogger<DepartmentSyncService> _logger;

    public DepartmentSyncService(
        TaskManageDbContext dbContext,
        IFeishuDepartmentApi departmentApi,
        ILogger<DepartmentSyncService> logger)
    {
        _dbContext = dbContext;
        _departmentApi = departmentApi;
        _logger = logger;
    }

    public async Task<int> SyncAllDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var syncedCount = 0;
        var departmentId = "0";

        var result = await _departmentApi.GetDepartmentListAsync(departmentId, fetchChild: true, cancellationToken: cancellationToken);

        if (result?.Data?.Items == null || result.Data.Items.Count == 0)
        {
            _logger.LogWarning("未获取到部门数据");
            return 0;
        }

        foreach (var dept in result.Data.Items)
        {
            await SyncSingleDepartmentAsync(dept, cancellationToken);
            syncedCount++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("部门同步完成，共同步 {Count} 个部门", syncedCount);

        return syncedCount;
    }

    public async Task<Department?> SyncDepartmentAsync(string departmentId, CancellationToken cancellationToken = default)
    {
        var result = await _departmentApi.GetDepartmentInfoAsync(departmentId, cancellationToken: cancellationToken);

        if (result?.Data?.Department == null)
        {
            _logger.LogWarning("未找到部门: {DepartmentId}", departmentId);
            return null;
        }

        var dept = await SyncSingleDepartmentAsync(result.Data.Department, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return dept;
    }

    private async Task<Department> SyncSingleDepartmentAsync(FeishuDepartmentData deptData, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(d => d.FeishuId == deptData.DepartmentId, cancellationToken);

        if (department == null)
        {
            department = new Department
            {
                FeishuId = deptData.DepartmentId ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Departments.Add(department);
        }

        department.Name = deptData.Name ?? string.Empty;
        department.ParentDepartmentId = deptData.ParentDepartmentId;
        department.LeaderId = deptData.LeaderUserId;
        department.Order = deptData.DepartmentId == "0" ? 0 : int.TryParse(deptData.DepartmentId, out var order) ? order : 0;
        department.UpdatedAt = DateTime.UtcNow;

        return department;
    }

    public async Task<List<DepartmentTreeNode>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _dbContext.Departments
            .OrderBy(d => d.Order)
            .ToListAsync(cancellationToken);

        var rootDepartments = departments.Where(d => string.IsNullOrEmpty(d.ParentDepartmentId) || d.ParentDepartmentId == "0").ToList();

        return rootDepartments.Select(d => BuildDepartmentTree(d, departments)).ToList();
    }

    private static DepartmentTreeNode BuildDepartmentTree(Department department, List<Department> allDepartments)
    {
        var children = allDepartments
            .Where(d => d.ParentDepartmentId == department.FeishuId)
            .Select(d => BuildDepartmentTree(d, allDepartments))
            .ToList();

        return new DepartmentTreeNode
        {
            Id = department.Id,
            FeishuId = department.FeishuId,
            Name = department.Name,
            LeaderId = department.LeaderId,
            Children = children
        };
    }

    public async Task<List<User>> GetDepartmentUsersAsync(string departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(u => u.DepartmentId == departmentId)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SyncDepartmentUsersAsync(string departmentId, CancellationToken cancellationToken = default)
    {
        var syncedCount = 0;
        var pageToken = string.Empty;

        do
        {
            var result = await _departmentApi.GetDepartmentUsersAsync(
                departmentId,
                pageToken: pageToken,
                pageSize: 50,
                cancellationToken: cancellationToken);

            if (result?.Data?.Items == null || result.Data.Items.Count == 0)
            {
                break;
            }

            foreach (var userData in result.Data.Items)
            {
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.FeishuId == userData.UserId, cancellationToken);

                if (user == null)
                {
                    user = new User
                    {
                        FeishuId = userData.UserId ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Users.Add(user);
                }

                user.Name = userData.Name ?? string.Empty;
                user.DepartmentId = departmentId;
                user.UpdatedAt = DateTime.UtcNow;

                syncedCount++;
            }

            pageToken = result.Data.PageToken ?? string.Empty;

        } while (!string.IsNullOrEmpty(pageToken));

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("部门用户同步完成，共同步 {Count} 个用户", syncedCount);

        return syncedCount;
    }

    public async Task<OrganizationSyncResult> SyncOrganizationAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new OrganizationSyncResult();

        try
        {
            _logger.LogInformation("开始全量同步组织架构...");

            // 1. 同步所有部门
            var departmentIds = new List<string>();
            var pageToken = string.Empty;

            do
            {
                var deptResult = await _departmentApi.GetDepartmentListAsync(
                    "0",
                    fetchChild: true,
                    pageSize: 50,
                    pageToken: pageToken,
                    cancellationToken: cancellationToken);

                if (deptResult?.Data?.Items == null || deptResult.Data.Items.Count == 0)
                {
                    _logger.LogWarning("未获取到部门数据");
                    break;
                }

                foreach (var deptData in deptResult.Data.Items)
                {
                    try
                    {
                        await SyncSingleDepartmentAsync(deptData, cancellationToken);
                        departmentIds.Add(deptData.DepartmentId ?? string.Empty);
                        result.DepartmentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "同步部门失败: {DepartmentId}", deptData.DepartmentId);
                        result.FailedDepartmentCount++;
                        result.Errors.Add($"部门同步失败: {deptData.DepartmentId} - {ex.Message}");
                    }
                }

                pageToken = deptResult.Data.PageToken ?? string.Empty;

            } while (!string.IsNullOrEmpty(pageToken));

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("部门同步完成，成功: {Success}, 失败: {Failed}", result.DepartmentCount, result.FailedDepartmentCount);

            // 2. 同步所有部门的用户
            foreach (var deptId in departmentIds)
            {
                try
                {
                    var userCount = await SyncDepartmentUsersAsync(deptId, cancellationToken);
                    result.UserCount += userCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步部门用户失败: {DepartmentId}", deptId);
                    result.FailedUserCount++;
                    result.Errors.Add($"部门用户同步失败: {deptId} - {ex.Message}");
                }

                // 避免 API 限流
                await Task.Delay(100, cancellationToken);
            }

            _logger.LogInformation("用户同步完成，成功: {Success}, 失败: {Failed}", result.UserCount, result.FailedUserCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全量同步组织架构失败");
            result.Errors.Add($"同步失败: {ex.Message}");
        }

        result.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("组织架构同步完成，耗时: {Duration}ms", result.DurationMs);

        return result;
    }
}
