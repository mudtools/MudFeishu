// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

namespace TaskManageDemo.Backend.Interceptors;

/// <summary>
/// 审计日志拦截器 - 记录所有事件处理详情
/// </summary>
public class AuditLogInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<AuditLogInterceptor> _logger;

    public AuditLogInterceptor(ILogger<AuditLogInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[审计日志] 事件开始处理: EventType={EventType}, EventId={EventId}, TenantKey={TenantKey}, AppId={AppId}, CreateTime={CreateTime}",
            eventType,
            eventData.EventId,
            eventData.TenantKey,
            eventData.AppId,
            eventData.CreateTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(eventData.CreateTime).ToString("yyyy-MM-dd HH:mm:ss") : "N/A");

        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var status = exception == null ? "成功" : "失败";

        if (exception != null)
        {
            _logger.LogError(exception,
                "[审计日志] 事件处理{Status}: EventType={EventType}, EventId={EventId}, ErrorMessage={ErrorMessage}",
                status, eventType, eventData.EventId, exception.Message);
        }
        else
        {
            _logger.LogInformation("[审计日志] 事件处理{Status}: EventType={EventType}, EventId={EventId}",
                status, eventType, eventData.EventId);
        }

        return Task.CompletedTask;
    }
}
