// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// <para>获取人才标签列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class TalentTagListQuery : IQueryParameter
{
    /// <summary>
    /// <para>关键词</para>
    /// <para>必填：否</para>
    /// <para>示例值：985</para>
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>标签 ID 列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：6887469228283299336</para>
    /// </summary>
    public string[]? IdList { get; set; }

    /// <summary>
    /// <para>标签类型：1 手动标签 / 2 自动标签</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// <para>是否包含停用的标签</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    public bool? IncludeInactive { get; set; }

    /// <summary>
    /// <para>每页数量，默认 20，最大 100</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，首次请求不填，翻页时取上一次返回的 page_token</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(Keyword))
        {
            yield return new KeyValuePair<string, string?>("keyword", Keyword);
        }

        if (IdList is { Length: > 0 })
        {
            yield return new KeyValuePair<string, string?>("id_list", string.Join(",", IdList));
        }

        if (Type.HasValue)
        {
            yield return new KeyValuePair<string, string?>("type", Type.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (IncludeInactive.HasValue)
        {
            yield return new KeyValuePair<string, string?>("include_inactive", IncludeInactive.Value ? "true" : "false");
        }

        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }
    }
}
