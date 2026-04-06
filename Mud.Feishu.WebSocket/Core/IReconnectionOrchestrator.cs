// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket.Core;

/// <summary>
/// 重连协调器接口，负责统一管理WebSocket重连逻辑
/// </summary>
public interface IReconnectionOrchestrator
{
    /// <summary>
    /// 尝试重连
    /// </summary>
    /// <param name="reason">重连原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重连是否成功</returns>
    Task<bool> TryReconnectAsync(string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置重连计数器
    /// </summary>
    void ResetReconnectCounter();

    /// <summary>
    /// 获取当前重连状态
    /// </summary>
    /// <returns>重连状态信息</returns>
    ReconnectState GetReconnectState();

    /// <summary>
    /// 重连成功事件
    /// </summary>
    event EventHandler<ReconnectSuccessEventArgs>? ReconnectSucceeded;

    /// <summary>
    /// 重连失败事件
    /// </summary>
    event EventHandler<ReconnectFailedEventArgs>? ReconnectFailed;

    /// <summary>
    /// 达到重连限制事件
    /// </summary>
    event EventHandler<ReconnectLimitReachedEventArgs>? ReconnectLimitReached;
}
