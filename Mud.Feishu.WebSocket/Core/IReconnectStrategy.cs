// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 重连策略接口，支持可插拔的重连策略实现
/// </summary>
public interface IReconnectStrategy
{
    /// <summary>
    /// 计算下一次重连的延迟时间
    /// </summary>
    /// <param name="attemptCount">当前尝试次数（从1开始）</param>
    /// <returns>延迟时间</returns>
    TimeSpan CalculateDelay(int attemptCount);

    /// <summary>
    /// 判断是否应该继续重连
    /// </summary>
    /// <param name="attemptCount">当前尝试次数</param>
    /// <param name="totalElapsedTime">已消耗的总时间</param>
    /// <returns>是否应该继续重连</returns>
    bool ShouldContinueReconnect(int attemptCount, TimeSpan totalElapsedTime);
}
