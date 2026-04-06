// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.HttpUtils;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 默认的加密密钥提供程序
/// 从配置文件中读取密钥（向后兼容实现）
/// </summary>
/// <remarks>
/// 安全警告：
/// - 此实现直接从配置文件读取密钥，不建议在生产环境使用
/// - 生产环境建议实现自定义 IEncryptKeyProvider，从安全的密钥存储获取
/// - 如 Azure KeyVault、AWS Secrets Manager 或 HashiCorp Vault
/// </remarks>
public class DefaultEncryptKeyProvider : IEncryptKeyProvider
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly ILogger<DefaultEncryptKeyProvider> _logger;

    /// <summary>
    /// 初始化默认加密密钥提供程序
    /// </summary>
    /// <param name="optionsMonitor">配置监视器</param>
    /// <param name="logger">日志记录器</param>
    public DefaultEncryptKeyProvider(
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        ILogger<DefaultEncryptKeyProvider> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<string?> GetEncryptKeyAsync(string appKey, CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(appKey, nameof(appKey));

        var options = _optionsMonitor.CurrentValue;
        var appConfig = options.GetAppConfig(appKey);

        if (appConfig == null)
        {
            _logger.LogWarning("未找到应用 {AppKey} 的配置", appKey);
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrEmpty(appConfig.EncryptKey))
        {
            _logger.LogWarning("应用 {AppKey} 的 EncryptKey 为空", appKey);
            return Task.FromResult<string?>(null);
        }

        _logger.LogDebug("从配置文件获取应用 {AppKey} 的加密密钥", appKey);
        return Task.FromResult<string?>(appConfig.EncryptKey);
    }

    /// <inheritdoc />
    public Task<string?> GetVerificationTokenAsync(string appKey, CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(appKey, nameof(appKey));

        var options = _optionsMonitor.CurrentValue;
        var appConfig = options.GetAppConfig(appKey);

        if (appConfig == null)
        {
            _logger.LogWarning("未找到应用 {AppKey} 的配置", appKey);
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrEmpty(appConfig.VerificationToken))
        {
            _logger.LogWarning("应用 {AppKey} 的 VerificationToken 为空", appKey);
            return Task.FromResult<string?>(null);
        }

        _logger.LogDebug("从配置文件获取应用 {AppKey} 的验证 Token", appKey);
        return Task.FromResult<string?>(appConfig.VerificationToken);
    }
}
