// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace TaskManageDemo.Backend.Utils;

/// <summary>
/// 敏感数据脱敏器接口
/// </summary>
public interface ISensitiveDataMasker
{
    /// <summary>
    /// 掩码敏感数据
    /// </summary>
    string Mask(string? input, SensitiveDataType type);

    /// <summary>
    /// 掩码对象中的敏感字段
    /// </summary>
    T MaskObject<T>(T obj) where T : class;
}

/// <summary>
/// 敏感数据类型
/// </summary>
public enum SensitiveDataType
{
    /// <summary>令牌/密钥</summary>
    Token,

    /// <summary>密码</summary>
    Password,

    /// <summary>手机号</summary>
    Phone,

    /// <summary>邮箱</summary>
    Email,

    /// <summary>身份证号</summary>
    IdCard,

    /// <summary>银行卡号</summary>
    BankCard,

    /// <summary>通用</summary>
    Generic
}

/// <summary>
/// 敏感数据脱敏器实现
/// </summary>
public class SensitiveDataMasker : ISensitiveDataMasker
{
    private static readonly string[] SensitiveFieldNames = new[]
    {
        "password", "passwd", "pwd",
        "secret", "appsecret", "app_secret",
        "token", "accesstoken", "access_token", "refreshtoken", "refresh_token",
        "apikey", "api_key", "api_secret",
        "authorization", "auth_token",
        "creditcard", "credit_card", "cardnumber", "card_number",
        "ssn", "socialsecurity", "social_security"
    };

    /// <summary>
    /// 掩码敏感数据
    /// </summary>
    public string Mask(string? input, SensitiveDataType type)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return type switch
        {
            SensitiveDataType.Token => MaskToken(input),
            SensitiveDataType.Password => MaskPassword(input),
            SensitiveDataType.Phone => MaskPhone(input),
            SensitiveDataType.Email => MaskEmail(input),
            SensitiveDataType.IdCard => MaskIdCard(input),
            SensitiveDataType.BankCard => MaskBankCard(input),
            SensitiveDataType.Generic => MaskGeneric(input),
            _ => MaskGeneric(input)
        };
    }

    /// <summary>
    /// 掩码对象中的敏感字段
    /// </summary>
    public T MaskObject<T>(T obj) where T : class
    {
        if (obj == null)
            return obj;

        var type = typeof(T);
        var properties = type.GetProperties()
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string));

        foreach (var property in properties)
        {
            var propertyName = property.Name.ToLowerInvariant();
            if (SensitiveFieldNames.Any(sf => propertyName.Contains(sf)))
            {
                var value = property.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    property.SetValue(obj, MaskGeneric(value));
                }
            }
        }

        return obj;
    }

    /// <summary>
    /// 掩码配置字典中的敏感数据
    /// </summary>
    public static Dictionary<string, string?> MaskConfiguration(Dictionary<string, string?> config)
    {
        var masked = new Dictionary<string, string?>();
        foreach (var kvp in config)
        {
            var keyLower = kvp.Key.ToLowerInvariant();
            if (SensitiveFieldNames.Any(sf => keyLower.Contains(sf)))
            {
                masked[kvp.Key] = MaskGeneric(kvp.Value ?? string.Empty);
            }
            else
            {
                masked[kvp.Key] = kvp.Value;
            }
        }
        return masked;
    }

    /// <summary>
    /// 掩码令牌
    /// </summary>
    private static string MaskToken(string token)
    {
        if (token.Length <= 8)
            return "****";

        return token.Substring(0, 4) + "****" + token.Substring(token.Length - 4);
    }

    /// <summary>
    /// 掩码密码
    /// </summary>
    private static string MaskPassword(string password)
    {
        return new string('*', Math.Min(password.Length, 8));
    }

    /// <summary>
    /// 掩码手机号
    /// </summary>
    private static string MaskPhone(string phone)
    {
        if (phone.Length < 7)
            return "****";

        return phone.Substring(0, 3) + "****" + phone.Substring(phone.Length - 4);
    }

    /// <summary>
    /// 掩码邮箱
    /// </summary>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
            return "****" + email.Substring(atIndex);

        var localPart = email.Substring(0, atIndex);
        var domain = email.Substring(atIndex);

        if (localPart.Length <= 2)
            return localPart + "****" + domain;

        return localPart.Substring(0, 2) + "****" + domain;
    }

    /// <summary>
    /// 掩码身份证号
    /// </summary>
    private static string MaskIdCard(string idCard)
    {
        if (idCard.Length < 8)
            return "****";

        return idCard.Substring(0, 4) + "**********" + idCard.Substring(idCard.Length - 4);
    }

    /// <summary>
    /// 掩码银行卡号
    /// </summary>
    private static string MaskBankCard(string cardNumber)
    {
        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 8)
            return "****";

        return digitsOnly.Substring(0, 4) + " **** **** " + digitsOnly.Substring(digitsOnly.Length - 4);
    }

    /// <summary>
    /// 通用掩码（保留前4后4）
    /// </summary>
    private static string MaskGeneric(string input)
    {
        if (input.Length <= 8)
            return new string('*', input.Length);

        return input.Substring(0, 4) + new string('*', input.Length - 8) + input.Substring(input.Length - 4);
    }
}

/// <summary>
/// Serilog 敏感数据脱敏扩展
/// </summary>
public static class SerilogSensitiveDataExtensions
{
    /// <summary>
    /// 掩码日志中的敏感数据
    /// </summary>
    public static string MaskSensitiveData(this string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // 掩码常见的敏感数据模式
        var patterns = new (Regex regex, string replacement)[]
        {
            // Bearer Token
            (new Regex(@"Bearer\s+[\w-]+\.[\w-]+\.[\w-]+", RegexOptions.IgnoreCase), "Bearer ***MASKED***"),
            // 密码字段
            (new Regex(@"(password|passwd|pwd)[:=]\s*[^\s&]+", RegexOptions.IgnoreCase), "$1=***MASKED***"),
            // Secret字段
            (new Regex(@"(secret|appsecret|app_secret)[:=]\s*[^\s&]+", RegexOptions.IgnoreCase), "$1=***MASKED***"),
            // Token字段
            (new Regex(@"(token|access_token|refresh_token)[:=]\s*[^\s&]+", RegexOptions.IgnoreCase), "$1=***MASKED***"),
            // API Key
            (new Regex(@"(apikey|api_key)[:=]\s*[^\s&]+", RegexOptions.IgnoreCase), "$1=***MASKED***"),
        };

        var result = message;
        foreach (var (regex, replacement) in patterns)
        {
            result = regex.Replace(result, replacement);
        }

        return result;
    }
}
