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
    private static IJsonTypeInfoResolver? _userResolver;
    private static IJsonTypeInfoResolver? _combinedResolver;

    /// <summary>
    /// 合并 SDK 内置 Context 与用户自定义 Context（AOT 必需）。
    /// 必须在任何反序列化发生前调用一次。
    /// </summary>
    /// <param name="userResolver">用户自定义类型的 JsonTypeInfoResolver。</param>
    public static void ConfigureUserResolver(IJsonTypeInfoResolver userResolver)
    {
        _userResolver = userResolver ?? throw new ArgumentNullException(nameof(userResolver));

#if NET8_0_OR_GREATER
        // 累加模式：若已有 resolver，则合并新 resolver
        if (_combinedResolver != null)
        {
            _combinedResolver = JsonTypeInfoResolver.Combine(_combinedResolver, userResolver);
        }
        else
        {
            _combinedResolver = JsonTypeInfoResolver.Combine(FeishuJsonContext.Default, userResolver);
        }

        DeserializerOptions = new JsonSerializerOptions(FeishuJsonContext.Default.Options)
        {
            TypeInfoResolver = _combinedResolver
        };
        SerializerOptions = new JsonSerializerOptions(FeishuJsonContext.Default.Options)
        {
            TypeInfoResolver = _combinedResolver
        };
#else
        // 非 AOT 路径：使用用户 resolver + 反射兜底
        DeserializerOptions = new JsonSerializerOptions(GetDefaultDeserializerOptions())
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(userResolver, new DefaultJsonTypeInfoResolver())
        };
        SerializerOptions = new JsonSerializerOptions(GetDefaultSerializerOptions())
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(userResolver, new DefaultJsonTypeInfoResolver())
        };
#endif
    }

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

    private static JsonSerializerOptions GetDefaultDeserializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    private static JsonSerializerOptions GetDefaultSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}