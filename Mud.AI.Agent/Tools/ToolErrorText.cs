// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// 结构化错误文本构造（模型可读、带工具名前缀）与错误类别字面量映射。
/// </summary>
/// <remarks>
/// v2.3 中立化迁入：原为 <c>FeishuToolBinding</c>（飞书执行链）的静态助手——
/// 执行器骨架 <c>ToolExecutor</c> 与 <c>ToolResultPipeline</c> 都要调用它们，
/// 而 <c>FeishuToolBinding</c> 留飞书侧（它是授权门禁 + 租户切换的执行链，深度产品耦合）。
/// 故把这两个纯文本构造点上移到骨架；飞书侧 <c>FeishuToolBinding</c> 的同类静态方法
/// 转为对本类型的转发（20+ 执行器调用点零改动）。
/// </remarks>
public static class ToolErrorText
{
    /// <summary>构造结构化错误文本（模型可读、带工具名前缀）。</summary>
    public static string StructuredError(string toolName, string reason)
        => $"[tool_error] {toolName}: {reason}";

    /// <summary>
    /// 构造带错误语义分类的结构化错误文本（B2 错误契约：分类 + 原因 + 建议——
    /// 授权拒绝与参数错误可区分，模型据此自我修正：重试 or 换参数 or 放弃）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ToolErrorCategory.Confirmation"/> 是<b>独立</b>语义，
    /// 不得复用 <see cref="ToolErrorCategory.Validation"/> 的"请修正参数"后缀——待确认不是参数错，
    /// 让模型去改参数会让它陷入无意义的重试循环。
    /// </para>
    /// <para>
    /// <b>B2 错误契约</b>：此方法返回人类可读正文；结构化 JSON 载荷由调用方置于正文首行。
    /// </para>
    /// </remarks>
    public static string StructuredError(string toolName, ToolErrorCategory category, string reason, int? apiCode = null, string? subtype = null)
    {
        // 无 code 时不产出空槽（保持既有文案形态不变）。
        var codeSegment = apiCode.HasValue ? $" code={apiCode.Value}" : string.Empty;

        var authLabel = subtype switch
        {
            ToolErrorSubtype.UserAuthRequired => "user_auth_required",
            ToolErrorSubtype.UserTokenRefreshFailed => "user_token_refresh_failed",
            _ => "forbidden",
        };
        var authNextStep = subtype switch
        {
            ToolErrorSubtype.UserAuthRequired => "请告知用户完成授权后重试——不是权限不足，无需放弃或换只读方案",
            ToolErrorSubtype.UserTokenRefreshFailed => "请告知用户重新完成授权后重试——令牌续期失败，不是权限不足",
            _ => "授权被拒绝，请放弃或改用只读方案",
        };

        return category switch
        {
            ToolErrorCategory.Retryable => $"[tool_error] {toolName} (retryable){codeSegment}: {reason}——服务端繁忙/网络异常，可稍后重试同一调用",
            ToolErrorCategory.Validation => $"[tool_error] {toolName} (invalid_args){codeSegment}: {reason}——{InvalidArgsNextStep}",
            ToolErrorCategory.Authorization => $"[tool_error] {toolName} ({authLabel}){codeSegment}: {reason}——{authNextStep}",
            // tool_budget_exceeded 是策略拒绝（非授权、非参数）——模型应基于已有信息直接回答。
            // 本分支必须排在通用 Policy 分支之前（switch 自上而下匹配，when 是更具体的子形态）。
            ToolErrorCategory.Policy when subtype == ToolErrorSubtype.ToolBudgetExceeded
                => $"[tool_error] {toolName} (tool_budget_exceeded){codeSegment}: {reason}",
            ToolErrorCategory.Policy => $"[tool_error] {toolName} (policy_denied){codeSegment}: {reason}——{PolicyNextStep}",
            ToolErrorCategory.Confirmation => $"[tool_error] {toolName} (needs_confirmation){codeSegment}: {reason}——该操作需要用户确认后方可执行；"
                + "已交由宿主确认通道处理，未获得确认前不得重试同一调用，请告知用户确认进度",
            _ => $"[tool_error] {toolName} (api_error){codeSegment}: {reason}——{ApiErrorNextStep}",
        };
    }

    /// <summary>
    /// <c>invalid_args</c> 的<b>可执行下一步</b>。
    /// </summary>
    /// <remarks>
    /// 闭集（合法取值清单）与 anyOf（"至少提供一个"约束）已由 Schema 侧提供机器可读来源，
    /// 本后缀只补两者都覆盖不到的剩余情形（形状错、类型错、必填缺失），并指向上述来源——
    /// 刻意不在这里重复罗列取值（避免第二份真相源）。
    /// </remarks>
    private const string InvalidArgsNextStep =
        "请修正参数后重试：取值范围见工具参数描述中的 enum 闭集（非法取值时错误会直接列出全部合法值）；"
        + "多个参数'至少提供一个'的约束见 anyOf；仍是形状/类型错误请对照参数描述的类型重填";

    /// <summary><b>策略拒绝的可执行下一步</b>（<c>policy_denied</c>：风险超限 / 身份不符 / glob·域级策略 / 配置非法）。</summary>
    /// <remarks>
    /// 策略拒绝是<b>确定性</b>的（同一调用在同一配置下永远被拒），默认分支的 <c>api_error</c>
    /// 文案会引导"核实后重试"——那会让模型在确定性失败上白白消耗一轮。此处明确"放弃或改方案"。
    /// </remarks>
    private const string PolicyNextStep =
        "宿主策略禁止该调用（重试不会改变结果）：请放弃该工具，或改用其他（通常只读的）方案；"
        + "若确需该能力，请宿主调整 Agent 的策略配置";

    /// <summary><c>api_error</c> 的<b>可执行下一步</b>：先核实只读事实 → 再重试 → 仍失败则带上 code 上报。</summary>
    private const string ApiErrorNextStep =
        "该调用已被下游拒绝。建议顺序：① 先用同域只读工具核实目标是否存在/参数是否正确；"
        + "② 确认无误后重试同一调用；③ 仍失败请把上面的 code 一并提供给用户（code 是排查所需的唯一标识）";

    /// <summary>
    /// 将 <see cref="ToolErrorCategory"/> 映射为 JSON 载荷中的 <c>category</c> 字面量。
    /// </summary>
    public static string CategoryLiteral(ToolErrorCategory category) => category switch
    {
        ToolErrorCategory.Validation => "validation",
        ToolErrorCategory.Policy => "policy",
        ToolErrorCategory.Authorization => "authorization",
        ToolErrorCategory.Confirmation => "confirmation",
        ToolErrorCategory.Retryable => "retryable",
        ToolErrorCategory.Api => "api",
        ToolErrorCategory.Internal => "internal",
        ToolErrorCategory.ContentSafety => "content_safety",
        _ => "internal",
    };
}
