// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 飞书 Webhook 统一的 JSON 序列化选项
/// </summary>
public static class FeishuJsonOptions
{
    /// <summary>
    /// 请求体反序列化选项（忽略大小写、驼峰命名）
    /// </summary>
    public static JsonSerializerOptions Deserialize { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        MaxDepth = 64,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
#if NET8_0_OR_GREATER
        ,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
#endif
    };

    /// <summary>
    /// 响应体序列化选项（驼峰命名、不缩进）
    /// </summary>
    public static JsonSerializerOptions Serialize { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 启用枚举字符串序列化
    /// </summary>
    public static void EnableEnumStringConverter()
    {
        var converter = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
        Deserialize.Converters.Add(converter);
        Serialize.Converters.Add(converter);
    }
}
