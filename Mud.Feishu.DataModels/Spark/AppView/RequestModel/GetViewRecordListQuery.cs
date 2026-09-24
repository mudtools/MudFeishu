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
/// <para>查询视图数据记录查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class GetViewRecordListQuery : IQueryParameter
{
    /// <summary>
    /// <para>分页大小，用于限制一次请求所返回的数据条目数。默认 10，最大 500</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// <para>取值范围：1 ～ 500</para>
    /// <para>默认值：10</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>返回的列，默认为 *，即返回所有列。遵循 PostgREST 语法（vertical filtering）</para>
    /// <para>必填：否</para>
    /// <para>示例值：_id,_created_at,name</para>
    /// </summary>
    public string? Select { get; set; }

    /// <summary>
    /// <para>筛选条件，遵循 PostgREST 语法（horizontal filtering）</para>
    /// <para>必填：否</para>
    /// <para>示例值：age=gt.10</para>
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// <para>排序条件，如果没指定 asc/desc，默认为 asc。遵循 PostgREST 语法（ordering）</para>
    /// <para>必填：否</para>
    /// <para>示例值：age.desc,score.asc</para>
    /// </summary>
    public string? Order { get; set; }

    /// <summary>
    /// <para>访问的 database 环境</para>
    /// <para>必填：否</para>
    /// <para>示例值：online、dev</para>
    /// <para>默认值：online</para>
    /// </summary>
    public string? Env { get; set; }

    /// <summary>
    /// <para>此次调用使用的用户 ID 类型，将使用指定的 ID 来标示某个用户在接口入参和出参中的值</para>
    /// <para>必填：否</para>
    /// <para>示例值：miaoda_user_id</para>
    /// <para>可选值：<list type="bullet">
    /// <item>miaoda_user_id：标识一个用户在飞书开发套件应用中的身份</item>
    /// <item>open_id：标识一个用户在某个应用中的身份</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份</item>
    /// </list></para>
    /// <para>默认值：miaoda_user_id</para>
    /// </summary>
    public string? UserIdentifierType { get; set; }

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

        if (!string.IsNullOrEmpty(Select))
        {
            yield return new KeyValuePair<string, string?>("select", Select);
        }

        if (!string.IsNullOrEmpty(Filter))
        {
            yield return new KeyValuePair<string, string?>("filter", Filter);
        }

        if (!string.IsNullOrEmpty(Order))
        {
            yield return new KeyValuePair<string, string?>("order", Order);
        }

        if (!string.IsNullOrEmpty(Env))
        {
            yield return new KeyValuePair<string, string?>("env", Env);
        }

        if (!string.IsNullOrEmpty(UserIdentifierType))
        {
            yield return new KeyValuePair<string, string?>("user_identifier_type", UserIdentifierType);
        }
    }
}
