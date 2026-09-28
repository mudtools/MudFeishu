// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// <see cref="FeishuAgent"/> 配置项（配置节 <see cref="SectionName"/>）。
/// </summary>
/// <remarks>
/// <para>
/// 配置面治理（R4/R5）：每个属性必须有真实消费点（读取处见各属性 XML 注释）；
/// 配置 DTO 不得使用 <c>required</c>（源生成配置绑定器经 <c>new T()</c> 构造会报 CS9035），
/// 合法性统一在 <see cref="Validate"/> 校验；模型 API Key / AppSecret 走
/// <c>FeishuAppConfig</c> 既有安全面，<b>不得</b>在本类复制任何 Key 字段。
/// </para>
/// </remarks>
public sealed class FeishuAgentOptions
{
    /// <summary>配置节名称（<c>FeishuAgent</c>）。</summary>
    public const string SectionName = "FeishuAgent";

    /// <summary>
    /// 键控 <see cref="IChatClient"/> 的服务键（消费点：服务注册工厂解析键控模型客户端）。
    /// </summary>
    /// <remarks>
    /// 为空时解析未键控的 <see cref="IChatClient"/>。对应
    /// <c>AddFeishuOpenAIChatClient(serviceKey, …)</c> 注册的服务键。
    /// </remarks>
    public string? ModelServiceKey { get; set; }

    /// <summary>
    /// 系统指令（system prompt；消费点：装配进 <c>ChatClientAgentOptions.ChatOptions.Instructions</c>）。
    /// </summary>
    /// <remarks>非空校验见 <see cref="Validate"/>；内容仅由开发者控制（提示注入防线，§7.2）。</remarks>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Agent 展示名（消费点：<c>ChatClientAgentOptions.Name</c> 与 OTel Span 的 <c>feishu.agent.name</c> 属性）。
    /// </summary>
    public string Name { get; set; } = "FeishuAgent";

    /// <summary>
    /// 会话历史裁剪窗（消费点：<c>MessageCountingChatReducer(targetCount)</c> 注入
    /// <c>InMemoryChatHistoryProvider</c>，超出窗口的旧消息被折叠）。
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 50;

    /// <summary>
    /// 工具白名单——<c>MapTool</c> 的配置面等价物（消费点：
    /// <c>Mud.Feishu.AI.FeishuTools</c> 的 <c>AddFeishuReadonlyTools</c> 逐名启用注册表工具）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 值域 = 已注册工具名（Phase 1 为 §3.3.2 的 10 个只读工具名）；工具默认
    /// 「收进注册表不启用」，仅本名单中的名字被 <c>MapTool</c> 启用后才会暴露给模型。
    /// 白名单中出现未注册的名字在工具注册期即失败（fail-fast，防配置漂移）。
    /// </para>
    /// </remarks>
    public string[] Tools { get; set; } = [];

    /// <summary>
    /// 工具结果回填文本的最大长度（消费点：<c>FeishuToolBinding</c> 对下游结果做
    /// 白名单投影后按此上限截断并标记 <c>truncated</c>——防大结果撑爆模型上下文，Phase 1 §3.3.1）。
    /// </summary>
    public int MaxToolResultLength { get; set; } = 4000;

    /// <summary>
    /// 是否强制工具授权门禁（消费点：<c>FeishuToolBinding</c>——为 <see langword="true"/> 时
    /// 写类（<c>IsWrite</c>）工具未注册授权器即拒绝、未过授权即不调下游接口；只读工具钩子预留）。
    /// </summary>
    /// <remarks>
    /// 安全默认：与宿主强制门禁语义一致（总体设计 §7.2；与官方 CLI 的自愿 dry-run 相区分，已决策⑥）。
    /// </remarks>
    public bool EnforceToolAuthorization { get; set; } = true;

    /// <summary>
    /// 写类工具白名单（Phase 2 §4：写工具白名单单独键控，默认空=不启用任何写工具；
    /// 消费点：<c>Mud.Feishu.AI.FeishuTools</c> 注册扩展——仅写类（<c>IsWrite</c>）工具可经本名单
    /// <c>MapTool</c> 启用，且必须先过 <c>IToolExecutionAuthorizer</c> 强制门禁）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 与 <see cref="Tools"/>（只读白名单）互斥：<see cref="Tools"/> 中的写类工具名、
    /// 本名单中的只读工具名均在工具注册期 fail-fast（读写分离，防误启用）。
    /// </para>
    /// </remarks>
    public string[] WriteAllowList { get; set; } = [];

    /// <summary>
    /// 流式回复的分片编辑阈值（字符数；消费点：<c>EditMessageChannel</c>——增量缓冲达到该长度
    /// 才执行一次消息编辑，避免逐 token 编辑触发飞书频率限制，Phase 2 §3.1）。
    /// </summary>
    public int MaxStreamChunkLength { get; set; } = 200;

    /// <summary>
    /// 触发会话历史摘要压缩的历史 token 上限（消费点：<see cref="Conversations.ConversationSummarizer"/>
    /// token 维度判定——超限时摘要先于条数窗口触发；0 = 不启用，仅按条数阈值判定）。
    /// </summary>
    /// <remarks>默认 8000；长消息场景条数窗口失控（50 条 ≠ 50 token 级别）的对策（P2D-3a）。</remarks>
    public int MaxHistoryTokens { get; set; } = 8000;

    /// <summary>
    /// 触发会话历史摘要压缩的消息数阈值（消费点：<see cref="Conversations.ConversationSummarizer"/>；
    /// 0 = 禁用摘要，仅保留既有历史裁剪窗行为）。
    /// </summary>
    /// <remarks>
    /// 触发时保留最近 <c>MaxHistoryMessages/2</c> 条（钳制到阈值以下），更早历史压缩为一条系统要点纪要
    /// （渐进式，摘要驻留会话历史内）。
    /// </remarks>
    public int SummaryThreshold { get; set; } = 30;

    /// <summary>
    /// 工具风险上限（AT-B13 策略轴；消费点：<c>FeishuToolBinding</c> 在授权门禁<b>之前</b>按此判定）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取值 = 生成器产出的 <c>x-feishu.risk</c> 字面量闭集：<c>read</c> / <c>write</c> /
    /// <c>high-risk-write</c>（<b>刻意用字符串而非 C# 枚举</b>：Schema 的字面量是连字符风格，
    /// 枚举绑定无法消费 <c>high-risk-write</c>，字符串可保证"配置词汇 == Schema 词汇"单一来源）。
    /// 风险超额的工具在校验失败时返回 <c>risk_exceeded</c>，<b>零调用下游、不切租户</b>。
    /// </para>
    /// <para>
    /// <b>默认 <c>high-risk-write</c>（不额外收紧）</b>：现行为 = 写工具需显式 <c>WriteAllowList</c>
    /// 且过授权门禁（已足够严），本键是宿主的<b>显式收敛工具</b>而非新增默认限制（方案 §10.2 R-1）。
    /// </para>
    /// </remarks>
    public string MaxToolRisk { get; set; } = "high-risk-write";

    /// <summary>
    /// 允许执行的工具身份闭集（AT-B13 Identity 轴；消费点：<c>FeishuToolBinding</c> 策略判定）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取值 = Schema 的 <c>x-feishu.identity</c>（<c>tenant</c> / <c>user</c>）。默认仅 <c>tenant</c>
    /// （默认最小权限，不匹配返回 <c>identity_mismatch</c>）。
    /// </para>
    /// <para>
    /// <b>引入首个 user 身份工具（<c>task.list_my_tasks</c>）后的语义（R4 WP2 / T2-4，决策 D-1 ⓑ）</b>：
    /// 默认值保留 <c>["tenant"]</c>，但 <c>AddFeishuTools</c> 在<b>白名单映射完成后</b>做集中校验——
    /// 凡已启用工具的 <c>Identity</c> 不在本闭集内即抛可读异常（列出工具名与身份）。
    /// 这把"默认值误伤 user 工具"从<b>运行期静默拒绝</b>（宿主会误判为权限问题）
    /// 变成<b>装配期可读错误</b>。校验落点必须在装配层：<c>Validate()</c> 感知不到工具面
    /// （工具身份在注册表、白名单映射在装配层）。
    /// </para>
    /// </remarks>
    public string[] AllowedIdentities { get; set; } = ["tenant"];

    /// <summary>
    /// 出站内容安全模式（AT-F14；消费点：<c>FeishuToolBinding</c> 出站净化<b>之前</b>的扫描阶段）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取值：<c>off</c>（不扫描）/ <c>warn</c>（<b>默认</b>：命中即在结果前加 <c>[untrusted_content: 规则]</c>
    /// 标注，不阻断）/ <c>block</c>（命中即返回结构化拒绝，不下发结果）。
    /// </para>
    /// <para>
    /// 与"净化"的区别：净化是<b>安全基线</b>（强制、无开关），内容安全是<b>策略</b>
    /// （会改变工具语义，故允许 <c>off</c>，默认 <c>warn</c> 比官方 CLI 的默认 <c>off</c> 更安全）。
    /// </para>
    /// </remarks>
    public string ContentSafetyMode { get; set; } = ContentSafetyModes.Warn;

    /// <summary>
    /// 校验配置合法性（注册时与 <see cref="FeishuAgent"/> 构造时双触发，fail-fast）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Instructions))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(Instructions)} 不能为空——空指令将使 Agent 失去角色与边界约束");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"FeishuAgent:{nameof(Name)} 不能为空");

        if (MaxHistoryMessages < 1)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxHistoryMessages)} 须 ≥ 1，实际值: {MaxHistoryMessages.ToString(CultureInfo.InvariantCulture)}");

        if (MaxToolResultLength < 1)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxToolResultLength)} 须 ≥ 1，实际值: {MaxToolResultLength.ToString(CultureInfo.InvariantCulture)}");

        if (Tools.Any(t => string.IsNullOrWhiteSpace(t)))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(Tools)} 白名单不能包含空项——工具名是模型可见契约");

        if (WriteAllowList.Any(t => string.IsNullOrWhiteSpace(t)))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(WriteAllowList)} 白名单不能包含空项——工具名是模型可见契约");

        if (MaxStreamChunkLength < 1)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxStreamChunkLength)} 须 ≥ 1，实际值: {MaxStreamChunkLength.ToString(CultureInfo.InvariantCulture)}");

        // 阈值语义：0 = 禁用；启用时 ≥ 4 保证「重建后条数（保留窗+1）低于阈值」，防每轮重复摘要。
        if (SummaryThreshold is < 0 or (> 0 and < 4))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(SummaryThreshold)} 为 0（禁用）或 ≥ 4，实际值: {SummaryThreshold.ToString(CultureInfo.InvariantCulture)}");

        // token 上限语义：0 = 不启用（仅条数阈值判定）；启用时须为正数。
        if (MaxHistoryTokens < 0)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxHistoryTokens)} 为 0（不启用）或正数，实际值: {MaxHistoryTokens.ToString(CultureInfo.InvariantCulture)}");

        // AT-B13 策略轴（取值必须与 Schema 的 x-feishu.risk 词汇一致，否则静默失效）。
        if (!FeishuToolRiskNames.TryParse(MaxToolRisk, out _))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxToolRisk)} 取值非法: '{MaxToolRisk}'——合法值为 {FeishuToolRiskNames.AllowedValuesText}（与 Schema 的 x-feishu.risk 词汇一致）");

        if (AllowedIdentities.Length == 0 || AllowedIdentities.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(AllowedIdentities)} 不能为空且不能含空项——空集将拒绝全部工具身份（与 Schema 的 x-feishu.identity 词汇一致：tenant / user）");

        // AT-F14 内容安全模式（策略开关，闭集校验防拼写错误静默降级为 off 语义）。
        if (!ContentSafetyModes.IsValid(ContentSafetyMode))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(ContentSafetyMode)} 取值非法: '{ContentSafetyMode}'——合法值为 {ContentSafetyModes.AllowedValuesText}");
    }
}
