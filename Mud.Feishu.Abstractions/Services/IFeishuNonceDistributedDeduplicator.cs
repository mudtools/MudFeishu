// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 飞书分布式 Nonce 去重服务接口
/// 适用于分布式部署场景，使用外部存储（如 Redis）实现跨实例的 Nonce 去重
/// 用于防止重放攻击
/// </summary>
public interface IFeishuNonceDistributedDeduplicator : IAsyncDisposable
{
    /// <summary>
    /// 尝试将 Nonce 标记为已使用
    /// </summary>
    /// <param name="nonce">Nonce 值</param>
    /// <param name="ttl">过期时间（可选），不指定则使用默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果 Nonce 已被使用返回 true（重放攻击），否则返回 false 并标记为已使用</returns>
    Task<bool> TryMarkAsUsedAsync(string nonce, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查 Nonce 是否已被使用（不标记）
    /// </summary>
    /// <param name="nonce">Nonce 值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果 Nonce 已被使用返回 true，否则返回 false</returns>
    Task<bool> IsUsedAsync(string nonce, CancellationToken cancellationToken = default);

    /// <summary>
    /// 手动清理过期条目
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的过期条目数量</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
