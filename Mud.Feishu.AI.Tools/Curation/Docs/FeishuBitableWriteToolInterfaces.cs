// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

// 工作流接口落在 Mud.Feishu.Interfaces（其余 Bitable Tenant 接口在 Mud.Feishu，见 GlobalUsings）。
using Mud.Feishu.Interfaces;

namespace Mud.Feishu.AI.Tools.Curation;

/// <summary>工具接口：bitable.add_record（映射 <c>IFeishuTenantV1BitableRecord.AddRecordAsync</c>）。</summary>
[AgentTool("bitable.add_record",
    Description = "向多维表格数据表新增一条记录，fields 为「字段名 → 值」JSON 对象（字段名先经 bitable.list_fields 确认；字段名拼错会在下发前被拒绝）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.AddRecordAsync))]
public interface IFeishuTenantBitableAddRecordTool
{
    /// <summary>新增记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddRecordAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("fields", "记录字段 JSON 对象字符串，须是 JSON 对象（非数组/标量），如 {\"任务名称\":\"写周报\",\"状态\":\"待办\"}", Required = true)] string fields,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键的重复请求返回同一条记录（不会重复创建）；省略时不保证幂等。建议由调用方给出稳定值（如「单据号+动作」），不要用随机数。幂等键不跨工具共享（不同工具的同名键互不影响）。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.update_record（映射 <c>IFeishuTenantV1BitableRecord.UpdateRecordAsync</c>）。</summary>
[AgentTool("bitable.update_record",
    Description = "更新多维表格中的指定记录（按 record_id 更新 fields）。fields 为「字段名 → 值」JSON 对象字符串。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.UpdateRecordAsync))]
public interface IFeishuTenantBitableUpdateRecordTool
{
    /// <summary>更新记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateRecordAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("record_id", "记录 ID（形如 recXxx，来自 bitable.query_records）", Required = true)] string record_id,
        [ToolParameter("fields", "更新字段 JSON 对象字符串，如 {\"状态\":\"已完成\"}", Required = true)] string fields,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 重复请求不会产生副作用。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.delete_record（映射 <c>IFeishuTenantV1BitableRecord.DeleteRecordAsync</c>）。</summary>
[AgentTool("bitable.delete_record",
    Description = "删除多维表格中的指定记录（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.DeleteRecordAsync))]
public interface IFeishuTenantBitableDeleteRecordTool
{
    /// <summary>删除记录（不可恢复）。</summary>
    /// <returns>白名单投影后的 JSON 文本（删除确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteRecordAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("record_id", "要删除的记录 ID（形如 recXxx）", Required = true)] string record_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.record_batch_create（映射 <c>IFeishuTenantV1BitableRecord.AddRecordsAsync</c>）。</summary>
[AgentTool("bitable.record_batch_create",
    Description = "批量新增多维表格记录（单次最多 1000 条）。records 为 JSON 数组字符串，每项形如 {\"fields\":{\"字段名\":\"值\"}}；字段名先经 bitable.list_fields 确认。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.AddRecordsAsync))]
public interface IFeishuTenantBitableRecordBatchCreateTool
{
    /// <summary>批量新增记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_ids/count）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddRecordsAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("records", "新增记录数组 JSON 字符串，每项形如 {\"fields\":{\"任务名称\":\"写周报\",\"状态\":\"待办\"}}，最多 1000 条", Required = true)] string records,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键的重复请求不会重复创建；省略时不保证幂等。建议由调用方给出稳定值（如「批次号+动作」），不要用随机数。幂等键不跨工具共享（不同工具的同名键互不影响）。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.record_batch_update（映射 <c>IFeishuTenantV1BitableRecord.UpdateRecordsAsync</c>）。</summary>
[AgentTool("bitable.record_batch_update",
    Description = "批量更新多维表格记录（单次最多 1000 条）。records 为 JSON 数组字符串，每项形如 {\"record_id\":\"recXxx\",\"fields\":{\"字段名\":\"新值\"}}。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.UpdateRecordsAsync))]
public interface IFeishuTenantBitableRecordBatchUpdateTool
{
    /// <summary>批量更新记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_ids/count）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateRecordsAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("records", "更新记录数组 JSON 字符串，每项形如 {\"record_id\":\"recXxx\",\"fields\":{\"状态\":\"已完成\"}}，最多 1000 条", Required = true)] string records,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.form_update（映射 <c>IFeishuTenantV1BitableForm.UpdateFormAsync</c>）。</summary>
[AgentTool("bitable.form_update",
    Description = "更新多维表格表单的元数据（名称/描述/是否共享/分享范围/仅可填写一次）；未提供的字段保持不变，但至少提供一个。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableForm) + "." + nameof(IFeishuTenantV1BitableForm.UpdateFormAsync))]
public interface IFeishuTenantBitableFormUpdateTool
{
    /// <summary>更新表单元数据。</summary>
    /// <returns>白名单投影后的 JSON 文本（form：名称/共享/分享链接）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateFormAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("form_id", "表单 ID（形如 vewXxx）", Required = true)] string form_id,
        [ToolParameter("name", "表单名称（可选更新）")] string? name = null,
        [ToolParameter("description", "表单描述（可选更新）")] string? description = null,
        [ToolParameter("shared", "是否共享表单（可选更新）：true 开启共享 / false 关闭")] bool? shared = null,
        [ToolParameter("shared_limit", "分享范围（可选更新，仅在 shared=true 时有意义）：off 仅受邀的人 / tenant_editable 组织内获得链接的人 / anyone_editable 互联网上获得链接的人")] string? shared_limit = null,
        [ToolParameter("submit_limit_once", "仅可填写一次（可选更新）：true 限制为一次 / false 不限制")] bool? submit_limit_once = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.form_upgrade（映射 <c>IFeishuTenantV1BitableForm.UpgradeFormAsync</c>）。</summary>
[AgentTool("bitable.form_upgrade",
    Description = "把旧版表单升级为「新版表单」并指定布局（traditional 传统布局 / one_question_per_page 一页一题布局）；升级后表单形态会改变、不可撤销，请先 dry_run 预演确认。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableForm) + "." + nameof(IFeishuTenantV1BitableForm.UpgradeFormAsync))]
public interface IFeishuTenantBitableFormUpgradeTool
{
    /// <summary>升级表达表单。</summary>
    /// <returns>白名单投影后的 JSON 文本（form_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpgradeFormAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("form_id", "表单 ID（形如 vewXxx）", Required = true)] string form_id,
        [ToolParameter("form_name", "升级后的表单名称", Required = true)] string form_name,
        [ToolParameter("display_mode", "布局模式（traditional 传统布局 / one_question_per_page 一页一题）", Required = true)] string display_mode,
        [ToolParameter("dry_run", "仅预演不升级（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.form_question_update（映射 <c>IFeishuTenantV1BitableForm.UpdateFormFieldAsync</c>）。</summary>
[AgentTool("bitable.form_question_update",
    Description = "更新表单中某道题目（字段）的展示属性（标题/描述/是否必填/是否可见/前序题目位置）；未提供的字段保持不变，但至少提供一个。field_id 来自 bitable.form_question_list。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableForm) + "." + nameof(IFeishuTenantV1BitableForm.UpdateFormFieldAsync))]
public interface IFeishuTenantBitableFormQuestionUpdateTool
{
    /// <summary>更新表单题目属性。</summary>
    /// <returns>白名单投影后的 JSON 文本（field_id/title/required/visible）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateFormFieldAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("form_id", "表单 ID（形如 vewXxx）", Required = true)] string form_id,
        [ToolParameter("field_id", "表单题目对应的字段 ID（来自 bitable.form_question_list）", Required = true)] string field_id,
        [ToolParameter("pre_field_id", "排到该字段之后（可选）：目标前序字段 ID，用于调整题目顺序")] string? pre_field_id = null,
        [ToolParameter("title", "题目标题（可选更新）")] string? title = null,
        [ToolParameter("description", "题目描述（可选更新）")] string? description = null,
        [ToolParameter("required", "是否必填（可选更新）")] bool? required = null,
        [ToolParameter("visible", "是否可见（可选更新）")] bool? visible = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.dashboard_copy（映射 <c>IFeishuTenantV1BitableDashboard.CopyDashboardAsync</c>）。</summary>
[AgentTool("bitable.dashboard_copy",
    Description = "复制多维表格仪表盘（生成一份命名副本）。block_id 来自 bitable.dashboard_list。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableDashboard) + "." + nameof(IFeishuTenantV1BitableDashboard.CopyDashboardAsync))]
public interface IFeishuTenantBitableDashboardCopyTool
{
    /// <summary>复制仪表盘。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id/name）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CopyDashboardAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("block_id", "仪表盘 ID（形如 blkXxx，来自 bitable.dashboard_list）", Required = true)] string block_id,
        [ToolParameter("name", "副本名称", Required = true)] string name,
        [ToolParameter("dry_run", "仅预演不复制（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.workflow_update（映射 <c>IFeishuTenantV1BitableWorkflow.UpdateAppWorkflowAsync</c>）。</summary>
[AgentTool("bitable.workflow_update",
    Description = "启用 / 停用多维表格工作流（status：Enable 启用 / Disable 停用）。workflow_id 来自 bitable.workflow_list。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1BitableWorkflow) + "." + nameof(IFeishuTenantV1BitableWorkflow.UpdateAppWorkflowAsync))]
public interface IFeishuTenantBitableWorkflowUpdateTool
{
    /// <summary>更新工作流状态。</summary>
    /// <returns>白名单投影后的 JSON 文本（workflow_id/status）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateAppWorkflowAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("workflow_id", "工作流 ID（来自 bitable.workflow_list）", Required = true)] string workflow_id,
        [ToolParameter("status", "目标状态：Enable 启用 / Disable 停用", Required = true)] string status,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.role_create（映射 <c>IFeishuTenantV2BitableRole.CreateRoleAsync</c>）。</summary>
[AgentTool("bitable.role_create",
    Description = "创建多维表格高级权限的自定义角色（role_name 必填；table_roles / block_roles 为 JSON 数组字符串，省略时按官方默认权限创建）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2BitableRole) + "." + nameof(IFeishuTenantV2BitableRole.CreateRoleAsync))]
public interface IFeishuTenantBitableRoleCreateTool
{
    /// <summary>创建自定义角色。</summary>
    /// <returns>白名单投影后的 JSON 文本（role_id/role_name）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateRoleAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("role_name", "角色名称（不可与既有角色重名）", Required = true)] string role_name,
        [ToolParameter("table_roles", "数据表权限数组 JSON 字符串（可选），每项形如 {\"table_id\":\"tblXxx\",\"table_perm\":1}（table_perm：0 无权限 / 1 可阅读 / 2 可编辑 / 3 可管理）")] string? table_roles = null,
        [ToolParameter("block_roles", "仪表盘等区块权限数组 JSON 字符串（可选），每项形如 {\"block_id\":\"blkXxx\",\"block_perm\":1}")] string? block_roles = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.role_update（映射 <c>IFeishuTenantV2BitableRole.UpdateRoleAsync</c>）。</summary>
[AgentTool("bitable.role_update",
    Description = "更新多维表格高级权限的自定义角色（role_name 必填：官方接口要求即便不改名也需回传现有名称；table_roles / block_roles 可选，未提供时保持现状）。role_id 与现有名称可先用 bitable.role_list 取回。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2BitableRole) + "." + nameof(IFeishuTenantV2BitableRole.UpdateRoleAsync))]
public interface IFeishuTenantBitableRoleUpdateTool
{
    /// <summary>更新自定义角色。</summary>
    /// <returns>白名单投影后的 JSON 文本（role_id/role_name）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateRoleAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("role_id", "自定义角色 ID（形如 rolXxx，来自 bitable.role_list）", Required = true)] string role_id,
        [ToolParameter("role_name", "角色名称（必填：官方接口要求，即便不改名也需回传现有名称——可先用 bitable.role_list 取回）", Required = true)] string role_name,
        [ToolParameter("table_roles", "数据表权限数组 JSON 字符串（可选更新；省略时保持旧值，注意传空数组会清空既有权限），每项形如 {\"table_id\":\"tblXxx\",\"table_perm\":1}")] string? table_roles = null,
        [ToolParameter("block_roles", "区块权限数组 JSON 字符串（可选更新；省略时保持旧值），每项形如 {\"block_id\":\"blkXxx\",\"block_perm\":1}")] string? block_roles = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.role_delete（映射 <c>IFeishuTenantV2BitableRole.DeleteRoleAsync</c>）。</summary>
[AgentTool("bitable.role_delete",
    Description = "删除多维表格高级权限的自定义角色（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2BitableRole) + "." + nameof(IFeishuTenantV2BitableRole.DeleteRoleAsync))]
public interface IFeishuTenantBitableRoleDeleteTool
{
    /// <summary>删除自定义角色（不可恢复）。</summary>
    /// <returns>白名单投影后的 JSON 文本（删除确认）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteRoleAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("role_id", "要删除的自定义角色 ID（形如 rolXxx，来自 bitable.role_list）", Required = true)] string role_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.role_member_add（映射 <c>IFeishuTenantV2BitableRole.AddRoleMemberAsync</c>）。</summary>
[AgentTool("bitable.role_member_add",
    Description = "把成员（用户 / 群 / 部门，由 member_id_type 指定 ID 类型）加入多维表格自定义角色；加入后该成员按角色权限访问多维表格。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2BitableRole) + "." + nameof(IFeishuTenantV2BitableRole.AddRoleMemberAsync))]
public interface IFeishuTenantBitableRoleMemberAddTool
{
    /// <summary>向自定义角色添加成员。</summary>
    /// <returns>白名单投影后的 JSON 文本（role_id/member_id/added）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddRoleMemberAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("role_id", "自定义角色 ID（形如 rolXxx，来自 bitable.role_list）", Required = true)] string role_id,
        [ToolParameter("member_id", "成员 ID（类型由 member_id_type 指定）", Required = true)] string member_id,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / union_id / user_id / chat_id / open_department_id，默认 open_id）")] string? member_id_type = null,
        [ToolParameter("dry_run", "仅预演不添加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.role_member_remove（映射 <c>IFeishuTenantV2BitableRole.DeleteRoleMemberAsync</c>）。</summary>
[AgentTool("bitable.role_member_remove",
    Description = "把成员从多维表格自定义角色中移除（仅收回该角色的高级权限，不影响成员本身；移除后成员将立即失去该角色的可见范围，建议先 dry_run 预演确认）；member_id 类型由 member_id_type 指定，默认 open_id。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2BitableRole) + "." + nameof(IFeishuTenantV2BitableRole.DeleteRoleMemberAsync))]
public interface IFeishuTenantBitableRoleMemberRemoveTool
{
    /// <summary>从自定义角色移除成员。</summary>
    /// <returns>白名单投影后的 JSON 文本（role_id/member_id/removed）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteRoleMemberAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("role_id", "自定义角色 ID（形如 rolXxx，来自 bitable.role_list）", Required = true)] string role_id,
        [ToolParameter("member_id", "要移除的成员 ID（可由 bitable.role_member_list 获得）", Required = true)] string member_id,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / union_id / user_id / chat_id / open_department_id，默认 open_id）")] string? member_id_type = null,
        [ToolParameter("dry_run", "仅预演不移除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
