// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using Mud.Feishu.WebSocket.Services;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Demo.Handlers;

/// <summary>
/// 演示审批事件处理器
/// </summary>
public class DemoApprovalEventHandler : IFeishuEventHandler
{
    private readonly ILogger<DemoApprovalEventHandler> _logger;
    private readonly DemoEventService _eventService;

    public DemoApprovalEventHandler(ILogger<DemoApprovalEventHandler> logger, DemoEventService eventService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    public string SupportedEventType => "approval.approval.approved_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("✅ [审批事件] 开始处理审批事件: {EventId}", eventData.EventId);

        try
        {
            // 解析审批数据
            var approvalData = ParseApprovalData(eventData);

            // 记录事件到服务
            await _eventService.RecordApprovalEventAsync(approvalData, cancellationToken);

            // 模拟业务处理
            await ProcessApprovalEventAsync(approvalData, cancellationToken);

            _logger.LogInformation("✅ [审批事件] 审批事件处理完成: 审批ID {ApprovalId}, 状态 {ApprovalStatus}",
                approvalData.ApprovalId, approvalData.ApprovalStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [审批事件] 处理审批事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private ApprovalData ParseApprovalData(EventData eventData)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventData.Data?.ToString() ?? "{}");

            // 尝试从不同的JSON结构中解析审批信息
            var approvalElement = jsonElement.GetProperty("approval");

            return new ApprovalData
            {
                ApprovalId = approvalElement.GetProperty("approval_id").GetString() ?? "",
                DefinitionCode = approvalElement.GetProperty("definition_code").GetString() ?? "",
                InstanceId = approvalElement.GetProperty("instance_id").GetString() ?? "",
                ApprovalStatus = approvalElement.GetProperty("approval_status").GetString() ?? "",
                ApplicantId = approvalElement.GetProperty("applicant_id").GetString() ?? "",
                ApproverId = TryGetProperty(approvalElement, "approver_id"),
                ApprovalTitle = approvalElement.GetProperty("title").GetString() ?? "",
                ApprovalType = TryGetProperty(approvalElement, "approval_type") ?? "general",
                Priority = TryGetIntProperty(approvalElement, "priority", 1),
                Comment = TryGetProperty(approvalElement, "comment") ?? "",
                ApprovedAt = TryGetDateTimeProperty(approvalElement, "approved_at"),
                CreatedAt = TryGetDateTimeProperty(approvalElement, "created_at", DateTime.UtcNow),
                UpdatedAt = TryGetDateTimeProperty(approvalElement, "updated_at", DateTime.UtcNow),
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析审批数据失败");
            throw new InvalidOperationException("无法解析审批数据", ex);
        }
    }

    private async Task ProcessApprovalEventAsync(ApprovalData approvalData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔄 [审批事件] 开始处理审批数据: {ApprovalId}", approvalData.ApprovalId);

        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 模拟验证逻辑
        if (string.IsNullOrWhiteSpace(approvalData.ApprovalId))
        {
            throw new ArgumentException("审批ID不能为空");
        }

        if (!IsValidApprovalStatus(approvalData.ApprovalStatus))
        {
            throw new ArgumentException($"无效的审批状态: {approvalData.ApprovalStatus}");
        }

        // 模拟审批状态处理
        if (approvalData.ApprovalStatus == "approved")
        {
            await ProcessApprovedApprovalAsync(approvalData, cancellationToken);
        }
        else if (approvalData.ApprovalStatus == "rejected")
        {
            await ProcessRejectedApprovalAsync(approvalData, cancellationToken);
        }
        else if (approvalData.ApprovalStatus == "pending")
        {
            await ProcessPendingApprovalAsync(approvalData, cancellationToken);
        }

        // 模拟更新统计信息
        _eventService.IncrementApprovalCount();

        await Task.CompletedTask;
    }

    private async Task ProcessApprovedApprovalAsync(ApprovalData approvalData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📋 [审批事件] 审批已通过: {ApprovalId}, 标题: {ApprovalTitle}",
            approvalData.ApprovalId, approvalData.ApprovalTitle);

        // 模拟通过审批的业务处理
        _logger.LogInformation("📧 [审批事件] 发送通过通知给申请人: {ApplicantId}", approvalData.ApplicantId);

        if (!string.IsNullOrWhiteSpace(approvalData.ApproverId))
        {
            _logger.LogInformation("📧 [审批事件] 发送完成通知给审批人: {ApproverId}", approvalData.ApproverId);
        }

        // 模拟数据归档
        _logger.LogInformation("📁 [审批事件] 归档审批记录: {ApprovalId}", approvalData.ApprovalId);

        await Task.Delay(50, cancellationToken);
    }

    private async Task ProcessRejectedApprovalAsync(ApprovalData approvalData, CancellationToken cancellationToken)
    {
        _logger.LogWarning("🚫 [审批事件] 审批被拒绝: {ApprovalId}, 标题: {ApprovalTitle}, 意见: {Comment}",
            approvalData.ApprovalId, approvalData.ApprovalTitle, approvalData.Comment);

        // 模拟拒绝审批的业务处理
        _logger.LogInformation("📧 [审批事件] 发送拒绝通知给申请人: {ApplicantId}", approvalData.ApplicantId);

        // 模拟记录拒绝原因
        if (!string.IsNullOrWhiteSpace(approvalData.Comment))
        {
            _logger.LogInformation("📝 [审批事件] 记录拒绝原因: {Comment}", approvalData.Comment);
        }

        await Task.Delay(50, cancellationToken);
    }

    private async Task ProcessPendingApprovalAsync(ApprovalData approvalData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ [审批事件] 审批待处理: {ApprovalId}, 标题: {ApprovalTitle}",
            approvalData.ApprovalId, approvalData.ApprovalTitle);

        // 模拟待处理审批的业务逻辑
        _logger.LogInformation("🔔 [审批事件] 发送待审批提醒: {ApprovalId}", approvalData.ApprovalId);

        await Task.Delay(50, cancellationToken);
    }

    private static bool IsValidApprovalStatus(string status)
    {
        var validStatuses = new[] { "approved", "rejected", "pending", "withdrawn" };
        return validStatuses.Contains(status?.ToLowerInvariant());
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }

    private static int TryGetIntProperty(JsonElement element, string propertyName, int defaultValue)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetInt32() : defaultValue;
    }

    private static DateTime? TryGetDateTimeProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.TryGetDateTime(out var dateTime))
                return dateTime;

            // 尝试解析时间戳
            if (value.TryGetInt64(out var timestamp))
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

            // 尝试解析字符串日期时间
            if (value.TryGetString(out var dateString) &&
                DateTime.TryParse(dateString, out var parsedDate))
                return parsedDate;
        }

        return null;
    }

    private static DateTime TryGetDateTimeProperty(JsonElement element, string propertyName, DateTime defaultValue)
    {
        return TryGetDateTimeProperty(element, propertyName) ?? defaultValue;
    }
}

/// <summary>
/// 审批数据模型
/// </summary>
public class ApprovalData
{
    public string ApprovalId { get; init; } = string.Empty;
    public string DefinitionCode { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public string ApplicantId { get; init; } = string.Empty;
    public string? ApproverId { get; init; }
    public string ApprovalTitle { get; init; } = string.Empty;
    public string ApprovalType { get; init; } = "general";
    public int Priority { get; init; } = 1;
    public string Comment { get; init; } = string.Empty;
    public DateTime? ApprovedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime ProcessedAt { get; init; }
}