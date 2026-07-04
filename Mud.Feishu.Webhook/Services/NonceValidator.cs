// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件 Nonce 验证器实现
/// 负责防重放攻击的 Nonce 去重验证，支持多应用场景下的隔离
/// </summary>
/// <remarks>
/// 初始化 Nonce 验证器
/// </remarks>
/// <param name="logger">日志记录器</param>
/// <param name="nonceDeduplicator">Nonce 去重器服务</param>
/// <param name="appKeyAccessor">应用键上下文访问器</param>
/// <param name="optionsMonitor">Webhook 配置选项监控器</param>
public class NonceValidator(
    ILogger<NonceValidator> logger,
    IFeishuNonceDistributedDeduplicator nonceDeduplicator,
    IWebhookAppKeyAccessor appKeyAccessor,
    IOptionsMonitor<FeishuWebhookOptions> optionsMonitor) : WebhookValidatorBase(appKeyAccessor, logger), INonceValidator
{
    private readonly IFeishuNonceDistributedDeduplicator _nonceDeduplicator = nonceDeduplicator ?? throw new ArgumentNullException(nameof(nonceDeduplicator));
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <summary>
    /// 获取当前 Nonce 验证降级策略
    /// </summary>
    private NonceFailureMode FailureMode => _optionsMonitor.CurrentValue.NonceValidationFailureMode;

    /// <inheritdoc />
    public async Task<bool> TryMarkNonceAsUsedAsync(string nonce)
    {
        try
        {
            // TryMarkAsUsedAsync 返回 true 表示 Nonce 已被使用（重放攻击）
            // 返回 false 表示 Nonce 未被使用，并成功标记为已使用
            var isAlreadyUsed = await _nonceDeduplicator.TryMarkAsUsedAsync(nonce, CurrentAppKey);

            if (isAlreadyUsed)
            {
                Logger.LogWarning("Nonce {Nonce} 已使用过（AppKey: {AppKey}），检测到重放攻击", nonce, CurrentAppKey ?? "null");
            }
            else
            {
                Logger.LogDebug("Nonce {Nonce} 验证通过并已标记为已使用（AppKey: {AppKey}）", nonce, CurrentAppKey ?? "null");
            }

            return isAlreadyUsed;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "检查 Nonce 使用状态时发生错误, Nonce: {Nonce}, AppKey: {AppKey}, 降级策略: {FailureMode}",
                nonce, CurrentAppKey ?? "null", FailureMode);

            // 根据配置的降级策略决定异常时的行为
            // - Reject: 认为已使用（返回 true = 拒绝请求，安全优先）
            // - Allow: 认为未使用（返回 false = 允许请求，可用性优先）
            return FailureMode != NonceFailureMode.Allow;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateNonceAsync(string nonce, bool isProductionEnvironment = true)
    {
        try
        {
            // 检查 Nonce 是否为空
            if (string.IsNullOrEmpty(nonce))
            {
                if (isProductionEnvironment)
                {
                    Logger.LogError(
                        "Nonce 为空，拒绝请求（生产环境不允许空 Nonce），AppKey: {AppKey}",
                        CurrentAppKey ?? "null");
                    return false; // 生产环境拒绝空 Nonce
                }
                else
                {
                    Logger.LogWarning(
                        "Nonce 为空，跳过验证（开发环境，警告：此配置存在安全风险），AppKey: {AppKey}",
                        CurrentAppKey ?? "null");
                    return true; // 开发环境允许空 Nonce
                }
            }

            // 检查 Nonce 是否已被使用
            var isAlreadyUsed = await TryMarkNonceAsUsedAsync(nonce);

            // 如果已被使用，则验证失败（重放攻击）
            return !isAlreadyUsed;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "验证 Nonce 时发生错误, Nonce: {Nonce}, AppKey: {AppKey}", nonce, CurrentAppKey ?? "null");
            return false;
        }
    }

}
