// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 幂等性飞书事件处理器基类
/// <para>提供业务层幂等性支持，防止同一事件的业务逻辑重复执行</para>
/// <para>适用于需要在 Handler 层面保证幂等性的场景</para>
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
/// <remarks>
/// 使用方式：
/// 1. 继承此类并重写 <see cref="GetBusinessKey"/> 方法，定义业务去重键
/// 2. 重写 <see cref="HandleEventInternalAsync"/> 方法实现业务逻辑
/// 3. 基类会自动处理业务去重，确保同一业务键只处理一次
/// </remarks>
public abstract class IdempotentFeishuEventHandler<T> : DefaultFeishuEventHandler<T>
    where T : class, IEventResult, new()
{
    private readonly IFeishuEventDeduplicator _businessDeduplicator;
  

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="businessDeduplicator">业务层去重服务</param>
    /// <param name="logger">日志记录器</param>
    protected IdempotentFeishuEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger logger)
        : base(logger)
    {
        _businessDeduplicator = businessDeduplicator ?? throw new ArgumentNullException(nameof(businessDeduplicator));
    }


    /// <summary>
    /// 处理飞书事件（带业务层幂等性保护）
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    public sealed override async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        var businessKey = GetBusinessKey(eventData);

        if (string.IsNullOrEmpty(businessKey))
        {
            _logger.LogWarning("业务键为空，跳过业务层幂等性检查，直接处理事件 {EventId}", eventData.EventId);
            await HandleEventInternalAsync(eventData, cancellationToken);
            return;
        }

        // 检查业务键是否已处理
        if (_businessDeduplicator.TryMarkAsProcessing(businessKey ?? string.Empty))
        {
            _logger.LogDebug("业务键 {BusinessKey} 已处理或在处理中，跳过事件 {EventId}", businessKey, eventData.EventId);
            return;
        }

        try
        {
            // 处理事件
            await HandleEventInternalAsync(eventData, cancellationToken);

            // 标记为已完成
            _businessDeduplicator.MarkAsCompleted(businessKey ?? string.Empty);
            _logger.LogDebug("业务键 {BusinessKey} 处理完成", businessKey);
        }
        catch (Exception)
        {
            // 处理失败，回滚状态
            _businessDeduplicator.RollbackProcessing(businessKey ?? string.Empty);
            throw;
        }
    }

    /// <summary>
    /// 获取业务去重键
    /// <para>重写此方法以定义业务的唯一标识</para>
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>业务唯一标识，返回 null 或空字符串时不进行业务去重</returns>
    /// <remarks>
    /// 示例：
    /// <code>
    /// protected override string? GetBusinessKey(EventData eventData)
    /// {
    ///     // 使用事件类型 + 用户ID作为业务键
    ///     var userId = eventData.Event.GetProperty("user_id").GetString();
    ///     return $"{eventData.EventType}:{userId}";
    /// }
    /// </code>
    /// </remarks>
    protected virtual string? GetBusinessKey(EventData eventData)
    {
        return eventData.EventId;
    }

    /// <summary>
    /// 处理事件的内部逻辑
    /// <para>此方法由基类调用，已确保幂等性</para>
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected abstract Task HandleEventInternalAsync(EventData eventData, CancellationToken cancellationToken);
}
