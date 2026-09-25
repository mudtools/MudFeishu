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
/// <para>获取面试官信息列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class InterviewerListQuery : IQueryParameter
{
    /// <summary>
    /// <para>每页数量，最大 200</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，首次请求不填，翻页时取上一次返回的 page_token</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>面试官 userID 列表，最大 50 个</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</para>
    /// </summary>
    public string[]? UserIds { get; set; }

    /// <summary>
    /// <para>认证状态：1 未认证 / 2 已认证</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    public int? VerifyStatus { get; set; }

    /// <summary>
    /// <para>最早更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1714374796615</para>
    /// </summary>
    public string? EarliestUpdateTime { get; set; }

    /// <summary>
    /// <para>最晚更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1714374796616</para>
    /// </summary>
    public string? LatestUpdateTime { get; set; }

    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id，默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (UserIds is { Length: > 0 })
        {
            yield return new KeyValuePair<string, string?>("user_ids", string.Join(",", UserIds));
        }

        if (VerifyStatus.HasValue)
        {
            yield return new KeyValuePair<string, string?>("verify_status", VerifyStatus.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(EarliestUpdateTime))
        {
            yield return new KeyValuePair<string, string?>("earliest_update_time", EarliestUpdateTime);
        }

        if (!string.IsNullOrEmpty(LatestUpdateTime))
        {
            yield return new KeyValuePair<string, string?>("latest_update_time", LatestUpdateTime);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }
    }
}
