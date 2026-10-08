// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Departments;
using Mud.Feishu.DataModels.Employees;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// 通讯录部门轴工具执行器（<c>contact.list_departments</c> / <c>contact.list_department_members</c>，WP5/R5）。
/// </summary>
/// <remarks>
/// <para>
/// 部门列表走 <c>IFeishuTenantV3Departments.GetDepartmentsByParentIdAsync</c>（租户令牌），
/// 部门成员走 <c>IFeishuTenantV1Employees.QueryEmployeePageListAsync</c>（租户令牌，按 department_id 过滤）。
/// </para>
/// <para>
/// <b>部门成员过滤</b>：飞书员工列表 API 支持按 <c>base_info.departments.department_id</c> 条件过滤
/// （<see cref="FieldCondition"/>），工具层把模型传入的 <c>department_id</c> 转为 FilterSearchRequest。
/// </para>
/// <para>
/// 执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。
/// </para>
/// </remarks>
internal sealed class ContactDepartmentTools(
    Mud.Feishu.IFeishuTenantV3Departments? departmentsClient,
    Mud.Feishu.IFeishuTenantV1Employees? employeesClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV3Departments? _departmentsClient = departmentsClient;
    private readonly Mud.Feishu.IFeishuTenantV1Employees? _employeesClient = employeesClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>contact.list_departments：列出指定部门下的子部门（分页，白名单 department_id/name/parent_department_id）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantContactListDepartmentsTool))]
    public Task<FeishuToolResult> ListDepartmentsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ContactListDepartments, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_departmentsClient is null)
            {
                throw new ArgumentException(
                    "contact.list_departments 需要 IFeishuTenantV3Departments——宿主须启用 AddOrganizationApi 的部门侧客户端");
            }

            var args = ContactListDepartmentsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _departmentsClient
                .GetDepartmentsByParentIdAsync(
                    args.DepartmentId,
                    fetch_child: args.FetchChild ?? false,
                    page_size: PageSizes.Departments,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectDepartments);
        });
    }

    /// <summary>contact.list_department_members：按部门 ID 列出该部门下的员工（分页，白名单 open_id/name/employee_id）。</summary>
    /// <remarks>
    /// 飞书员工列表 API 通过 <see cref="FilterSearchRequest"/> 的 <c>filter.conditions</c> 按字段过滤。
    /// 部门 ID 过滤条件：<c>field="base_info.departments.department_id"</c>, <c>operator="eq"</c>,
    /// <c>value="\"{department_id}\""</c>（转义 JSON 字符串）。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTenantContactListDepartmentMembersTool))]
    public Task<FeishuToolResult> ListDepartmentMembersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ContactListDepartmentMembers, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_employeesClient is null)
            {
                throw new ArgumentException(
                    "contact.list_department_members 需要 IFeishuTenantV1Employees——宿主须启用 AddOrganizationApi 的员工侧客户端");
            }

            var args = ContactListDepartmentMembersArgs.Unpack(arguments);

            var filterRequest = new FilterSearchRequest
            {
                Filter = new FieldFilter
                {
                    Conditions =
                    [
                        new FieldCondition
                        {
                            Field = "base_info.departments.department_id",
                            Operator = "eq",
                            Value = $"\"{args.DepartmentId}\"",
                        },
                    ],
                },
                RequiredFields = ["base_info.employee_id", "base_info.name", "base_info.mobile", "base_info.email"],
                PageRequest = new PageRequest
                {
                    PageSize = PageSizes.DepartmentMembers,
                    PageToken = args.PageToken,
                },
            };

            var outcome = FeishuApiResultReader.Read(await _employeesClient
                .QueryEmployeePageListAsync(filterRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectDepartmentMembers);
        });
    }

    /// <summary>list_departments 投影：items（department_id/name/parent_department_id）+ 翻页契约。</summary>
    private static JsonObject ProjectDepartments(ApiPageListResult<GetDepartmentInfo> data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var dept in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["department_id"] = dept.DepartmentId,
                ["open_department_id"] = dept.OpenDepartmentId,
                ["name"] = dept.Name,
                ["parent_department_id"] = dept.ParentDepartmentId,
                ["member_count"] = dept.MemberCount,
            });
        }

        return envelope;
    }

    /// <summary>list_department_members 投影：items（employee_id/name/email/mobile）+ 翻页契约。</summary>
    private static JsonObject ProjectDepartmentMembers(EmployeePageListResult data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
        };

        // EmployeePageListResult 继承 PageListResult，Page 属性含分页信息
        if (data.Page is not null)
        {
            envelope["has_more"] = data.Page.HasMore;
            if (!string.IsNullOrEmpty(data.Page.PageToken))
            {
                envelope["page_token"] = data.Page.PageToken;
            }
        }

        foreach (var emp in data.Employees ?? [])
        {
            var baseInfo = emp.BaseInfo;
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["employee_id"] = baseInfo?.EmployeeId,
                ["name"] = baseInfo?.Name?.Name?.DefaultValue,
                ["email"] = baseInfo?.Email,
                ["mobile"] = baseInfo?.Mobile,
            });
        }

        return envelope;
    }
}
