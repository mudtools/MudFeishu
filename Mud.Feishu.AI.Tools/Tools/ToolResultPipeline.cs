// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// R-1：<b>工具出站唯一出口</b>——一切回填模型的文本（成功信封 / 手拼信封 / 错误载荷）都经此完成
/// 「序列化 → <c>MaxToolResultLength</c> 截断 → <c>Truncated</c>/<c>TruncationReason</c> 标记」。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（B-1 的根因）</b>：截断此前是"某几个执行器记得写的一步"，而不是"出口的性质"。
/// 于是 8 处手拼信封产出<b>静默截断</b>（<c>Truncated</c> 恒 <c>false</c> ⇒ 模型把不完整结果当完整事实，
/// 且 OTel 的 <c>tag_truncated</c> 恒 false 使监控对截断率完全失明），另有 12 处写工具回执信封
/// <b>完全无预算意识</b>。收口到本类型后，这两个失败模式在结构上不可能再出现。
/// </para>
/// <para>
/// <b>无状态（有意）</b>：全部方法显式接收 <c>maxLength</c> 形参，<b>不</b>持有长度。
/// 原因：库内有两个出口持有者——<c>ToolExecutor</c>（<c>readonly struct</c>，per-method 实例，
/// 写工具用 <c>0</c> 表示"无预算"）与 <c>FeishuToolBinding</c>（<c>sealed class</c>，持
/// <c>FeishuAgentOptions</c>）。若本类型也持长度，两者各自 new 一个实例 ⇒ "唯一出口"退化为
/// "唯一实现"，还多出两个状态持有者。
/// </para>
/// <para>
/// <b>为什么净化不在这里</b>：净化（<c>ToolResultSanitizer</c>）属于<b>"模型可见"边界</b>而非
/// "工具结果构造"边界——它在 <c>FeishuToolBinding</c> 的第 ⑥ 步对所有执行器产出统一施加
/// （无开关、不可绕过），拒绝/异常等<b>由执行链自身生成</b>的文本则由 <c>EgressResult</c> 净化。
/// 本类型负责的是<b>预算与标记</b>（以及错误载荷的单一构造），职责边界清晰。
/// </para>
/// <para>
/// <b><c>maxLength</c> ≤ 0 的语义 = 无预算</b>（不截断、不标记）——与
/// <c>ToolExecutor(string toolName)</c> 单参构造（写工具载荷是单 ID 迷你信封）的既有语义一致。
/// ⚠️ 不能直接把 0 传给 <c>ToolResultText.Truncate</c>：那里会把 &lt;1 的值钳制为 1 并截成 1 个字符。
/// </para>
/// </remarks>
internal static class ToolResultPipeline
{
    /// <summary>纯文本截断原因（<c>ToolResultText.Truncate</c> 路径）。</summary>
    public const string PlainTextTruncationReason = "文本超长截断";

    /// <summary>JSON 感知截断原因（<c>ToolResultText.TruncateJson</c> 路径）。</summary>
    public const string JsonTruncationReason = "JSON 感知截断";

    /// <summary>
    /// 纯文本出口：按预算截断并回填截断标记。
    /// </summary>
    /// <param name="text">待回填的文本（可空——投影函数常把"本来就没有值"直传进来）。</param>
    /// <param name="maxLength">预算上限；≤ 0 表示无预算。</param>
    /// <returns>带截断标记的工具结果。</returns>
    public static FeishuToolResult Ok(string? text, int maxLength)
    {
        var full = text ?? string.Empty;
        if (!IsOverBudget(full, maxLength))
        {
            return FeishuToolResult.FromText(full);
        }

        return FeishuToolResult.FromText(
            ToolResultText.Truncate(full, maxLength), truncated: true, PlainTextTruncationReason);
    }

    /// <summary>
    /// JSON 信封出口：序列化为保留中文的紧凑 JSON → JSON 感知截断 → 回填截断标记。
    /// </summary>
    /// <remarks>
    /// 这是手拼信封的<b>唯一</b>合法形态（B-1 的 8 处一类 + 12 处二类都收敛于此）。
    /// 直接 <c>FeishuToolResult.FromText(ToolResultText.TruncateJson(ToolResultJson.ToText(x), n))</c>
    /// 由 <c>TruncationVisibilityContractGuards</c> 机械拦截。
    /// </remarks>
    /// <param name="node">信封节点（非空——入口信封必须存在，否则说明调用方搞错了路径）。</param>
    /// <param name="maxLength">预算上限；≤ 0 表示无预算。</param>
    /// <returns>带截断标记的工具结果。</returns>
    public static FeishuToolResult OkJson(JsonNode node, int maxLength)
    {
        var full = ToolResultJson.ToText(node);
        if (!IsOverBudget(full, maxLength))
        {
            return FeishuToolResult.FromText(full);
        }

        return FeishuToolResult.FromText(
            ToolResultText.TruncateJson(full, maxLength), truncated: true, JsonTruncationReason);
    }

    /// <summary>
    /// <b>写工具回执出口</b>：写工具的成功回执是"单 ID 迷你信封"（2–4 个标量字段，
    /// 由入参 Schema 与平台返回共同界定），<b>按构造有界，显式声明无预算</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 存在的理由（B-1 二类）：这些回执此前直接 <c>FeishuToolResult.FromText(ToolResultJson.ToText(...))</c>，
    /// 即"出口"由每个写执行器各写一次——<b>预算纪律在结构上无处可依</b>
    /// （同一文件里 <c>FromEnvelope</c> 截断留痕、<c>Untouched</c> 不截断，态度自相矛盾）。
    /// 收口到本方法后，"写回执无预算"是一个<b>被显式声明并被单一地点评审</b>的决策，
    /// 而不是"某处忘了截断"。
    /// </para>
    /// <para>
    /// <b>为什么不给它们预算</b>：这 6 个执行器（Board / DocxSheetsDrive / Mail /
    /// MessageAndApproval / Spark / DriveCollab）的构造签名不接 <c>IOptions&lt;FeishuAgentOptions&gt;</c>
    /// （由生成器按执行器类发射的域注册器解析）。为"由构造即有界的迷你信封"引入 options 依赖
    /// 会改动 6 个构造函数 + 其生成注册器，换不到可观测收益。若将来某个回执开始承载<b>无界的
    /// 平台数据</b>（如列表/正文），它必须改用 <see cref="OkJson"/> 而不是本方法。
    /// </para>
    /// </remarks>
    /// <param name="receipt">回执信封。</param>
    /// <returns>工具结果（无截断标记）。</returns>
    public static FeishuToolResult OkReceipt(JsonObject receipt)
        => FeishuToolResult.FromText(ToolResultJson.ToText(receipt));

    /// <summary>
    /// 错误出口（<b>唯一</b>的错误载荷构造点）：F 首行结构化 JSON + 人类可读正文，经预算截断。
    /// </summary>
    /// <remarks>
    /// B-2 的三处同构构造（<c>ToolExecutor.FromStructuredError</c>、
    /// <c>FeishuToolBinding.EgressResult(toolName, category, subtype, reason, apiCode)</c> 与
    /// <c>FeishuToolBinding</c> 的 catch 路径）全部收敛到此处——错误契约演进时只需改一处。
    /// 三处的唯一差异是 <paramref name="trace"/>（catch 路径携带 Span Id），已作为显式可选形参保留。
    /// </remarks>
    /// <param name="toolName">工具名（载荷锚点）。</param>
    /// <param name="category">错误语义分类。</param>
    /// <param name="subtype">错误子类型（机器可读）。</param>
    /// <param name="reason">原因（人类可读；由调用方决定是原始事实还是白名单化文案）。</param>
    /// <param name="maxLength">模型出口的预算上限；≤ 0 表示无预算。</param>
    /// <param name="apiCode">飞书业务 code（可空）。</param>
    /// <param name="trace">追踪号（可空；仅 catch 路径携带）。</param>
    /// <param name="attempts">尝试次数（默认 1；重试路径回填实际次数）。</param>
    /// <returns>带结构化载荷的错误结果（<b>未净化</b>——净化由调用方按各自出口纪律施加）。</returns>
    public static FeishuToolResult Error(
        string toolName,
        ToolErrorCategory category,
        string subtype,
        string reason,
        int maxLength,
        int? apiCode = null,
        string? trace = null,
        int attempts = 1)
    {
        var payload = ToolErrorFactory.Create(toolName, category, subtype, apiCode, trace, attempts);
        return FromErrorText(payload, ToolErrorFactory.Compose(payload, reason), maxLength);
    }

    /// <summary>
    /// 错误出口的<b>文本</b>形态：调用方已完成净化/文案加工，只需本出口施加预算。
    /// </summary>
    /// <remarks>
    /// 存在的理由：<c>FeishuToolBinding</c> 的错误出口必须在**同一段代码**里先净化再截断
    /// （净化是"模型可见"边界，且它会改变文本长度），故不能经 <see cref="Error"/> 的
    /// "先拼装再截断"路径。本方法让两条路径共用同一个预算实现，不留第二处截断逻辑。
    /// </remarks>
    /// <param name="payload">错误载荷（已经 <see cref="ToolErrorFactory"/> 构造）。</param>
    /// <param name="combined">完整文本（首行 JSON + 正文；调用方负责净化）。</param>
    /// <param name="maxLength">预算上限；≤ 0 表示无预算。</param>
    /// <returns>错误结果。</returns>
    public static FeishuToolResult FromErrorText(ToolError payload, string combined, int maxLength)
        => FeishuToolResult.FromError(
            payload,
            maxLength > 0 ? ToolResultText.Truncate(combined, maxLength) : combined);

    /// <summary>
    /// 单源截断判据：文本长度是否超过预算。<b>不用"截断后长度比较"</b>——两个截断器的标记/提示文本
    /// 都<b>附加</b>在结果里（JSON 感知截断还会保留末尾 <c>truncated</c>/<c>hint</c> 键），
    /// 因此"截断后更长"完全可能（如 101 字符被截到 100 再附约 80 字符标记），
    /// 用长度比较会漏判——本判据只看"截断动作是否会被触发"，与两个截断器的进入条件逐字一致。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="maxLength">预算上限。</param>
    private static bool IsOverBudget(string text, int maxLength)
        => maxLength > 0 && text.Length > maxLength;
}

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
/// <c>Category</c> 经 <c>FeishuToolBinding.CategoryLiteral</c> 单源映射。
/// </para>
/// </remarks>
internal static class ToolErrorFactory
{
    /// <summary>构造结构化错误载荷。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="category">错误语义分类。</param>
    /// <param name="subtype">错误子类型（机器可读）。</param>
    /// <param name="apiCode">飞书业务 code（可空）。</param>
    /// <param name="trace">追踪号（可空）。</param>
    /// <param name="attempts">尝试次数（默认 1）。</param>
    /// <returns>错误载荷。</returns>
    public static ToolError Create(
        string toolName,
        ToolErrorCategory category,
        string subtype,
        int? apiCode = null,
        string? trace = null,
        int attempts = 1)
        => new(
            Category: FeishuToolBinding.CategoryLiteral(category),
            Subtype: subtype,
            Retryable: category == ToolErrorCategory.Retryable,
            RetryAfterSeconds: null,
            ApiCode: apiCode,
            Attempts: attempts,
            Trace: trace,
            Tool: toolName);

    /// <summary>拼装"首行结构化 JSON 载荷 + 人类可读正文"。</summary>
    /// <param name="payload">错误载荷。</param>
    /// <param name="humanReadable">人类可读正文（<c>[tool_error] …</c> 形态）。</param>
    /// <returns>组合文本（未净化、未截断）。</returns>
    public static string Compose(ToolError payload, string humanReadable)
        => ToolErrorPayloadSerializer.Serialize(payload) + "\n" + humanReadable;
}
