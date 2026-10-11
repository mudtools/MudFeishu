// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// 审计入参摘要器（AI-FD-D12 P1D-3b 脱敏责任）：SDK 侧把工具入参折叠为
/// 「参数名 + 值形态/长度」摘要，<b>敏感键名一律掩码</b>——宿主 sink 不接触原始参数，
/// 防审计通道变成敏感数据泄漏面（模型 API Key/AppSecret 绝不入审计载荷）。
/// </summary>
internal static class ToolArgsDigester
{
    /// <summary>敏感键名清单（键名命中即掩码值；集中维护，对齐 MaskSensitiveData 精神）。</summary>
    /// <remarks>
    /// R3-05：此处为<b>入站审计口径</b>（子串匹配，宁可多掩），与出站脱敏
    /// <see cref="SecurityTextPrimitives.CredentialKeys"/>（精确匹配 + 非 JSON 形态）是<b>有意分离</b>的——
    /// 审计侧可接受过掩（摘要不影响模型），出站侧不可误伤标识类字段（会影响模型可用性）。
    /// 两侧键集的<b>差异是有意的</b>，合并会引入误伤。
    /// </remarks>
    private static readonly string[] SensitiveKeys =
    [
        "token", "secret", "password", "authorization", "api_key", "apikey", "app_secret", "credential",
    ];

    /// <summary>单参数摘要长度上限（含值长度描述）。</summary>
    private const int MaxDigestLength = 512;

    public static string Digest(IReadOnlyDictionary<string, object?> arguments)
    {
        var builder = new StringBuilder();
        foreach (var pair in arguments.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            var key = pair.Key.ToLowerInvariant();
            // netstandard2.0 的 string.Contains 无 StringComparison 重载，用 IndexOf。
            var isSensitive = SensitiveKeys.Any(k => key.IndexOf(k, StringComparison.Ordinal) >= 0);
            if (isSensitive)
            {
                builder.Append(pair.Key).Append("=***");
                continue;
            }

            var (kind, length) = Describe(pair.Value);
            builder.Append(pair.Key).Append('=').Append(kind);
            if (length is { } lengthValue)
            {
                builder.Append('(').Append(lengthValue.ToString(CultureInfo.InvariantCulture)).Append(")");
            }

            if (builder.Length > MaxDigestLength)
            {
                return builder.ToString(0, MaxDigestLength) + "…";
            }
        }

        return builder.ToString();
    }

    /// <summary>值形态描述（审计摘要词汇：null/str/array/json/list/bool/num）。</summary>
    /// <remarks>
    /// R-2：<c>JsonElement</c> 的形态判定经唯一认知入口（<see cref="ToolArgumentNormalizer.IsJsonString"/> /
    /// <see cref="ToolArgumentNormalizer.IsJsonArray"/>）——本文件不再自带 <c>ValueKind</c> 字面量
    /// （由 <c>ToolArgumentShapeContractGuards</c> 机械锁定）。产出词汇（str/array/json…）是**审计摘要**
    /// 的表示法，与取值门面 <c>ToolArgs</c> 的强类型转换不同职责，故保留在本类型。
    /// </remarks>
    private static (string Kind, int? Length) Describe(object? value)
    {
        switch (value)
        {
            case null:
                return ("null", null);
            case string s:
                return ("str", s.Length);
            case System.Text.Json.JsonElement element when ToolArgumentNormalizer.IsJsonArray(element, out var array):
                return ("array", array.GetArrayLength());
            case System.Text.Json.JsonElement element when ToolArgumentNormalizer.IsJsonString(element, out var text):
                return ("str", text?.Length ?? 0);
            case System.Text.Json.JsonElement:
                return ("json", null);
            case System.Collections.IEnumerable and not string:
                return ("list", null);
            case bool:
                return ("bool", null);
            case sbyte or byte or short or ushort or int or uint or long or ulong or double or decimal:
                return ("num", null);
            default:
                return ("str", value.ToString()?.Length);
        }
    }
}
