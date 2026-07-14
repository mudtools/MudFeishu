// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 查询会议室配置响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class GetScopeConfigResult
{
    /// <summary>
    /// <para>当前节点的配置，根据层级顺序从底向上进行合并计算后的结果；如果当前节点某个值已配置，则取该节点的值，否则会从该节点的父层级节点获取，如果父节点依然未配置，则继续向上递归获取；若所有节点均未配置，则该值返回为空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("current_config")]
    public ScopeConfig? CurrentConfig { get; set; }

    /// <summary>
    /// <para>所有节点的原始配置，按照层级顺序从底向上返回；如果某节点某个值未配置，则该值返回为空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("origin_configs")]
    public ScopeConfig[]? OriginConfigs { get; set; }
}
