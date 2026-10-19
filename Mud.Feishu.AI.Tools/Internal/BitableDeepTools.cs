// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Bitable 深化执行器（7 只读 + 10 写）：表单（<c>form_*</c>）/ 仪表盘（<c>dashboard_*</c>）/
/// 工作流（<c>workflow_*</c>）/ 高级权限角色（<c>role_*</c>）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class BitableDeepTools(
    Mud.Feishu.IFeishuTenantV1BitableForm formClient,
    Mud.Feishu.IFeishuTenantV1BitableDashboard dashboardClient,
    Mud.Feishu.Interfaces.IFeishuTenantV1BitableWorkflow workflowClient,
    Mud.Feishu.IFeishuTenantV2BitableRole roleClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1BitableForm _formClient = formClient
        ?? throw new ArgumentNullException(nameof(formClient));
    private readonly Mud.Feishu.IFeishuTenantV1BitableDashboard _dashboardClient = dashboardClient
        ?? throw new ArgumentNullException(nameof(dashboardClient));
    private readonly Mud.Feishu.Interfaces.IFeishuTenantV1BitableWorkflow _workflowClient = workflowClient
        ?? throw new ArgumentNullException(nameof(workflowClient));
    private readonly Mud.Feishu.IFeishuTenantV2BitableRole _roleClient = roleClient
        ?? throw new ArgumentNullException(nameof(roleClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>bitable.form_get：获取表单元数据（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableFormGetTool))]
    public Task<ToolResult> GetFormAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableFormGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableFormGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _formClient
                .GetFormAsync(args.AppToken, args.TableId, args.FormId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectFormInfo);
        });
    }

    /// <summary>bitable.form_question_list：分页列出表单题目（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableFormQuestionListTool))]
    public Task<ToolResult> GetFormFieldsPageListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableFormQuestionList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableFormQuestionListArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ApiPageListTotalResult<AppTableFormFieldInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _formClient
                    .GetFormFieldsPageListAsync(args.AppToken, args.TableId, args.FormId, PageSizes.BitableFormQuestions, token, ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectFormQuestions, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>bitable.dashboard_list：分页列出仪表盘（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableDashboardListTool))]
    public Task<ToolResult> GetDashboardPageListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableDashboardList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableDashboardListArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ApiPageListResult<AppDashboard>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _dashboardClient
                    .GetDashboardPageListAsync(args.AppToken, PageSizes.BitableDashboards, token, ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectDashboards, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>bitable.workflow_list：列出全部工作流（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableWorkflowListTool))]
    public Task<ToolResult> GetAppWorkflowListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableWorkflowList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableWorkflowListArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _workflowClient
                .GetAppWorkflowListAsync(args.AppToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectWorkflows);
        });
    }

    /// <summary>bitable.workflow_block_list：列出与数据表相关的工作流（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableWorkflowBlockListTool))]
    public Task<ToolResult> GetAppBlockWorkflowListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableWorkflowBlockList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableWorkflowBlockListArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _workflowClient
                .GetAppBlockWorkflowListAsync(args.AppToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectWorkflows);
        });
    }

    /// <summary>bitable.role_list：分页列出自定义角色（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleListTool))]
    public Task<ToolResult> GetRolesPageListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleListArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ApiPageListTotalResult<AppRoleInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _roleClient
                    .GetRolesPageListAsync(args.AppToken, PageSizes.BitableRoles, token, ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectRoles, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>bitable.role_member_list：分页列出角色成员（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleMemberListTool))]
    public Task<ToolResult> GetRoleMembersPageListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleMemberList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleMemberListArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ApiPageListResult<AppRoleMember>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _roleClient
                    .GetRoleMembersPageListAsync(args.AppToken, args.RoleId, PageSizes.BitableRoleMembers, token, ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectRoleMembers, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>bitable.form_update：更新表单元数据（未提供的字段保持不变）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableFormUpdateTool))]
    public Task<ToolResult> UpdateFormAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableFormUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableFormUpdateArgs.Unpack(arguments);

            if (args.Name is null && args.Description is null && args.Shared is null
                && args.SharedLimit is null && args.SubmitLimitOnce is null)
            {
                throw new ArgumentException("至少提供一个待更新字段（name / description / shared / shared_limit / submit_limit_once）");
            }

            var request = new UpdateFormRequest
            {
                Name = args.Name,
                Description = args.Description,
                Shared = args.Shared,
                SharedLimit = args.SharedLimit,
                SubmitLimitOnce = args.SubmitLimitOnce,
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("form_id", args.FormId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _formClient
                .UpdateFormAsync(args.AppToken, args.TableId, args.FormId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectFormSummary);
        });
    }

    /// <summary>bitable.form_upgrade：把旧版表单升级为「新版表单」（不可撤销，先 dry_run 确认）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableFormUpgradeTool))]
    public Task<ToolResult> UpgradeFormAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableFormUpgrade, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableFormUpgradeArgs.Unpack(arguments);

            if (args.DisplayMode != "traditional" && args.DisplayMode != "one_question_per_page")
            {
                throw new ArgumentException("display_mode 必须为 'traditional' 或 'one_question_per_page'");
            }

            var request = new UpgradeFormRequest
            {
                FormName = args.FormName,
                DisplayMode = args.DisplayMode,
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/upgrade",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length),
                    ("form_id", args.FormId.Length), ("form_name", args.FormName.Length), ("display_mode", args.DisplayMode.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _formClient
                .UpgradeFormAsync(args.AppToken, args.TableId, args.FormId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["form_id"] = data.Form?.Id,
            });
        });
    }

    /// <summary>bitable.form_question_update：更新表单题目展示属性（未提供的字段保持不变）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableFormQuestionUpdateTool))]
    public Task<ToolResult> UpdateFormFieldAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableFormQuestionUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableFormQuestionUpdateArgs.Unpack(arguments);

            if (args.PreFieldId is null && args.Title is null && args.Description is null
                && args.Required is null && args.Visible is null)
            {
                throw new ArgumentException("至少提供一个待更新字段（pre_field_id / title / description / required / visible）");
            }

            var request = new UpdateFormFieldRequest
            {
                PreFieldId = args.PreFieldId,
                Title = args.Title,
                Description = args.Description,
                Required = args.Required,
                Visible = args.Visible,
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/fields/{field_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length),
                    ("form_id", args.FormId.Length), ("field_id", args.FieldId.Length)));
            }

            // UpdateFormFieldResult.Fields 无 FieldId（官方回包只带展示属性），投影的 field_id 取入参。
            var outcome = FeishuApiResultReader.Read(await _formClient
                .UpdateFormFieldAsync(args.AppToken, args.TableId, args.FormId, args.FieldId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["field_id"] = args.FieldId,
                ["title"] = data.Fields?.Title,
                ["required"] = data.Fields?.Required,
                ["visible"] = data.Fields?.Visible,
            });
        });
    }

    /// <summary>bitable.dashboard_copy：复制仪表盘。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableDashboardCopyTool))]
    public Task<ToolResult> CopyDashboardAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableDashboardCopy, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableDashboardCopyArgs.Unpack(arguments);

            var request = new CopyDashboardRequest { Name = args.Name };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/bitable/v1/apps/{app_token}/dashboards/{block_id}/copy",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("block_id", args.BlockId.Length), ("name", args.Name.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _dashboardClient
                .CopyDashboardAsync(args.AppToken, args.BlockId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["block_id"] = data.BlockId,
                ["name"] = data.Name,
            });
        });
    }

    /// <summary>bitable.workflow_update：启用 / 停用工作流。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableWorkflowUpdateTool))]
    public Task<ToolResult> UpdateAppWorkflowAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableWorkflowUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableWorkflowUpdateArgs.Unpack(arguments);

            if (args.Status != "Enable" && args.Status != "Disable")
            {
                throw new ArgumentException("status 必须为 'Enable'（启用）或 'Disable'（停用）");
            }

            var request = new UpdateAppWorkflowRequest { Status = args.Status };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", "/open-apis/bitable/v1/apps/{app_token}/workflows/{workflow_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("workflow_id", args.WorkflowId.Length), ("status", args.Status.Length)));
            }

            // UpdateAppWorkflowAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _workflowClient
                .UpdateAppWorkflowAsync(args.AppToken, args.WorkflowId, request, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["workflow_id"] = args.WorkflowId,
                ["status"] = args.Status,
                ["updated"] = true,
            });
        });
    }

    /// <summary>bitable.role_create：创建自定义角色。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleCreateTool))]
    public Task<ToolResult> CreateRoleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleCreateArgs.Unpack(arguments);

            var tableRoles = ParseOptionalArray<TableRole>(args.TableRoles, "table_roles");
            var blockRoles = ParseOptionalArray<BlockRole>(args.BlockRoles, "block_roles");

            var request = new CreateRoleRequest { RoleName = args.RoleName };
            if (tableRoles is not null)
            {
                request.TableRoles = tableRoles;
            }

            if (blockRoles is not null)
            {
                request.BlockRoles = blockRoles;
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/base/v2/apps/{app_token}/roles",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("role_name", args.RoleName.Length),
                    ("table_roles", args.TableRoles?.Length ?? 0), ("block_roles", args.BlockRoles?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _roleClient
                .CreateRoleAsync(args.AppToken, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectRoleOps);
        });
    }

    /// <summary>bitable.role_update：更新自定义角色（role_name 必填——官方要求即便不改名也需回传）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleUpdateTool))]
    public Task<ToolResult> UpdateRoleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleUpdateArgs.Unpack(arguments);

            var tableRoles = ParseOptionalArray<TableRole>(args.TableRoles, "table_roles");
            var blockRoles = ParseOptionalArray<BlockRole>(args.BlockRoles, "block_roles");

            var request = new UpdateRoleRequest { RoleName = args.RoleName };
            if (tableRoles is not null)
            {
                request.TableRoles = tableRoles;
            }

            if (blockRoles is not null)
            {
                request.BlockRoles = blockRoles;
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", "/open-apis/base/v2/apps/{app_token}/roles/{role_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("role_id", args.RoleId.Length), ("role_name", args.RoleName.Length),
                    ("table_roles", args.TableRoles?.Length ?? 0), ("block_roles", args.BlockRoles?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _roleClient
                .UpdateRoleAsync(args.AppToken, args.RoleId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectRoleOps);
        });
    }

    /// <summary>bitable.role_delete：删除自定义角色（不可恢复）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleDeleteTool))]
    public Task<ToolResult> DeleteRoleAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleDelete, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                // 删除不走 base/v2：官方删除角色接口为 bitable/v1（与 create/update/list 不同层）。
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("role_id", args.RoleId.Length)));
            }

            // DeleteRoleAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _roleClient
                .DeleteRoleAsync(args.AppToken, args.RoleId, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["role_id"] = args.RoleId,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>bitable.role_member_add：把成员加入自定义角色。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleMemberAddTool))]
    public Task<ToolResult> AddRoleMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleMemberAdd, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleMemberAddArgs.Unpack(arguments);

            var memberIdType = args.MemberIdType ?? "open_id";
            var request = new AddRoleMemberRequest { MemberId = args.MemberId };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("role_id", args.RoleId.Length),
                    ("member_id", args.MemberId.Length), ("member_id_type", memberIdType.Length)));
            }

            // AddRoleMemberAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _roleClient
                .AddRoleMemberAsync(args.AppToken, args.RoleId, request, member_id_type: memberIdType, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["role_id"] = args.RoleId,
                ["member_id"] = args.MemberId,
                ["added"] = true,
            });
        });
    }

    /// <summary>bitable.role_member_remove：把成员从自定义角色中移除。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableRoleMemberRemoveTool))]
    public Task<ToolResult> DeleteRoleMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRoleMemberRemove, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRoleMemberRemoveArgs.Unpack(arguments);

            var memberIdType = args.MemberIdType ?? "open_id";

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members/{member_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("role_id", args.RoleId.Length),
                    ("member_id", args.MemberId.Length), ("member_id_type", memberIdType.Length)));
            }

            // DeleteRoleMemberAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _roleClient
                .DeleteRoleMemberAsync(args.AppToken, args.RoleId, args.MemberId, member_id_type: memberIdType, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["role_id"] = args.RoleId,
                ["member_id"] = args.MemberId,
                ["removed"] = true,
            });
        });
    }

    /// <summary>form_get 投影：form（名称/描述/共享/分享范围/仅可填写一次/分享链接）。</summary>
    private static JsonObject ProjectFormInfo(FormResult data)
    {
        var form = data.Form;
        return new JsonObject
        {
            ["form"] = new JsonObject
            {
                ["name"] = form?.Name,
                ["description"] = form?.Description,
                ["shared"] = form?.Shared,
                ["shared_limit"] = form?.SharedLimit,
                ["submit_limit_once"] = form?.SubmitLimitOnce,
                ["shared_url"] = form?.SharedUrl,
            },
        };
    }

    /// <summary>form_update 投影：form（名称/共享/分享链接）。</summary>
    private static JsonObject ProjectFormSummary(FormResult data)
    {
        var form = data.Form;
        return new JsonObject
        {
            ["form"] = new JsonObject
            {
                ["name"] = form?.Name,
                ["shared"] = form?.Shared,
                ["shared_url"] = form?.SharedUrl,
            },
        };
    }

    /// <summary>form_question_list 投影：items（field_id/title/description/required/visible）+ 翻页契约。</summary>
    private static JsonObject ProjectFormQuestions(ApiPageListTotalResult<AppTableFormFieldInfo> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        foreach (var field in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["field_id"] = field.FieldId,
                ["title"] = field.Title,
                ["description"] = field.Description,
                ["required"] = field.Required,
                ["visible"] = field.Visible,
            });
        }

        return envelope;
    }

    /// <summary>dashboard_list 投影：items（block_id/name）+ 翻页契约。</summary>
    private static JsonObject ProjectDashboards(ApiPageListResult<AppDashboard> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        foreach (var dashboard in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["block_id"] = dashboard.BlockId,
                ["name"] = dashboard.Name,
            });
        }

        return envelope;
    }

    /// <summary>workflow_list / workflow_block_list 投影：items（workflow_id/status/title）。</summary>
    private static JsonObject ProjectWorkflows(GetAppWorkflowListResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var workflow in data.Workflows ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["workflow_id"] = workflow.WorkflowId,
                ["status"] = workflow.Status,
                ["title"] = workflow.Title,
            });
        }

        return envelope;
    }

    /// <summary>role_list 投影：items（role_id/role_name）+ 翻页契约。</summary>
    private static JsonObject ProjectRoles(ApiPageListTotalResult<AppRoleInfo> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        foreach (var role in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["role_id"] = role.RoleId,
                ["role_name"] = role.RoleName,
            });
        }

        return envelope;
    }

    /// <summary>role_member_list 投影：items（member_type/member_id/member_name）+ 翻页契约。</summary>
    private static JsonObject ProjectRoleMembers(ApiPageListResult<AppRoleMember> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        foreach (var member in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["member_type"] = member.MemberType,
                ["member_id"] = member.MemberId,
                ["member_name"] = member.MemberName,
            });
        }

        return envelope;
    }

    /// <summary>role_create / role_update 投影：role_id/role_name。</summary>
    private static JsonObject ProjectRoleOps(RoleOpsResult data)
    {
        var role = data.Role;
        return new JsonObject
        {
            ["role_id"] = role?.RoleId,
            ["role_name"] = role?.RoleName,
        };
    }

    /// <summary>role_create / role_update 的可选数组参数：缺失 ⇒ null；非法 JSON / 非数组 ⇒ 结构化错误。</summary>
    private static T[]? ParseOptionalArray<T>(string? json, string parameterName) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        T[]? items;
        try
        {
            // ⚠️ 必须走 FeishuJsonAot（AOT-3）：泛型 JsonSerializer.Deserialize<T> 重载带
            // RequiresUnreferencedCode/RequiresDynamicCode 标注，会破坏 AOT 严格模式 0 诊断门禁。
            // DTO 全部字段带 [JsonPropertyName]，故与解析器链的命名策略无关。
            items = Mud.Feishu.Abstractions.Utilities.FeishuJsonAot
                .Deserialize<T[]>(json, FeishuJsonDefaults.DeserializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"{parameterName} 解析失败：{ex.Message}");
        }

        if (items is null)
        {
            throw new ArgumentException($"{parameterName} 须为 JSON 数组");
        }

        return items;
    }

    /// <summary>
    /// 解包 <c>FeishuNullDataApiResult</c>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，
    /// T 不可推断且 Data 天然为 null）。
    /// </summary>
    private static void RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }
}
