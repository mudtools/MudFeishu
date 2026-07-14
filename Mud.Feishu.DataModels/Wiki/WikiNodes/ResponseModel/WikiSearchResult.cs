// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// Wiki 搜索结果项
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Wiki")]
public class WikiSearchResult
{
    /// <summary>
    /// <para>wiki 节点的 token</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>wiki 所属知识空间 Id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("space_id")]
    public string? SpaceId { get; set; }

    /// <summary>
    /// <para>wiki 类型, 参考文档类型表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public int? ObjType { get; set; }

    /// <summary>
    /// <para>节点的真实文档的 token，如果要获取或编辑节点内容，需要使用此 token 调用对应的接口</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("obj_token")]
    public string? ObjToken { get; set; }

    /// <summary>
    /// <para>暂未生效，一律返回空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// <para>该知识库文档的序号，从 1 开始计数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sort_id")]
    public int? SortId { get; set; }

    /// <summary>
    /// <para>wiki 标题</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>wiki 的访问 url</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>wiki 对应图标的 url</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
