// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录下的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 用户令牌存储的「仅失效访问令牌」可选能力契约。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IUserTokenStore"/> 只提供 <c>RemoveAsync</c>（同时删除 access 与 refresh 两个物理键），
/// 无法表达「访问令牌被 IdP 拒绝，但 refresh_token 仍须保留供续期」这一语义。
/// 实现本接口的存储可单独删除访问令牌键，使失效在多实例 / 冷启动场景下同样生效。
/// </para>
/// <para>
/// <b>能力探测口径</b>：调用方必须按 optional-capability（<c>is</c> 探测）使用；
/// 未实现时调用方回落到「仅内存失效」的既有行为。
/// <see cref="UserTokenManager"/> 在<b>未装饰的原始 store</b> 上探测——内部清库门装饰器刻意不声明本能力，
/// 避免内层未实现时探测假命中。实现方：
/// <see cref="FeishuUserTokenStore"/>、<c>Mud.Feishu.Redis.RedisUserTokenStore</c>、
/// 加密装饰器 <see cref="EncryptedUserTokenStore"/>（按内层能力透传）。
/// </para>
/// </remarks>
public interface IUserTokenStoreAccessTokenInvalidation
{
    /// <summary>
    /// 仅失效（删除）指定用户的访问令牌，保留 refresh_token。
    /// </summary>
    /// <remarks>
    /// 键不存在时为无害 no-op（幂等）。实现方不得删除 refresh_token ——
    /// 它是用户令牌唯一的续期路径，清除会使 401 恢复彻底无路。
    /// </remarks>
    /// <param name="userId">用户唯一标识符。</param>
    /// <param name="tokenType">令牌类型标识符。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task InvalidateAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default);
}
