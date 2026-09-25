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
/// <para>获取关联组织双方共享成员范围查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class ShareEntityListQuery : IQueryParameter
{
    /// <summary>
    /// <para>对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获得</para>
    /// <para>必填：是</para>
    /// <para>示例值：test_key</para>
    /// </summary>
    public string? TargetTenantKey { get; set; }

    /// <summary>
    /// <para>请求关联组织的部门ID；不填则查询整个组织分享范围；填 0 时若为全员分享则展示一级部门，否则展示分享的部门+成员；可递归下钻</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? TargetDepartmentId { get; set; }

    /// <summary>
    /// <para>获取用户组下的成员，填写后忽略 target_department_id；可用返回的用户组 ID 继续查询</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? TargetGroupId { get; set; }

    /// <summary>
    /// <para>是否查询主体组织分享范围，默认查客体组织</para>
    /// <para>必填：否</para>
    /// </summary>
    public bool? IsSelectSubject { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>分页大小，取值 0~100，默认 100</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(TargetTenantKey))
        {
            yield return new KeyValuePair<string, string?>("target_tenant_key", TargetTenantKey);
        }

        if (!string.IsNullOrEmpty(TargetDepartmentId))
        {
            yield return new KeyValuePair<string, string?>("target_department_id", TargetDepartmentId);
        }

        if (!string.IsNullOrEmpty(TargetGroupId))
        {
            yield return new KeyValuePair<string, string?>("target_group_id", TargetGroupId);
        }

        if (IsSelectSubject.HasValue)
        {
            yield return new KeyValuePair<string, string?>("is_select_subject", IsSelectSubject.Value ? "true" : "false");
        }

        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
