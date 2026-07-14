// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// 搜索文档请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class SearchDocWikiRequest
{
    /// <summary>
    /// <para>搜索关键词（query至少搭配一种doc/wiki筛选器）</para>
    /// <para>必填：是</para>
    /// <para>示例值：飞书文档使用指南</para>
    /// <para>最大长度：30</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// <para>文档过滤参数（doc_filter与wiki_filter至少传一个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"folder_tokens": ["fld_123456"]}</para>
    /// </summary>
    [JsonPropertyName("doc_filter")]
    public DocFilterParam? DocFilter { get; set; }


    /// <summary>
    /// <para>Wiki过滤参数（doc_filter与wiki_filter至少传一个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"creator_ids": ["ou_789012"], "space_ids": ["space_123456"]}</para>
    /// </summary>
    [JsonPropertyName("wiki_filter")]
    public WikiFilterParam? WikiFilter { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：token_1234567890fedcba</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>分页大小</para>
    /// <para>必填：否</para>
    /// <para>示例值：15</para>
    /// <para>最大值：20</para>
    /// <para>最小值：0</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }
}
