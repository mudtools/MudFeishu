// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 飞书事件处理器基类，提供默认的事件消息反序列化功能。
/// </summary>
/// <typeparam name="T">事件实体类型</typeparam>
public abstract class DefaultFeishuEventHandler<T> : IFeishuEventHandler
    where T : class, IEventResult, new()
{
    /// <summary>
    /// 日志记录器。
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当logger为null时抛出</exception>
    public DefaultFeishuEventHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 支持的事件类型（默认处理器返回空字符串）
    /// </summary>
    public virtual string SupportedEventType => string.Empty;

    /// <summary>
    /// 处理未知类型事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    /// <exception cref="ArgumentNullException">当eventData为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当事件数据无效时抛出</exception>
    public virtual async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(eventData, nameof(eventData));

        _logger.LogInformation("开始处理事件，事件类型: {EventType}, 应用ID: {AppId}, 租户: {TenantKey}",
            eventData.EventType, eventData.AppId, eventData.TenantKey);

        try
        {
            var eventEntity = DeserializeEvent(eventData);
            await ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "事件数据反序列化失败，事件类型: {EventType}, 原始数据: {RawData}",
                eventData.EventType, eventData.Event?.ToString());
            throw new InvalidOperationException("事件数据格式无效", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "处理事件时发生错误，事件类型: {EventType}", eventData.EventType);
            throw;
        }
    }

    /// <summary>
    /// 反序列化事件数据
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>反序列化后的事件实体</returns>
    /// <exception cref="InvalidOperationException">当事件数据为空或反序列化失败时抛出</exception>
    private T? DeserializeEvent(EventData eventData)
    {
        if (eventData.Event == null)
        {
            _logger.LogWarning("事件数据为空，事件ID：{EventId}，事件类型: {EventType}", eventData.EventId, eventData.EventType);
            return default;
        }

        try
        {
            var eventJson = eventData.Event?.ToString();
            if (string.IsNullOrWhiteSpace(eventJson))
            {
                _logger.LogWarning("事件JSON数据为空，事件ID：{EventId}，事件类型: {EventType}", eventData.EventId, eventData.EventType);
                return default;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<T>(eventJson, options);
            return result ?? default;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "反序列化事件数据失败，事件ID：{EventId}，事件类型: {EventType}", eventData.EventId, eventData.EventType);
            throw;
        }
    }

    /// <summary>
    /// 处理业务逻辑。
    /// </summary>
    /// <param name="eventData">完整的事件原始数据。</param>
    /// <param name="eventEntity">事件实体数据。</param>
    /// <param name="cancellationToken">取消操作令牌</param>
    /// <returns>处理任务</returns>
    protected abstract Task ProcessBusinessLogicAsync(
        EventData eventData,
        T? eventEntity,
        CancellationToken cancellationToken = default);
}
