// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utilities;
using System.Security.Cryptography;
using System.Text;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件签名验证器实现
/// 支持 HMAC-SHA256 和 SHA-256 两种签名算法
/// </summary>
public class SignatureValidator : ValidatorBase, ISignatureValidator
{
    private readonly ILogger<SignatureValidator> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly ISecurityAuditService? _securityAuditService;
    private readonly IEnvironmentService _environmentService;

    /// <summary>
    /// 初始化签名验证器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">Webhook 配置选项</param>
    /// <param name="securityAuditService">安全审计服务</param>
    /// <param name="environmentService">环境服务</param>
    public SignatureValidator(
        ILogger<SignatureValidator> logger,
        IOptionsMonitor<FeishuWebhookOptions> options,
        ISecurityAuditService? securityAuditService = null,
        IEnvironmentService? environmentService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
            _currentAppKey);
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
            _currentAppKey);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSignatureAsync(long timestamp, string nonce, string encrypt, string signature, string encryptKey)
    {
        try
        {
            // 检查必要参数
            if (timestamp == 0 || string.IsNullOrEmpty(nonce))
            {
                if (_environmentService.IsProduction)
                {
                    _logger.LogError(
                        "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），拒绝请求（生产环境不允许跳过签名验证）",
                        timestamp, nonce);

                    // 记录安全审计日志
                    LogSecurityFailure($"时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），拒绝请求");

                    return false; // 生产环境拒绝请求
                }

                _logger.LogWarning(
                    "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），跳过签名验证（开发环境，警告：此配置存在安全风险）",
                    timestamp, nonce);

                // 记录安全审计日志
                LogSecurityFailure($"开发环境：时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），跳过签名验证");

                return true; // 开发环境允许跳过
            }

            // 构建签名字符串：timestamp + "\n" + nonce + "\n" + encrypt
            var signString = $"{timestamp}\n{nonce}\n{encrypt}";

            // 使用 HMAC-SHA256 计算签名
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
            var computedSignature = Convert.ToBase64String(hashBytes);

            // 使用固定时间比较防止计时攻击
            var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
            var expectedBytes = Encoding.UTF8.GetBytes(signature);
            var isValid = FixedTimeEquals(computedBytes, expectedBytes);

            if (!isValid)
            {
                var computedPrefix = computedSignature.Length > 8 ? computedSignature.Substring(0, 8) : computedSignature;
                var signaturePrefix = signature.Length > 8 ? signature.Substring(0, 8) : signature;
                _logger.LogWarning("签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                    computedPrefix + "...",
                    signaturePrefix + "...",
                    _currentAppKey ?? "null");

                // 记录安全审计日志
                LogSecurityFailure($"签名验证失败: 计算 {computedPrefix}..., 期望 {signaturePrefix}...");
            }
            else
            {
                _logger.LogDebug("签名验证成功, AppKey: {AppKey}", _currentAppKey ?? "null");

                // 记录安全审计日志
                LogSecuritySuccess($"签名验证成功, AppKey: {_currentAppKey ?? "null"}");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证签名时发生错误, AppKey: {AppKey}", _currentAppKey ?? "null");
            return false;
        }
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
                if (!string.IsNullOrEmpty(_currentAppKey))
                {
                    var appConfig = options.GetAppConfig(_currentAppKey);
                    if (appConfig != null)
                    {
                        // 注意：当前 FeishuAppWebhookOptions 没有 EnforceHeaderSignatureValidation 属性
                        // 这里使用全局配置
                        _logger.LogDebug("使用应用 {AppKey} 的全局签名验证配置: {EnforceValidation}",
                            _currentAppKey, enforceValidation);
                    }
                }

                // 如果配置为强制验证，则拒绝请求
                if (enforceValidation)
                {
                    _logger.LogWarning(
                        "请求头中缺少 X-Lark-Signature，拒绝请求（配置为强制验证，当前环境: {Environment}）",
                        _environmentService.EnvironmentName);

                    // 记录安全审计日志
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

                    // 记录安全审计日志
                    LogSecurityFailure($"时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），拒绝请求");

                    return false; // 生产环境拒绝请求
                }

                _logger.LogWarning(
                    "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），跳过签名验证（开发环境，警告：此配置存在安全风险）",
                    timestamp, nonce);

                // 记录安全审计日志
                LogSecurityFailure($"开发环境：时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），跳过签名验证");

                return true; // 开发环境允许跳过
            }

            // 根据飞书官方文档，签名字符串格式为：
            // timestamp + nonce + encryptKey + body
            // 注意：这里不使用换行符连接！
            var signString = $"{timestamp}{nonce}{encryptKey}{body}";

            // 调试日志：显示签名计算信息（不记录敏感的 EncryptKey 内容，仅记录长度）
            _logger.LogDebug("请求头签名计算 - Timestamp: {Timestamp}, Nonce: {Nonce}, EncryptKey长度: {KeyLength}, Body长度: {BodyLength}",
                timestamp, nonce, encryptKey.Length, body.Length);

            // 使用 SHA-256 计算签名（不是 HMAC-SHA256！）
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signString));
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

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
                _logger.LogWarning("请求头签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                    computedPrefix + "...",
                    headerPrefix + "...",
                    _currentAppKey ?? "null");

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "SignatureValidator",
                    $"请求头签名验证失败: 计算 {computedPrefix}..., 期望 {headerPrefix}...",
                    "",
                    _currentAppKey);
            }
            else
            {
                _logger.LogDebug("请求头签名验证成功, AppKey: {AppKey}", _currentAppKey ?? "null");

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecuritySuccessAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "SignatureValidator",
                    $"请求头签名验证成功, AppKey: {_currentAppKey ?? "null"}",
                    "",
                    _currentAppKey);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求头签名时发生错误, AppKey: {AppKey}", _currentAppKey ?? "null");
            return false;
        }
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