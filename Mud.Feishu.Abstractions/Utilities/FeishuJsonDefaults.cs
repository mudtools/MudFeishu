// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// 飞书 SDK 统一的 JSON 序列化默认选项。
/// 在 net8.0+ 下支持 AOT 源生成器注入；在 netstandard2.0/net6.0 下走反射路径。
/// </summary>
public static class FeishuJsonDefaults
{
    private static readonly object _sync = new();

    /// <summary>
    /// 已合并的用户 resolver 列表（按引用去重，保证 ConfigureUserResolver 幂等）。
    /// </summary>
    private static readonly List<IJsonTypeInfoResolver> _userResolvers = new();

    /// <summary>
    /// 合并 SDK 内置 Context 与用户自定义 Context（AOT 必需）。
    /// <para>
    /// 支持多次调用（累加模式），每次调用将新 resolver 追加到解析器链；
    /// 同一 resolver 实例重复传入时会被忽略，保证幂等。
    /// 必须在任何反序列化发生前调用。
    /// </para>
    /// </summary>
    /// <param name="userResolver">用户自定义类型的 JsonTypeInfoResolver。</param>
#if !NET8_0_OR_GREATER
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("netstandard2.0/net6.0 路径使用 DefaultJsonTypeInfoResolver 反射兜底，不支持 AOT。AOT 部署请使用 net8.0+ 目标框架。")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("netstandard2.0/net6.0 路径使用 DefaultJsonTypeInfoResolver 反射兜底，不支持 AOT。AOT 部署请使用 net8.0+ 目标框架。")]
#endif
#endif
    public static void ConfigureUserResolver(IJsonTypeInfoResolver userResolver)
    {
        if (userResolver == null) throw new ArgumentNullException(nameof(userResolver));

        lock (_sync)
        {
            // 幂等：同一实例只合并一次（此前重复调用会让解析器链无限膨胀，见 ARC-3）
            if (!ContainsInstance(_userResolvers, userResolver))
            {
                _userResolvers.Add(userResolver);
            }

#if NET8_0_OR_GREATER
            // AOT 路径：SDK 内置 Context → 用户 Context → 反射兜底。
            // 反射兜底不可省略：DeserializerOptions/SerializerOptions 是公开 API，需要能处理
            // 未被任何源生成 Context 覆盖的类型（如用户自定义事件负载）。缺失兜底会导致
            // net8+/net10 上反序列化用户类型直接抛 NotSupportedException，而 net6/ns2.0 正常，
            // 构成跨 TFM 的行为不一致。源生成 Context 位于链首，覆盖类型仍走 AOT 安全路径。
            var chain = new List<IJsonTypeInfoResolver>(_userResolvers.Count + 2)
            {
                FeishuJsonContext.Default
            };
            chain.AddRange(_userResolvers);
            chain.Add(new DefaultJsonTypeInfoResolver());

            var combined = JsonTypeInfoResolver.Combine(chain.ToArray());
            ApplyOptions(FeishuJsonContext.Default.Options, combined);
#else
            // 非 AOT 路径：用户 resolver + 反射兜底
            var combined = JsonTypeInfoResolver.Combine(ToChain(userResolver));
            ApplyOptions(DeserializerOptions, combined);
#endif
        }
    }

    /// <summary>
    /// 以给定的 TypeInfoResolver 重建序列化/反序列化选项（在 <see cref="_sync"/> 锁内调用）。
    /// </summary>
    private static void ApplyOptions(JsonSerializerOptions template, IJsonTypeInfoResolver resolver)
    {
        DeserializerOptions = new JsonSerializerOptions(template)
        {
            TypeInfoResolver = resolver,
            PropertyNameCaseInsensitive = true
        };
        SerializerOptions = new JsonSerializerOptions(template)
        {
            TypeInfoResolver = resolver,
            PropertyNameCaseInsensitive = true
        };
    }

    private static IJsonTypeInfoResolver[] ToChain(IJsonTypeInfoResolver userResolver)
    {
        var chain = new List<IJsonTypeInfoResolver>(_userResolvers.Count + 2);
        if (!ContainsInstance(chain, userResolver))
        {
            chain.Add(userResolver);
        }
        foreach (var r in _userResolvers)
        {
            if (!ContainsInstance(chain, r))
            {
                chain.Add(r);
            }
        }
        chain.Add(new DefaultJsonTypeInfoResolver());
        return chain.ToArray();
    }

    private static bool ContainsInstance(List<IJsonTypeInfoResolver> list, IJsonTypeInfoResolver resolver)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], resolver))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 移除全部已配置的用户 resolver，将序列化选项恢复为 SDK 内置默认状态。
    /// 主要供单元测试在用例间隔离静态状态使用。
    /// </summary>
    public static void Reset()
    {
        lock (_sync)
        {
            _userResolvers.Clear();
#if NET8_0_OR_GREATER
            var combined = JsonTypeInfoResolver.Combine(
                FeishuJsonContext.Default, new DefaultJsonTypeInfoResolver());
            ApplyOptions(FeishuJsonContext.Default.Options, combined);
#else
            DeserializerOptions = CreateDefaultDeserializerOptions();
            SerializerOptions = CreateDefaultSerializerOptions();
#endif
        }
    }

    private static JsonSerializerOptions CreateDefaultDeserializerOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    private static JsonSerializerOptions CreateDefaultSerializerOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    /// <summary>
    /// 默认的反序列化选项（忽略大小写、驼峰命名、忽略 null 值写入）
    /// </summary>
    public static JsonSerializerOptions DeserializerOptions { get; private set; } =
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    /// <summary>
    /// 默认的序列化选项（驼峰命名、不缩进、忽略 null 值写入）
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; private set; } =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
}