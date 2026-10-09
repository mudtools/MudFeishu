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
/// 文档业务智能体的配置解析、fail-fast 校验与工具白名单常量。
/// </summary>
/// <remarks>
/// <para>
/// <b>配置来源</b>（高 → 低，仅配置文件）：
/// <list type="number">
///   <item><description><c>appsettings.local.json</c>（本地覆盖，已被 <c>.gitignore</c> 忽略）；</description></item>
///   <item><description><c>appsettings.json</c>（节 <see cref="SectionName"/>；随产物复制）。</description></item>
/// </list>
/// 参数<b>不再从环境变量读取</b>；配置项命名即配置文件键（如 <c>FeishuDocAgent:Policy</c>）。
/// </para>
/// <para>
/// <b>密钥纪律</b>：<see cref="ApiKey"/> 与 <see cref="AppSecret"/> 只写
/// <c>appsettings.local.json</c>（<b>该文件已被 <c>.gitignore</c> 忽略</b>，模板文件里只留空串），
/// 不写日志、不进审计载荷；终端横幅与 <c>/help</c> 一律掩码。
/// </para>
/// <para>
/// <b>白名单是唯一定义处</b>：<see cref="ReadonlyTools"/> 与  <see cref="WriteTools"/> 同时被
/// <c>DocAgentDemo</c>（装配）与契约守卫（<c>DocAgentDemoContractGuards</c>）消费——
/// 工具名是模型可见契约，改名后 Demo 不得只在启动期才炸。
/// </para>
/// <para>
/// <b>可测性</b>：文件读取经 <see cref="IConfiguration"/> 注入，单测无需触碰进程环境。
/// </para>
/// </remarks>
internal sealed record DocAgentSettings
{
    // ────────── 配置文件键（节 FeishuDocAgent 下；单一真相：装配、/help、README、单测共用） ──────────

    /// <summary>配置文件中的节名（<c>appsettings*.json</c> 下的 <c>FeishuDocAgent</c>）。</summary>
    public const string SectionName = "FeishuDocAgent";

    /// <summary>基础配置文件（随仓库提交；密钥留空，仅作模板）。</summary>
    public const string AppSettingsFile = "appsettings.json";

    /// <summary>本地覆盖配置文件（已被 <c>.gitignore</c> 忽略；真实密钥写这里）。</summary>
    public const string LocalAppSettingsFile = "appsettings.local.json";

    /// <summary>SDK 的飞书多应用节名（<c>AddFeishuApp</c> 读它；本 Demo 也支持由配置文件提供）。</summary>
    public const string AppSectionName = "FeishuApps";

    /// <summary>模式开关键（<c>FeishuDocAgent:Enabled</c>）。</summary>
    public const string ConfigEnabledKey = SectionName + ":Enabled";

    /// <summary>模型 ID（必填）。</summary>
    public const string KeyModelId = "ModelId";

    /// <summary>模型 API Key（必填）。</summary>
    public const string KeyApiKey = "ApiKey";

    /// <summary>OpenAI-compatible 端点（可选；须 HTTPS 或环回）。</summary>
    public const string KeyEndpoint = "Endpoint";

    /// <summary>飞书应用 AppId（必填）。</summary>
    public const string KeyAppId = "AppId";

    /// <summary>飞书应用 AppSecret（必填）。</summary>
    public const string KeyAppSecret = "AppSecret";

    /// <summary>应用键（可选；默认 <see cref="DefaultAppKey"/>）。</summary>
    public const string KeyAppKey = "AppKey";

    /// <summary>会话键 subject 段（可选；<b>不是</b>真实 open_id）。</summary>
    public const string KeyUserId = "UserId";

    /// <summary>剧本 S1 的知识库空间 ID（可选）。</summary>
    public const string KeyWikiSpaceId = "WikiSpaceId";

    /// <summary>授权策略（可选；<c>strict</c> / <c>ask</c> / <c>readonly</c>）。</summary>
    public const string KeyPolicy = "Policy";

    /// <summary>摘要触发条数阈值（可选；调低可加速演示摘要）。</summary>
    public const string KeySummaryThreshold = "SummaryThreshold";

    /// <summary><c>/export</c> 默认落盘路径（可选）。</summary>
    public const string KeyAuditExportPath = "AuditExportPath";

    /// <summary>附件大小上限（MB；可选）。</summary>
    public const string KeyAttachmentMaxMb = "AttachmentMaxMb";

    /// <summary>
    /// 配置文件中受支持的键名（<see cref="SectionName"/> 节下，不含 <c>Enabled</c>）。
    /// </summary>
    /// <remarks>
    /// 模型与飞书凭证（<c>ModelId/ApiKey/Endpoint/AppId/AppSecret</c>）已统一移至
    /// <see cref="FeishuDemoSettings.SectionName"/> 节，不再出现在本节，故不在此列。
    /// 契约守卫据此校验随仓库提交的 <c>appsettings.json</c> 模板不漂移：模板里的每个键都必须受支持，
    /// 且每个受支持的键都必须在模板里出现（避免"改了代码键名、忘了改模板"这类静默失效）。
    /// </remarks>
    public static IReadOnlyCollection<string> ConfigurationKeys { get; } =
    [
        KeyAppKey,
        KeyUserId, KeyWikiSpaceId, KeyPolicy, KeySummaryThreshold, KeyAuditExportPath, KeyAttachmentMaxMb,
    ];

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

    /// <summary>模型 API Key（只从本地覆盖文件读；禁止落盘/入日志）。</summary>
    public required string ApiKey { get; init; }

    /// <summary>OpenAI-compatible 端点（可空 = SDK 默认端点）。</summary>
    public string? Endpoint { get; init; }

    /// <summary>飞书应用 AppId。</summary>
    public required string AppId { get; init; }

    /// <summary>飞书应用 AppSecret（只从本地覆盖文件读；禁止落盘/入日志）。</summary>
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
    /// 从配置文件读取配置（模型与飞书凭证统一取自 <see cref="FeishuDemoSettings.SectionName"/> 节，
    /// 模式专属键取自 <see cref="SectionName"/> 节）。
    /// </summary>
    /// <param name="configuration">配置文件来源（<c>appsettings*.json</c>；见 <see cref="SectionName"/>）。</param>
    /// <returns>配置实例（缺必填项时抛错，消息指明配置文件键）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项，或整数项非法。</exception>
    /// <remarks>
    /// 模型三项与 <c>AppId/AppSecret</c> 的唯一来源是 <c>FeishuDemo</c> 节（四个模式共用，只此一处）；
    /// <c>AppKey/UserId/WikiSpaceId/Policy</c> 等模式专属键取自 <c>FeishuDocAgent</c> 节。
    /// </remarks>
    public static DocAgentSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var shared = FeishuDemoSettings.FromConfiguration(configuration);

        string? Section(string key) => NullIfBlank(configuration[$"{SectionName}:{key}"]);

        return new DocAgentSettings
        {
            ModelId = shared.Model.ModelId,
            ApiKey = shared.Model.ApiKey,
            Endpoint = shared.Model.Endpoint,
            AppId = Require(shared.AppId, KeyAppId),
            AppSecret = Require(shared.AppSecret, KeyAppSecret),
            AppKey = Section(KeyAppKey) ?? DefaultAppKey,
            UserId = Section(KeyUserId) ?? DefaultConsoleUserId,
            WikiSpaceId = Section(KeyWikiSpaceId),
            Policy = Section(KeyPolicy) ?? PolicyStrict,
            SummaryThreshold = ParseInt(Section(KeySummaryThreshold), KeySummaryThreshold, DefaultSummaryThreshold),
            AuditExportPath = Section(KeyAuditExportPath) ?? string.Empty,
            AttachmentMaxBytes = ParseInt(Section(KeyAttachmentMaxMb), KeyAttachmentMaxMb, DefaultAttachmentMaxMb)
                * 1024L * 1024L,
        };
    }

    /// <summary>
    /// 判断配置是否显式启用了本模式（<c>FeishuDocAgent:Enabled=true</c>）。
    /// </summary>
    /// <param name="configuration">配置文件来源。</param>
    /// <returns>是否启用。</returns>
    public static bool IsEnabledByConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetValue<bool?>(ConfigEnabledKey) is true;
    }

    /// <summary>
    /// 校验配置合法性（fail-fast；在装配之前调用，错误消息指明具体配置键）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException($"请先设置 {SharedFullKey(KeyModelId)}");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"请先设置 {SharedFullKey(KeyApiKey)}");
        }

        if (string.IsNullOrWhiteSpace(AppId))
        {
            throw new InvalidOperationException($"请先设置 {SharedFullKey(KeyAppId)}");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException($"请先设置 {SharedFullKey(KeyAppSecret)}");
        }

        // 与 AddFeishuOpenAIChatClient 的 EnsureHttpsEndpoint 同口径，但提前给出更友好的错误
        // （SDK 侧抛错发生在 BuildServiceProvider 之前，缺上下文定位）。
        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException(
                    $"{SharedFullKey(KeyEndpoint)} 不是合法的绝对 URI：'{Endpoint}'");
            }

            EnsureHttpsOrLoopback(endpoint);
        }

        if (!Policies.Contains(Policy, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{FullKey(KeyPolicy)} 取值非法：'{Policy}'——合法值为 {string.Join(" / ", Policies)}");
        }

        if (string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException($"{FullKey(KeyUserId)} 不能为空白");
        }

        // 会话键布局（{prefix}:{appKey}:conversation:user:{subjectId}）：appKey 含 ':' 会被
        // ConversationKeyBuilder 转义，键可读性下降且排障困难——此处提前拒绝。
        if (string.IsNullOrWhiteSpace(AppKey))
        {
            throw new InvalidOperationException($"{FullKey(KeyAppKey)} 不能为空白");
        }

        if (AppKey.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{FullKey(KeyAppKey)} 不能包含 ':'（会话键以 ':' 分段，含分隔符会触发转义、降低键可读性）：'{AppKey}'");
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
                $"{FullKey(KeySummaryThreshold)} 为 0（禁用摘要）或 ≥ 4，实际值：{SummaryThreshold.ToString(CultureInfo.InvariantCulture)}"
                + "（1~3 无法保证「摘要后条数低于阈值」，会导致每轮重复摘要）");
        }

        if (AttachmentMaxBytes <= 0)
        {
            throw new InvalidOperationException($"{FullKey(KeyAttachmentMaxMb)} 必须为正数");
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

    /// <summary>配置键全名（<c>FeishuDocAgent:{key}</c>）。</summary>
    private static string FullKey(string key) => $"{SectionName}:{key}";

    /// <summary>共享配置键全名（<c>FeishuDemo:{key}</c>）。</summary>
    private static string SharedFullKey(string key) => $"{FeishuDemoSettings.SectionName}:{key}";

    private static string Require(string? value, string key)
        => value ?? throw new InvalidOperationException(
            key is KeyAppId or KeyAppSecret
                ? $"请先设置 {SharedFullKey(key)}"
                : $"请先设置 {FullKey(key)}");

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static int ParseInt(string? raw, string key, int fallback)
    {
        if (raw is null)
        {
            return fallback;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException(
                $"{FullKey(key)} 必须是整数，实际值：'{raw}'");
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
                $"{SharedFullKey(KeyEndpoint)} 必须为 HTTPS（环回地址例外，对齐 SDK 的 EnsureHttpsEndpoint 安全默认），实际：{endpoint}");
        }
    }
}