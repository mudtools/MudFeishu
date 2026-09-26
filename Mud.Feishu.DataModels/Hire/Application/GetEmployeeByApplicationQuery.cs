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
/// <para>通过投递 ID 获取入职信息查询参数（查询对象模式，见 AGENTS.md API-2）</para>
/// </summary>
public class GetEmployeeByApplicationQuery : IQueryParameter
{
    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：是</para>
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>用户的 ID 类型：open_id（默认）/ union_id / user_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>部门 ID 类型：people_admin_department_id（默认）/ open_department_id / department_id</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? DepartmentIdType { get; set; }

    /// <summary>
    /// <para>职级 ID 类型：people_admin_job_level_id（默认）</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? JobLevelIdType { get; set; }

    /// <summary>
    /// <para>序列（职位类别）ID 类型：people_admin_job_category_id（默认）</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? JobFamilyIdType { get; set; }

    /// <summary>
    /// <para>人员类型 ID 类型：people_admin_employee_type_id（默认）</para>
    /// <para>必填：否</para>
    /// </summary>
    public string? EmployeeTypeIdType { get; set; }

    /// <summary>
    /// 将对象转换为查询参数键值对集合（仅输出已设置的参数）。
    /// </summary>
    /// <returns>包含查询参数的键值对集合。</returns>
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        if (!string.IsNullOrEmpty(ApplicationId))
        {
            yield return new KeyValuePair<string, string?>("application_id", ApplicationId);
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
