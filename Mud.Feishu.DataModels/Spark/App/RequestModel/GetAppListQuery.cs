// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// <para>批量获取妙搭应用查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class GetAppListQuery : IQueryParameter
{
    /// <summary>
    /// <para>每页返回的应用数量，取值范围 1～100</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// <para>默认值：20</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>筛选应用类型，仅返回指定类型的应用</para>
    /// <para>必填：否</para>
    /// <para>示例值：full_stack</para>
    /// <para>可选值举例：frontend（纯前端应用）、full_stack（全栈应用）</para>
    /// </summary>
    public string? AppType { get; set; }

    /// <summary>
    /// <para>用于模糊匹配应用名称或描述。输入后将返回名称或描述中包含该关键词的应用，为空时返回全部有权限查看的应用</para>
    /// <para>必填：否</para>
    /// <para>示例值：我的应用</para>
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>应用归属范围过滤，用于筛选不同权限范围内的应用</para>
    /// <para>必填：否</para>
    /// <para>示例值：created_by_me</para>
    /// <para>可选值：<list type="bullet">
    /// <item>all：返回所有有权限查看的应用，包含自己创建的和共享给自己的</item>
    /// <item>created_by_me：仅返回当前用户创建的应用</item>
    /// <item>shared_with_me：仅返回其他用户共享给当前用户的应用</item>
    /// </list></para>
    /// <para>默认值：all</para>
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// <para>应用归属过滤</para>
    /// <para>必填：否</para>
    /// <para>示例值：all</para>
    /// <para>可选值：<list type="bullet">
    /// <item>all：所有</item>
    /// <item>mine：我创建的</item>
    /// <item>shared：共享给我的</item>
    /// </list></para>
    /// <para>默认值：all</para>
    /// </summary>
    public string? Ownership { get; set; }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (PageSize is int pageSize)
        {
            yield return new KeyValuePair<string, string?>(
                "page_size",
                pageSize.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (!string.IsNullOrEmpty(AppType))
        {
            yield return new KeyValuePair<string, string?>("app_type", AppType);
        }

        if (!string.IsNullOrEmpty(Keyword))
        {
            yield return new KeyValuePair<string, string?>("keyword", Keyword);
        }

        if (!string.IsNullOrEmpty(Scope))
        {
            yield return new KeyValuePair<string, string?>("scope", Scope);
        }

        if (!string.IsNullOrEmpty(Ownership))
        {
            yield return new KeyValuePair<string, string?>("ownership", Ownership);
        }
    }
}
