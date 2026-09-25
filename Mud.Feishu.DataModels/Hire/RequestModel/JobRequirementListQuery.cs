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
/// <para>获取招聘需求列表查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class JobRequirementListQuery : IQueryParameter
{
    /// <summary>
    /// <para>分页标记，首次请求不填，翻页时取上一次返回的 page_token</para>
    /// <para>必填：否</para>
    /// <para>示例值：1231231987</para>
    /// </summary>
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>每页数量，最大 100，默认 1</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>职位 ID，可通过获取职位列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：1111</para>
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// <para>起始创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1658980233000</para>
    /// </summary>
    public string? CreateTimeBegin { get; set; }

    /// <summary>
    /// <para>截止创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1658980233000</para>
    /// </summary>
    public string? CreateTimeEnd { get; set; }

    /// <summary>
    /// <para>起始更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1658980233000</para>
    /// </summary>
    public string? UpdateTimeBegin { get; set; }

    /// <summary>
    /// <para>截止更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1658980233000</para>
    /// </summary>
    public string? UpdateTimeEnd { get; set; }

    /// <summary>
    /// <para>用户 ID 类型：open_id/union_id/user_id，默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>部门 ID 类型：open_department_id/department_id，默认 open_department_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? DepartmentIdType { get; set; }

    /// <summary>
    /// <para>职级 ID 类型：people_admin_job_level_id/job_level_id，默认 people_admin_job_level_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? JobLevelIdType { get; set; }

    /// <summary>
    /// <para>职位序列 ID 类型：people_admin_job_category_id/job_family_id，默认 people_admin_job_category_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? JobFamilyIdType { get; set; }

    /// <summary>
    /// <para>人员类型 ID 类型：people_admin_employee_type_id/employee_type_enum_id，默认 people_admin_employee_type_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? EmployeeTypeIdType { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(PageToken))
        {
            yield return new KeyValuePair<string, string?>("page_token", PageToken);
        }

        if (PageSize.HasValue)
        {
            yield return new KeyValuePair<string, string?>("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(JobId))
        {
            yield return new KeyValuePair<string, string?>("job_id", JobId);
        }

        if (!string.IsNullOrEmpty(CreateTimeBegin))
        {
            yield return new KeyValuePair<string, string?>("create_time_begin", CreateTimeBegin);
        }

        if (!string.IsNullOrEmpty(CreateTimeEnd))
        {
            yield return new KeyValuePair<string, string?>("create_time_end", CreateTimeEnd);
        }

        if (!string.IsNullOrEmpty(UpdateTimeBegin))
        {
            yield return new KeyValuePair<string, string?>("update_time_begin", UpdateTimeBegin);
        }

        if (!string.IsNullOrEmpty(UpdateTimeEnd))
        {
            yield return new KeyValuePair<string, string?>("update_time_end", UpdateTimeEnd);
        }

        if (!string.IsNullOrEmpty(UserIdType))
        {
            yield return new KeyValuePair<string, string?>("user_id_type", UserIdType);
        }

        if (!string.IsNullOrEmpty(DepartmentIdType))
        {
            yield return new KeyValuePair<string, string?>("department_id_type", DepartmentIdType);
        }

        if (!string.IsNullOrEmpty(JobLevelIdType))
        {
            yield return new KeyValuePair<string, string?>("job_level_id_type", JobLevelIdType);
        }

        if (!string.IsNullOrEmpty(JobFamilyIdType))
        {
            yield return new KeyValuePair<string, string?>("job_family_id_type", JobFamilyIdType);
        }

        if (!string.IsNullOrEmpty(EmployeeTypeIdType))
        {
            yield return new KeyValuePair<string, string?>("employee_type_id_type", EmployeeTypeIdType);
        }
    }
}
