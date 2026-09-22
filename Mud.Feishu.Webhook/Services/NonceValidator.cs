// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Abstractions.Utilities;
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
    /// 空标识符是否拒绝（WHF-05，fail-closed 默认）
    /// </summary>
    private bool RejectEmptyIdentifiers => _optionsMonitor.CurrentValue.RejectEmptyIdentifiers;

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
    public Task<bool> CheckNonceAsync(string nonce) => CheckNonceAsync(nonce, CancellationToken.None);

    /// <inheritdoc />
    public async Task<bool> CheckNonceAsync(string nonce, CancellationToken cancellationToken)
    {
        // WHF-05：空 Nonce 无法防重放——fail-closed
        if (string.IsNullOrEmpty(nonce))
        {
            if (RejectEmptyIdentifiers)
            {
                Logger.LogWarning("Nonce 为空，拒绝请求（RejectEmptyIdentifiers=true），AppKey: {AppKey}", CurrentAppKey ?? "null");
                return false;
            }
            return true; // 空 Nonce 视为有效（允许通过），由调用方根据环境决定
        }

        try
        {
            // 仅检查是否已被使用，不标记
            var isUsed = await _nonceDeduplicator.IsUsedAsync(nonce, CurrentAppKey);
            return !isUsed; // 未被使用返回 true（有效）
        }
        catch (Exception ex) when (IsDegradableException(ex))
        {
            // T-M2-10：仅 Connection/Timeout 类故障走降级策略
            Logger.LogError(ex, "检查 Nonce 使用状态时发生可降级错误, Nonce: {Nonce}, AppKey: {AppKey}, 降级策略: {FailureMode}",
                LogSanitizer.Clean(nonce), CurrentAppKey ?? "null", FailureMode);

            if (FailureMode == NonceFailureMode.Allow)
                return true;   // 可用性优先：视为未使用（放行）——须由告警看住

            // R3-P0-3/D3：Reject 的语义是“本请求未被处理，且要求对端稍后重推”——**不是**验签失败，
            // 更不是重放攻击。此前返回 false（=“验证失败”）会被上层当作验签失败映射 403，
            // 而 403 是终态、飞书不重推，且不写失败存储 → 事件永久丢失。
            // 现改为抛 FeishuDeduplicationFatalException（FailureKind=Server），
            // 复用既有链路转 503（中间件 FeishuMultiAppMiddleware 的 catch when FailureKind==Server）。
            throw new FeishuDeduplicationFatalException(
                "Nonce 去重服务不可用（NonceValidationFailureMode=Reject），无法确认重放状态", ex);
        }
        // Server 类故障直接抛出（不做降级）
    }

    /// <inheritdoc />
    public Task<bool> TryMarkNonceAsUsedAsync(string nonce) => TryMarkNonceAsUsedAsync(nonce, CancellationToken.None);

    /// <inheritdoc />
    public async Task<bool> TryMarkNonceAsUsedAsync(string nonce, CancellationToken cancellationToken)
    {
        // WHF-05：空 Nonce 无法标记去重——fail-closed（返回 true=已使用，上层判定拒绝）
        if (string.IsNullOrEmpty(nonce))
        {
            if (RejectEmptyIdentifiers)
            {
                Logger.LogWarning("Nonce 为空，拒绝请求（RejectEmptyIdentifiers=true），AppKey: {AppKey}", CurrentAppKey ?? "null");
                return true;
            }
            return false;
        }

        try
        {
            // TryMarkAsUsedAsync 返回 true 表示 Nonce 已被使用（重放攻击）
            // 返回 false 表示 Nonce 未被使用，并成功标记为已使用
            var isAlreadyUsed = await _nonceDeduplicator.TryMarkAsUsedAsync(nonce, CurrentAppKey);

            if (isAlreadyUsed)
            {
                Logger.LogWarning("Nonce {Nonce} 已使用过（AppKey: {AppKey}），检测到重放攻击", LogSanitizer.Clean(nonce), CurrentAppKey ?? "null");
            }
            else
            {
                Logger.LogDebug("Nonce {Nonce} 验证通过并已标记为已使用（AppKey: {AppKey}）", LogSanitizer.Clean(nonce), CurrentAppKey ?? "null");
            }

            return isAlreadyUsed;
        }
        catch (Exception ex) when (IsDegradableException(ex))
        {
            // T-M2-10：仅 Connection/Timeout 类故障走降级策略
            Logger.LogError(ex, "标记 Nonce 时发生可降级错误, Nonce: {Nonce}, AppKey: {AppKey}, 降级策略: {FailureMode}",
                LogSanitizer.Clean(nonce), CurrentAppKey ?? "null", FailureMode);

            if (FailureMode == NonceFailureMode.Allow)
                return false;   // 可用性优先：视为未使用（放行）——须由告警看住

            // R3-P0-3/D3：此前 Reject 返回 true（=“已被使用”），上层据此记“检测到重放攻击”
            // 并映射 403；而 403 是终态、飞书不重推，且不写失败存储 → 事件永久丢失，
            // 且把基础设施故障伪装成安全攻击事件污染审计。
            // 现改为抛 FeishuDeduplicationFatalException（FailureKind=Server）→ 既有链路转 503 → 飞书重推。
            throw new FeishuDeduplicationFatalException(
                "Nonce 去重服务不可用（NonceValidationFailureMode=Reject），已拒绝本次请求并要求对端重推", ex);
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
            Logger.LogError(ex, "验证 Nonce 时发生可降级错误, Nonce: {Nonce}, AppKey: {AppKey}, 降级策略: {FailureMode}",
                LogSanitizer.Clean(nonce), CurrentAppKey ?? "null", FailureMode);

            if (FailureMode == NonceFailureMode.Allow)
                return true;

            // R3-P0-3/D3：同 TryMarkNonceAsUsedAsync 口径——Reject 表示“未处理、请重推”，不是验证失败。
            throw new FeishuDeduplicationFatalException(
                "Nonce 去重服务不可用（NonceValidationFailureMode=Reject），无法完成重放防护验证", ex);
        }
        // Server 类故障直接抛出（不做降级）
    }

}
