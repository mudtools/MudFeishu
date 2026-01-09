// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息脱敏工具
/// </summary>
public static class MessageSanitizer
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "app_access_token",
        "appAccessToken",
        "token",
        "password",
        "secret",
        "access_token",
        "refresh_token",
        "auth_token",
        "session_token",
        "api_key",
        "private_key",
        "phone",
        "mobile",
        "email",
        "id_card"
    };

    /// <summary>
    /// 脱敏消息内容
    /// </summary>
    /// <param name="message">原始消息</param>
    /// <param name="maxLength">最大输出长度（截断用）</param>
    /// <returns>脱敏后的消息</returns>
    public static string Sanitize(string message, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        // 如果消息过长，先截断
        if (message.Length > maxLength)
            message = message.Substring(0, maxLength) + "...";

        try
        {
            var json = JsonDocument.Parse(message);
            var sanitized = SanitizeJsonElement(json.RootElement);
            return sanitized.ToString();
        }
        catch (JsonException)
        {
            // 不是JSON格式，直接返回截断后的内容
            return message;
        }
    }

    /// <summary>
    /// 递归脱敏JSON元素
    /// </summary>
    private static JsonElement SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = new Dictionary<string, JsonElement>();
                foreach (var property in element.EnumerateObject())
                {
                    if (SensitiveFields.Contains(property.Name))
                    {
                        properties[property.Name] = JsonSerializer.Deserialize<JsonElement>("\"***\"");
                    }
                    else
                    {
                        properties[property.Name] = SanitizeJsonElement(property.Value);
                    }
                }
                return JsonSerializer.SerializeToElement(properties);

            case JsonValueKind.Array:
                var items = new List<JsonElement>();
                foreach (var item in element.EnumerateArray())
                {
                    items.Add(SanitizeJsonElement(item));
                }
                return JsonSerializer.SerializeToElement(items);

            case JsonValueKind.String:
                // 字符串值也检查是否为token格式
                var strValue = element.GetString();
                if (IsSensitiveString(strValue))
                {
                    return JsonSerializer.Deserialize<JsonElement>("\"***\"");
                }
                return element;

            default:
                return element;
        }
    }

    /// <summary>
    /// 判断字符串是否为敏感信息
    /// </summary>
    private static bool IsSensitiveString(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 20)
            return false;

        // 检查是否符合token的常见特征
        // 假设token通常包含字母、数字、下划线、短横线，长度通常大于20
        bool hasLetters = value.Any(char.IsLetter);
        bool hasDigits = value.Any(char.IsDigit);
        bool hasSpecialChars = value.Contains('_') || value.Contains('-');

        if (!hasLetters || !hasDigits)
            return false;

        // 如果看起来像token，进行脱敏
        return hasSpecialChars || value.Length > 32;
    }
}
