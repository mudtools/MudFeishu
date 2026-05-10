// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Approval;

/// <summary>
/// 审批服务实现
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly IFeishuApproval _approvalApi;
    private readonly ILogger<ApprovalService> _logger;

    private const string TaskDelayApprovalCode = "task_delay";
    private const string TaskDeleteApprovalCode = "task_delete";

    public ApprovalService(IFeishuApproval approvalApi, ILogger<ApprovalService> logger)
    {
        _approvalApi = approvalApi;
        _logger = logger;
    }

    public async Task<ApprovalInstanceDto?> CreateApprovalAsync(
        CreateApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        var formList = request.FormData.Select(kv => new Dictionary<string, object>
        {
            ["id"] = kv.Key,
            ["type"] = "input",
            ["value"] = kv.Value
        }).ToList();

        if (!string.IsNullOrEmpty(request.TaskGuid))
        {
            formList.Add(new Dictionary<string, object>
            {
                ["id"] = "task_guid",
                ["type"] = "input",
                ["value"] = request.TaskGuid
            });
        }

        var approvalRequest = new CreateApprovalInstanceRequest
        {
            ApprovalCode = request.ApprovalCode,
            UserId = request.UserId,
            Form = JsonSerializer.Serialize(formList)
        };

        var result = await _approvalApi.CreateApprovalInstanceAsync(approvalRequest, cancellationToken: cancellationToken);

        if (result?.Data?.InstanceId != null)
        {
            _logger.LogInformation("审批实例创建成功: {InstanceId}", result.Data.InstanceId);

            return new ApprovalInstanceDto
            {
                InstanceId = result.Data.InstanceId,
                ApprovalCode = request.ApprovalCode,
                UserId = request.UserId,
                TaskGuid = request.TaskGuid,
                FormData = request.FormData,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
        }

        _logger.LogWarning("审批实例创建失败: {Result}", JsonSerializer.Serialize(result));
        return null;
    }

    public async Task<ApprovalInstanceDto?> GetApprovalAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        var result = await _approvalApi.GetApprovalInstanceAsync(instanceId, cancellationToken: cancellationToken);

        if (result?.Data?.Instance != null)
        {
            return new ApprovalInstanceDto
            {
                InstanceId = result.Data.Instance.InstanceId ?? instanceId,
                ApprovalCode = result.Data.Instance.ApprovalCode ?? string.Empty,
                UserId = result.Data.Instance.UserId ?? string.Empty,
                Status = result.Data.Instance.Status ?? string.Empty,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(result.Data.Instance.CreateTime).DateTime,
                CompletedAt = result.Data.Instance.EndTime.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(result.Data.Instance.EndTime.Value).DateTime
                    : null
            };
        }

        return null;
    }

    public async Task<bool> CancelApprovalAsync(
        string instanceId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var request = new CancelApprovalInstanceRequest
        {
            InstanceId = instanceId,
            UserId = userId
        };

        var result = await _approvalApi.CancelApprovalInstanceAsync(request, cancellationToken: cancellationToken);

        if (result != null && result.Code == 0)
        {
            _logger.LogInformation("审批实例取消成功: {InstanceId}", instanceId);
            return true;
        }

        _logger.LogWarning("审批实例取消失败: {InstanceId}", instanceId);
        return false;
    }

    public async Task<PagedResponse<ApprovalInstanceDto>> GetUserApprovalsAsync(
        string userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var request = new GetApprovalInstanceListRequest
        {
            UserId = userId,
            PageToken = (page - 1) * pageSize,
            PageSize = pageSize
        };

        if (!string.IsNullOrEmpty(status))
        {
            request.Status = status;
        }

        var result = await _approvalApi.GetApprovalInstanceListAsync(request, cancellationToken: cancellationToken);

        var items = result?.Data?.InstanceIds?.Select(i => new ApprovalInstanceDto
        {
            InstanceId = i.InstanceId ?? string.Empty,
            ApprovalCode = i.ApprovalCode ?? string.Empty,
            UserId = i.UserId ?? string.Empty,
            Status = i.Status ?? string.Empty,
            CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(i.CreateTime).DateTime
        }).ToList() ?? new List<ApprovalInstanceDto>();

        return new PagedResponse<ApprovalInstanceDto>
        {
            Items = items,
            Total = items.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ApprovalInstanceDto?> CreateTaskDelayApprovalAsync(
        string taskGuid,
        string userId,
        DateTime newDueTime,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var formData = new Dictionary<string, object>
        {
            ["new_due_time"] = newDueTime.ToString("yyyy-MM-dd HH:mm:ss"),
            ["reason"] = reason
        };

        return await CreateApprovalAsync(new CreateApprovalRequest
        {
            ApprovalCode = TaskDelayApprovalCode,
            UserId = userId,
            TaskGuid = taskGuid,
            FormData = formData
        }, cancellationToken);
    }

    public async Task<ApprovalInstanceDto?> CreateTaskDeleteApprovalAsync(
        string taskGuid,
        string userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var formData = new Dictionary<string, object>
        {
            ["reason"] = reason
        };

        return await CreateApprovalAsync(new CreateApprovalRequest
        {
            ApprovalCode = TaskDeleteApprovalCode,
            UserId = userId,
            TaskGuid = taskGuid,
            FormData = formData
        }, cancellationToken);
    }
}
