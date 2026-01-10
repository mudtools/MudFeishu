// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 飞书事件去重服务接口
/// 用于防止重复事件的处理，保证事件处理的幂等性
/// </summary>
public interface IFeishuEventDeduplicator : IAsyncDisposable
{
    /// <summary>
    /// 尝试将事件标记为已处理
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <returns>如果事件已被处理过返回 true（重复事件），否则返回 false 并标记为已处理</returns>
    bool TryMarkAsProcessed(string eventId);

    /// <summary>
    /// 检查事件是否已被处理（不标记）
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <returns>如果事件已被处理返回 true，否则返回 false</returns>
    bool IsProcessed(string eventId);
}
