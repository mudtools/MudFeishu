// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webbook.Configuration;

namespace Mud.Feishu.Webbook.Services;

/// <summary>
/// 飞书事件验证服务实现
/// </summary>
public class FeishuEventValidator : IFeishuEventValidator
{
    private readonly ILogger<FeishuEventValidator> _logger;

    /// <inheritdoc />
    public FeishuEventValidator(ILogger<FeishuEventValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool ValidateSubscriptionRequest(EventVerificationRequest request, string expectedToken)
    {
        try
        {
            if (request.Type != "url_verification")
            {
                _logger.LogWarning("无效的验证请求类型: {Type}", request.Type);
                return false;
            }

            if (string.IsNullOrEmpty(request.Token))
            {
                _logger.LogWarning("验证请求缺少 Token");
                return false;
            }

            if (string.IsNullOrEmpty(request.Challenge))
            {
                _logger.LogWarning("验证请求缺少 Challenge");
                return false;
            }

            if (request.Token != expectedToken)
            {
                _logger.LogWarning("验证 Token 不匹配: 期望 {ExpectedToken}, 实际 {ActualToken}", expectedToken, request.Token);
                return false;
            }

            _logger.LogInformation("事件订阅验证请求验证成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateSignature(long timestamp, string nonce, string encrypt, string signature, string encryptKey)
    {
        try
        {
            // 验证时间戳
            if (!ValidateTimestamp(timestamp))
            {
                _logger.LogWarning("请求时间戳无效: {Timestamp}", timestamp);
                return false;
            }

            // 构建签名字符串
            var signString = $"{timestamp}\n{nonce}\n{encrypt}";

            // 使用 HMAC-SHA256 计算签名
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
            var computedSignature = Convert.ToBase64String(hashBytes);

            // 比较签名
            var isValid = computedSignature == signature;

            if (!isValid)
            {
                _logger.LogWarning("签名验证失败: 计算 {ComputedSignature}, 期望 {ExpectedSignature}", computedSignature, signature);
            }
            else
            {
                _logger.LogDebug("签名验证成功");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证签名时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300)
    {
        try
        {
            var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            var now = DateTimeOffset.UtcNow;
            var diff = Math.Abs((now - requestTime).TotalSeconds);

            var isValid = diff <= toleranceSeconds;

            if (!isValid)
            {
                _logger.LogWarning("时间戳超出容错范围: 请求时间 {RequestTime}, 当前时间 {CurrentTime}, 差异 {Diff}秒",
                    requestTime, now, diff);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证时间戳时发生错误");
            return false;
        }
    }
}