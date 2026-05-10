// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu;
using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.ApprovalQuery;

namespace TaskManageDemo.Backend.Services.Approval;

/// <summary>
/// 飞书审批API适配器，将Mud.Feishu的IFeishuTenantV4Approval适配到本地接口
/// </summary>
public class FeishuApprovalAdapter : IFeishuApproval
{
    private readonly IFeishuTenantV4Approval _approvalApi;
    private readonly IFeishuTenantV4ApprovalQuery _approvalQueryApi;
    private readonly ILogger<FeishuApprovalAdapter> _logger;

    public FeishuApprovalAdapter(
        IFeishuTenantV4Approval approvalApi,
        IFeishuTenantV4ApprovalQuery approvalQueryApi,
        ILogger<FeishuApprovalAdapter> logger)
    {
        _approvalApi = approvalApi;
        _approvalQueryApi = approvalQueryApi;
        _logger = logger;
    }

    public async Task<CreateApprovalInstanceResponse?> CreateApprovalInstanceAsync(
        CreateApprovalInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var feishuRequest = new CreateInstanceRequest
            {
                ApprovalCode = request.ApprovalCode,
                UserId = request.UserId,
                Form = request.Form
            };

            var result = await _approvalApi.CreateInstanceAsync(feishuRequest, cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new CreateApprovalInstanceResponse
            {
                Code = result.Code,
                Data = result.Data != null ? new CreateApprovalInstanceData
                {
                    InstanceId = result.Data.InstanceCode
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建审批实例失败，ApprovalCode: {ApprovalCode}", request.ApprovalCode);
            return null;
        }
    }

    public async Task<GetApprovalInstanceResponse?> GetApprovalInstanceAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _approvalApi.GetInstanceByIdAsync(instanceId, cancellationToken: cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new GetApprovalInstanceResponse
            {
                Code = result.Code,
                Data = result.Data != null ? new GetApprovalInstanceData
                {
                    Instance = new ApprovalInstanceData
                    {
                        InstanceId = result.Data.InstanceCode,
                        Status = result.Data.Status,
                        ApprovalCode = result.Data.ApprovalCode,
                        UserId = result.Data.UserId,
                        Form = result.Data.Form,
                        CreateTime = long.TryParse(result.Data.StartTime, out var startTime) ? startTime : 0,
                        EndTime = long.TryParse(result.Data.EndTime, out var endTime) ? endTime : null
                    }
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审批实例失败，InstanceId: {InstanceId}", instanceId);
            return null;
        }
    }

    public async Task<CancelApprovalInstanceResponse?> CancelApprovalInstanceAsync(
        CancelApprovalInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var feishuRequest = new CancelInstancesRequest
            {
                InstanceCode = request.InstanceId,
                UserId = request.UserId
            };

            var result = await _approvalApi.CancelInstanceAsync(feishuRequest, cancellationToken: cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new CancelApprovalInstanceResponse
            {
                Code = result.Code
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消审批实例失败，InstanceId: {InstanceId}", request.InstanceId);
            return null;
        }
    }

    public async Task<GetApprovalInstanceListResponse?> GetApprovalInstanceListAsync(
        GetApprovalInstanceListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var feishuRequest = new ApprovalInstancesQueryRequest
            {
                UserId = request.UserId
            };

            var result = await _approvalQueryApi.GetInstancesPageListAsync(
                feishuRequest,
                page_size: request.PageSize,
                page_token: request.PageToken > 0 ? request.PageToken.ToString() : null,
                cancellationToken: cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new GetApprovalInstanceListResponse
            {
                Code = result.Code,
                Data = result.Data != null ? new GetApprovalInstanceListData
                {
                    InstanceIds = result.Data.InstanceLists?.Select(item => new ApprovalInstanceData
                    {
                        InstanceId = item.Instance?.Code,
                        Status = item.Instance?.Status,
                        UserId = item.Instance?.UserId
                    }).ToList(),
                    PageToken = int.TryParse(result.Data.PageToken, out var token) ? token : 0,
                    HasMore = result.Data.HasMore
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审批实例列表失败，UserId: {UserId}", request.UserId);
            return null;
        }
    }
}
