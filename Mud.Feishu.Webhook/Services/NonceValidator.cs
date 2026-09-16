// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
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

    /// <summary>
    /// 判断异常是否为可降级的 Redis 连接类故障（T-M2-10 / ADR-6.2）。
    /// <para>仅 <see cref="FeishuRedisFailureKind.Connection"/>（可配置扩展为 <see cref="FeishuRedisFailureKind.Timeout"/>）
    /// 走 <see cref="NonceFailureMode"/> 降级；<see cref="FeishuRedisFailureKind.Server"/> 类故障（如配置错误）按致命错误抛出。</para>
    /// </summary>
    private static bool IsDegradableException(Exception ex)
    {
        if (ex is FeishuRedisException redisEx)
        {
            return redisEx.FailureKind == FeishuRedisFailureKind.Connection
                || redisEx.FailureKind == FeishuRedisFailureKind.Timeout;
        }
        // 非 FeishuRedisException 的异常视为不可降级（应抛出，不静默放行）
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> CheckNonceAsync(string nonce)
    {
        try
        {
            // 空Nonce 处理逻辑与 ValidateNonceAsync 一致
            if (string.IsNullOrEmpty(nonce))
            {
                return true; // 空Nonce视为有效（允许通过），由调用方根据环境决定
            }

            // 仅检查是否已被使用，不标记
            var isUsed = await _nonceDeduplicator.IsUsedAsync(nonce, CurrentAppKey);
            return !isUsed; // 未被使用返回 true（有效）
        }
        catch (Exception ex) when (IsDegradableException(ex))
        {
            // T-M2-10：仅 Connection/Timeout 类故障走降级策略
            Logger.LogError(ex, "检查 Nonce 使用状态时发生可降级错误, Nonce: {Nonce}, AppKey: {AppKey}, 降级策略: {FailureMode}",
                nonce, CurrentAppKey ?? "null", FailureMode);

            return FailureMode != NonceFailureMode.Reject;
        }
        // Server 类故障直接抛出（不做降级）
    }

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
        catch (Exception ex) when (IsDegradableException(ex))
        {
            // T-M2-10：仅 Connection/Timeout 类故障走降级策略
            // - Reject: 认为已使用（返回 true = 拒绝请求，安全优先）
            // - Allow: 认为未使用（返回 false = 允许请求，可用性优先）
            Logger.LogError(ex, "标记 Nonce 时发生可降级错误, Nonce: {Nonce}, AppKey: {AppKey}, 降级策略: {FailureMode}",
                nonce, CurrentAppKey ?? "null", FailureMode);

            return FailureMode != NonceFailureMode.Allow;
        }
        // Server 类故障直接抛出（不做降级）
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
        catch (Exception ex) when (IsDegradableException(ex))
        {
            // T-M2-10：仅 Connection/Timeout 类故障走降级
            Logger.LogError(ex, "验证 Nonce 时发生可降级错误, Nonce: {Nonce}, AppKey: {AppKey}", nonce, CurrentAppKey ?? "null");
            return FailureMode == NonceFailureMode.Allow;
        }
        // Server 类故障直接抛出（不做降级）
    }

}
