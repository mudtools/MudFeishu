// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

/// <summary>工具接口：bitable.add_record（映射 <c>IFeishuTenantV1BitableRecord.AddRecordAsync</c>）。</summary>
[FeishuTool("bitable.add_record",
    Description = "向多维表格数据表新增一条记录，fields 为「字段名 → 值」JSON 对象（字段名先经 bitable.list_fields 确认；字段名拼错会在下发前被拒绝）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = "IFeishuTenantV1BitableRecord.AddRecordAsync")]
public interface IFeishuBitableAddRecordTool
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
[FeishuTool("bitable.update_record",
    Description = "更新多维表格中的指定记录（按 record_id 更新 fields）。fields 为「字段名 → 值」JSON 对象字符串。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = "IFeishuTenantV1BitableRecord.UpdateRecordAsync")]
public interface IFeishuBitableUpdateRecordTool
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
[FeishuTool("bitable.delete_record",
    Description = "删除多维表格中的指定记录（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true,
    Source = "IFeishuTenantV1BitableRecord.DeleteRecordAsync")]
public interface IFeishuBitableDeleteRecordTool
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
