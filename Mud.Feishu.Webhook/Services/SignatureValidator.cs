// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件签名验证器实现
/// 使用 SHA-256 算法验证请求头签名（X-Lark-Signature）
/// </summary>
/// <remarks>
/// <para>
/// 飞书 Webhook 签名验证方式：
/// </para>
/// <list type="number">
/// <item>
/// <term>SHA-256 签名（请求头验证）</term>
/// <description>
/// - 签名字符串格式：timestamp + nonce + encryptKey + body
/// - 算法：SHA-256(signString)
/// - 用途：验证整个请求的完整性和来源
/// - 触发条件：EnforceHeaderSignatureValidation = true
/// - 注意：此方式与飞书官方 SDK（Python/Go）一致，生产环境强烈推荐
/// </description>
/// </item>
/// </list>
/// <para>
/// 安全建议：
/// </para>
/// <list type="bullet">
/// <item><description>生产环境必须启用 EnforceHeaderSignatureValidation</description></item>
/// <item><description>时间戳容错范围建议设置为 30 秒或更短</description></item>
/// <item><description>使用固定时间比较防止计时攻击</description></item>
/// </list>
/// </remarks>
/// <remarks>
/// 初始化签名验证器
/// </remarks>
/// <param name="logger">日志记录器</param>
/// <param name="options">Webhook 配置选项</param>
/// <param name="appKeyAccessor">应用键上下文访问器</param>
/// <param name="securityAuditService">安全审计服务</param>
/// <param name="environmentService">环境服务</param>
/// <param name="httpContextAccessor">HTTP 上下文访问器（可选，用于获取客户端 IP）</param>
public class SignatureValidator(
    ILogger<SignatureValidator> logger,
    IOptionsMonitor<FeishuWebhookOptions> options,
    IWebhookAppKeyAccessor appKeyAccessor,
    ISecurityAuditService? securityAuditService = null,
    IEnvironmentService? environmentService = null,
    IHttpContextAccessor? httpContextAccessor = null) : WebhookValidatorBase(appKeyAccessor, logger), ISignatureValidator
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ISecurityAuditService? _securityAuditService = securityAuditService;
    private readonly IEnvironmentService _environmentService = environmentService ?? new EnvironmentService();
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// 获取当前客户端 IP（从 HttpContext 中提取）
    /// </summary>
    private string CurrentClientIp => _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// 记录安全审计失败日志
    /// </summary>
    private void LogSecurityFailure(string message)
    {
        _ = _securityAuditService?.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            CurrentClientIp,
            "SignatureValidator",
            message,
            "",
            CurrentAppKey);
    }

    /// <summary>
    /// 记录安全审计成功日志
    /// </summary>
    private void LogSecuritySuccess(string message)
    {
        _ = _securityAuditService?.LogSecuritySuccessAsync(
            SecurityEventType.SignatureValidation,
            CurrentClientIp,
            "SignatureValidator",
            message,
            "",
            CurrentAppKey);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
    {
        try
        {
            // 获取当前配置
            var options = _options.CurrentValue;
            var enforceValidation = options.EnforceHeaderSignatureValidation;

            // 多应用场景：检查应用特定配置
            if (!string.IsNullOrEmpty(CurrentAppKey))
            {
                var appConfig = options.GetAppConfig(CurrentAppKey!);
                if (appConfig != null)
                {
                    // 使用应用级配置，null 时继承全局配置
                    enforceValidation = appConfig.GetEffectiveEnforceHeaderSignatureValidation(enforceValidation);
                    Logger.LogDebug("使用应用 {AppKey} 的签名验证配置: {EnforceValidation}",
                        CurrentAppKey, enforceValidation);
                }
            }

            // 检查请求头签名是否为空
            if (string.IsNullOrEmpty(headerSignature))
            {
                // 如果配置为强制验证，则拒绝请求
                if (enforceValidation)
                {
                    Logger.LogWarning(
                        "请求头中缺少 X-Lark-Signature，拒绝请求（配置为强制验证，当前环境: {Environment}）",
                        _environmentService.EnvironmentName);

                    LogSecurityFailure(_environmentService.IsProduction
                        ? "生产环境：请求头缺少 X-Lark-Signature，拒绝请求"
                        : "非生产环境：请求头缺少 X-Lark-Signature（警告：此配置存在安全风险）");

                    return false;
                }

                // 否则跳过验证（兼容旧版本）
                Logger.LogDebug(
                    "请求头中未包含 X-Lark-Signature，跳过头部签名验证（警告：此配置存在严重安全风险，" +
                    "建议在生产环境设置 EnforceHeaderSignatureValidation = true）");
                return true;
            }

            // 检查必要参数
            if (timestamp == 0 || string.IsNullOrEmpty(nonce))
            {
                // NEW-SEC-01 修复：统一使用 enforceValidation 配置控制，而非环境判断
                if (enforceValidation)
                {
                    Logger.LogError(
                        "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），拒绝请求（enforceValidation=true 不允许跳过签名验证）",
                        timestamp, nonce);

                    LogSecurityFailure($"时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），拒绝请求（enforceValidation=true）");

                    return false;
                }

                Logger.LogWarning(
                    "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），跳过签名验证（enforceValidation=false，警告：此配置存在安全风险）",
                    timestamp, nonce);

                LogSecurityFailure($"timestamp/nonce 缺失但 enforceValidation=false，跳过验证");

                return true;
            }

            // 根据飞书官方文档，签名字符串格式为：
            // timestamp + nonce + encryptKey + body
            // 注意：这里不使用换行符连接！
            var signString = $"{timestamp}{nonce}{encryptKey}{body}";

            // 调试日志：显示签名计算信息（不记录敏感的 EncryptKey 内容，仅记录长度）
            Logger.LogDebug("请求头签名计算 - Timestamp: {Timestamp}, Nonce: {Nonce}, EncryptKey长度: {KeyLength}, Body长度: {BodyLength}",
                timestamp, nonce, encryptKey.Length, body.Length);

            // 使用 SHA-256 计算签名（不是 HMAC-SHA256！）
            var computedSignature = ComputeSha256Signature(signString);

            // 使用固定时间比较防止计时攻击
            var isValid = !string.IsNullOrEmpty(headerSignature) &&
                FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedSignature),
                    Encoding.UTF8.GetBytes(headerSignature));

            if (!isValid)
            {
                var computedPrefix = computedSignature.Length > 8 ? computedSignature.Substring(0, 8) : computedSignature;
                var headerPrefix = headerSignature is null ? "null" :
                    (headerSignature.Length > 8 ? headerSignature.Substring(0, 8) : headerSignature);
                Logger.LogDebug("请求头签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                    computedPrefix + "...",
                    headerPrefix + "...",
                    CurrentAppKey ?? "null");

                LogSecurityFailure($"请求头签名验证失败: 计算 {computedPrefix}..., 期望 {headerPrefix}...");
            }
            else
            {
                Logger.LogDebug("请求头签名验证成功, AppKey: {AppKey}", CurrentAppKey ?? "null");

                LogSecuritySuccess($"请求头签名验证成功, AppKey: {CurrentAppKey ?? "null"}");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "验证请求头签名时发生错误, AppKey: {AppKey}", CurrentAppKey ?? "null");
            return false;
        }
    }

    /// <summary>
    /// 计算 SHA-256 签名
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>十六进制小写签名字符串</returns>
    /// <remarks>
    /// 用于飞书事件签名验证，返回小写十六进制格式的签名字符串
    /// </remarks>
    public static string ComputeSha256Signature(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 固定时间比较方法，防止计时攻击
    /// </summary>
    /// <param name="left">第一个字节数组</param>
    /// <param name="right">第二个字节数组</param>
    /// <returns>如果两个数组相等返回 true，否则返回 false</returns>
    public static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < left.Length; i++)
        {
            result |= left[i] ^ right[i];
        }

        return result == 0;
    }

}
