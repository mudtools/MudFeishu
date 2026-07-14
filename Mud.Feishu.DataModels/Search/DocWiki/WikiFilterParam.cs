// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary>
/// <para>Wiki过滤参数（doc_filter与wiki_filter至少传一个）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class WikiFilterParam
{
    /// <summary>
    /// <para>Wiki所有者OpenID</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("creator_ids")]
    public string[]? CreatorIds { get; set; }

    /// <summary>
    /// <para>Wiki类型</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>DOC：文档</item>
    /// <item>SHEET：表格</item>
    /// <item>BITABLE：多维表格</item>
    /// <item>MINDNOTE：思维导图</item>
    /// <item>FILE：文件</item>
    /// <item>WIKI：维基</item>
    /// <item>DOCX：新版文档</item>
    /// <item>FOLDER：space文件夹</item>
    /// <item>CATALOG：wiki2.0文件夹</item>
    /// <item>SLIDES：新版本幻灯片</item>
    /// <item>SHORTCUT：快捷方式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("doc_types")]
    public string[]? DocTypes { get; set; }

    /// <summary>
    /// <para>搜索某个Space下的Wiki（Space ID列表）</para>
    /// <para>必填：否</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("space_ids")]
    public string[]? SpaceIds { get; set; }

    /// <summary>
    /// <para>仅搜Wiki标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("only_title")]
    public bool? OnlyTitle { get; set; }

    /// <summary>
    /// <para>浏览文档的时间范围（秒级时间戳，包含start和end字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_time")]
    public FilterTimeRange? OpenTime { get; set; }


    /// <summary>
    /// <para>排序方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：CREATE_TIME</para>
    /// <para>可选值：<list type="bullet">
    /// <item>DEFAULT_TYPE：默认排序</item>
    /// <item>OPEN_TIME：User打开时间排序</item>
    /// <item>EDIT_TIME：User编辑时间降序</item>
    /// <item>EDIT_TIME_ASC：User编辑时间升序</item>
    /// <item>ENTITY_CREATE_TIME_ASC：实体创建时间升序（已废弃）</item>
    /// <item>ENTITY_CREATE_TIME_DESC：实体创建时间降序（已废弃）</item>
    /// <item>CREATE_TIME：按文档创建时间排序</item>
    /// <item>CREATE_TIME_ASC：按文档创建时间正序（该排序暂不支持）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("sort_type")]
    public string? SortType { get; set; }

    /// <summary>
    /// <para>Wiki创建的时间范围（秒级时间戳，包含start和end字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public FilterTimeRange? CreateTime { get; set; }

    /// <summary>
    /// <para>搜索在会话内的文档</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("chat_ids")]
    public string[]? ChatIds { get; set; }

    /// <summary>
    /// <para>文档分享者OpenID</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("sharer_ids")]
    public string[]? SharerIds { get; set; }

    /// <summary>
    /// <para>仅搜文档评论</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("only_comment")]
    public bool? OnlyComment { get; set; }

    /// <summary>
    /// <para>【我编辑的文档】的时间范围（秒级时间戳，包含start和end字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("my_edit_time")]
    public FilterTimeRange? MyEditTime { get; set; }

    /// <summary>
    /// <para>【我评论的文档】的时间范围（秒级时间戳，包含start和end字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("my_comment_time")]
    public FilterTimeRange? MyCommentTime { get; set; }

    /// <summary>
    /// <para>文档创建者者OpenID，注意和creator_ids区分开</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("original_creator_ids")]
    public string[]? OriginalCreatorIds { get; set; }
}
