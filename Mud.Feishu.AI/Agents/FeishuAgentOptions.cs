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
/// 配置面治理：每个属性必须有真实消费点（读取处见各属性 XML 注释）；
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
    /// <remarks>非空校验见 <see cref="Validate"/>；内容仅由开发者控制（提示注入防线）。</remarks>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Agent 展示名（消费点：<c>ChatClientAgentOptions.Name</c> 与 OTel Span 的 <c>feishu.agent.name</c> 属性）。
    /// </summary>
    public string Name { get; set; } = "FeishuAgent";

    /// <summary>
    /// 会话历史裁剪窗（<b>非 system 消息</b>条数上限）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 消费点：<c>MessageCountingChatReducer(targetCount)</c> 注入 <c>InMemoryChatHistoryProvider</c>
    /// （<c>FeishuAgent</c> 构造期装配）。
    /// </para>
    /// <para>
    /// <b>实测语义（MEAI 10.9.0；与直觉中的「超出窗口的旧消息被折叠」不符）</b>：
    /// <list type="bullet">
    /// <item>每次取历史时执行，并把裁剪结果<b>写回会话状态（不可逆落库删除）</b>，不是「本次请求视图」；</item>
    /// <item><b>只统计非 system 消息</b>，且<b>只保留第一条 system 消息</b>；</item>
    /// <item><b>含 <c>FunctionCallContent</c>/<c>FunctionResultContent</c> 的消息被永久丢弃</b>——
    /// 宿主若需要保留工具调用轨迹，须改用不裁剪的 <c>ChatReducer</c> 实现（Host 自定义）。</item>
    /// </list>
    /// 不理解本语义会误判「模型忘了刚调过的工具」。
    /// </para>
    /// <para>
    /// 另：摘要器的保留窗由本值派生（<c>MaxHistoryMessages / 2</c>，见 <c>ConversationSummarizer</c>）——
    /// 调小本值会同时收紧摘要保留窗。
    /// </para>
    /// </remarks>
    public int MaxHistoryMessages { get; set; } = 50;

    /// <summary>
    /// 工具白名单——<c>MapTool</c> 的配置面等价物（消费点：
    /// <c>Mud.Feishu.AI.Tools</c> 的全域入口 <c>AddFeishuTools</c> 逐名启用注册表工具；
    /// 只读子集不单独提供同义入口，一律经本名单收窄）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 值域 = 已注册工具名（默认 10 个只读工具名）；工具默认
    /// 「收进注册表不启用」，仅本名单中的名字被 <c>MapTool</c> 启用后才会暴露给模型。
    /// 白名单中出现未注册的名字在工具注册期即失败（fail-fast，防配置漂移）。
    /// </para>
    /// </remarks>
    public string[] Tools { get; set; } = [];

    /// <summary>
    /// 工具结果回填文本的最大长度（消费点：<c>FeishuToolBinding</c> 对下游结果做
    /// 白名单投影后按此上限截断并标记 <c>truncated</c>——防大结果撑爆模型上下文）。
    /// </summary>
    public int MaxToolResultLength { get; set; } = 4000;

    /// <summary>
    /// 是否强制工具授权门禁（消费点：<c>FeishuToolBinding</c>——为 <see langword="true"/> 时
    /// 写类（<c>IsWrite</c>）工具未注册授权器即拒绝、未过授权即不调下游接口；只读工具钩子预留）。
    /// </summary>
    /// <remarks>
    /// 安全默认：与宿主强制门禁语义一致（与官方 CLI 的自愿 dry-run 相区分）。
    /// </remarks>
    public bool EnforceToolAuthorization { get; set; } = true;

    /// <summary>
    /// 写类工具白名单（写工具白名单单独键控，默认空=不启用任何写工具；
    /// 消费点：<c>Mud.Feishu.AI.Tools</c> 注册扩展——仅写类（<c>IsWrite</c>）工具可经本名单
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
    /// 才执行一次消息编辑，避免逐 token 编辑触发飞书频率限制）。
    /// </summary>
    public int MaxStreamChunkLength { get; set; } = 200;

    /// <summary>
    /// 触发会话历史摘要压缩的历史 token 上限（消费点：<see cref="Conversations.ConversationSummarizer"/>
    /// token 维度判定——超限时摘要先于条数窗口触发；0 = 不启用，仅按条数阈值判定）。
    /// </summary>
    /// <remarks>默认 8000；长消息场景条数窗口失控（50 条 ≠ 50 token 级别）的对策。</remarks>
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
    /// 工具风险上限（策略轴；消费点：<c>FeishuToolBinding</c> 在授权门禁<b>之前</b>按此判定）。
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
    /// 且过授权门禁（已足够严），本键是宿主的<b>显式收敛工具</b>而非新增默认限制。
    /// </para>
    /// </remarks>
    public string MaxToolRisk { get; set; } = "high-risk-write";

    /// <summary>
    /// 允许执行的工具身份闭集（Identity 策略轴；消费点：<c>FeishuToolBinding</c> 策略判定）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取值 = Schema 的 <c>x-feishu.identity</c>（<c>tenant</c> / <c>user</c>）。默认仅 <c>tenant</c>
    /// （默认最小权限，不匹配返回 <c>identity_mismatch</c>）。
    /// </para>
    /// <para>
    /// <b>引入首个 user 身份工具（<c>task.list_my_tasks</c>）后的语义</b>：
    /// 默认值保留 <c>["tenant"]</c>，但 <c>AddFeishuTools</c> 在<b>白名单映射完成后</b>做集中校验——
    /// 凡已启用工具的 <c>Identity</c> 不在本闭集内即抛可读异常（列出工具名与身份）。
    /// 这把"默认值误伤 user 工具"从<b>运行期静默拒绝</b>（宿主会误判为权限问题）
    /// 变成<b>装配期可读错误</b>。校验落点必须在装配层：<c>Validate()</c> 感知不到工具面
    /// （工具身份在注册表、白名单映射在装配层）。
    /// </para>
    /// </remarks>
    public string[] AllowedIdentities { get; set; } = ["tenant"];

    /// <summary>
    /// 出站内容安全模式（消费点：<c>FeishuToolBinding</c> 出站净化<b>之前</b>的扫描阶段）。
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
    /// 自动分页：单次 <c>fetch_all</c> 的条目预算上限（消费点：<c>ToolPagination.ResolveMaxItems</c>）。
    /// </summary>
    /// <remarks>
    /// 默认 200，硬上限 1000（<c>ToolPagination.HardMaxItems</c>）。模型可用 <c>max_items</c> 参数覆盖（不超过硬上限）。
    /// </remarks>
    public int MaxAutoFetchItems { get; set; } = 200;

    /// <summary>
    /// 自动分页：单次 <c>fetch_all</c> 的页数上限（消费点：<c>ToolPagination.ResolveMaxPages</c>）。
    /// </summary>
    /// <remarks>
    /// 默认 10，硬上限 50（<c>ToolPagination.HardMaxPages</c>）。防失控翻页（如循环 token 场景）。
    /// </remarks>
    public int MaxAutoFetchPages { get; set; } = 10;

    /// <summary>
    /// 工具重试策略（消费点：<c>ToolRetryPolicy</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 只读工具遇到 <c>retryable</c> 错误（429/5xx/超时）时自动重试；写工具默认零重试。
    /// </para>
    /// <para>
    /// 配置面治理：每个属性必须有真实消费点——<see cref="ToolRetryOptions.Enabled"/>/
    /// <see cref="ToolRetryOptions.MaxAttempts"/>/
    /// <see cref="ToolRetryOptions.BaseDelayMilliseconds"/>/
    /// <see cref="ToolRetryOptions.MaxDelayMilliseconds"/>/
    /// <see cref="ToolRetryOptions.Jitter"/>/
    /// <see cref="ToolRetryOptions.AllowWriteRetry"/>
    /// 均由 <c>ToolRetryPolicy</c> 在执行链消费。
    /// </para>
    /// </remarks>
    public ToolRetryOptions ToolRetry { get; set; } = new();

    /// <summary>
    /// 单轮工具调用预算上限（消费点：<c>FeishuToolCallBudget</c> + <c>ToolBudgetAIFunction</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 一轮 <c>RunAsync</c> 内工具调用的总次数上限。越限时工具装饰器短路返回
    /// <c>tool_budget_exceeded</c> 结构化错误（不抛异常，防重复计费），模型应基于已有信息直接回答。
    /// </para>
    /// <para>
    /// <b>默认 <c>10</c></b>：在成本控制与工具链完成度之间取平衡。
    /// <c>0</c> = 不限制（依赖 MAF 内建上限约 40 次，文档化风险）。
    /// </para>
    /// </remarks>
    public int MaxToolCallsPerTurn { get; set; } = 10;

    /// <summary>
    /// 全局最大并发轮数（消费点：<c>GlobalConcurrencyGate</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 进程内同时处理的最大对话轮数。超过此值的请求排队等待（<see cref="TurnQueueTimeoutSeconds"/> 控制超时）。
    /// </para>
    /// <para>
    /// <b>默认 <c>32</c></b>：单实例通常处理 4~8 个活跃会话，32 留有余量。
    /// 多实例部署仍需 Redis 闸门做集群总量限制（本闸门是进程内兜底）。
    /// </para>
    /// </remarks>
    public int MaxConcurrentTurns { get; set; } = 32;

    /// <summary>
    /// 全局闸门队列等待超时（秒；消费点：<c>GlobalConcurrencyGate</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 全局并发已满时，新请求排队等待空位的超时时间。超时抛 <c>ConversationBusyException</c>
    /// （幂等回滚 + 重投递）。<c>0</c> = 不限等待（等待至获得空位或被外部取消）。
    /// </para>
    /// <para>
    /// <b>默认 <c>30</c></b>：与既有 Redis 闸门语义一致，避免无限排队。
    /// </para>
    /// </remarks>
    public int TurnQueueTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 流式文本抑制选项（消费点：<c>ConversationalFeishuEventHandler.RunConversationAsync</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>true</c> 时缓存本轮全部文本增量，仅在本轮无工具调用时一次性 Flush；
    /// 出现工具调用则该轮文本并入终态（工具结果后的最终文本才是用户可见答案）。
    /// </para>
    /// <para>
    /// <b>默认 <c>false</c></b>（与既有流式行为逐字节一致）：既有流式行为逐字节推送。
    /// 权衡：首字延迟 ↑ / 观感一致性 ↑。
    /// </para>
    /// </remarks>
    public bool DeferTextUntilTurnComplete { get; set; } = false;

    /// <summary>
    /// 工具策略（glob / 域级；消费点：<c>FeishuToolBinding</c> 策略轴，早于授权门禁）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 与 <see cref="Tools"/> / <see cref="WriteAllowList"/> <b>叠加收敛</b>（任一拒绝即拒）：
    /// 白名单决定"哪些工具被启用"（装配期），本策略决定"已启用的工具里哪些允许执行"（运行期）。
    /// </para>
    /// <para>
    /// <b>默认（四集合全空）不生效</b>——逐字节等价既有行为；详见 <see cref="FeishuToolPolicyOptions"/>。
    /// </para>
    /// </remarks>
    public FeishuToolPolicyOptions ToolPolicy { get; set; } = new();

    /// <summary>
    /// 域 guidance 下发形态（消费点：
    /// <c>FeishuToolAIFunction.GetGuidance</c>——进程内 Agent 与 MCP <c>initialize.instructions</c> 共用同一份）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取值：<c>full</c>（每域 L1 全文拼接）/ <c>routing</c>（<b>默认</b>：每域一行路由摘要 ≤256 字符，
    /// 全文经 <c>feishu.guidance_read</c> 或 MCP <c>resources/read feishu-guidance://{domain}</c> 按需读取）。
    /// </para>
    /// <para>
    /// <b>为什么默认 routing</b>：L1 全文总量随域数线性增长（实测 23 域 ≈25,209 / 预算 32,768），
    /// 域扩容后必然触顶，而超限策略是<b>整域丢弃</b>（尾部域静默失导）——routing 在任何域数下都零丢弃。
    /// </para>
    /// </remarks>
    public string GuidanceMode { get; set; } = GuidanceModes.Routing;

    /// <summary>
    /// 用户令牌就绪探测模式（消费点：<c>UserTokenPrecheck.ProbeAsync</c>）。</summary>
    /// <remarks>
    /// <para>
    /// 取值：<c>off</c>（跳过探测）/ <c>cache_only</c>（<b>默认</b>：只调
    /// <c>HasValidTokenAsync</c>，内存镜像命中即零 IO）/ <c>full</c>（先 HasValid 再 CanRefresh）。
    /// </para>
    /// <para>
    /// <b>默认 <c>cache_only</c></b>：在零 IO 与前置诊断之间取平衡——内存镜像未命中时放行，
    /// 由下游 401 + <c>TokenRecoveryExecutor</c> 兜底。
    /// </para>
    /// </remarks>
    public string UserTokenPrecheck { get; set; } = UserTokenPrecheckModes.CacheOnly;

    /// <summary>
    /// 投影出<b>中立运行期配置</b>：把「与产品语义无关的运行时旋钮」交给中立机制消费。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么是投影而不是"让中立实现直接读本类"</b>：中立实现（会话摘要器 / 流式通道 /
    /// 回合管线）若读产品配置类，就形成 <c>Mud.AI.*</c> → 产品配置的隐式依赖——编译期不报错，
    /// 拆分到独立 Mud.AI 仓库时才会爆发。本方法把产品事实<b>在边界上</b>翻译成中立值类型。
    /// </para>
    /// <para>
    /// <b>本类的公开面一个字节都不变</b>：配置节名、属性名、异常文案全是宿主可见面；
    /// 宿主<b>不</b>直接配置 <see cref="AgentRuntimeOptions"/>，它只由本投影产生。
    /// </para>
    /// <para>
    /// <b>为什么不再单独写一个 <c>AgentRuntimeOptionsValidator</c></b>：
    /// 投影的<b>来源</b>已在校验之后（<see cref="Validate"/> + 注册期
    /// <see cref="IValidateOptions{T}"/> 双触发），再写第二份校验必然与第一份漂移，
    /// 而漂移无法被编译器发现（两份规则同时"看起来对"）。中立侧只保留
    /// <see cref="AgentRuntimeOptions.Validate"/> 作为独立构造时的兜底。
    /// </para>
    /// </remarks>
    /// <returns>中立运行期配置（含 9 个中立旋钮）。</returns>
    internal AgentRuntimeOptions ToRuntimeOptions() => new()
    {
        MaxHistoryMessages = MaxHistoryMessages,
        SummaryThreshold = SummaryThreshold,
        MaxHistoryTokens = MaxHistoryTokens,
        MaxStreamChunkLength = MaxStreamChunkLength,
        MaxToolCallsPerTurn = MaxToolCallsPerTurn,
        MaxConcurrentTurns = MaxConcurrentTurns,
        TurnQueueTimeoutSeconds = TurnQueueTimeoutSeconds,
        DeferTextUntilTurnComplete = DeferTextUntilTurnComplete,
        // 重试策略本身已是中立值类型，直接传递引用——刻意不复制，
        // 否则宿主对 ToolRetry 的后续改动会被投影快照切断（"配置改了但不生效"）。
        ToolRetry = ToolRetry,
    };

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

        // 数值项必须**同时**有下界与上界——只有下界时，把 1 写成 100000000 是合法配置，
        // 表现为「内存/上下文预算被静默吃光」，而 fail-fast 是配置面治理的基本盘（AGENTS.md WebSocket I16 同款纪律）。
        // 上界取值见各条异常文案（按项目未发布的现状直接收紧，不做兼容过渡）。
        if (MaxHistoryMessages is < 1 or > 10_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxHistoryMessages)} 须在 1..10000 之间，实际值: {MaxHistoryMessages.ToString(CultureInfo.InvariantCulture)}"
                + "（上界约束会话历史窗口与逐轮序列化开销）");

        if (MaxToolResultLength is < 1 or > 200_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxToolResultLength)} 须在 1..200000 之间，实际值: {MaxToolResultLength.ToString(CultureInfo.InvariantCulture)}"
                + "（上界约束单次工具结果回填模型的上下文占用）");

        if (Tools.Any(t => string.IsNullOrWhiteSpace(t)))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(Tools)} 白名单不能包含空项——工具名是模型可见契约");

        if (WriteAllowList.Any(t => string.IsNullOrWhiteSpace(t)))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(WriteAllowList)} 白名单不能包含空项——工具名是模型可见契约");

        if (MaxStreamChunkLength is < 1 or > 100_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxStreamChunkLength)} 须在 1..100000 之间，实际值: {MaxStreamChunkLength.ToString(CultureInfo.InvariantCulture)}"
                + "（上界约束流式编辑的增量缓冲）");

        // 阈值语义：0 = 禁用；启用时 ≥ 4 保证「重建后条数（保留窗+1）低于阈值」，防每轮重复摘要。
        if (SummaryThreshold is < 0 or (> 0 and < 4) or > 10_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(SummaryThreshold)} 为 0（禁用）或 4..10000，实际值: {SummaryThreshold.ToString(CultureInfo.InvariantCulture)}"
                + "（上界约束摘要触发点；1~3 无法保证「重建后条数低于阈值」，会导致每轮重复摘要）");

        // token 上限语义：0 = 不启用（仅条数阈值判定）；启用时须为正数。
        if (MaxHistoryTokens is < 0 or > 1_000_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxHistoryTokens)} 为 0（不启用）或 1..1000000，实际值: {MaxHistoryTokens.ToString(CultureInfo.InvariantCulture)}"
                + "（上界约束 token 维度摘要触发点）");

        // 策略轴词汇校验（取值必须与 Schema 的 x-feishu.risk 词汇一致，否则静默失效）。
        if (!FeishuToolRiskNames.TryParse(MaxToolRisk, out _))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxToolRisk)} 取值非法: '{MaxToolRisk}'——合法值为 {FeishuToolRiskNames.AllowedValuesText}（与 Schema 的 x-feishu.risk 词汇一致）");

        if (AllowedIdentities.Length == 0 || AllowedIdentities.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(AllowedIdentities)} 不能为空且不能含空项——空集将拒绝全部工具身份（与 Schema 的 x-feishu.identity 词汇一致：tenant / user）");

        // 身份闭集校验：与 MaxToolRisk / ContentSafetyMode 同款治理——
        // 拼错的字面量不会命中任何工具身份，表现为「所有工具被静默拒绝」，必须在装配期拦截。
        if (AllowedIdentities.Any(identity => !FeishuToolIdentityNames.IsValid(identity)))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(AllowedIdentities)} 取值非法: [{string.Join(",", AllowedIdentities)}]——"
                + $"合法值为 {FeishuToolIdentityNames.AllowedValuesText}（与 Schema 的 x-feishu.identity 词汇一致）");

        // 内容安全模式（策略开关，闭集校验防拼写错误静默降级为 off 语义）。
        if (!ContentSafetyModes.IsValid(ContentSafetyMode))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(ContentSafetyMode)} 取值非法: '{ContentSafetyMode}'——合法值为 {ContentSafetyModes.AllowedValuesText}");

        // 自动分页预算校验（数值项须同时有下界与上界）。
        if (MaxAutoFetchItems is < 1 or > 1_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxAutoFetchItems)} 须在 1..1000 之间，实际值: {MaxAutoFetchItems.ToString(CultureInfo.InvariantCulture)}"
                + "（硬上限 1000，防失控翻页撑爆上下文）");

        if (MaxAutoFetchPages is < 1 or > 50)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxAutoFetchPages)} 须在 1..50 之间，实际值: {MaxAutoFetchPages.ToString(CultureInfo.InvariantCulture)}"
                + "（硬上限 50，防循环 token 失控）");

        // 重试策略校验
        if (ToolRetry.MaxAttempts is < 0 or > 10)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(ToolRetry)}.{nameof(ToolRetry.MaxAttempts)} 须在 0..10 之间，实际值: {ToolRetry.MaxAttempts.ToString(CultureInfo.InvariantCulture)}"
                + "（0 = 禁用重试；上界防指数退避风暴）");

        if (ToolRetry.BaseDelayMilliseconds is < 0 or > 60_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(ToolRetry)}.{nameof(ToolRetry.BaseDelayMilliseconds)} 须在 0..60000 之间，实际值: {ToolRetry.BaseDelayMilliseconds.ToString(CultureInfo.InvariantCulture)}");

        if (ToolRetry.MaxDelayMilliseconds is < 1 or > 300_000)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(ToolRetry)}.{nameof(ToolRetry.MaxDelayMilliseconds)} 须在 1..300000 之间，实际值: {ToolRetry.MaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture)}");

        if (ToolRetry.MaxDelayMilliseconds < ToolRetry.BaseDelayMilliseconds)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(ToolRetry)}.{nameof(ToolRetry.MaxDelayMilliseconds)} ({ToolRetry.MaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture)}) "
                + $"不得小于 {nameof(ToolRetry.BaseDelayMilliseconds)} ({ToolRetry.BaseDelayMilliseconds.ToString(CultureInfo.InvariantCulture)})");

        // 工具策略合法性（glob 语法非法即拒——"拼错一个字符 ⇒ 策略静默不生效"是策略面
        // 最危险的失败形态：宿主以为已收敛，实际零约束）。
        (ToolPolicy ?? throw new InvalidOperationException(
            $"FeishuAgent:{nameof(ToolPolicy)} 不得为 null（省略该节即「未配置」，无需显式置 null）")).Validate();

        // guidance 模式闭集校验（防拼写错误静默退回全文拼接语义）。
        if (!GuidanceModes.IsValid(GuidanceMode))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(GuidanceMode)} 取值非法: '{GuidanceMode}'——"
                + $"合法值为 {GuidanceModes.AllowedValuesText}（拼写错误会静默退回全文拼接，"
                + "在域数增长后表现为整域丢弃——故必须装配期 fail-fast）");

        if (!UserTokenPrecheckModes.IsValid(UserTokenPrecheck))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(UserTokenPrecheck)} 取值非法: '{UserTokenPrecheck}'——"
                + $"合法值为 {UserTokenPrecheckModes.AllowedValuesText}");

        // 单轮工具预算校验（0 = 不限制；>0 时须 ≤ 100 防配置打字吃光预算）。
        if (MaxToolCallsPerTurn is < 0 or > 100)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxToolCallsPerTurn)} 为 0（不限制）或 1..100，实际值: {MaxToolCallsPerTurn.ToString(CultureInfo.InvariantCulture)}"
                + "（上界防配置打字把 10 写成 1000 造成成本爆炸；MAF 内建上限约 40 是最终兜底）");

        // 全局并发背压闸门校验（数值项须同时有下界与上界）。
        if (MaxConcurrentTurns is < 1 or > 1024)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxConcurrentTurns)} 须在 1..1024 之间，实际值: {MaxConcurrentTurns.ToString(CultureInfo.InvariantCulture)}"
                + "（上界防配置打字吃光线程池；下界 1 保证至少串行处理）");

        if (TurnQueueTimeoutSeconds is < 0 or > 600)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(TurnQueueTimeoutSeconds)} 为 0（不限等待）或 1..600，实际值: {TurnQueueTimeoutSeconds.ToString(CultureInfo.InvariantCulture)}"
                + "（上界防无效请求长期占用排队位置）");
    }
}
