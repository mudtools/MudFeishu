// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 飞书 Webhook 统一的 JSON 序列化选项
/// </summary>
public static class FeishuJsonOptions
{
    private static JsonSerializerOptions? _cachedDeserialize;
    private static JsonSerializerOptions? _cachedDeserializeSource;

    /// <summary>
    /// 请求体反序列化选项（基于共享默认选项，增加严格校验配置）。
    /// 使用缓存+引用比较策略：仅当 FeishuJsonDefaults.DeserializerOptions 引用变更时重新计算，
    /// 确保 ConfigureUserResolver 后 TypeInfoResolver 变更能传播到 Webhook 层，同时避免每次调用创建新对象。
    /// </summary>
    public static JsonSerializerOptions Deserialize
    {
        get
        {
            var source = FeishuJsonDefaults.DeserializerOptions;
            if (_cachedDeserialize == null || !ReferenceEquals(_cachedDeserializeSource, source))
            {
                _cachedDeserializeSource = source;
                _cachedDeserialize = new JsonSerializerOptions(source)
                {
                    ReadCommentHandling = JsonCommentHandling.Disallow,
                    AllowTrailingCommas = false,
                    MaxDepth = 64
#if NET8_0_OR_GREATER
                    ,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
#endif
                };
            }
            return _cachedDeserialize;
        }
    }

    /// <summary>
    /// 响应体序列化选项
    /// </summary>
    public static JsonSerializerOptions Serialize => FeishuJsonDefaults.SerializerOptions;
}