// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 无状态确认令牌（T4-2 / D-3）：把 <c>NeedsUserConfirmation</c> 的「批准」从一次会话语境
/// 变成<b>可校验的凭据</b>——执行链签发，模型转述给用户，用户同意后模型以
/// <c>confirm_token=&lt;令牌&gt;</c> 重试同一调用，执行链验签通过则跳过授权器的 Confirm 分支。
/// </summary>
/// <remarks>
/// <para>
/// <b>无状态</b>：令牌 = <c>v1.{过期时刻}.{HMAC}</c>，批准状态不落任何存储（执行链禁区不变）。
/// <b>防伪造</b>：HMAC-SHA256 覆盖 <c>toolName | 参数摘要 | appKey | userId | expiry</c> 五元组，
/// 换参数/换应用/换用户即失效——模型无法把 A 场景的批准搬到 B 场景（R-5 用例矩阵锁定）。
/// <b>防篡改</b>：参数摘要是<b>值敏感</b>的规范化序列化（键序固定、原始值入摘）——
/// 注意与 <see cref="ToolArgsDigester.Digest"/>（审计显示用，值掩码）<b>不同</b>：掩码摘要无法区分
/// 同形状不同值的两次调用，会造成批准转移。
/// </para>
/// <para>
/// <b>时间源可注入</b>（R4.1 评审 R-5）：签发/校验的时刻由调用方传入，过期语义可测。
/// </para>
/// </remarks>
internal static class ToolConfirmationToken
{
    /// <summary>默认有效期（R4 方案 T4-2 建议 10 分钟）。</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(10);

    /// <summary>确认令牌在模型入参中的键名（重试时填入；不参与参数摘要）。</summary>
    public const string ArgumentName = "confirm_token";

    private const string Prefix = "v1.";

    /// <summary>
    /// 签发确认令牌（密钥未配置时返回 <see langword="null"/>，调用方维持既有降级行为）。
    /// </summary>
    public static string? Issue(
        string? secret,
        string toolName,
        string argumentsDigest,
        string appKey,
        string? userId,
        DateTimeOffset now,
        TimeSpan? lifetime = null)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var effectiveLifetime = lifetime ?? DefaultLifetime;
        var expiryUnix = now.Add(effectiveLifetime).ToUnixTimeSeconds();
        var payload = BuildPayload(toolName, argumentsDigest, appKey, userId, expiryUnix);
        return Prefix + expiryUnix.ToString(CultureInfo.InvariantCulture) + "." + Encode(Sign(secret!, payload));
    }

    /// <summary>
    /// 校验确认令牌：签名一致、未过期、且绑定当前 <c>toolName/参数摘要/appKey/userId</c>。
    /// </summary>
    public static bool TryValidate(
        string? token,
        string? secret,
        string toolName,
        string argumentsDigest,
        string appKey,
        string? userId,
        DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        if (!token!.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var body = token[Prefix.Length..];
        var separator = body.IndexOf('.'); // netstandard2.0 无 IndexOf(char, StringComparison) 重载。
        if (separator <= 0)
        {
            return false;
        }

        if (!long.TryParse(body[..separator], NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiryUnix))
        {
            return false;
        }

        if (now.ToUnixTimeSeconds() >= expiryUnix)
        {
            return false;
        }

        var expected = Sign(secret!, BuildPayload(toolName, argumentsDigest, appKey, userId, expiryUnix));
        var actual = Decode(body[(separator + 1)..]);
        return actual is not null && ConstantTimeEquals(expected, actual);
    }

    /// <summary>
    /// 计算参数摘要（值敏感、键序固定；<see cref="ArgumentName"/> 有意排除——
    /// 否则「带令牌重试」本身的参数变化会让令牌永远验不过）。
    /// </summary>
    public static string ComputeArgumentsDigest(IReadOnlyDictionary<string, object?> arguments)
    {
        var builder = new StringBuilder();
        foreach (var pair in arguments.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            if (string.Equals(pair.Key, ArgumentName, StringComparison.Ordinal))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(pair.Key).Append('=').Append(RawValue(pair.Value));
        }

        return builder.ToString();
    }

    private static string BuildPayload(string toolName, string argumentsDigest, string appKey, string? userId, long expiryUnix)
        => string.Join("|", toolName, argumentsDigest, appKey, userId ?? string.Empty, expiryUnix.ToString(CultureInfo.InvariantCulture));

    private static string RawValue(object? value) => value switch
    {
        null => "\u2205",
        System.Text.Json.JsonElement e => e.GetRawText(),
        string s => s,
        _ => value.ToString() ?? "\u2205",
    };

    private static byte[] Sign(string secret, string payload)
        => new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes(payload));

    private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? Decode(string encoded)
    {
        try
        {
            var base64 = encoded.Replace('-', '+').Replace('_', '/');
            base64 += new string('=', (4 - base64.Length % 4) % 4);
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>常数时间比较（netstandard2.0 无 CryptographicOperations.FixedTimeEquals，手写等价实现）。</summary>
    private static bool ConstantTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }
}
