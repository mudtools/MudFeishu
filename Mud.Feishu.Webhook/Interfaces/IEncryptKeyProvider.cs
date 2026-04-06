// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书加密密钥提供程序接口
/// 支持从外部源（如 Azure KeyVault、AWS Secrets Manager、环境变量等）获取加密密钥
/// </summary>
/// <remarks>
/// 使用场景：
/// 1. 从 Azure KeyVault 获取密钥
/// 2. 从 AWS Secrets Manager 获取密钥
/// 3. 从环境变量获取密钥
/// 4. 从自定义密钥管理服务获取密钥
/// </remarks>
public interface IEncryptKeyProvider
{
    /// <summary>
    /// 异步获取指定应用的加密密钥
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>加密密钥；如果未找到返回 null</returns>
    /// <exception cref="ArgumentNullException">appKey 为 null</exception>
    Task<string?> GetEncryptKeyAsync(string appKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取指定应用的验证 Token
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证 Token；如果未找到返回 null</returns>
    /// <exception cref="ArgumentNullException">appKey 为 null</exception>
    Task<string?> GetVerificationTokenAsync(string appKey, CancellationToken cancellationToken = default);
}
