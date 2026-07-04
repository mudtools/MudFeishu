// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 组合飞书事件验证器
/// 实现原有 IFeishuEventValidator 接口，委托给各个专门的验证器
/// 保持向后兼容性的同时提供单一职责的验证器架构
/// </summary>
/// <remarks>
/// 各子验证器通过 IWebhookAppKeyAccessor 自动获取当前 AppKey，
/// 无需在组合验证器中手动传播 SetCurrentAppKey。
/// </remarks>
public class CompositeFeishuEventValidator : WebhookValidatorBase, IFeishuEventValidator
{
    private readonly ISignatureValidator _signatureValidator;
    private readonly ITimestampValidator _timestampValidator;
    private readonly INonceValidator _nonceValidator;
    private readonly ISubscriptionValidator _subscriptionValidator;
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
    /// <param name="appKeyAccessor">应用键上下文访问器</param>
    /// <param name="environmentService">环境服务</param>
    public CompositeFeishuEventValidator(
        ISignatureValidator signatureValidator,
        ITimestampValidator timestampValidator,
        INonceValidator nonceValidator,
        ISubscriptionValidator subscriptionValidator,
        ILogger<CompositeFeishuEventValidator> logger,
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        IWebhookAppKeyAccessor appKeyAccessor,
        IEnvironmentService? environmentService = null)
        : base(appKeyAccessor, logger)
    {
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _timestampValidator = timestampValidator ?? throw new ArgumentNullException(nameof(timestampValidator));
        _nonceValidator = nonceValidator ?? throw new ArgumentNullException(nameof(nonceValidator));
        _subscriptionValidator = subscriptionValidator ?? throw new ArgumentNullException(nameof(subscriptionValidator));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _environmentService = environmentService ?? new EnvironmentService();
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSubscriptionRequestAsync(EventVerificationRequest request, string expectedToken, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("开始验证订阅请求（异步）");
        return await _subscriptionValidator.ValidateSubscriptionRequestAsync(request, expectedToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
    {
        Logger.LogDebug("开始验证请求头签名 - Timestamp: {Timestamp}, Nonce: {Nonce}", timestamp, nonce);

        try
        {
            // 1. 首先验证时间戳（传 null 让验证器从配置读取应用级或全局级容差）
            if (!_timestampValidator.ValidateTimestamp(timestamp, null))
            {
                Logger.LogWarning("时间戳验证失败");
                return false;
            }

            // 2. 然后验证 Nonce（防重放攻击）
            if (!await _nonceValidator.ValidateNonceAsync(nonce, _environmentService.IsProduction))
            {
                Logger.LogWarning("Nonce 验证失败");
                return false;
            }

            // 3. 最后验证请求头签名
            var signatureResult = await _signatureValidator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);
            if (!signatureResult)
            {
                Logger.LogWarning("请求头签名验证失败");
                return false;
            }

            Logger.LogDebug("请求头签名验证成功");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "验证请求头签名时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int? toleranceSeconds = null)
    {
        Logger.LogDebug("验证时间戳 - Timestamp: {Timestamp}, Tolerance: {Tolerance}秒", timestamp, toleranceSeconds?.ToString() ?? "(从配置读取)");
        return _timestampValidator.ValidateTimestamp(timestamp, toleranceSeconds);
    }
}
