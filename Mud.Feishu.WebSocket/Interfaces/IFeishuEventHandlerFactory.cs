// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书事件处理器工厂接口
/// </summary>
public interface IFeishuEventHandlerFactory
{
    /// <summary>
    /// 根据事件类型获取事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>事件处理器，如果未找到则返回默认处理器</returns>
    IFeishuEventHandler GetHandler(string eventType);

    /// <summary>
    /// 根据事件类型获取所有事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>事件处理器列表，如果未找到则返回包含默认处理器的列表</returns>
    IReadOnlyList<IFeishuEventHandler> GetHandlers(string eventType);

    /// <summary>
    /// 并行处理事件（使用所有匹配的处理器）
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    Task HandleEventParallelAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <param name="handler">事件处理器</param>
    void RegisterHandler(IFeishuEventHandler handler);

    /// <summary>
    /// 取消注册事件处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>是否成功取消注册</returns>
    bool UnregisterHandler(string eventType);

    /// <summary>
    /// 获取所有已注册的事件类型
    /// </summary>
    /// <returns>事件类型列表</returns>
    IReadOnlyList<string> GetRegisteredEventTypes();

    /// <summary>
    /// 检查是否已注册指定事件类型的处理器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns>是否已注册</returns>
    bool IsHandlerRegistered(string eventType);
}