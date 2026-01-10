// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 分布式事件去重接口
/// 支持多种存储后端（内存、Redis、数据库等），适用于分布式部署场景
/// </summary>
public interface IFeishuWebhookDistributedDeduplicator
{
    /// <summary>
    /// 检查并标记事件是否已处理
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="ttl">缓存存活时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果事件已处理返回 true，否则返回 false 并标记</returns>
    Task<bool> TryMarkAsProcessedAsync(string eventId, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查事件是否已处理（不标记）
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果事件已处理返回 true，否则返回 false</returns>
    Task<bool> IsProcessedAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理已过期的事件记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的记录数量</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 分布式 Nonce 去重接口
/// 支持多种存储后端（内存、Redis、数据库等），适用于分布式部署场景
/// </summary>
public interface IFeishuWebhookDistributedNonceDeduplicator
{
    /// <summary>
    /// 检查并标记 nonce 是否已使用
    /// </summary>
    /// <param name="nonce">随机数</param>
    /// <param name="ttl">缓存存活时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果 nonce 已使用返回 true，否则返回 false 并标记</returns>
    Task<bool> TryMarkAsUsedAsync(string nonce, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查 nonce 是否已使用（不标记）
    /// </summary>
    /// <param name="nonce">随机数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果 nonce 已使用返回 true，否则返回 false</returns>
    Task<bool> IsUsedAsync(string nonce, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理已过期的 nonce 记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的记录数量</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
