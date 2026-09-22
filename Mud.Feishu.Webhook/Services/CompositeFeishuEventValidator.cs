// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Abstractions.Utilities;
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
    public Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
        => ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey, CancellationToken.None);

    /// <inheritdoc />
    public async Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey, CancellationToken cancellationToken)
    {
        Logger.LogDebug("开始验证请求头签名 - Timestamp: {Timestamp}, Nonce: {Nonce}",
            timestamp, LogSanitizer.Clean(nonce));

        try
        {
            // 1. 首先验证时间戳（传 null 让验证器从配置读取应用级或全局级容差）
            if (!_timestampValidator.ValidateTimestamp(timestamp, null))
            {
                Logger.LogWarning("时间戳验证失败");
                return false;
            }

            // WHF-R2/B2：移除 CheckNonceAsync 预检查（原第 2 步），省 1 RTT。
            // 防重放正确性由 TryMarkNonceAsUsedAsync 的 SET NX EX 单独保证（已核实原子）。
            // 语义不变：签名失败 → 不触碰 nonce（Mark 在验签后）；签名通过 + nonce 已用 → Mark 返回 true → 拒绝。

            // 2. 验证请求头签名
            var signatureResult = await _signatureValidator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey, cancellationToken);
            if (!signatureResult)
            {
                Logger.LogWarning("请求头签名验证失败");
                return false;
            }

            // 3. 签名验证通过后，标记 Nonce 为已使用（防重放攻击）
            // 此时标记是安全的：签名已验证通过，不会因为签名失败导致 Nonce 被误消费
            // TryMarkNonceAsUsedAsync 返回 true 表示 Nonce 已被使用（重放攻击），false 表示成功标记
            if (await _nonceValidator.TryMarkNonceAsUsedAsync(nonce, cancellationToken))
            {
                // 并发场景：在预检查和标记之间，其他请求可能已标记了同一 Nonce
                Logger.LogWarning("Nonce 在签名验证后被其他请求标记为已使用，检测到重放攻击");
                return false;
            }

            Logger.LogDebug("请求头签名验证成功");
            return true;
        }
        catch (OperationCanceledException)
        {
            // R3-P1-6/WHF-16：客户端断开 / 宿主关停——交由中间件的 OCE 分支处理，
            // 禁止伪装成“验签失败 403”写向已中止连接（此前该 OCE 被下方 catch 吞成 false，
            // 使中间件 :261 的 WHF-16 分支在验签链路上永不可达）。
            throw;
        }
        catch (Exception ex) when (ex is not FeishuRedisException { FailureKind: FeishuRedisFailureKind.Server })
        {
            Logger.LogError(ex, "验证请求头签名时发生错误");
            return false;
        }
        // WHF-02：Server 类 FeishuRedisException 直接上抛，由中间件转为 503——
        // T-M2-10 契约：去重体系致命故障不得伪装成“验签失败 403”
    }

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int? toleranceSeconds = null)
    {
        Logger.LogDebug("验证时间戳 - Timestamp: {Timestamp}, Tolerance: {Tolerance}秒", timestamp, toleranceSeconds?.ToString() ?? "(从配置读取)");
        return _timestampValidator.ValidateTimestamp(timestamp, toleranceSeconds);
    }
}
