// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

// <summary>
// Phase 2 写类工具接口集（§3.3：发消息 / 新增记录 / 发起审批实例，全部 <c>IsWrite = true</c>）。
// </summary>
// <remarks>
// <para>
// 复用 Phase 1 <c>[FeishuTool]</c> 源生成器（<c>IsWrite</c> 经 Schema 扩展 <c>x-feishu.is_write</c>
// 产出）；执行链（<c>FeishuToolBinding</c>）对写工具强制授权门禁——
// <c>EnforceToolAuthorization=true</c> 且未注册 <c>IToolExecutionAuthorizer</c> 即拒绝（安全默认），
// 白名单单独键控（<c>FeishuAgent:WriteAllowList</c>，默认空=不启用任何写工具）。
// </para>
// <para>
// 与只读工具同一约定：只声明模型可见 Schema（扁平参数）；请求体（<c>SendMessageRequest</c>/
// <c>RecordOpsRequest</c>/<c>CreateInstanceRequest</c>）由绑定层构造。
// </para>
// </remarks>

// ─────────────────────────── IM 写（1 个） ───────────────────────────

/// <summary>工具接口：im.send_message（映射 <c>IFeishuTenantV1Message.SendMessageAsync</c>）。</summary>
[FeishuTool("im.send_message",
    Description = "发送文本消息到指定群聊或用户。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message:send_as_bot"],
    IsWrite = true)]
public interface IFeishuImSendMessageTool
{
    /// <summary>发送文本消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）。</returns>
    Task<string> SendMessageAsync(
        [ToolParameter("receive_id", "接收者 ID（群聊 ocXxx 或用户 open_id）", Required = true)] string receive_id,
        [ToolParameter("text", "消息文本内容（纯文本）", Required = true)] string text,
        [ToolParameter("receive_id_type", "接收者 ID 类型（可选：chat_id=群聊 / open_id=用户 / user_id / union_id / email，默认 chat_id）")] string? receive_id_type = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Bitable 写（1 个） ───────────────────────────

/// <summary>工具接口：bitable.add_record（映射 <c>IFeishuTenantV1BitableRecord.AddRecordAsync</c>）。</summary>
[FeishuTool("bitable.add_record",
    Description = "向多维表格数据表新增一条记录，fields 为「字段名 → 值」JSON 对象（字段名先经 bitable.list_fields 确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。",
    RequiredScopes = ["bitable:app"],
    IsWrite = true)]
public interface IFeishuBitableAddRecordTool
{
    /// <summary>新增记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_id）。</returns>
    Task<string> AddRecordAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("fields", "记录字段 JSON 对象字符串，如 {\"任务名称\":\"写周报\",\"状态\":\"待办\"}", Required = true)] string fields,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 写（1 个） ───────────────────────────

/// <summary>工具接口：approval.create_instance（映射 <c>IFeishuTenantV4Approval.CreateInstanceAsync</c>）。</summary>
[FeishuTool("approval.create_instance",
    Description = "按审批定义 Code 发起一个审批实例，form 为审批表单 Value（JSON 数组字符串，按定义的表单控件结构填写）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true)]
public interface IFeishuApprovalCreateInstanceTool
{
    /// <summary>发起审批实例。</summary>
    /// <returns>白名单投影后的 JSON 文本（instance_code）。</returns>
    Task<string> CreateInstanceAsync(
        [ToolParameter("approval_code", "审批定义 Code", Required = true)] string approval_code,
        [ToolParameter("form", "审批表单 Value（JSON 数组字符串，按审批定义的表单控件结构）", Required = true)] string form,
        [ToolParameter("user_id", "发起人用户 ID（可选；user_id 类型，数据权限校验用）")] string? user_id = null,
        CancellationToken cancellationToken = default);
}
