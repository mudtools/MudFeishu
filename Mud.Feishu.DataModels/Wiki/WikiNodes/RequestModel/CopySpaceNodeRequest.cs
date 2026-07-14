// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 创建知识空间节点副本请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Wiki")]
public class CopySpaceNodeRequest
{
    /// <summary>
    /// <para>目标父节点 Token。</para>
    /// <para>- 目标知识空间 ID 与目标父节点 Token 不可同时为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：wikcnKQ1k3p******8Vabce</para>
    /// <para>最大长度：999</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("target_parent_token")]
    public string? TargetParentToken { get; set; }

    /// <summary>
    /// <para>目标知识空间 ID。</para>
    /// <para>- 目标知识空间 ID 与目标父节点 Token 不可同时为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：6946843325487912356</para>
    /// </summary>
    [JsonPropertyName("target_space_id")]
    public string? TargetSpaceId { get; set; }

    /// <summary>
    /// <para>复制后的新标题。如果填空，则新标题为空。如果不填，则使用原节点标题。</para>
    /// <para>必填：否</para>
    /// <para>示例值：新标题。</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
