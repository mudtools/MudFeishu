// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// R-1 / B-2：<c>ToolError</c> 载荷的<b>唯一构造点</b>。
/// </summary>
/// <remarks>
/// <para>
/// 此前该模板在 3 处逐字复制（<c>ToolExecutor.FromStructuredError</c>、<c>FeishuToolBinding.EgressResult</c>、
/// <c>FeishuToolBinding</c> catch 路径）。三处当前输出一致（除 <c>Trace</c>），
/// 但任何契约演进（新增字段、改 <c>Attempts</c> 语义）都要改 3 处且漏改不被编译发现。
/// </para>
/// <para>
/// <b>不变量</b>：<c>Retryable</c> 由 <c>category</c> 派生（不得由调用方各写一份判断）；
/// <c>Category</c> 经 <c>ToolErrorText.CategoryLiteral</c> 单源映射。
/// </para>
/// </remarks>
public static class ToolErrorFactory
{
    /// <summary>构造结构化错误载荷。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="category">错误语义分类。</param>
    /// <param name="subtype">错误子类型（机器可读）。</param>
    /// <param name="apiCode">飞书业务 code（可空）。</param>
    /// <param name="trace">追踪号（可空）。</param>
    /// <param name="attempts">尝试次数（默认 1）。</param>
    /// <param name="suggestions">恢复候选（BUG-8；可空）。</param>
    /// <param name="requiredScopes">E-1：工具声明的所需权限（仅授权类拒绝路径传入；可空）。</param>
    /// <param name="retryAfterSeconds">建议等待秒数（见 <see cref="ToolError.RetryAfterSeconds"/> 的语义说明）。</param>
    /// <returns>错误载荷。</returns>
    /// <remarks>
    /// <para>
    /// <b>E-2（codemeta 接入的<b>唯一</b>消费点）</b>：<paramref name="apiCode"/> 命中码表时，
    /// 把"语义 + 行动"并入 <c>suggestions</c>（未收录的码<b>不产出</b>候选——保持中性 <c>upstream_error</c> 语义）。
    /// 放在本工厂而不是各调用点，是为了让"新增一条错误路径"自动获得码表增强（漏接线不可能发生）。
    /// </para>
    /// <para>
    /// <b>合并顺序</b>：码表候选在前（它是码级事实）、调用方候选在后（更具体，如闭集参数取值）；
    /// 载荷侧仍有条数上界（<c>MaxSuggestionCount</c>），故两段都可能被截。
    /// </para>
    /// </remarks>
    public static ToolError Create(
        string toolName,
        ToolErrorCategory category,
        string subtype,
        int? apiCode = null,
        string? trace = null,
        int attempts = 1,
        IReadOnlyList<string>? suggestions = null,
        IReadOnlyList<string>? requiredScopes = null,
        int? retryAfterSeconds = null)
        => new(
            Category: ToolErrorText.CategoryLiteral(category),
            Subtype: subtype,
            Retryable: category == ToolErrorCategory.Retryable,
            RetryAfterSeconds: retryAfterSeconds,
            ApiCode: apiCode,
            Attempts: attempts,
            Trace: trace,
            Tool: toolName,
            Suggestions: MergeSuggestions(ToolErrorCodeMeta.BuildSuggestions(apiCode), suggestions),
            RequiredScopes: requiredScopes);

    /// <summary>
    /// 合并码表候选与调用方候选（先码表后调用方；两者都空时为 <see langword="null"/>——
    /// 不得产出空数组，那会被模型读成"有建议但为空"）。
    /// </summary>
    private static IReadOnlyList<string>? MergeSuggestions(
        IReadOnlyList<string>? codeMetaSuggestions,
        IReadOnlyList<string>? callerSuggestions)
    {
        var hasCodeMeta = codeMetaSuggestions is { Count: > 0 };
        var hasCaller = callerSuggestions is { Count: > 0 };

        if (!hasCodeMeta)
        {
            return hasCaller ? callerSuggestions : null;
        }

        if (!hasCaller)
        {
            return codeMetaSuggestions;
        }

        var merged = new List<string>(codeMetaSuggestions!.Count + callerSuggestions!.Count);
        merged.AddRange(codeMetaSuggestions);
        merged.AddRange(callerSuggestions);
        return merged;
    }

    /// <summary>
    /// 拼装"首行结构化 JSON 载荷 + 恢复提示行 + 人类可读正文"。
    /// </summary>
    /// <param name="payload">错误载荷。</param>
    /// <param name="humanReadable">人类可读正文（<c>[tool_error] …</c> 形态）。</param>
    /// <returns>组合文本（未净化、未截断）。</returns>
    /// <remarks>
    /// <b>E-3</b>：hint 行插在载荷之后、正文之前——模型与宿主都<b>不需要解析载荷结构</b>
    /// 就能拿到"下一步做什么"（<c>internal/recovery/hint.go</c> 的本地等价物）。
    /// 该插入点唯一，故"新增错误路径自动带 hint"与"载荷 ≤512 字节首行"两个不变量同时成立。
    /// </remarks>
    public static string Compose(ToolError payload, string humanReadable)
        => ToolErrorPayloadSerializer.Serialize(payload)
            + "\n"
            + ToolErrorHintRenderer.Render(payload)
            + "\n"
            + humanReadable;
}
