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

public static class FeishuJsonDefaults
{
    private static IJsonTypeInfoResolver? _userResolver;

    /// <summary>
    /// 合并 SDK 内置 Context 与用户自定义 Context（AOT 必需）。
    /// 必须在任何反序列化发生前调用一次。
    /// </summary>
    public static void ConfigureUserResolver(IJsonTypeInfoResolver userResolver)
    {
        _userResolver = userResolver ?? throw new ArgumentNullException(nameof(userResolver));
        
        // TODO: Phase 1 - 临时使用基础设置，后续连接真实的 Context
        // var combined = JsonTypeInfoResolver.Combine(FeishuJsonContext.Default, userResolver);
        
        // 临时方案：仅设置用户 resolver，后续阶段连接 DataModels Context
        DeserializerOptions = new JsonSerializerOptions(GetDefaultOptions())
        {
            TypeInfoResolver = userResolver
        };
        SerializerOptions = new JsonSerializerOptions(GetDefaultOptions())
        {
            TypeInfoResolver = userResolver
        };
    }

    private static JsonSerializerOptions GetDefaultOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public static JsonSerializerOptions DeserializerOptions { get; private set; } =
        new() {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    public static JsonSerializerOptions SerializerOptions { get; private set; } =
        new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
}
