// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 搜索 Wiki 请求体
/// </summary>
public class WikiSearchRequest
{
    /// <summary>
    /// <para>搜索关键词，长度不超过50个字符</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// <para>文档所属的知识空间ID，为空搜索全部知识空间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("space_id")]
    public string? SpaceId { get; set; }

    /// <summary>
    /// <para>不为空搜索该节点及其所有子节点，为空搜索所有 wiki（使用 node_id 过滤必须传入 space_id）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }
}
