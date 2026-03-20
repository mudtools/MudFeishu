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
/// 部门同步服务接口
/// </summary>
public interface IDepartmentSyncService
{
    /// <summary>
    /// 同步所有部门
    /// </summary>
    Task<int> SyncAllDepartmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步单个部门
    /// </summary>
    Task<Department?> SyncDepartmentAsync(string departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门树
    /// </summary>
    Task<List<DepartmentTreeNode>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门下的用户
    /// </summary>
    Task<List<User>> GetDepartmentUsersAsync(string departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步部门用户
    /// </summary>
    Task<int> SyncDepartmentUsersAsync(string departmentId, CancellationToken cancellationToken = default);
}

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
        department.ParentId = deptData.ParentDepartmentId;
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

        var rootDepartments = departments.Where(d => string.IsNullOrEmpty(d.ParentId) || d.ParentId == "0").ToList();

        return rootDepartments.Select(d => BuildDepartmentTree(d, departments)).ToList();
    }

    private static DepartmentTreeNode BuildDepartmentTree(Department department, List<Department> allDepartments)
    {
        var children = allDepartments
            .Where(d => d.ParentId == department.FeishuId)
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
}

/// <summary>
/// 部门树节点
/// </summary>
public class DepartmentTreeNode
{
    /// <summary>
    /// 本地ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书部门ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 部门负责人ID
    /// </summary>
    public string? LeaderId { get; set; }

    /// <summary>
    /// 子部门
    /// </summary>
    public List<DepartmentTreeNode> Children { get; set; } = new();
}

/// <summary>
/// 飞书部门API接口（占位，实际由Mud.Feishu提供）
/// </summary>
public interface IFeishuDepartmentApi
{
    Task<GetDepartmentListResponse?> GetDepartmentListAsync(
        string departmentId,
        bool fetchChild = false,
        int pageSize = 50,
        string? pageToken = null,
        CancellationToken cancellationToken = default);

    Task<GetDepartmentInfoResponse?> GetDepartmentInfoAsync(
        string departmentId,
        CancellationToken cancellationToken = default);

    Task<GetDepartmentUsersResponse?> GetDepartmentUsersAsync(
        string departmentId,
        int pageSize = 50,
        string? pageToken = null,
        CancellationToken cancellationToken = default);
}

#pragma warning disable CS8618
public class GetDepartmentListResponse
{
    public int Code { get; set; }
    public GetDepartmentListData? Data { get; set; }
}

public class GetDepartmentListData
{
    public List<FeishuDepartmentData>? Items { get; set; }
    public string? PageToken { get; set; }
    public bool HasMore { get; set; }
}

public class FeishuDepartmentData
{
    public string? DepartmentId { get; set; }
    public string? Name { get; set; }
    public string? ParentDepartmentId { get; set; }
    public string? LeaderUserId { get; set; }
}

public class GetDepartmentInfoResponse
{
    public int Code { get; set; }
    public GetDepartmentInfoData? Data { get; set; }
}

public class GetDepartmentInfoData
{
    public FeishuDepartmentData? Department { get; set; }
}

public class GetDepartmentUsersResponse
{
    public int Code { get; set; }
    public GetDepartmentUsersData? Data { get; set; }
}

public class GetDepartmentUsersData
{
    public List<FeishuUserData>? Items { get; set; }
    public string? PageToken { get; set; }
    public bool HasMore { get; set; }
}

public class FeishuUserData
{
    public string? UserId { get; set; }
    public string? Name { get; set; }
}
#pragma warning restore CS8618
