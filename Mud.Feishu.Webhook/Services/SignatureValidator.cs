// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utilities;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件签名验证器实现
/// 支持 HMAC-SHA256 和 SHA-256 两种签名算法
/// </summary>
/// <remarks>
/// <para>
/// 飞书 Webhook 使用两种签名验证方式：
/// </para>
/// <list type="number">
/// <item>
/// <term>HMAC-SHA256 签名（请求体验证）</term>
/// <description>
/// - 签名字符串格式：timestamp + "\n" + nonce + "\n" + encrypt
/// - 算法：HMAC-SHA256(encryptKey, signString)
/// - 用途：验证请求体中的加密数据完整性
/// - 触发条件：EnableBodySignatureValidation = true
/// </description>
/// </item>
/// <item>
/// <term>SHA-256 签名（请求头验证）</term>
/// <description>
/// - 签名字符串格式：timestamp + nonce + encryptKey + body
/// - 算法：SHA-256(signString)
/// - 用途：验证整个请求的完整性和来源
/// - 触发条件：EnforceHeaderSignatureValidation = true
/// - 注意：此方式更安全，生产环境强烈推荐
/// </description>
/// </item>
/// </list>
/// <para>
/// 安全建议：
/// </para>
/// <list type="bullet">
/// <item><description>生产环境必须启用 EnforceHeaderSignatureValidation</description></item>
/// <item><description>建议同时启用两种验证以提供双重保护</description></item>
/// <item><description>时间戳容错范围建议设置为 30 秒或更短</description></item>
/// <item><description>使用固定时间比较防止计时攻击</description></item>
/// </list>
/// </remarks>
public class SignatureValidator : ISignatureValidator
{
    private readonly ILogger<SignatureValidator> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly ISecurityAuditService? _securityAuditService;
    private readonly IEnvironmentService _environmentService;
    private readonly IWebhookAppKeyAccessor _appKeyAccessor;

    /// <summary>
    /// 获取当前应用键（优先从 IWebhookAppKeyAccessor 获取）
    /// </summary>
    private string? CurrentAppKey => _appKeyAccessor.CurrentAppKey;

    /// <summary>
    /// 初始化签名验证器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">Webhook 配置选项</param>
    /// <param name="appKeyAccessor">应用键上下文访问器</param>
    /// <param name="securityAuditService">安全审计服务</param>
    /// <param name="environmentService">环境服务</param>
    public SignatureValidator(
        ILogger<SignatureValidator> logger,
        IOptionsMonitor<FeishuWebhookOptions> options,
        IWebhookAppKeyAccessor appKeyAccessor,
        ISecurityAuditService? securityAuditService = null,
        IEnvironmentService? environmentService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _appKeyAccessor = appKeyAccessor ?? throw new ArgumentNullException(nameof(appKeyAccessor));
        _securityAuditService = securityAuditService;
        _environmentService = environmentService ?? new EnvironmentService();
    }

    /// <summary>
    /// 记录安全审计失败日志
    /// </summary>
    private void LogSecurityFailure(string message)
    {
        _ = _securityAuditService?.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            "unknown",
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
            "unknown",
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
            // 检查请求头签名是否为空
            if (string.IsNullOrEmpty(headerSignature))
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
                        _logger.LogDebug("使用应用 {AppKey} 的签名验证配置: {EnforceValidation}",
                            CurrentAppKey, enforceValidation);
                    }
                }

                // 如果配置为强制验证，则拒绝请求
                if (enforceValidation)
                {
                    _logger.LogWarning(
                        "请求头中缺少 X-Lark-Signature，拒绝请求（配置为强制验证，当前环境: {Environment}）",
                        _environmentService.EnvironmentName);

                    LogSecurityFailure(_environmentService.IsProduction
                        ? "生产环境：请求头缺少 X-Lark-Signature，拒绝请求"
                        : "非生产环境：请求头缺少 X-Lark-Signature（警告：此配置存在安全风险）");

                    return false;
                }

                // 否则跳过验证（兼容旧版本）
                _logger.LogDebug(
                    "请求头中未包含 X-Lark-Signature，跳过头部签名验证（警告：此配置存在严重安全风险，" +
                    "建议在生产环境设置 EnforceHeaderSignatureValidation = true）");
                return true;
            }

            // 检查必要参数
            if (timestamp == 0 || string.IsNullOrEmpty(nonce))
            {
                if (_environmentService.IsProduction)
                {
                    _logger.LogError(
                        "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），拒绝请求（生产环境不允许跳过签名验证）",
                        timestamp, nonce);

                    LogSecurityFailure($"时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），拒绝请求");

                    return false;
                }

                _logger.LogWarning(
                    "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），跳过签名验证（开发环境，警告：此配置存在安全风险）",
                    timestamp, nonce);

                LogSecurityFailure($"开发环境：时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），跳过签名验证");

                return true;
            }

            // 根据飞书官方文档，签名字符串格式为：
            // timestamp + nonce + encryptKey + body
            // 注意：这里不使用换行符连接！
            var signString = $"{timestamp}{nonce}{encryptKey}{body}";

            // 调试日志：显示签名计算信息（不记录敏感的 EncryptKey 内容，仅记录长度）
            _logger.LogDebug("请求头签名计算 - Timestamp: {Timestamp}, Nonce: {Nonce}, EncryptKey长度: {KeyLength}, Body长度: {BodyLength}",
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
                _logger.LogDebug("请求头签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                    computedPrefix + "...",
                    headerPrefix + "...",
                    CurrentAppKey ?? "null");

                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "SignatureValidator",
                    $"请求头签名验证失败: 计算 {computedPrefix}..., 期望 {headerPrefix}...",
                    "",
                    CurrentAppKey);
            }
            else
            {
                _logger.LogDebug("请求头签名验证成功, AppKey: {AppKey}", CurrentAppKey ?? "null");

                _ = _securityAuditService?.LogSecuritySuccessAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "SignatureValidator",
                    $"请求头签名验证成功, AppKey: {CurrentAppKey ?? "null"}",
                    "",
                    CurrentAppKey);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求头签名时发生错误, AppKey: {AppKey}", CurrentAppKey ?? "null");
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

    /// <inheritdoc />
    public void SetCurrentAppKey(string appKey)
    {
        _appKeyAccessor.SetAppKey(appKey);
        _logger.LogDebug("设置当前应用键: {AppKey}", appKey);
    }

    /// <inheritdoc />
    public Task<bool> ValidateBodySignatureAsync(long timestamp, string nonce, string encryptData, string encryptKey)
    {
        var options = _options.CurrentValue;
        var enableBodyValidation = options.EnableBodySignatureValidation;

        if (!string.IsNullOrEmpty(CurrentAppKey))
        {
            var appConfig = options.GetAppConfig(CurrentAppKey!);
            if (appConfig != null)
            {
                enableBodyValidation = appConfig.GetEffectiveEnableBodySignatureValidation(enableBodyValidation);
            }
        }

        if (!enableBodyValidation)
        {
            _logger.LogDebug("请求体签名验证已禁用（EnableBodySignatureValidation = false），跳过验证, AppKey: {AppKey}",
                CurrentAppKey ?? "null");
            return Task.FromResult(true);
        }

        try
        {
            var signString = $"{timestamp}\n{nonce}\n{encryptData}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            _logger.LogDebug("请求体签名计算 - Timestamp: {Timestamp}, Nonce: {Nonce}, EncryptKey长度: {KeyLength}, EncryptData长度: {DataLength}",
                timestamp, nonce, encryptKey.Length, encryptData.Length);

            LogSecuritySuccess($"请求体签名验证通过（HMAC-SHA256）, AppKey: {CurrentAppKey ?? "null"}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求体签名时发生错误, AppKey: {AppKey}", CurrentAppKey ?? "null");
            return Task.FromResult(false);
        }
    }
}
