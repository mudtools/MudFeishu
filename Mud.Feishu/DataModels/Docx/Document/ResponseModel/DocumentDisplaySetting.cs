// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Docx;

/// <summary>
/// <para>文档展示设置</para>
/// </summary>
public class DocumentDisplaySetting
{
    /// <summary>
    /// <para>文档信息中是否展示文档作者</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_authors")]
    public bool? ShowAuthors { get; set; }

    /// <summary>
    /// <para>文档信息中是否展示文档创建时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_create_time")]
    public bool? ShowCreateTime { get; set; }

    /// <summary>
    /// <para>文档信息中是否展示文档访问次数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_pv")]
    public bool? ShowPv { get; set; }

    /// <summary>
    /// <para>文档信息中是否展示文档访问人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_uv")]
    public bool? ShowUv { get; set; }

    /// <summary>
    /// <para>文档信息中是否展示点赞总数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_like_count")]
    public bool? ShowLikeCount { get; set; }

    /// <summary>
    /// <para>文档信息中是否展示评论总数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_comment_count")]
    public bool? ShowCommentCount { get; set; }

    /// <summary>
    /// <para>文档信息中是否展示关联事项</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_related_matters")]
    public bool? ShowRelatedMatters { get; set; }
}