// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mud.Feishu.Abstractions.Interceptors;

/// <summary>
/// 遥测拦截器
/// 使用 OpenTelemetry API 收集事件处理的遥测数据
/// </summary>
public class TelemetryEventInterceptor : IFeishuEventInterceptor
{
    private readonly ActivitySource _activitySource;
    private readonly ConcurrentDictionary<string, Activity> _activities = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="componentName">组件名称，用于 ActivitySource</param>
    public TelemetryEventInterceptor(string componentName)
    {
        _activitySource = new ActivitySource(componentName);
    }

    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var activity = _activitySource.StartActivity("HandleFeishuEvent");
        if (activity != null)
        {
            activity.SetTag("component", _activitySource.Name);
            activity.SetTag("event.type", eventType);
            activity.SetTag("event.id", eventData.EventId);
            activity.SetTag("event.tenant_key", eventData.TenantKey);
        }

        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();
        _activities.TryAdd(eventId, activity!);

        return Task.FromResult(true);
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? string.Empty;

        if (_activities.TryRemove(eventId, out var activity))
        {
            if (exception != null)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", exception.Message);
                activity?.SetTag("error.type", exception.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            activity?.Dispose();
        }

        return Task.CompletedTask;
    }
}
