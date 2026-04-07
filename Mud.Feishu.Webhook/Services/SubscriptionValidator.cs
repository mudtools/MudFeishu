// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件订阅验证器实现
/// 负责验证飞书事件订阅请求的有效性
/// </summary>
public class SubscriptionValidator : ValidatorBase, ISubscriptionValidator
{
    private readonly ILogger<SubscriptionValidator> _logger;
    private readonly IEncryptKeyProvider _encryptKeyProvider;

    /// <summary>
    /// 初始化订阅验证器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="encryptKeyProvider">加密密钥提供程序</param>
    public SubscriptionValidator(
        ILogger<SubscriptionValidator> logger,
        IEncryptKeyProvider encryptKeyProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _encryptKeyProvider = encryptKeyProvider ?? throw new ArgumentNullException(nameof(encryptKeyProvider));
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSubscriptionRequestAsync(EventVerificationRequest request, string expectedToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证请求对象不为空
            if (request == null)
            {
                _logger.LogWarning("验证请求对象为空, AppKey: {AppKey}", _currentAppKey ?? "null");
                return false;
            }

            // 验证请求类型
            if (request.Type != "url_verification")
            {
                _logger.LogWarning("无效的验证请求类型: {Type}, AppKey: {AppKey}", request.Type, _currentAppKey ?? "null");
                return false;
            }

            // 验证 Token 字段
            if (string.IsNullOrEmpty(request.Token))
            {
                _logger.LogWarning("验证请求缺少 Token, AppKey: {AppKey}", _currentAppKey ?? "null");
                return false;
            }

            // 验证 Challenge 字段
            if (string.IsNullOrEmpty(request.Challenge))
            {
                _logger.LogWarning("验证请求缺少 Challenge, AppKey: {AppKey}", _currentAppKey ?? "null");
                return false;
            }

            // 获取期望的 Token，支持多应用配置
            var effectiveExpectedToken = await GetEffectiveTokenAsync(expectedToken);

            if (string.IsNullOrEmpty(effectiveExpectedToken))
            {
                _logger.LogError("无法获取有效的验证 Token, AppKey: {AppKey}", _currentAppKey ?? "null");
                return false;
            }

            // 验证 Token 是否匹配
            if (request.Token != effectiveExpectedToken)
            {
                // 为了安全，不在日志中记录完整的 Token 值，只记录前几位
                var actualTokenPrefix = request.Token?.Length > 4 ? request.Token.Substring(0, 4) + "***" : "***";
                var expectedTokenPrefix = effectiveExpectedToken!.Length > 4 ? effectiveExpectedToken.Substring(0, 4) + "***" : "***";

                _logger.LogWarning("验证 Token 不匹配: 期望 {ExpectedToken}, 实际 {ActualToken}, AppKey: {AppKey}",
                    expectedTokenPrefix, actualTokenPrefix, _currentAppKey ?? "null");
                return false;
            }

            _logger.LogInformation("事件订阅验证请求验证成功, AppKey: {AppKey}", _currentAppKey ?? "null");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误, AppKey: {AppKey}", _currentAppKey ?? "null");
            return false;
        }
    }

    /// <summary>
    /// 获取有效的验证 Token
    /// </summary>
    /// <param name="fallbackToken">后备 Token</param>
    /// <returns>有效的验证 Token</returns>
    private async Task<string?> GetEffectiveTokenAsync(string? fallbackToken)
    {
        try
        {
            // 多应用场景：从密钥提供程序获取验证 Token
            if (!string.IsNullOrEmpty(_currentAppKey))
            {
                var verificationToken = await _encryptKeyProvider.GetVerificationTokenAsync(_currentAppKey!);
                if (!string.IsNullOrEmpty(verificationToken))
                {
                    _logger.LogDebug("使用应用 {AppKey} 的验证 Token", _currentAppKey);
                    return verificationToken;
                }
            }

            // 使用传入的后备 Token（用于非多应用场景的兼容）
            if (!string.IsNullOrEmpty(fallbackToken))
            {
                _logger.LogDebug("使用传入的后备验证 Token, AppKey: {AppKey}", _currentAppKey ?? "null");
                return fallbackToken;
            }

            _logger.LogWarning("未找到有效的验证 Token, AppKey: {AppKey}", _currentAppKey ?? "null");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取有效验证 Token 时发生错误, AppKey: {AppKey}", _currentAppKey ?? "null");
            return null;
        }
    }
}