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
/// <para>获取人才列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class TalentListQuery : IQueryParameter
{
    /// <summary>
    /// <para>搜索关键词，支持布尔表达式（and/or/not），如「张三 and 产品经理」</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>最早更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UpdateStartTime { get; set; }

    /// <summary>
    /// <para>最晚更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UpdateEndTime { get; set; }

    /// <summary>
    /// <para>每页数量，默认 10，最大 20</para>
    /// <para>必填：否</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>排序方式：1 按更新时间降序（默认）/ 2 按相关性降序 / 3 按投递时间降序 / 4 按入库时间降序</para>
    /// <para>必填：否</para>
    /// </summary>
    public int? SortBy { get; set; }

    /// <summary>
    /// <para>分页标记，首次请求不填，翻页时取上一次返回的 page_token</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id/people_admin_id，默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>请求控制参数，枚举：ignore_empty_error（结果为空时忽略错误）</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? QueryOption { get; set; }

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

        if (!string.IsNullOrEmpty(UpdateStartTime))
        {
            yield return new KeyValuePair<string, string?>("update_start_time", UpdateStartTime);
        }

        if (!string.IsNullOrEmpty(UpdateEndTime))
        {
            yield return new KeyValuePair<string, string?>("update_end_time", UpdateEndTime);
        }

        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (SortBy.HasValue)
        {
            yield return new KeyValuePair<string, string?>("sort_by", SortBy.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }

        if (!string.IsNullOrEmpty(QueryOption))
        {
            yield return new KeyValuePair<string, string?>("query_option", QueryOption);
        }
    }
}
