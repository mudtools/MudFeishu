// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 创建知识空间节点请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Wiki")]
public class CreateSpaceNodeRequest
{
    /// <summary>
    /// <para>文档类型，对于快捷方式，该字段是对应的实体的obj_type。</para>
    /// <para>必填：是</para>
    /// <para>示例值：docx</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：已废弃，创建文档请使用`docx`。</item>
    /// <item>sheet：表格</item>
    /// <item>mindnote：思维导图</item>
    /// <item>bitable：多维表格</item>
    /// <item>file：文件</item>
    /// <item>docx：新版文档</item>
    /// <item>slides：幻灯片</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public string ObjType { get; set; } = string.Empty;

    /// <summary>
    /// <para>父节点 token。若当前节点为一级节点，父节点 token 为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：wikcnKQ1k3p******8Vabcef</para>
    /// </summary>
    [JsonPropertyName("parent_node_token")]
    public string? ParentNodeToken { get; set; }

    /// <summary>
    /// <para>节点类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：origin</para>
    /// <para>可选值：<list type="bullet">
    /// <item>origin：实体</item>
    /// <item>shortcut：快捷方式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("node_type")]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// <para>快捷方式对应的实体node_token，当节点为快捷方式时，该值不为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：wikcnKQ1k3p******8Vabcef</para>
    /// </summary>
    [JsonPropertyName("origin_node_token")]
    public string? OriginNodeToken { get; set; }

    /// <summary>
    /// <para>文档标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：标题</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
