// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 审批实例DTO
/// </summary>
public class ApprovalInstanceDto
{
    /// <summary>
    /// 审批实例ID
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 审批定义Code
    /// </summary>
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// 发起人ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 审批状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 关联任务ID
    /// </summary>
    public string? TaskGuid { get; set; }

    /// <summary>
    /// 表单数据
    /// </summary>
    public Dictionary<string, object> FormData { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 创建审批请求
/// </summary>
public class CreateApprovalRequest
{
    /// <summary>
    /// 审批定义Code
    /// </summary>
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// 发起人ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 关联任务ID
    /// </summary>
    public string? TaskGuid { get; set; }

    /// <summary>
    /// 表单数据
    /// </summary>
    public Dictionary<string, object> FormData { get; set; } = new();
}

/// <summary>
/// 审批状态查询参数
/// </summary>
public class ApprovalQueryParameters
{
    /// <summary>
    /// 审批实例ID
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 审批状态
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}
