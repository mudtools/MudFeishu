// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 部门创建事件处理器
/// </summary>
public class DepartmentCreatedEventHandler : DefaultFeishuEventHandler
{
    public DepartmentCreatedEventHandler(ILogger<DepartmentCreatedEventHandler> logger) : base(logger)
    {
    }

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public override string SupportedEventType => FeishuEventTypes.DepartmentCreated;

    /// <summary>
    /// 处理部门创建事件的业务逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public override async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("处理部门创建事件: {Event}", JsonSerializer.Serialize(eventData.Event));

        try
        {
            // 提取部门创建事件的具体数据
            var departmentInfo = ExtractDepartmentInfo(eventData);
            if (departmentInfo != null)
            {
                _logger.LogInformation("部门已创建 - 部门名称: {DepartmentName}, 部门ID: {DepartmentId}, 父部门ID: {ParentDepartmentId}",
                    departmentInfo.Name, departmentInfo.DepartmentId, departmentInfo.ParentDepartmentId);

                // 这里可以添加具体的部门创建处理逻辑，例如：
                // 1. 同步到本地数据库
                // 2. 更新缓存
                // 3. 发送通知
                // 4. 触发相关业务流程

                await SyncDepartmentToDatabase(departmentInfo, cancellationToken);
                await NotifyDepartmentCreated(departmentInfo, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理部门创建事件时发生错误");
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 从事件数据中提取部门信息
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>部门信息</returns>
    private DepartmentInfo? ExtractDepartmentInfo(EventData eventData)
    {
        try
        {
            if (eventData.Event != null)
            {
                // 将Event对象转换为JsonElement
                var eventJson = JsonSerializer.Serialize(eventData.Event);
                var eventElement = JsonSerializer.Deserialize<JsonElement>(eventJson);
                
                if (eventElement.ValueKind == JsonValueKind.Object && 
                    eventElement.TryGetProperty("department", out var departmentElement))
                {
                    var departmentInfo = new DepartmentInfo
                    {
                        DepartmentId = departmentElement.TryGetProperty("department_id", out var deptIdElement) 
                            ? deptIdElement.GetString() ?? string.Empty 
                            : string.Empty,
                        Name = departmentElement.TryGetProperty("name", out var nameElement) 
                            ? nameElement.GetString() ?? string.Empty 
                            : string.Empty,
                        ParentDepartmentId = departmentElement.TryGetProperty("parent_department_id", out var parentDeptIdElement) 
                            ? parentDeptIdElement.GetString() 
                            : null,
                        LeaderUserId = departmentElement.TryGetProperty("leader_user_id", out var leaderUserIdElement) 
                            ? leaderUserIdElement.GetString() 
                            : null,
                        Status = departmentElement.TryGetProperty("status", out var statusElement) 
                            ? statusElement.TryGetProperty("status", out var statusValueElement) 
                                ? statusValueElement.GetInt32() 
                                : 0 
                            : 0,
                        CreateTime = departmentElement.TryGetProperty("create_time", out var createTimeElement) 
                            ? createTimeElement.GetInt64() 
                            : DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    };

                    return departmentInfo;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析部门创建事件数据失败");
        }

        return null;
    }

    /// <summary>
    /// 同步部门信息到数据库
    /// </summary>
    /// <param name="departmentInfo">部门信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task SyncDepartmentToDatabase(DepartmentInfo departmentInfo, CancellationToken cancellationToken)
    {
        // TODO: 实现数据库同步逻辑
        _logger.LogInformation("同步部门信息到数据库: {DepartmentId} - {DepartmentName}", 
            departmentInfo.DepartmentId, departmentInfo.Name);
        
        // 示例：调用数据库服务进行同步
        // await _departmentService.CreateOrUpdateAsync(departmentInfo, cancellationToken);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 发送部门创建通知
    /// </summary>
    /// <param name="departmentInfo">部门信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task NotifyDepartmentCreated(DepartmentInfo departmentInfo, CancellationToken cancellationToken)
    {
        // TODO: 实现通知逻辑
        _logger.LogInformation("发送部门创建通知: {DepartmentName} 已创建", departmentInfo.Name);
        
        // 示例：发送消息通知相关人员
        // await _notificationService.NotifyDepartmentCreatedAsync(departmentInfo, cancellationToken);
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// 部门信息模型
/// </summary>
public class DepartmentInfo
{
    /// <summary>
    /// 部门ID
    /// </summary>
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父部门ID
    /// </summary>
    public string? ParentDepartmentId { get; set; }

    /// <summary>
    /// 部门负责人用户ID
    /// </summary>
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// 部门状态 (0: 正常, 1: 停用)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 创建时间（毫秒时间戳）
    /// </summary>
    public long CreateTime { get; set; }
}