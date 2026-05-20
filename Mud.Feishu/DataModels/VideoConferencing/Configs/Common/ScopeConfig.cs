// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>当前节点的配置，根据层级顺序从底向上进行合并计算后的结果；如果当前节点某个值已配置，则取该节点的值，否则会从该节点的父层级节点获取，如果父节点依然未配置，则继续向上递归获取；若所有节点均未配置，则该值返回为空</para>
/// </summary>
public class ScopeConfig
{
    /// <summary>
    /// <para>查询节点范围</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：会议室层级</item>
    /// <item>2：会议室</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("scope_type")]
    public int ScopeType { get; set; }

    /// <summary>
    /// <para>查询节点ID：如果scope_type为1，则为层级ID，如果scope_type为2，则为会议室ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：omm_608d34d82d531b27fa993902d350a307</para>
    /// </summary>
    [JsonPropertyName("scope_id")]
    public string ScopeId { get; set; } = string.Empty;

    /// <summary>
    /// <para>节点配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope_config")]
    public RoomConfig? ScopeConfigSuffix { get; set; }

}
