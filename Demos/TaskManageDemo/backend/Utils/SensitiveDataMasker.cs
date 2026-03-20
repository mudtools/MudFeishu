// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace TaskManageDemo.Backend.Utils;

/// <summary>
/// 敏感数据脱敏工具
/// </summary>
public static class SensitiveDataMasker
{
    /// <summary>
    /// 敏感字段名称（不区分大小写）
    /// </summary>
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "secret",
        "token",
        "apikey",
        "api_key",
        "accesstoken",
        "access_token",
        "refreshtoken",
        "refresh_token",
        "appsecret",
        "app_secret",
        "privatekey",
        "private_key",
        "creditcard",
        "credit_card",
        "ssn",
        "socialsecurity",
        "idcard",
        "phone",
        "mobile",
        "email"
    };

    /// <summary>
    /// 脱敏手机号
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns>脱敏后的手机号</returns>
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        if (phone.Length <= 7)
            return "***" + phone[^1..];

        return phone[..3] + "****" + phone[^4..];
    }

    /// <summary>
    /// 脱敏邮箱
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>脱敏后的邮箱</returns>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
            return "***" + email;

        var prefix = email[..atIndex];
        var domain = email[atIndex..];

        var maskedPrefix = prefix.Length <= 2
            ? prefix[0] + "***"
            : prefix[0] + "***" + prefix[^1];

        return maskedPrefix + domain;
    }

    /// <summary>
    /// 脱敏身份证号
    /// </summary>
    /// <param name="idCard">身份证号</param>
    /// <returns>脱敏后的身份证号</returns>
    public static string MaskIdCard(string? idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard))
            return string.Empty;

        if (idCard.Length <= 10)
            return "****" + idCard[^4..];

        return idCard[..6] + "********" + idCard[^4..];
    }

    /// <summary>
    /// 脱敏银行卡号
    /// </summary>
    /// <param name="cardNumber">银行卡号</param>
    /// <returns>脱敏后的银行卡号</returns>
    public static string MaskBankCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return string.Empty;

        if (cardNumber.Length <= 8)
            return "****" + cardNumber[^4..];

        return cardNumber[..4] + "****" + cardNumber[^4..];
    }

    /// <summary>
    /// 脱敏 Token 或密钥
    /// </summary>
    /// <param name="token">Token 或密钥</param>
    /// <returns>脱敏后的字符串</returns>
    public static string MaskToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        if (token.Length <= 8)
            return "****";

        return token[..4] + "****" + token[^4..];
    }

    /// <summary>
    /// 脱敏字符串中间部分
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="visibleChars">两端可见字符数</param>
    /// <returns>脱敏后的字符串</returns>
    public static string MaskMiddle(string? value, int visibleChars = 2)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (value.Length <= visibleChars * 2)
            return new string('*', value.Length);

        var start = value[..visibleChars];
        var end = value[^visibleChars..];
        var middleLength = value.Length - visibleChars * 2;

        return start + new string('*', middleLength) + end;
    }

    /// <summary>
    /// 脱敏 JSON 字符串中的敏感字段
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>脱敏后的 JSON 字符串</returns>
    public static string MaskJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        // 匹配 JSON 中的键值对
        var pattern = @"""([^""]+)""\s*:\s*""([^""]*)""";
        
        return Regex.Replace(json, pattern, match =>
        {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;

            if (IsSensitiveField(key))
            {
                var maskedValue = GetMaskedValue(key, value);
                return $@"""{key}"":""{maskedValue}""";
            }

            return match.Value;
        });
    }

    /// <summary>
    /// 判断是否是敏感字段
    /// </summary>
    private static bool IsSensitiveField(string fieldName)
    {
        return SensitiveFields.Contains(fieldName);
    }

    /// <summary>
    /// 根据字段名获取脱敏后的值
    /// </summary>
    private static string GetMaskedValue(string fieldName, string value)
    {
        var lowerName = fieldName.ToLowerInvariant();

        if (lowerName.Contains("phone") || lowerName.Contains("mobile"))
            return MaskPhone(value);
        
        if (lowerName.Contains("email"))
            return MaskEmail(value);
        
        if (lowerName.Contains("idcard") || lowerName.Contains("ssn"))
            return MaskIdCard(value);
        
        if (lowerName.Contains("card"))
            return MaskBankCard(value);
        
        if (lowerName.Contains("token") || lowerName.Contains("secret") || lowerName.Contains("key"))
            return MaskToken(value);

        return MaskMiddle(value);
    }

    /// <summary>
    /// 完全隐藏字符串
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>完全隐藏的字符串</returns>
    public static string Hide(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string('*', value.Length);
    }
}
