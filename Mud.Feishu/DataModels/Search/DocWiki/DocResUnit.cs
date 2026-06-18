// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// <para>搜索结果列表</para>
/// </summary>
public class DocResUnit
{
    /// <summary>
    /// <para>标题高亮</para>
    /// <para>必填：否</para>
    /// <para>示例值：&lt;h&gt;飞书文档&lt;/h&gt;使用指南</para>
    /// </summary>
    [JsonPropertyName("title_highlighted")]
    public string? TitleHighlighted { get; set; }

    /// <summary>
    /// <para>摘要高亮</para>
    /// <para>必填：否</para>
    /// <para>示例值：本文介绍&lt;h&gt;飞书文档&lt;/h&gt;的创建、编辑与分享功能</para>
    /// </summary>
    [JsonPropertyName("summary_highlighted")]
    public string? SummaryHighlighted { get; set; }

    /// <summary>
    /// <para>结果类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：DOC</para>
    /// <para>可选值：<list type="bullet">
    /// <item>DOC：doc实体</item>
    /// <item>WIKI：wiki类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("entity_type")]
    public string? EntityType { get; set; }

    /// <summary>
    /// <para>文档搜索元信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"title_highlighted":"&lt;h&gt;飞书文档&lt;/h&gt;使用指南","summary_highlighted":"本文介绍&lt;h&gt;飞书文档&lt;/h&gt;的创建、编辑与分享功能","entity_type":"DOC","result_meta":"{"type":SHORTCUT,"update_time":1766567613}"}</para>
    /// </summary>
    [JsonPropertyName("result_meta")]
    public DocMeta? ResultMeta { get; set; }

}
