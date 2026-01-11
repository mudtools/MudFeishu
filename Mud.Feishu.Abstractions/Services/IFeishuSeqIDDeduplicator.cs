// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// WebSocket 消息 SeqID 去重服务接口
/// <para>用于处理 ProtoBuf 二进制消息中的序列号去重，防止重复处理</para>
/// <para>可与 EventId 去重配合使用，提供双重防护</para>
/// </summary>
public interface IFeishuSeqIDDeduplicator : IAsyncDisposable
{
    /// <summary>
    /// 尝试标记 SeqID 为已处理
    /// </summary>
    /// <param name="seqId">消息序列号</param>
    /// <returns>
    /// <c>true</c> 表示该 SeqID 已被处理过，应跳过当前消息
    /// <c>false</c> 表示该 SeqID 未被处理过，是新的消息
    /// </returns>
    bool TryMarkAsProcessed(ulong seqId);

    /// <summary>
    /// 检查指定 SeqID 是否已处理
    /// </summary>
    /// <param name="seqId">消息序列号</param>
    /// <returns><c>true</c> 表示已处理，<c>false</c> 表示未处理</returns>
    bool IsProcessed(ulong seqId);

    /// <summary>
    /// 检查指定 SeqID 是否已处理（异步版本）
    /// </summary>
    /// <param name="seqId">消息序列号</param>
    /// <returns><c>true</c> 表示已处理，<c>false</c> 表示未处理</returns>
    Task<bool> IsProcessedAsync(ulong seqId);

    /// <summary>
    /// 清空缓存
    /// </summary>
    void ClearCache();

    /// <summary>
    /// 获取当前缓存的 SeqID 数量
    /// </summary>
    int GetCacheCount();

    /// <summary>
    /// 获取已处理的最大 SeqID
    /// </summary>
    /// <returns>最大的 SeqID 值，如果没有则返回 0</returns>
    ulong GetMaxProcessedSeqId();
}
