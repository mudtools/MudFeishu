// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Workflows;

/// <summary>工作流步骤状态（闭集字面量；对外 JSON 的取值，改名属破坏性变更）。</summary>
internal static class FeishuWorkflowStepStatuses
{
    /// <summary>该步已执行成功。</summary>
    public const string Ok = "ok";

    /// <summary>该步失败并中断链路（其后的步骤不执行）。</summary>
    public const string Failed = "failed";

    /// <summary>因 <c>start_at</c> 跳过（断点续跑的已完成部分）。</summary>
    public const string Skipped = "skipped";

    /// <summary>预演模式：只列出计划，不执行。</summary>
    public const string Planned = "planned";
}

/// <summary>
/// 工作流的一步：调用哪枚工具、取什么入参。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么入参用委托而不是字符串模板</b>：模板语言会引入"变量没填/类型错"的静默失败面，
/// 而委托在编译期就固定了「哪个入参来自工作流入参」，无需在运行期解释一层。
/// 定义表是 <c>static readonly</c> 常量（无 IO、无反射、无配置读）。
/// </para>
/// <para>
/// <b>步骤只能调<b>已启用</b>的工具</b>：白名单是宿主的策略边界，工作流不得成为绕过白名单的旁路
/// （未启用的步骤直接判 <c>failed</c>，而不是"反正内部调用没关系"）。
/// </para>
/// </remarks>
/// <param name="Index">步骤序号（1 起，<c>start_at</c> 的口径）。</param>
/// <param name="ToolName">被调用的工具名（须在注册表中且已启用）。</param>
/// <param name="Purpose">这一步在报告里的可读用途（写进步骤轨迹，便于模型与宿主理解）。</param>
/// <param name="Bind">从工作流入参构造该步入参。</param>
internal sealed record FeishuWorkflowStep(
    int Index,
    string ToolName,
    string Purpose,
    Func<IReadOnlyDictionary<string, object?>, Dictionary<string, object?>> Bind);

/// <summary>一步的执行结论。</summary>
/// <param name="Index">步骤序号。</param>
/// <param name="ToolName">工具名。</param>
/// <param name="Purpose">用途。</param>
/// <param name="Status">状态字面量。</param>
/// <param name="DurationMs">耗时毫秒。</param>
/// <param name="Error">失败时的错误文本（成功为 null）。</param>
/// <param name="Data">成功时的结果 JSON（解析成功时；否则为 null）。</param>
internal sealed record FeishuWorkflowStepOutcome(
    int Index,
    string ToolName,
    string Purpose,
    string Status,
    long DurationMs,
    string? Error,
    JsonNode? Data);

/// <summary>
/// 工作流定义：步骤表 + 报告组装器。
/// </summary>
/// <remarks>
/// <b>报告组装器为什么在定义里而不是执行器里</b>：报告的"哪些字段对用户有意义"是<b>业务知识</b>，
/// 每个工作流各不相同；执行器只负责顺序、失败中断与轨迹记录，两者分离后新增工作流不需要动执行器。
/// </remarks>
/// <param name="ToolName">工作流工具名（与 <c>[AgentTool]</c> 一致，报告里回显）。</param>
/// <param name="Title">报告标题（中文可读）。</param>
/// <param name="Steps">步骤表（按 Index 升序）。</param>
/// <param name="Compose">以成功步骤数据组装报告节点。</param>
internal sealed record FeishuWorkflowDefinition(
    string ToolName,
    string Title,
    IReadOnlyList<FeishuWorkflowStep> Steps,
    Func<IReadOnlyDictionary<int, JsonNode?>, JsonObject> Compose);
