// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// OKR 域工具接口集（R6 / S2）：okr/*。
// </summary>
// <remarks>
// <para>
// <b>为什么首批选 OKR</b>：SDK 已有 <c>Mud.Feishu/Interfaces/Okr/</c> 29 个接口文件（v1 + v2 两代），
// 但 AI 工具面此前 0 策展——「有没有能力」与「模型能不能用」之间的缺口对模型完全不可见。
// </para>
// <para>
// <b>v1 / v2 的分工（有意只策展 v2 主干）</b>：v2 是 <c>cycle → objective → key_result</c> 的
// 可写主干（含富文本 content），v1 的 <c>users/{user_id}/okrs</c>、<c>okrs/batch_get</c> 与
// <c>progress_records</c> 走的是<b>另一套富文本契约</b>（<c>ContentBlock</c> 而非 <c>ContentBlockV2</c>），
// 混入同一批会让模型在两套块模型间误用 ⇒ 本批只取 v2 主干 + v1 的<b>只读元数据</b>
// （<c>list_periods</c>，周期是 v2 cycle 的上游元数据）。
// </para>
// <para>
// <b>Source 锚定纪律</b>：`Source` 一律由 `nameof(具体接口)` 与 `nameof(具体接口.方法)` 常量拼接而成
// （不写字面量，接口改名即编译失败），
// 方法可声明在<b>基接口</b>上（<c>IFeishuV2OkrObjective</c> → 实现类 <c>IFeishuTenantV2OkrObjective</c>），
// 但必须在<b>该接口的继承链内</b>——否则 <c>nameof</c> 直接编译失败。
// ⚠️ 特别注意 <c>ListCycleObjectivesAsync</c> 声明在 <c>IFeishuV2OkrCycle</c>（不是 Objective 接口）。
// </para>
// <para>
// <b>score / weight 用 string 而非 double</b>：参数解包映射表（引擎侧 <c>TryResolveReader</c>）
// 目前只覆盖 string/string[]/int/bool ↔ <c>ToolArgs</c> 读取器，<c>double</c> 会触发 MUDFT020
// （UnpackMappingMissing）。故取值范围 [0,1] 的数值参数以字符串暴露并在执行器内解析 + 校验，
// 错误文案可携带取值范围（比裸 number 更可读）。
// </para>
// </remarks>

// ─────────────────────────── 只读（9 个） ───────────────────────────

/// <summary>工具接口：okr.list_cycles（映射 <c>IFeishuTenantV2OkrCycle.ListCyclesAsync</c>）。</summary>
[FeishuTool("okr.list_cycles",
    Description = "列出指定用户的 OKR 周期（Cycle）列表（含 id、周期状态、起止时间、得分），可翻页。只读，需 okr:okr.period:readonly。返回的 cycle_id 是 okr.list_objectives 的前置输入。",
    RequiredScopes = ["okr:okr.period:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrCycle) + "." + nameof(IFeishuTenantV2OkrCycle.ListCyclesAsync))]
public interface IFeishuTenantOkrListCyclesTool
{
    /// <summary>列出用户的 OKR 周期。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: id/tenant_cycle_id/owner_id/start_time/end_time/cycle_status/score + 翻页契约）。</returns>
    Task<string> ListCyclesAsync(
        [ToolParameter("user_id", "OKR 归属用户 ID（open_id，形如 ou_xxx）", Required = true)] string user_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.list_objectives（映射 <c>IFeishuTenantV2OkrCycle.ListCycleObjectivesAsync</c>）。</summary>
[FeishuTool("okr.list_objectives",
    Description = "列出某个 OKR 周期（Cycle）下的目标（Objective）列表（含 id、富文本内容、归属者、序号、得分、权重、截止时间），可翻页。只读，需 okr:okr.content:readonly。cycle_id 来自 okr.list_cycles。",
    RequiredScopes = ["okr:okr.content:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrCycle) + "." + nameof(IFeishuTenantV2OkrCycle.ListCycleObjectivesAsync))]
public interface IFeishuTenantOkrListObjectivesTool
{
    /// <summary>列出周期下的目标。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: objective_id/content/owner_id/position/score/weight/deadline + 翻页契约）。</returns>
    Task<string> ListObjectivesAsync(
        [ToolParameter("cycle_id", "OKR 周期 ID（来自 okr.list_cycles）", Required = true)] string cycle_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.get_objective（映射 <c>IFeishuTenantV2OkrObjective.GetObjectiveAsync</c>）。</summary>
[FeishuTool("okr.get_objective",
    Description = "获取单个目标（Objective）的详情（富文本内容、备注、归属者、得分、权重、截止时间、分类）。只读，需 okr:okr.content:readonly。",
    RequiredScopes = ["okr:okr.content:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrObjective) + "." + nameof(IFeishuTenantV2OkrObjective.GetObjectiveAsync))]
public interface IFeishuTenantOkrGetObjectiveTool
{
    /// <summary>获取目标详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（objective_id/content/notes/owner_id/score/weight/deadline/category_id）。</returns>
    Task<string> GetObjectiveAsync(
        [ToolParameter("objective_id", "目标 ID（来自 okr.list_objectives）", Required = true)] string objective_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.list_key_results（映射 <c>IFeishuTenantV2OkrKeyResult.ListObjectiveKeyResultsAsync</c>）。</summary>
[FeishuTool("okr.list_key_results",
    Description = "列出某个目标（Objective）下的关键结果（Key Result）列表（含 id、内容、序号、得分、权重、截止时间），可翻页。只读，需 okr:okr.content:readonly。",
    RequiredScopes = ["okr:okr.content:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrKeyResult) + "." + nameof(IFeishuTenantV2OkrKeyResult.ListObjectiveKeyResultsAsync))]
public interface IFeishuTenantOkrListKeyResultsTool
{
    /// <summary>列出目标下的关键结果。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: key_result_id/content/position/score/weight/deadline + 翻页契约）。</returns>
    Task<string> ListKeyResultsAsync(
        [ToolParameter("objective_id", "目标 ID（来自 okr.list_objectives）", Required = true)] string objective_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.get_key_result（映射 <c>IFeishuTenantV2OkrKeyResult.GetKeyResultAsync</c>）。</summary>
[FeishuTool("okr.get_key_result",
    Description = "获取单个关键结果（Key Result）的详情（内容、归属者、序号、得分、权重、截止时间）。只读，需 okr:okr.content:readonly。",
    RequiredScopes = ["okr:okr.content:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrKeyResult) + "." + nameof(IFeishuTenantV2OkrKeyResult.GetKeyResultAsync))]
public interface IFeishuTenantOkrGetKeyResultTool
{
    /// <summary>获取关键结果详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（key_result_id/objective_id/content/score/weight/deadline）。</returns>
    Task<string> GetKeyResultAsync(
        [ToolParameter("key_result_id", "关键结果 ID（来自 okr.list_key_results）", Required = true)] string key_result_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.list_objective_progresses（映射 <c>IFeishuTenantV2OkrProgress.ListObjectiveProgressesAsync</c>）。</summary>
[FeishuTool("okr.list_objective_progresses",
    Description = "列出某个目标（Objective）下的进展记录列表（含进展内容、完成度、归属者、创建/更新时间），可翻页。只读，需 okr:okr.progress:readonly。",
    RequiredScopes = ["okr:okr.progress:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrProgress) + "." + nameof(IFeishuTenantV2OkrProgress.ListObjectiveProgressesAsync))]
public interface IFeishuTenantOkrListObjectiveProgressesTool
{
    /// <summary>列出目标下的进展记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: progress_id/content/progress_percent/progress_status/owner_id/create_time + 翻页契约）。</returns>
    Task<string> ListObjectiveProgressesAsync(
        [ToolParameter("objective_id", "目标 ID（来自 okr.list_objectives）", Required = true)] string objective_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.list_key_result_progresses（映射 <c>IFeishuTenantV2OkrProgress.ListKeyResultProgressesAsync</c>）。</summary>
[FeishuTool("okr.list_key_result_progresses",
    Description = "列出某个关键结果（Key Result）下的进展记录列表（含进展内容、完成度、归属者、创建/更新时间），可翻页。只读，需 okr:okr.progress:readonly。",
    RequiredScopes = ["okr:okr.progress:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrProgress) + "." + nameof(IFeishuTenantV2OkrProgress.ListKeyResultProgressesAsync))]
public interface IFeishuTenantOkrListKeyResultProgressesTool
{
    /// <summary>列出关键结果下的进展记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: progress_id/content/progress_percent/progress_status/owner_id/create_time + 翻页契约）。</returns>
    Task<string> ListKeyResultProgressesAsync(
        [ToolParameter("key_result_id", "关键结果 ID（来自 okr.list_key_results）", Required = true)] string key_result_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.list_periods（映射 <c>IFeishuTenantV1OkrPeriod.ListPeriodsAsync</c>）。</summary>
[FeishuTool("okr.list_periods",
    Description = "列出当前租户下的 OKR 周期（Period）定义（id、中英文名称、启用状态、起止时间），可翻页。只读，需 okr:okr:readonly。period 是周期模板元数据，与用户 Cycle 不同（create 时需要的是 cycle_id）。",
    RequiredScopes = ["okr:okr:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1OkrPeriod) + "." + nameof(IFeishuTenantV1OkrPeriod.ListPeriodsAsync))]
public interface IFeishuTenantOkrListPeriodsTool
{
    /// <summary>列出 OKR 周期定义。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: period_id/zh_name/en_name/status/period_start_time/period_end_time + 翻页契约）。</returns>
    Task<string> ListPeriodsAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.list_categories（映射 <c>IFeishuTenantV2OkrCategory.ListCategoriesAsync</c>）。</summary>
[FeishuTool("okr.list_categories",
    Description = "列出系统中的全部 OKR 分类（id、多语言名称、颜色、类型、启用状态），可翻页。只读，需 okr:okr.setting:read。category_id 是 okr.create_objective / okr.update_objective 的可选输入。",
    RequiredScopes = ["okr:okr.setting:read"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV2OkrCategory) + "." + nameof(IFeishuTenantV2OkrCategory.ListCategoriesAsync))]
public interface IFeishuTenantOkrListCategoriesTool
{
    /// <summary>列出 OKR 分类。</summary>
    /// <returns>白名单投影后的 JSON 文本（items: category_id/name_zh/enabled/color/category_type + 翻页契约）。</returns>
    Task<string> ListCategoriesAsync(
        [ToolParameter("owner_type", "分类归属类型（可选，默认 user）：user 员工 / department 部门")] string? owner_type = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 写入（6 个） ───────────────────────────

/// <summary>工具接口：okr.create_objective（映射 <c>IFeishuTenantV2OkrCycle.CreateCycleObjectiveAsync</c>）。</summary>
[FeishuTool("okr.create_objective",
    Description = "在指定 OKR 周期（Cycle）下创建一个目标（Objective）（content 为纯文本，工具层负责包装为飞书富文本块）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 okr:okr.content:writeonly。",
    RequiredScopes = ["okr:okr.content:writeonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2OkrCycle) + "." + nameof(IFeishuTenantV2OkrCycle.CreateCycleObjectiveAsync))]
public interface IFeishuTenantOkrCreateObjectiveTool
{
    /// <summary>创建目标。</summary>
    /// <returns>白名单投影后的 JSON 文本（objective_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateObjectiveAsync(
        [ToolParameter("cycle_id", "OKR 周期 ID（来自 okr.list_cycles）", Required = true)] string cycle_id,
        [ToolParameter("content", "目标内容（纯文本；工具层包装为飞书富文本块）", Required = true)] string content,
        [ToolParameter("notes", "目标备注（纯文本，可选）")] string? notes = null,
        [ToolParameter("deadline", "截止时间（毫秒时间戳字符串，可选）")] string? deadline = null,
        [ToolParameter("score", "初始得分（可选，取值 0~1，字符串形式，如 \"0.5\"）")] string? score = null,
        [ToolParameter("weight", "权重（可选，取值 0~1，字符串形式，如 \"0.5\"）")] string? weight = null,
        [ToolParameter("category_id", "分类 ID（可选，来自 okr.list_categories）")] string? category_id = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.update_objective（映射 <c>IFeishuTenantV2OkrObjective.UpdateObjectiveAsync</c>）。</summary>
[FeishuTool("okr.update_objective",
    Description = "修改指定目标（Objective）的字段（content/notes/deadline/score/category_id，至少传一个要更新的字段）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 okr:okr.content:writeonly。",
    RequiredScopes = ["okr:okr.content:writeonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2OkrObjective) + "." + nameof(IFeishuTenantV2OkrObjective.UpdateObjectiveAsync))]
public interface IFeishuTenantOkrUpdateObjectiveTool
{
    /// <summary>修改目标。</summary>
    /// <returns>白名单投影后的 JSON 文本（objective_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateObjectiveAsync(
        [ToolParameter("objective_id", "目标 ID（来自 okr.list_objectives）", Required = true)] string objective_id,
        [ToolParameter("content", "目标内容（纯文本，可选更新）")] string? content = null,
        [ToolParameter("notes", "目标备注（纯文本，可选更新）")] string? notes = null,
        [ToolParameter("deadline", "截止时间（毫秒时间戳字符串，可选更新）")] string? deadline = null,
        [ToolParameter("score", "得分（可选更新，取值 0~1，字符串形式）")] string? score = null,
        [ToolParameter("category_id", "分类 ID（可选更新，来自 okr.list_categories）")] string? category_id = null,
        [ToolParameter("dry_run", "仅预演不修改（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.delete_objective（映射 <c>IFeishuTenantV2OkrObjective.DeleteObjectiveAsync</c>）。</summary>
[FeishuTool("okr.delete_objective",
    Description = "删除指定目标（Objective），并连带删除其下的关键结果——不可恢复。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 okr:okr.content:writeonly。建议先 dry_run 预演确认。",
    RequiredScopes = ["okr:okr.content:writeonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2OkrObjective) + "." + nameof(IFeishuTenantV2OkrObjective.DeleteObjectiveAsync))]
public interface IFeishuTenantOkrDeleteObjectiveTool
{
    /// <summary>删除目标。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true + objective_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteObjectiveAsync(
        [ToolParameter("objective_id", "目标 ID（来自 okr.list_objectives）", Required = true)] string objective_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.create_key_result（映射 <c>IFeishuTenantV2OkrKeyResult.CreateObjectiveKeyResultAsync</c>）。</summary>
[FeishuTool("okr.create_key_result",
    Description = "在指定目标（Objective）下创建一个关键结果（Key Result）（content 为纯文本，工具层负责包装为飞书富文本块）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 okr:okr.content:writeonly。",
    RequiredScopes = ["okr:okr.content:writeonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2OkrKeyResult) + "." + nameof(IFeishuTenantV2OkrKeyResult.CreateObjectiveKeyResultAsync))]
public interface IFeishuTenantOkrCreateKeyResultTool
{
    /// <summary>创建关键结果。</summary>
    /// <returns>白名单投影后的 JSON 文本（key_result_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateKeyResultAsync(
        [ToolParameter("objective_id", "目标 ID（来自 okr.list_objectives）", Required = true)] string objective_id,
        [ToolParameter("content", "关键结果内容（纯文本；工具层包装为飞书富文本块）", Required = true)] string content,
        [ToolParameter("deadline", "截止时间（毫秒时间戳字符串，可选）")] string? deadline = null,
        [ToolParameter("score", "初始得分（可选，取值 0~1，字符串形式）")] string? score = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.update_key_result（映射 <c>IFeishuTenantV2OkrKeyResult.UpdateKeyResultAsync</c>）。</summary>
[FeishuTool("okr.update_key_result",
    Description = "修改指定关键结果（Key Result）的内容、得分或截止时间（至少传一个要更新的字段）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 okr:okr.content:writeonly。",
    RequiredScopes = ["okr:okr.content:writeonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2OkrKeyResult) + "." + nameof(IFeishuTenantV2OkrKeyResult.UpdateKeyResultAsync))]
public interface IFeishuTenantOkrUpdateKeyResultTool
{
    /// <summary>修改关键结果。</summary>
    /// <returns>白名单投影后的 JSON 文本（key_result_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateKeyResultAsync(
        [ToolParameter("key_result_id", "关键结果 ID（来自 okr.list_key_results）", Required = true)] string key_result_id,
        [ToolParameter("content", "关键结果内容（纯文本，可选更新）")] string? content = null,
        [ToolParameter("deadline", "截止时间（毫秒时间戳字符串，可选更新）")] string? deadline = null,
        [ToolParameter("score", "得分（可选更新，取值 0~1，字符串形式）")] string? score = null,
        [ToolParameter("dry_run", "仅预演不修改（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：okr.delete_key_result（映射 <c>IFeishuTenantV2OkrKeyResult.DeleteKeyResultAsync</c>）。</summary>
[FeishuTool("okr.delete_key_result",
    Description = "删除指定关键结果（Key Result）——不可恢复。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 okr:okr.content:writeonly。建议先 dry_run 预演确认。",
    RequiredScopes = ["okr:okr.content:writeonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2OkrKeyResult) + "." + nameof(IFeishuTenantV2OkrKeyResult.DeleteKeyResultAsync))]
public interface IFeishuTenantOkrDeleteKeyResultTool
{
    /// <summary>删除关键结果。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true + key_result_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteKeyResultAsync(
        [ToolParameter("key_result_id", "关键结果 ID（来自 okr.list_key_results）", Required = true)] string key_result_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
