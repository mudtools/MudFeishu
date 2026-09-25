// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils;
using System.Globalization;

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// <para>获取关联组织的成员信息查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class VisibleOrganizationQuery : IQueryParameter
{
    /// <summary>
    /// <para>此次调用中使用的部门ID的类型：department_id（默认）/ open_department_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? DepartmentIdType { get; set; }

    /// <summary>
    /// <para>请求关联组织的部门ID，0代表根部门，与 target_group_id 二选一；可从获取关联组织的成员信息接口中获得</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-4e6ac4d14bcd5071a37a39de902c7141</para>
    /// </summary>
    public string? TargetDepartmentId { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>请求的可见实体数量，取值 1~200，默认 100</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>此次调用中使用的用户组ID的类型：group_id（默认）/ open_group_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? GroupIdType { get; set; }

    /// <summary>
    /// <para>请求关联组织的用户组ID，与 target_department_id 二选一；可从获取关联组织的成员信息接口中获得</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? TargetGroupId { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(DepartmentIdType))
        {
            yield return new KeyValuePair<string, string?>("department_id_type", DepartmentIdType);
        }

        if (!string.IsNullOrEmpty(TargetDepartmentId))
        {
            yield return new KeyValuePair<string, string?>("target_department_id", TargetDepartmentId);
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(GroupIdType))
        {
            yield return new KeyValuePair<string, string?>("group_id_type", GroupIdType);
        }

        if (!string.IsNullOrEmpty(TargetGroupId))
        {
            yield return new KeyValuePair<string, string?>("target_group_id", TargetGroupId);
        }
    }
}
