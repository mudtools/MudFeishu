// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>设置自动通过的节点。</para>
/// </summary>
public class NodeAutoApproval
{
    /// <summary>
    /// <para>节点 ID 类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：NON_CUSTOM</para>
    /// <para>可选值：<list type="bullet">
    /// <item>CUSTOM：自定义节点ID</item>
    /// <item>NON_CUSTOM：非自定义节点ID</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("node_id_type")]
    public string? NodeIdType { get; set; }

    /// <summary>
    /// <para>节点 ID 值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：manager_node_id</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }
}