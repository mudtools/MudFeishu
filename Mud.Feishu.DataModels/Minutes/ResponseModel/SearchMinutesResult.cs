// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Minutes;

/// <summary>
/// 搜索妙记响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Minutes")]
public class SearchMinutesResult
{
    /// <summary>
    /// <para>妙记列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public MinutesSearchItem[]? Items { get; set; }

    /// <summary>
    /// <para>总数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// <para>是否还有更多</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }

    /// <summary>
    /// <para>翻页 token，下一页请求回填至 page_token 查询参数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>搜索补充提示信息，返回本次搜索的额外说明（例如 query 被截断、搜索结果不全等）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("notice")]
    public string? Notice { get; set; }
}
