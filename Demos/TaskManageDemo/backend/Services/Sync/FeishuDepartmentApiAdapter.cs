// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu;

namespace TaskManageDemo.Backend.Services.Sync;

/// <summary>
/// 飞书部门API适配器，将Mud.Feishu的IFeishuTenantV3Departments和IFeishuTenantV3User适配到本地接口
/// </summary>
public class FeishuDepartmentApiAdapter : IFeishuDepartmentApi
{
    private readonly IFeishuTenantV3Departments _departmentsApi;
    private readonly IFeishuTenantV3User _userApi;
    private readonly ILogger<FeishuDepartmentApiAdapter> _logger;

    public FeishuDepartmentApiAdapter(
        IFeishuTenantV3Departments departmentsApi,
        IFeishuTenantV3User userApi,
        ILogger<FeishuDepartmentApiAdapter> logger)
    {
        _departmentsApi = departmentsApi;
        _userApi = userApi;
        _logger = logger;
    }

    public async Task<GetDepartmentListResponse?> GetDepartmentListAsync(
        string departmentId,
        bool fetchChild = false,
        int pageSize = 50,
        string? pageToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _departmentsApi.GetDepartmentsByParentIdAsync(
                departmentId,
                fetch_child: fetchChild,
                page_size: pageSize,
                page_token: pageToken,
                cancellationToken: cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new GetDepartmentListResponse
            {
                Code = result.Code,
                Data = result.Data != null ? new GetDepartmentListData
                {
                    Items = result.Data.Items?.Select(item => new FeishuDepartmentData
                    {
                        DepartmentId = item.DepartmentId,
                        Name = item.Name,
                        ParentDepartmentId = item.ParentDepartmentId,
                        LeaderUserId = item.LeaderUserId
                    }).ToList(),
                    PageToken = result.Data.PageToken,
                    HasMore = result.Data.HasMore
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门列表失败，DepartmentId: {DepartmentId}", departmentId);
            return null;
        }
    }

    public async Task<GetDepartmentInfoResponse?> GetDepartmentInfoAsync(
        string departmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _departmentsApi.GetDepartmentInfoByIdAsync(
                departmentId,
                cancellationToken: cancellationToken);

            if (result == null || result.Data?.Department == null)
            {
                return null;
            }

            var dept = result.Data.Department;
            return new GetDepartmentInfoResponse
            {
                Code = result.Code,
                Data = new GetDepartmentInfoData
                {
                    Department = new FeishuDepartmentData
                    {
                        DepartmentId = dept.DepartmentId,
                        Name = dept.Name,
                        ParentDepartmentId = dept.ParentDepartmentId,
                        LeaderUserId = dept.LeaderUserId
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门信息失败，DepartmentId: {DepartmentId}", departmentId);
            return null;
        }
    }

    public async Task<GetDepartmentUsersResponse?> GetDepartmentUsersAsync(
        string departmentId,
        int pageSize = 50,
        string? pageToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userApi.GetUserByDepartmentIdAsync(
                departmentId,
                page_size: pageSize,
                page_token: pageToken,
                cancellationToken: cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new GetDepartmentUsersResponse
            {
                Code = result.Code,
                Data = result.Data != null ? new GetDepartmentUsersData
                {
                    Items = result.Data.Items?.Select(item => new FeishuUserData
                    {
                        UserId = item.UserId,
                        Name = item.Name
                    }).ToList(),
                    PageToken = null,
                    HasMore = false
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门用户列表失败，DepartmentId: {DepartmentId}", departmentId);
            return null;
        }
    }
}
