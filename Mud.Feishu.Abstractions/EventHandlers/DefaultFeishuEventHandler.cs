// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 默认的飞书事件处理器（用于处理未注册的事件类型）
/// </summary>
public abstract class DefaultFeishuEventHandler<T> : IFeishuEventHandler
    where T : IEventResult, new()
{
    /// <summary>
    /// 日志记录器。
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// 默认构造函数。
    /// </summary>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
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
    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        try
        {
            _logger.LogWarning("收到未处理的事件类型: {EventType}, 应用ID: {AppId}, 租户: {TenantKey}",
                eventData.EventType, eventData.AppId, eventData.TenantKey);

            var eventEntity = JsonSerializer.Deserialize<T>(eventData.Event!.ToString()!);

            await ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理默认事件时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 处理业务逻辑。
    /// </summary>
    /// <param name="eventData">完整的事件原始数据。</param>
    /// <param name="eventEntity">事件实体数据。</param>
    /// <param name="cancellationToken">取消操作<see cref="CancellationToken"/>令牌</param>
    /// <returns></returns>
    protected abstract Task ProcessBusinessLogicAsync(EventData eventData, T? eventEntity, CancellationToken cancellationToken = default);
}
