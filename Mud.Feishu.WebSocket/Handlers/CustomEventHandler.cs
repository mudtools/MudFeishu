// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 自定义事件处理器示例
/// 演示如何创建自定义的事件处理器
/// </summary>
public class CustomEventHandler : DefaultFeishuEventHandler
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public CustomEventHandler(ILogger<CustomEventHandler> logger) : base(logger)
    {
    }

    /// <summary>
    /// 支持的事件类型
    /// 这里使用一个虚构的事件类型作为示例
    /// </summary>
    public override string SupportedEventType => "custom.event.example_v1";

    /// <summary>
    /// 处理自定义事件的业务逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public override async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("处理自定义事件: {EventType}, 应用ID: {AppId}, 租户: {TenantKey}",
            eventData.EventType, eventData.AppId, eventData.TenantKey);

        // 这里可以添加具体的自定义事件处理逻辑
        // 例如：
        // 1. 解析事件内容
        // 2. 执行业务逻辑
        // 3. 调用外部服务
        // 4. 更新数据库
        // 5. 发送通知

        await Task.CompletedTask;
    }
}