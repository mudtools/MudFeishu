// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.WebSocket.Demo.Handlers;
using System.Collections.Concurrent;

namespace Mud.Feishu.WebSocket.Services;

/// <summary>
/// 演示事件服务，用于记录和管理事件统计
/// </summary>
public class DemoEventService
{
    private readonly ILogger<DemoEventService> _logger;
    private readonly ConcurrentBag<UserData> _userEvents = new();
    private readonly ConcurrentBag<DepartmentCreatedResult> _departmentEvents = new();
    private readonly ConcurrentBag<ApprovalData> _approvalEvents = new();

    private int _userCount = 0;
    private int _departmentCount = 0;
    private int _approvalCount = 0;

    public DemoEventService(ILogger<DemoEventService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 记录用户事件
    /// </summary>
    public async Task RecordUserEventAsync(UserData userData, CancellationToken cancellationToken = default)
    {
        _userEvents.Add(userData);
        _logger.LogInformation("📊 [统计] 记录用户事件: {UserId} - {UserName}", userData.UserId, userData.UserName);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 记录部门事件
    /// </summary>
    public async Task RecordDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken = default)
    {
        _departmentEvents.Add(departmentData);
        _logger.LogInformation("📊 [统计] 记录部门事件: {DepartmentId} - {DepartmentName}",
            departmentData.DepartmentId, departmentData.Name);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 记录审批事件
    /// </summary>
    public async Task RecordApprovalEventAsync(ApprovalData approvalData, CancellationToken cancellationToken = default)
    {
        _approvalEvents.Add(approvalData);
        _logger.LogInformation("📊 [统计] 记录审批事件: {ApprovalId} - {ApprovalTitle} ({Status})",
            approvalData.ApprovalId, approvalData.ApprovalTitle, approvalData.ApprovalStatus);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 增加用户计数
    /// </summary>
    public void IncrementUserCount() => Interlocked.Increment(ref _userCount);

    /// <summary>
    /// 增加部门计数
    /// </summary>
    public void IncrementDepartmentCount() => Interlocked.Increment(ref _departmentCount);

    /// <summary>
    /// 增加审批计数
    /// </summary>
    public void IncrementApprovalCount() => Interlocked.Increment(ref _approvalCount);

    /// <summary>
    /// 获取事件统计信息
    /// </summary>
    public EventStatistics GetStatistics()
    {
        return new EventStatistics
        {
            UserEventsCount = _userEvents.Count,
            DepartmentEventsCount = _departmentEvents.Count,
            ApprovalEventsCount = _approvalEvents.Count,
            TotalProcessedUsers = _userCount,
            TotalProcessedDepartments = _departmentCount,
            TotalProcessedApprovals = _approvalCount,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 获取最近的事件记录
    /// </summary>
    public RecentEvents GetRecentEvents(int count = 10)
    {
        return new RecentEvents
        {
            RecentUsers = _userEvents.OrderByDescending(u => u.ProcessedAt).Take(count).ToList(),
            RecentDepartments = _departmentEvents.OrderByDescending(d => d.Order).Take(count).ToList(),
            RecentApprovals = _approvalEvents.OrderByDescending(a => a.ProcessedAt).Take(count).ToList()
        };
    }

    /// <summary>
    /// 清空所有事件记录
    /// </summary>
    public void ClearAllEvents()
    {
        while (_userEvents.TryTake(out _)) { }
        while (_departmentEvents.TryTake(out _)) { }
        while (_approvalEvents.TryTake(out _)) { }

        Interlocked.Exchange(ref _userCount, 0);
        Interlocked.Exchange(ref _departmentCount, 0);
        Interlocked.Exchange(ref _approvalCount, 0);

        _logger.LogInformation("🗑️ [统计] 已清空所有事件记录");
    }

    /// <summary>
    /// 生成模拟用户事件数据
    /// </summary>
    public EventData GenerateMockUserEvent()
    {
        var random = new Random();
        var userId = $"user_{random.Next(10000, 99999)}";
        var departments = new[] { "技术部", "产品部", "运营部", "市场部", "人事部", "财务部" };
        var department = departments[random.Next(departments.Length)];

        return new EventData
        {
            EventType = "contact.user.created_v3",
            Event = new
            {
                user = new
                {
                    user_id = userId,
                    name = $"用户{userId.Split('_')[1]}",
                    email = $"user{userId.Split('_')[1]}@example.com",
                    department = department,
                    phone = $"138{random.Next(10000000, 99999999)}",
                    avatar = $"https://example.com/avatar/{userId}.jpg"
                }
            }
        };
    }

    /// <summary>
    /// 生成模拟部门事件数据
    /// </summary>
    public EventData GenerateMockDepartmentEvent()
    {
        var random = new Random();
        var deptId = $"dept_{random.Next(1000, 9999)}";
        var parentDeptId = random.Next(0, 3) == 0 ? null : $"dept_{random.Next(100, 999)}";

        return new EventData
        {
            EventType = "contact.department.created_v3",
            Event = new
            {
                department = new
                {
                    department_id = deptId,
                    name = $"部门{deptId.Split('_')[1]}",
                    parent_department_id = parentDeptId,
                    department_level = parentDeptId == null ? 1 : 2,
                    status = "active",
                    description = $"这是部门{deptId.Split('_')[1]}的描述",
                    leader_user_id = $"leader_{random.Next(1000, 9999)}",
                    created_by = "admin",
                    member_count = random.Next(5, 50)
                }
            },
        };
    }

    /// <summary>
    /// 生成模拟审批事件数据
    /// </summary>
    public EventData GenerateMockApprovalEvent()
    {
        var random = new Random();
        var approvalId = $"approval_{random.Next(1000, 9999)}";
        var statuses = new[] { "approved", "rejected", "pending" };
        var status = statuses[random.Next(statuses.Length)];
        var types = new[] { "leave", "expense", "overtime", "trip" };
        var type = types[random.Next(types.Length)];

        return new EventData
        {
            EventType = "approval.approval.approved_v1",
            Event = new
            {
                approval = new
                {
                    approval_id = approvalId,
                    definition_code = $"{type.ToUpper()}_REQUEST",
                    instance_id = $"instance_{random.Next(10000, 99999)}",
                    approval_status = status,
                    applicant_id = $"user_{random.Next(10000, 99999)}",
                    approver_id = status == "pending" ? null : $"manager_{random.Next(1000, 9999)}",
                    title = $"{GetChineseName(type)}申请",
                    approval_type = type,
                    priority = random.Next(1, 4),
                    comment = status == "rejected" ? "不满足审批条件" : (status == "approved" ? "审批通过" : "等待审批"),
                    approved_at = status == "pending" ? DateTime.MinValue : DateTime.UtcNow.AddDays(-random.Next(1, 7)),
                    created_at = DateTime.UtcNow.AddDays(-random.Next(7, 30)),
                    updated_at = DateTime.UtcNow
                }
            }
        };
    }

    private static string GetChineseName(string type)
    {
        return type switch
        {
            "leave" => "请假",
            "expense" => "费用报销",
            "overtime" => "加班",
            "trip" => "出差",
            _ => "通用"
        };
    }
}

/// <summary>
/// 事件统计信息
/// </summary>
public class EventStatistics
{
    public int UserEventsCount { get; init; }
    public int DepartmentEventsCount { get; init; }
    public int ApprovalEventsCount { get; init; }
    public int TotalProcessedUsers { get; init; }
    public int TotalProcessedDepartments { get; init; }
    public int TotalProcessedApprovals { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// 最近的事件记录
/// </summary>
public class RecentEvents
{
    public List<UserData> RecentUsers { get; init; } = new();
    public List<DepartmentCreatedResult> RecentDepartments { get; init; } = new();
    public List<ApprovalData> RecentApprovals { get; init; } = new();
}