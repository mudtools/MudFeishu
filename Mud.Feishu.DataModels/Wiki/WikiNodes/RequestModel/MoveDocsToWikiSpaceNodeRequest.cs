// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 移动云空间文档至知识空间请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Wiki")]
public class MoveDocsToWikiSpaceNodeRequest
{
    /// <summary>
    /// <para>节点的父亲token。</para>
    /// <para>传空或不传时将移动为知识空间一级节点。</para>
    /// <para>必填：否</para>
    /// <para>示例值：wikcnKQ1k3p******8Vabce</para>
    /// </summary>
    [JsonPropertyName("parent_wiki_token")]
    public string? ParentWikiToken { get; set; }

    /// <summary>
    /// <para>文档类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：doc</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>sheet：表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>mindnote：思维导图</item>
    /// <item>docx：新版文档</item>
    /// <item>file：文件</item>
    /// <item>slides：slides（幻灯片）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public string ObjType { get; set; } = string.Empty;

    /// <summary>
    /// <para>文档token</para>
    /// <para>必填：是</para>
    /// <para>示例值：doccnzAaOD******Wabcdef</para>
    /// </summary>
    [JsonPropertyName("obj_token")]
    public string ObjToken { get; set; } = string.Empty;

    /// <summary>
    /// <para>没有权限时，是否申请移动文档。</para>
    /// <para>如果申请移动，文档将在处理人同意时自动移动至指定位置。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("apply")]
    public bool? Apply { get; set; }
}
