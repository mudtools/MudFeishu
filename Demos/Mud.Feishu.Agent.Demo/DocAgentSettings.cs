// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 文档业务智能体（<c>FEISHU_DEMO_DOC_AGENT=1</c>）的配置解析、fail-fast 校验与工具白名单常量。
/// </summary>
/// <remarks>
/// <para>
/// <b>配置来源与优先级</b>（高 → 低）：
/// <list type="number">
///   <item><description>环境变量 <c>FEISHU_*</c>（见本类常量；<b>始终最高优先</b>，容器/CI 注入无需改文件）；</description></item>
///   <item><description><c>appsettings.local.json</c>（本地覆盖，已被 <c>.gitignore</c> 忽略）；</description></item>
///   <item><description><c>appsettings.{DOTNET_ENVIRONMENT}.json</c>（可选，与仓库其他 Demo 同约定）；</description></item>
///   <item><description><c>appsettings.json</c>（节 <see cref="SectionName"/>；随产物复制）；</description></item>
///   <item><description>本类里同名的代码默认值。</description></item>
/// </list>
/// 两种命名指向同一配置项：环境变量名 <c>FEISHU_DEMO_POLICY</c> ≡ 配置文件键 <c>FeishuDocAgent:Policy</c>。
/// </para>
/// <para>
/// <b>密钥纪律</b>：<see cref="ApiKey"/> 与 <see cref="AppSecret"/> 可来自环境变量或
/// <c>appsettings.local.json</c>（<b>该文件已被 <c>.gitignore</c> 忽略</b>，模板文件里只留空串），
/// 不写日志、不进审计载荷；终端横幅与 <c>/help</c> 一律掩码。
/// </para>
/// <para>
/// <b>白名单是唯一定义处</b>：<see cref="ReadonlyTools"/> 与  <see cref="WriteTools"/> 同时被
/// <c>DocAgentDemo</c>（装配）与契约守卫（<c>DocAgentDemoContractGuards</c>）消费——
/// 工具名是模型可见契约，改名后 Demo 不得只在启动期才炸。
/// </para>
/// <para>
/// <b>可测性</b>：环境读取经 <c>reader</c> 形参注入、文件读取经 <see cref="IConfiguration"/> 注入，
/// 单测无需触碰进程环境变量（进程级环境变量在并行用例下不可复现）。
/// </para>
/// </remarks>
internal sealed record DocAgentSettings
{
    // ────────── 环境变量名（单一真相：装配、/help、README、单测共用） ──────────

    /// <summary>模式开关（置 <c>1</c> 启用文档业务智能体）。</summary>
    public const string EnvDocAgent = "FEISHU_DEMO_DOC_AGENT";

    /// <summary>模型 ID（必填）。</summary>
    public const string EnvModelKey = "FEISHU_AI_MODEL_KEY";

    /// <summary>模型 API Key（必填；只走环境变量）。</summary>
    public const string EnvApiKey = "FEISHU_AI_API_KEY";

    /// <summary>OpenAI-compatible 端点（可选；须 HTTPS 或环回）。</summary>
    public const string EnvEndpoint = "FEISHU_AI_ENDPOINT";

    /// <summary>飞书应用 AppId（必填）。</summary>
    public const string EnvAppId = "FEISHU_APP_ID";

    /// <summary>飞书应用 AppSecret（必填；只走环境变量）。</summary>
    public const string EnvAppSecret = "FEISHU_APP_SECRET";

    /// <summary>应用键（可选；默认 <see cref="DefaultAppKey"/>）。</summary>
    public const string EnvAppKey = "FEISHU_DEMO_APP_KEY";

    /// <summary>会话键 subject 段（可选；<b>不是</b>真实 open_id）。</summary>
    public const string EnvUserId = "FEISHU_DEMO_USER_ID";

    /// <summary>剧本 S1 的知识库空间 ID（可选）。</summary>
    public const string EnvWikiSpaceId = "FEISHU_DEMO_WIKI_SPACE_ID";

    /// <summary>授权策略（可选；<c>strict</c> / <c>ask</c> / <c>readonly</c>）。</summary>
    public const string EnvPolicy = "FEISHU_DEMO_POLICY";

    /// <summary>摘要触发条数阈值（可选；调低可加速演示摘要）。</summary>
    public const string EnvSummaryThreshold = "FEISHU_DEMO_SUMMARY_THRESHOLD";

    /// <summary><c>/export</c> 默认落盘路径（可选）。</summary>
    public const string EnvAuditPath = "FEISHU_DEMO_AUDIT_PATH";

    /// <summary>附件大小上限（MB；可选）。</summary>
    public const string EnvAttachmentMaxMb = "FEISHU_DEMO_ATTACHMENT_MAX_MB";

    // ────────── 配置文件键（appsettings*.json） ──────────

    /// <summary>配置文件中的节名（<c>appsettings*.json</c> 下的 <c>FeishuDocAgent</c>）。</summary>
    public const string SectionName = "FeishuDocAgent";

    /// <summary>基础配置文件（随仓库提交；密钥留空，仅作模板）。</summary>
    public const string AppSettingsFile = "appsettings.json";

    /// <summary>本地覆盖配置文件（已被 <c>.gitignore</c> 忽略；真实密钥写这里）。</summary>
    public const string LocalAppSettingsFile = "appsettings.local.json";

    /// <summary>SDK 的飞书多应用节名（<c>AddFeishuApp</c> 读它；本 Demo 也支持由配置文件提供）。</summary>
    public const string AppSectionName = "FeishuApps";

    /// <summary>配置文件里的模式开关键（<c>FeishuDocAgent:Enabled</c>；等价 <see cref="EnvDocAgent"/>=1）。</summary>
    public const string ConfigEnabledKey = SectionName + ":Enabled";

    /// <summary>
    /// 环境变量名 → 配置文件键（<see cref="SectionName"/> 节下）的映射。
    /// </summary>
    /// <remarks>两套命名指向同一配置项，取值为环境变量优先（见 <see cref="FromConfiguration"/>）。</remarks>
    private static readonly Dictionary<string, string> EnvToKey = new(StringComparer.Ordinal)
    {
        [EnvModelKey] = "ModelId",
        [EnvApiKey] = "ApiKey",
        [EnvEndpoint] = "Endpoint",
        [EnvAppId] = "AppId",
        [EnvAppSecret] = "AppSecret",
        [EnvAppKey] = "AppKey",
        [EnvUserId] = "UserId",
        [EnvWikiSpaceId] = "WikiSpaceId",
        [EnvPolicy] = "Policy",
        [EnvSummaryThreshold] = "SummaryThreshold",
        [EnvAuditPath] = "AuditExportPath",
        [EnvAttachmentMaxMb] = "AttachmentMaxMb",
    };

    /// <summary>
    /// 可从 <see cref="AppSectionName"/> 节回退读取的三项（SDK 标准写法与本节写法等价）。
    /// </summary>
    /// <remarks>
    /// 配置文件同时提供 <c>FeishuApps</c> 与 <c>FeishuDocAgent:AppId</c> 时，前者优先——
    /// <c>AddFeishuApp</c> 实际消费的是 <c>FeishuApps</c>，若两者不一致，以真正生效的那份为准
    /// 才能保证「工具执行上下文的 appKey」与「默认应用」一致（否则会被授权器以 appKey 不匹配拒绝）。
    /// </remarks>
    private static readonly Dictionary<string, string> EnvToAppKey = new(StringComparer.Ordinal)
    {
        [EnvAppId] = "AppId",
        [EnvAppSecret] = "AppSecret",
        [EnvAppKey] = "AppKey",
    };

    /// <summary>
    /// 配置文件中受支持的键名（<see cref="SectionName"/> 节下）。
    /// </summary>
    /// <remarks>
    /// 契约守卫据此校验随仓库提交的 <c>appsettings.json</c> 模板不漂移：模板里的每个键都必须受支持，
    /// 且每个受支持的键都必须在模板里出现（避免"改了代码键名、忘了改模板"这类静默失效）。
    /// </remarks>
    public static IReadOnlyCollection<string> ConfigurationKeys { get; } = [.. EnvToKey.Values];

    // ────────── 取值常量 ──────────

    /// <summary>策略：高风险写工具一律由授权器独立拒绝（默认）。</summary>
    public const string PolicyStrict = "strict";

    /// <summary>策略：授权器全部放行（写工具仍受框架审批管线约束）。</summary>
    public const string PolicyAsk = "ask";

    /// <summary>策略：全部写工具由授权器拒绝。</summary>
    public const string PolicyReadonly = "readonly";

    /// <summary>默认应用键。</summary>
    public const string DefaultAppKey = "demo-app";

    /// <summary>默认会话键 subject 段（刻意不是真实 open_id）。</summary>
    public const string DefaultConsoleUserId = "ou_console_demo_user";

    /// <summary>键控模型客户端的服务键（与 <c>AddFeishuOpenAIChatClient</c> 一致）。</summary>
    public const string ModelServiceKey = "demo-model";

    /// <summary>默认摘要触发阈值（与 <c>FeishuAgentOptions.SummaryThreshold</c> 默认值一致）。</summary>
    public const int DefaultSummaryThreshold = 30;

    /// <summary>默认附件大小上限（MB）。</summary>
    public const int DefaultAttachmentMaxMb = 25;

    /// <summary>Agent 展示名（OTel <c>feishu.agent.name</c> 取值）。</summary>
    public const string AgentName = "FeishuDocAgentDemo";

    // ────────── 配置项 ──────────

    /// <summary>模型 ID（如 <c>glm-4-flash</c>）。</summary>
    public required string ModelId { get; init; }

    /// <summary>模型 API Key（只从环境变量读；禁止落盘/入日志）。</summary>
    public required string ApiKey { get; init; }

    /// <summary>OpenAI-compatible 端点（可空 = SDK 默认端点）。</summary>
    public string? Endpoint { get; init; }

    /// <summary>飞书应用 AppId。</summary>
    public required string AppId { get; init; }

    /// <summary>飞书应用 AppSecret（只从环境变量读；禁止落盘/入日志）。</summary>
    public required string AppSecret { get; init; }

    /// <summary>应用键（工具执行上下文的租户维度事实来源）。</summary>
    public string AppKey { get; init; } = DefaultAppKey;

    /// <summary>会话键 subject 段（单聊维度）。</summary>
    public string UserId { get; init; } = DefaultConsoleUserId;

    /// <summary>知识库空间 ID（剧本 S1 前置；为空时由模型自行列节点）。</summary>
    public string? WikiSpaceId { get; init; }

    /// <summary>初始授权策略（见 <see cref="PolicyStrict"/> 等三个常量）。</summary>
    public string Policy { get; init; } = PolicyStrict;

    /// <summary>摘要触发条数阈值（传给 <c>FeishuAgentOptions.SummaryThreshold</c>）。</summary>
    public int SummaryThreshold { get; init; } = DefaultSummaryThreshold;

    /// <summary><c>/export</c> 默认落盘路径（空 = 运行时生成 <c>%TEMP%</c> 路径）。</summary>
    public string AuditExportPath { get; init; } = string.Empty;

    /// <summary>附件大小上限（字节；传给 <c>DemoAttachmentStager</c>）。</summary>
    public long AttachmentMaxBytes { get; init; } = DefaultAttachmentMaxMb * 1024L * 1024L;

    // ────────── 工具白名单（唯一定义处；契约守卫读此处） ──────────

    /// <summary>
    /// 只读白名单（14 枚）：文档业务域的读面 + 2 枚元工具。
    /// </summary>
    /// <remarks>
    /// <b>易踩坑</b>：<c>docx.import_markdown</c> 名字里有「import」，但 <c>x-feishu.is_write = false</c> ——
    /// 它只调 <c>ContentConvertAsync</c> 做转换预览、<b>不落文档</b>，故属只读面且免审批。
    /// </remarks>
    public static readonly string[] ReadonlyTools =
    [
        FeishuToolNames.DocxGetRawContent,
        FeishuToolNames.DocxGetDocumentBlocks,
        FeishuToolNames.DocxImportMarkdown,
        FeishuToolNames.WikiGetNode,
        FeishuToolNames.WikiListNodes,
        FeishuToolNames.SearchDocWiki,
        FeishuToolNames.DriveListFolderFiles,
        FeishuToolNames.DriveGetFileMetas,
        FeishuToolNames.SheetsListSheets,
        FeishuToolNames.SheetsGetRangeValues,
        FeishuToolNames.BitableListTables,
        FeishuToolNames.BitableQueryRecords,
        FeishuToolNames.FeishuCapabilityLookup,
        FeishuToolNames.FeishuGuidanceRead,
    ];

    /// <summary>
    /// 写类白名单（13 枚）：文档业务域的全部写面（建/追加/改/删/替换/云盘/表格/知识库节点）。
    /// </summary>
    /// <remarks>
    /// 全部写工具在 <c>AddFeishuTools</c> 装配期被 <c>ApprovalRequiredAIFunction</c> 包装
    /// （框架审批管线，模型不可自批），且执行期仍会被 <c>ConsoleToolAuthorizer</c> 二次把关
    /// （<b>批准 ≠ 放行</b>）。
    /// </remarks>
    public static readonly string[] WriteTools =
    [
        FeishuToolNames.DocxCreateDocument,
        FeishuToolNames.DocxAppendBlocks,
        FeishuToolNames.DocxUpdateBlocks,
        FeishuToolNames.DocxDeleteBlocks,
        FeishuToolNames.DocxReplaceDocument,
        FeishuToolNames.WikiCreateNode,
        FeishuToolNames.WikiMoveNode,
        FeishuToolNames.DriveCreateFolder,
        FeishuToolNames.DriveMoveFile,
        FeishuToolNames.DriveUploadFile,
        FeishuToolNames.SheetsUpdateRange,
        FeishuToolNames.SheetsAppendRows,
        FeishuToolNames.BitableAddRecord,
    ];

    /// <summary>
    /// 预授权权限面（授权器第 ③ 条判定的闭集）：文档业务域白名单工具 <c>RequiredScopes</c> 的并集。
    /// </summary>
    /// <remarks>
    /// 授权器据此做<b>权限面</b>判定，而不是对工具名硬编码——工具改名/新增不影响本集合的正确性，
    /// 只有"新增了需要新权限点的工具"才需要同步本清单（契约守卫会就此报红）。
    /// </remarks>
    public static readonly string[] AllowedScopes =
    [
        "docx:document:readonly",
        "docx:document",
        "wiki:wiki:readonly",
        "wiki:wiki",
        "drive:drive:readonly",
        "drive:drive",
        "sheets:spreadsheet:readonly",
        "sheets:spreadsheet",
        "bitable:app:readonly",
        "bitable:app",
        "search:docs:readonly",
        "feishu:base",
    ];

    /// <summary>合法策略字面量（错误消息与 <c>/policy</c> 校验共用）。</summary>
    public static readonly string[] Policies = [PolicyStrict, PolicyAsk, PolicyReadonly];

    /// <summary>
    /// 从环境变量读取配置（<paramref name="reader"/> 为 <see langword="null"/> 时读进程环境变量）。
    /// </summary>
    /// <param name="reader">环境读取器（可空；单测注入替身以避免触碰进程环境）。</param>
    /// <returns>配置实例（缺必填项时抛错，消息指明变量名）。</returns>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static DocAgentSettings FromEnvironment(Func<string, string?>? reader = null)
    {
        var read = reader ?? Environment.GetEnvironmentVariable;

        return Load((name, _) => NullIfBlank(read(name)));
    }

    /// <summary>
    /// 从配置文件 + 环境变量读取配置（环境变量优先）。
    /// </summary>
    /// <param name="configuration">配置文件来源（<c>appsettings*.json</c>；见 <see cref="SectionName"/>）。</param>
    /// <param name="reader">环境读取器（可空 = 读进程环境变量；单测注入替身）。</param>
    /// <returns>配置实例（缺必填项时抛错，消息同时指明环境变量名与配置文件键）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项，或整数项非法。</exception>
    /// <remarks>
    /// 单个配置项的查找顺序：<c>环境变量</c> → <c>{SectionName}:{键}</c> → <c>{AppSectionName} 的主应用</c>。
    /// 最后一级让「SDK 标准写法（<c>FeishuApps</c> 数组）」与「本节写法（<c>AppId/AppSecret</c>）」等价：
    /// 只写其中一种即可，两种都写时以 <c>FeishuApps</c> 为准（那是 <c>AddFeishuApp</c> 真正消费的配置）。
    /// </remarks>
    public static DocAgentSettings FromConfiguration(
        IConfiguration configuration,
        Func<string, string?>? reader = null)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var read = reader ?? Environment.GetEnvironmentVariable;
        var primaryApp = ResolvePrimaryApp(configuration);

        return Load((name, appKey) =>
            NullIfBlank(read(name))
            // 租户三项以 FeishuApps 为准：它是 AddFeishuApp 真正消费的配置，
            // 若改取本节值，工具执行上下文的 appKey 会与实际默认应用不一致（被授权器以 appKey 不匹配拒绝）。
            ?? (appKey is null ? null : NullIfBlank(primaryApp?[appKey]))
            ?? (EnvToKey.TryGetValue(name, out var sectionKey)
                ? NullIfBlank(configuration[$"{SectionName}:{sectionKey}"])
                : null));
    }

    /// <summary>
    /// 判断配置是否显式启用了本模式（<c>FeishuDocAgent:Enabled=true</c>）。
    /// </summary>
    /// <param name="configuration">配置文件来源。</param>
    /// <returns>是否启用。</returns>
    /// <remarks>
    /// 这是<b>模式开关</b>（等价环境变量 <see cref="EnvDocAgent"/>=1），不是业务配置项：
    /// 环境变量一旦被设置就按它判定（含显式设为 <c>0</c> 关闭），配置文件只在环境变量缺席时生效。
    /// </remarks>
    public static bool IsEnabledByConfiguration(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration.GetValue<bool?>(ConfigEnabledKey) is true;
    }

    /// <summary>
    /// 取 <see cref="AppSectionName"/> 的"主应用"（<c>IsDefault=true</c> 优先，否则第 0 个）。
    /// </summary>
    /// <param name="configuration">配置来源。</param>
    /// <returns>主应用配置节；配置未提供该节时为 <see langword="null"/>。</returns>
    private static IConfigurationSection? ResolvePrimaryApp(IConfiguration configuration)
    {
        var apps = configuration.GetSection(AppSectionName).GetChildren().ToArray();

        return apps.FirstOrDefault(
                   static app => string.Equals(app["IsDefault"], "true", StringComparison.OrdinalIgnoreCase))
               ?? apps.FirstOrDefault();
    }

    /// <summary>
    /// 统一装载：<paramref name="read"/> 已封装"多来源优先级 + <c>FeishuApps</c> 回退"。
    /// </summary>
    /// <param name="read">读取单个配置项（入参：环境变量名、可空的 <c>FeishuApps</c> 子键名）。</param>
    /// <returns>配置实例。</returns>
    private static DocAgentSettings Load(Func<string, string?, string?> read)
    {
        return new DocAgentSettings
        {
            ModelId = Require(read, EnvModelKey, appKey: null),
            ApiKey = Require(read, EnvApiKey, appKey: null),
            Endpoint = NullIfBlank(read(EnvEndpoint, null)),
            AppId = Require(read, EnvAppId, EnvToAppKey[EnvAppId]),
            AppSecret = Require(read, EnvAppSecret, EnvToAppKey[EnvAppSecret]),
            AppKey = NullIfBlank(read(EnvAppKey, EnvToAppKey[EnvAppKey])) ?? DefaultAppKey,
            UserId = NullIfBlank(read(EnvUserId, null)) ?? DefaultConsoleUserId,
            WikiSpaceId = NullIfBlank(read(EnvWikiSpaceId, null)),
            Policy = NullIfBlank(read(EnvPolicy, null)) ?? PolicyStrict,
            SummaryThreshold = ParseInt(read, EnvSummaryThreshold, null, DefaultSummaryThreshold),
            AuditExportPath = NullIfBlank(read(EnvAuditPath, null)) ?? string.Empty,
            AttachmentMaxBytes = ParseInt(read, EnvAttachmentMaxMb, null, DefaultAttachmentMaxMb) * 1024L * 1024L,
        };
    }

    /// <summary>
    /// 校验配置合法性（fail-fast；在装配之前调用，错误消息指明具体变量）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException($"请先设置 {EnvModelKey}");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"请先设置 {EnvApiKey}");
        }

        if (string.IsNullOrWhiteSpace(AppId))
        {
            throw new InvalidOperationException($"请先设置 {EnvAppId}");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException($"请先设置 {EnvAppSecret}");
        }

        // 与 AddFeishuOpenAIChatClient 的 EnsureHttpsEndpoint 同口径，但提前给出更友好的错误
        // （SDK 侧抛错发生在 BuildServiceProvider 之前，缺上下文定位）。
        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException(
                    $"{EnvEndpoint} 不是合法的绝对 URI：'{Endpoint}'");
            }

            EnsureHttpsOrLoopback(endpoint);
        }

        if (!Policies.Contains(Policy, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{EnvPolicy} 取值非法：'{Policy}'——合法值为 {string.Join(" / ", Policies)}");
        }

        if (string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException($"{EnvUserId} 不能为空白");
        }

        // 会话键布局（{prefix}:{appKey}:conversation:user:{subjectId}）：appKey 含 ':' 会被
        // ConversationKeyBuilder 转义，键可读性下降且排障困难——此处提前拒绝。
        if (string.IsNullOrWhiteSpace(AppKey))
        {
            throw new InvalidOperationException($"{EnvAppKey} 不能为空白");
        }

        if (AppKey.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{EnvAppKey} 不能包含 ':'（会话键以 ':' 分段，含分隔符会触发转义、降低键可读性）：'{AppKey}'");
        }

        var intersection = ReadonlyTools.Intersect(WriteTools, StringComparer.Ordinal).ToArray();
        if (intersection.Length > 0)
        {
            // SDK 侧也会 fail-fast（读写白名单类别错配），但错误信息更晚（装配期）。
            throw new InvalidOperationException(
                $"只读白名单与写类白名单存在交集：{string.Join("、", intersection)}——读写分离，同一工具不得同时出现");
        }

        if (ReadonlyTools.Any(string.IsNullOrWhiteSpace) || WriteTools.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("工具白名单不能包含空项——工具名是模型可见契约");
        }

        if (SummaryThreshold is < 0 or (> 0 and < 4))
        {
            throw new InvalidOperationException(
                $"{EnvSummaryThreshold} 为 0（禁用摘要）或 ≥ 4，实际值：{SummaryThreshold.ToString(CultureInfo.InvariantCulture)}"
                + "（1~3 无法保证「摘要后条数低于阈值」，会导致每轮重复摘要）");
        }

        if (AttachmentMaxBytes <= 0)
        {
            throw new InvalidOperationException($"{EnvAttachmentMaxMb} 必须为正数");
        }
    }

    /// <summary>
    /// 掩码展示密钥（前 4 位 + 省略号；长度不足时全掩码）。
    /// </summary>
    /// <param name="secret">原始密钥（可空）。</param>
    /// <returns>可安全打印的掩码文本。</returns>
    public static string Mask(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return "（未设置）";
        }

        return secret.Length <= 4 ? "****" : string.Concat(secret.AsSpan(0, 4), "***");
    }

    private static string Require(Func<string, string?, string?> read, string name, string? appKey)
        => NullIfBlank(read(name, appKey))
           ?? throw new InvalidOperationException(
               $"请先设置 {name}（或在配置文件的 {SectionName}:{EnvToKey[name]} 中配置）");

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static int ParseInt(
        Func<string, string?, string?> read,
        string name,
        string? appKey,
        int fallback)
    {
        var raw = NullIfBlank(read(name, appKey));
        if (raw is null)
        {
            return fallback;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException(
                $"{name}（{SectionName}:{EnvToKey[name]}）必须是整数，实际值：'{raw}'");
        }

        return value;
    }

    private static void EnsureHttpsOrLoopback(Uri endpoint)
    {
        if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // IPv6 字面量经 Uri 规范化后 Host 带方括号（" [::1]"），须剥离后再判定环回。
        var host = endpoint.Host.Trim('[', ']');
        var isLoopback = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || (System.Net.IPAddress.TryParse(host, out var address)
                && System.Net.IPAddress.IsLoopback(address));

        if (!isLoopback)
        {
            throw new InvalidOperationException(
                $"{EnvEndpoint} 必须为 HTTPS（环回地址例外，对齐 SDK 的 EnsureHttpsEndpoint 安全默认），实际：{endpoint}");
        }
    }
}
