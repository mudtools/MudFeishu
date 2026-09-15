// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// AOT 安全的 JSON 序列化 / 反序列化入口（AOT-3）。
/// </summary>
/// <remarks>
/// <para>
/// <c>JsonSerializer.Serialize&lt;TValue&gt;(TValue, JsonSerializerOptions)</c> 与
/// <c>Deserialize&lt;TValue&gt;(string, JsonSerializerOptions)</c> 被标注为
/// <c>[RequiresUnreferencedCode]</c> / <c>[RequiresDynamicCode]</c>，在
/// <c>EnableAotAnalyzer=true</c> 的 net8+ 目标上会产生 <c>IL2026</c> / <c>IL3050</c>。
/// </para>
/// <para>
/// <b>语义等价性</b>：泛型重载内部即按 <c>typeof(TValue)</c> 解析元数据，本类先用
/// <see cref="JsonSerializerOptions.GetTypeInfo"/> 解析出同一个 <c>JsonTypeInfo</c>
/// 再走非泛型 <c>JsonTypeInfo</c> 重载，因此元数据解析结果与调用方类型完全一致
/// （不会改用运行时类型，避免多态序列化行为漂移）。<c>GetTypeInfo</c> 本身无 AOT 标注。
/// </para>
/// <para>
/// net6.0 / netstandard2.0 不启用 AOT 分析器，且不具备 <c>GetTypeInfo</c> API，
/// 故保留原泛型重载（条件编译），行为不变。
/// </para>
/// </remarks>
public static class FeishuJsonAot
{
    /// <summary>
    /// 序列化为 JSON 字符串。
    /// </summary>
    /// <typeparam name="TValue">值的编译期类型（与泛型重载语义一致）。</typeparam>
    /// <param name="value">待序列化的值。</param>
    /// <param name="options">序列化选项。</param>
    /// <returns>JSON 字符串。</returns>
    public static string Serialize<TValue>(TValue value, JsonSerializerOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

#if NET8_0_OR_GREATER
        if (options.TypeInfoResolver != null)
        {
            return JsonSerializer.Serialize(value, options.GetTypeInfo(typeof(TValue)));
        }
#endif

        // options 未配置 TypeInfoResolver 时无法使用 GetTypeInfo（会抛
        // NotSupportedException: ... TypeInfoResolver of type '<null>'），
        // 因为该场景下 options 本质由反射驱动，AOT 安全路径不存在。
        // 此处的反射调用由 #pragma 显式豁免，理由与 FeishuJsonDefaults.CreateReflectionFallback 一致。
#pragma warning disable IL2026, IL3050
        return JsonSerializer.Serialize(value, options);
#pragma warning restore IL2026, IL3050
    }

    /// <summary>
    /// 从 JSON 字符串反序列化。
    /// </summary>
    /// <typeparam name="TValue">目标类型。</typeparam>
    /// <param name="json">JSON 字符串。</param>
    /// <param name="options">反序列化选项。</param>
    /// <returns>反序列化结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> 为 null（与原泛型重载契约一致）。</exception>
    public static TValue? Deserialize<TValue>(string json, JsonSerializerOptions options)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (options == null) throw new ArgumentNullException(nameof(options));

#if NET8_0_OR_GREATER
        if (options.TypeInfoResolver != null)
        {
            var result = JsonSerializer.Deserialize(json, options.GetTypeInfo(typeof(TValue)));
            return result is null ? default : (TValue)result;
        }
#endif

#pragma warning disable IL2026, IL3050
        return JsonSerializer.Deserialize<TValue>(json, options);
#pragma warning restore IL2026, IL3050
    }
}
