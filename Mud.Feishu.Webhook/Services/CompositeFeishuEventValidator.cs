// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Utilities;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 组合飞书事件验证器
/// 实现原有 IFeishuEventValidator 接口，委托给各个专门的验证器
/// 保持向后兼容性的同时提供单一职责的验证器架构
/// </summary>
public class CompositeFeishuEventValidator : ValidatorBase, IFeishuEventValidator
{
    private readonly ISignatureValidator _signatureValidator;
    private readonly ITimestampValidator _timestampValidator;
    private readonly INonceValidator _nonceValidator;
    private readonly ISubscriptionValidator _subscriptionValidator;
    private readonly ILogger<CompositeFeishuEventValidator> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly IEnvironmentService _environmentService;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _optionsMonitor.CurrentValue;

    /// <summary>
    /// 初始化组合验证器
    /// </summary>
    /// <param name="signatureValidator">签名验证器</param>
    /// <param name="timestampValidator">时间戳验证器</param>
    /// <param name="nonceValidator">Nonce 验证器</param>
    /// <param name="subscriptionValidator">订阅验证器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="optionsMonitor">Webhook 配置选项监视器</param>
    /// <param name="environmentService">环境服务</param>
    public CompositeFeishuEventValidator(
        ISignatureValidator signatureValidator,
        ITimestampValidator timestampValidator,
        INonceValidator nonceValidator,
        ISubscriptionValidator subscriptionValidator,
        ILogger<CompositeFeishuEventValidator> logger,
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        IEnvironmentService? environmentService = null)
    {
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _timestampValidator = timestampValidator ?? throw new ArgumentNullException(nameof(timestampValidator));
        _nonceValidator = nonceValidator ?? throw new ArgumentNullException(nameof(nonceValidator));
        _subscriptionValidator = subscriptionValidator ?? throw new ArgumentNullException(nameof(subscriptionValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _environmentService = environmentService ?? new EnvironmentService();
    }

    /// <inheritdoc />
    public override void SetCurrentAppKey(string appKey)
    {
        base.SetCurrentAppKey(appKey);
        _logger.LogDebug("设置当前应用键: {AppKey}", appKey);

        // 将应用键传播到所有支持多应用的验证器
        _signatureValidator.SetCurrentAppKey(appKey);
        _timestampValidator.SetCurrentAppKey(appKey);
        _nonceValidator.SetCurrentAppKey(appKey);
        _subscriptionValidator.SetCurrentAppKey(appKey);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSubscriptionRequestAsync(EventVerificationRequest request, string expectedToken, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("开始验证订阅请求（异步）");
        return await _subscriptionValidator.ValidateSubscriptionRequestAsync(request, expectedToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSignatureAsync(long timestamp, string nonce, string encrypt, string signature, string encryptKey)
    {
        _logger.LogDebug("开始验证请求签名 - Timestamp: {Timestamp}, Nonce: {Nonce}", timestamp, nonce);

        try
        {
            // 1. 首先验证时间戳
            if (!_timestampValidator.ValidateTimestamp(timestamp, Options.TimestampToleranceSeconds))
            {
                _logger.LogWarning("时间戳验证失败");
                return false;
            }

            // 2. 然后验证 Nonce（防重放攻击）
            if (!await _nonceValidator.ValidateNonceAsync(nonce, _environmentService.IsProduction))
            {
                _logger.LogWarning("Nonce 验证失败");
                return false;
            }

            // 3. 最后验证签名
            var signatureResult = await _signatureValidator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);
            if (!signatureResult)
            {
                _logger.LogWarning("签名验证失败");
                return false;
            }

            _logger.LogDebug("请求签名验证成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求签名时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
    {
        _logger.LogDebug("开始验证请求头签名 - Timestamp: {Timestamp}, Nonce: {Nonce}", timestamp, nonce);

        try
        {
            // 1. 首先验证时间戳
            if (!_timestampValidator.ValidateTimestamp(timestamp, Options.TimestampToleranceSeconds))
            {
                _logger.LogWarning("时间戳验证失败");
                return false;
            }

            // 2. 然后验证 Nonce（防重放攻击）
            if (!await _nonceValidator.ValidateNonceAsync(nonce, _environmentService.IsProduction))
            {
                _logger.LogWarning("Nonce 验证失败");
                return false;
            }

            // 3. 最后验证请求头签名
            var signatureResult = await _signatureValidator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);
            if (!signatureResult)
            {
                _logger.LogWarning("请求头签名验证失败");
                return false;
            }

            _logger.LogDebug("请求头签名验证成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求头签名时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300)
    {
        _logger.LogDebug("验证时间戳 - Timestamp: {Timestamp}, Tolerance: {Tolerance}秒", timestamp, toleranceSeconds);
        return _timestampValidator.ValidateTimestamp(timestamp, toleranceSeconds);
    }
}