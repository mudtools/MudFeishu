// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

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
/// 2. 重写 <see cref="DefaultFeishuEventHandler&lt;T&gt;.ProcessBusinessLogicAsync"/> 方法实现业务逻辑
/// 3. 基类会自动处理业务去重，确保同一业务键只处理一次
/// </remarks>
public abstract class IdempotentFeishuEventHandler<T> : DefaultFeishuEventHandler<T>
    where T : class, IEventResult, new()
{
    private readonly IFeishuEventDeduplicator _businessDeduplicator;
    private IAppKeyAccessor? _appKeyAccessor;

    /// <summary>
    /// 获取当前应用键（从上下文获取）
    /// </summary>
    protected string? CurrentAppKey => _appKeyAccessor?.CurrentAppKey;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="businessDeduplicator">业务层去重服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="appKeyAccessor">应用键上下文访问器（可选）</param>
    public IdempotentFeishuEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger logger,
        IAppKeyAccessor? appKeyAccessor = null)
        : base(logger)
    {
        _businessDeduplicator = businessDeduplicator ?? throw new ArgumentNullException(nameof(businessDeduplicator));
        _appKeyAccessor = appKeyAccessor;
    }


    /// <summary>
    /// 处理飞书事件（带业务层幂等性保护）
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    public sealed override async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        var businessKey = GetBusinessKey(eventData);
        var appKey = CurrentAppKey;

        if (string.IsNullOrEmpty(businessKey))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogWarning("业务键为空，跳过业务层幂等性检查，直接处理事件 {EventId}", eventData.EventId);
            await ProcessBusinessLogicAsync(eventData, null, cancellationToken);
            return;
        }

        // 检查业务键是否已处理（传递 AppKey 实现多应用隔离）
        if (_businessDeduplicator.TryMarkAsProcessing(businessKey!, appKey))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("业务键 {BusinessKey} 已处理或在处理中，跳过事件 {EventId}", businessKey, eventData.EventId);
            return;
        }

        try
        {
            var eventEntity = DeserializeEvent(eventData);
            // 处理事件
            await ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);

            // 标记为已完成
            _businessDeduplicator.MarkAsCompleted(businessKey!, appKey);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("业务键 {BusinessKey} 处理完成", businessKey);
        }
        catch (Exception)
        {
            // 处理失败，回滚状态
            _businessDeduplicator.RollbackProcessing(businessKey!, appKey);
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
}

/// <summary>
/// 幂等性飞书事件处理器基类（带强类型 Header 支持）
/// <para>提供业务层幂等性支持，防止同一事件的业务逻辑重复执行</para>
/// <para>适用于 v2.0 事件，需要强类型访问 Header 数据的场景</para>
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
/// <typeparam name="THeader">Header 数据类型，必须实现 <see cref="IEventHeader"/> 接口</typeparam>
/// <remarks>
/// 使用方式：
/// 1. 继承此类并重写 <see cref="IdempotentFeishuEventHandler{T}.GetBusinessKey"/> 方法，定义业务去重键
/// 2. 重写 <see cref="ProcessBusinessLogicAsync(EventData, T?, THeader?, CancellationToken)"/> 方法实现业务逻辑
/// 3. 基类会自动反序列化 Header 数据并注入到业务逻辑方法中
/// </remarks>
public abstract class IdempotentFeishuEventHandler<T, THeader> : IdempotentFeishuEventHandler<T>
    where T : class, IEventResult, new()
    where THeader : class, IEventHeader, new()
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="businessDeduplicator">业务层去重服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="appKeyAccessor">应用键上下文访问器（可选）</param>
    public IdempotentFeishuEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger logger,
        IAppKeyAccessor? appKeyAccessor = null)
        : base(businessDeduplicator, logger, appKeyAccessor)
    {
    }

    /// <summary>
    /// 反序列化 Header 数据
    /// <para>将 <see cref="EventData.Header"/> 转换为强类型的 <typeparamref name="THeader"/> 实例</para>
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>反序列化后的 Header 实体，Header 为 null 或反序列化失败时返回 default</returns>
    protected THeader? DeserializeHeader(EventData eventData)
    {
        if (eventData.Header == null)
            return default;

        try
        {
            var json = JsonSerializer.Serialize(eventData.Header, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return JsonSerializer.Deserialize<THeader>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Header 数据反序列化失败，事件ID：{EventId}", eventData.EventId);
            return default;
        }
    }

    /// <summary>
    /// 处理业务逻辑（带强类型 Header）
    /// <para>子类应重写此方法实现业务逻辑</para>
    /// </summary>
    /// <param name="eventData">完整的事件原始数据</param>
    /// <param name="eventEntity">事件实体数据</param>
    /// <param name="header">强类型 Header 数据</param>
    /// <param name="cancellationToken">取消操作令牌</param>
    /// <returns>处理任务</returns>
    protected virtual Task ProcessBusinessLogicAsync(
        EventData eventData,
        T? eventEntity,
        THeader? header,
        CancellationToken cancellationToken = default)
    {
        // 默认实现：调用无 Header 的版本（向后兼容）
        return ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
    }

    /// <summary>
    /// 重写基类的 ProcessBusinessLogicAsync，自动注入 Header
    /// <para>此方法为 sealed，不可被进一步重写，确保 Header 注入逻辑不被绕过</para>
    /// </summary>
    protected sealed override Task ProcessBusinessLogicAsync(
        EventData eventData,
        T? eventEntity,
        CancellationToken cancellationToken = default)
    {
        var header = DeserializeHeader(eventData);
        return ProcessBusinessLogicAsync(eventData, eventEntity, header, cancellationToken);
    }
}
