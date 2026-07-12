// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using Mud.Feishu.Abstractions.Observability;

namespace Mud.Feishu.Abstractions.Interceptors;

/// <summary>
/// 遥测拦截器：为飞书事件处理创建分布式追踪 Span。
/// 使用统一的 <see cref="FeishuActivitySource"/>，确保 Span 可被 OTel SDK 导出。
/// </summary>
public class TelemetryEventInterceptor : IFeishuEventInterceptor
{
    private readonly string _appKey;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="appKey">飞书应用 AppKey，用于 Span 标签。</param>
    public TelemetryEventInterceptor(string appKey)
    {
        _appKey = appKey ?? throw new ArgumentNullException(nameof(appKey));
    }

    /// <inheritdoc />
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var activity = FeishuActivitySource.Instance.StartActivity(
            FeishuActivitySource.ActivityNameEventHandling,
            ActivityKind.Internal);

        if (activity != null)
        {
            activity.SetTag(FeishuActivitySource.Tags.AppKey, _appKey);
            activity.SetTag(FeishuActivitySource.Tags.EventType, eventType);
            activity.SetTag(FeishuActivitySource.Tags.EventId, eventData.EventId);
            activity.SetTag(FeishuActivitySource.Tags.TenantKey, eventData.TenantKey);
        }

        // 将 Activity 存入 eventData 上下文，避免 ConcurrentDictionary 泄漏
        // 仅当 Activity 非空时存储（无监听器时 StartActivity 返回 null）
        if (activity != null)
        {
            eventData.Items ??= new Dictionary<string, object?>();
            eventData.Items["__activity"] = activity;
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (eventData.Items != null && eventData.Items.TryGetValue("__activity", out var obj) && obj is Activity activity && activity != null)
        {
            if (exception != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag("exception.type", exception.GetType().FullName);
                activity.SetTag("exception.message", exception.Message);
#if NET8_0_OR_GREATER
                var exceptionTags = new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message },
                };
                activity.AddEvent(new ActivityEvent("exception", tags: exceptionTags));
#endif
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            activity.Dispose();
            eventData.Items.Remove("__activity");
        }

        return Task.CompletedTask;
    }
}
