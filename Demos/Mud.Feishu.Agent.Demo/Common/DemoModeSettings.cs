// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 配置校验的公共原语（原在 <c>ChatModelSettings</c> 与 <c>DocAgentSettings</c> 各写一份，收敛至此）：
/// 空白归 <see langword="null"/>、HTTPS/环回端点强制（与 SDK 的 <c>EnsureHttpsEndpoint</c> 同口径）。
/// </summary>
/// <remarks>
/// 错误消息**必须指明配置键**（<paramref name="keyName"/> 传完整键名，如 <c>FeishuDemo:Endpoint</c>），
/// 否则排障只能靠猜。
/// </remarks>
internal static class DemoConfigGuards
{
    /// <summary>空白（或只含空白）归 <see langword="null"/>。</summary>
    /// <param name="value">原始值。</param>
    /// <returns>归一结果。</returns>
    public static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// 强制端点为 HTTPS（环回地址例外——本地自建模型端点走明文）。
    /// </summary>
    /// <param name="keyName">配置键全名（错误消息用，如 <c>FeishuDemo:Endpoint</c>）。</param>
    /// <param name="endpoint">已解析的绝对 URI。</param>
    /// <exception cref="InvalidOperationException">非 HTTPS 且非环回。</exception>
    public static void EnsureHttpsOrLoopback(string keyName, Uri endpoint)
    {
        if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // IPv6 字面量经 Uri 规范化后 Host 带方括号（"[::1]"），须剥离后再判定环回。
        var host = endpoint.Host.Trim('[', ']');
        var isLoopback = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || (System.Net.IPAddress.TryParse(host, out var address)
                && System.Net.IPAddress.IsLoopback(address));

        if (!isLoopback)
        {
            throw new InvalidOperationException(
                $"{keyName} 必须为 HTTPS（环回地址例外，对齐 SDK 的 EnsureHttpsEndpoint 安全默认），实际：{endpoint}");
        }
    }
}

/// <summary>
/// 模型客户端三参数（裸模型 / 工具冒烟 / IM 事件接入 / 领域事件 / 文档智能体各模式共用的模型配置面）。
/// </summary>
/// <remarks>
/// 只从配置文件读取；必填项缺失时 fail-fast（消息指明配置键）。
/// </remarks>
internal sealed record ChatModelSettings
{
    /// <summary>模型 ID（如 <c>glm-4-flash</c>）。</summary>
    public required string ModelId { get; init; }

    /// <summary>模型 API Key（只从本地覆盖文件读；禁止落盘/入日志）。</summary>
    public required string ApiKey { get; init; }

    /// <summary>OpenAI-compatible 端点（可空 = SDK 默认端点；须 HTTPS 或环回）。</summary>
    public string? Endpoint { get; init; }

    /// <summary>从给定配置节读模型三参数（键：<c>{sectionName}:ModelId / ApiKey / Endpoint</c>）。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <param name="sectionName">配置节名（如 <c>FeishuDemo</c>）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static ChatModelSettings FromSection(IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        string? Read(string key) => DemoConfigGuards.NullIfBlank(configuration[$"{sectionName}:{key}"]);

        return new ChatModelSettings
        {
            ModelId = Require(Read("ModelId"), sectionName, "ModelId"),
            ApiKey = Require(Read("ApiKey"), sectionName, "ApiKey"),
            Endpoint = Read("Endpoint"),
        };
    }

    /// <summary>校验配置合法（fail-fast：缺必填项 / 非 HTTPS 端点）。</summary>
    /// <param name="sectionName">配置节名（错误消息用）。</param>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate(string sectionName)
    {
        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException($"请先设置 {sectionName}:ModelId");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"请先设置 {sectionName}:ApiKey");
        }

        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException($"{sectionName}:Endpoint 不是合法的绝对 URI：'{Endpoint}'");
            }

            DemoConfigGuards.EnsureHttpsOrLoopback($"{sectionName}:Endpoint", endpoint);
        }
    }

    private static string Require(string? value, string sectionName, string key)
        => value ?? throw new InvalidOperationException($"请先设置 {sectionName}:{key}");
}

/// <summary>
/// 统一连接配置节 <c>FeishuDemo</c>：模型三项 + 飞书自建应用凭证——
/// 四个运行模式**共用**，只在此处写一次（不再每个模式节重复）。
/// </summary>
/// <remarks>
/// <para>
/// 模型三项（<see cref="Model"/>）为全部模式必填；飞书凭证
/// <see cref="AppId"/>/<see cref="AppSecret"/> 仅工具冒烟与文档智能体需要，
/// 但统一放此处以避免跨节重复（不需要的模式不读取即可）。
/// </para>
/// <para>
/// 只从配置文件读取；必填项缺失时 fail-fast（消息指明 <c>FeishuDemo:*</c> 键）。
/// </para>
/// </remarks>
internal sealed record FeishuDemoSettings
{
    /// <summary>配置节名（各模式的统一连接配置面）。</summary>
    public const string SectionName = "FeishuDemo";

    /// <summary>模型 ID 键。</summary>
    public const string KeyModelId = "ModelId";

    /// <summary>模型 API Key 键。</summary>
    public const string KeyApiKey = "ApiKey";

    /// <summary>端点键。</summary>
    public const string KeyEndpoint = "Endpoint";

    /// <summary>飞书 AppId 键。</summary>
    public const string KeyAppId = "AppId";

    /// <summary>飞书 AppSecret 键。</summary>
    public const string KeyAppSecret = "AppSecret";

    /// <summary>
    /// 配置文件中受支持的键名（<see cref="SectionName"/> 节下）。
    /// </summary>
    /// <remarks>契约守卫据此校验随仓库提交的 <c>appsettings.json</c> 模板不漂移。</remarks>
    public static IReadOnlyCollection<string> ConfigurationKeys { get; } =
        [KeyModelId, KeyApiKey, KeyEndpoint, KeyAppId, KeyAppSecret];

    /// <summary>模型三参数。</summary>
    public required ChatModelSettings Model { get; init; }

    /// <summary>飞书应用 AppId（可空——不需要租户身份的模式不校验）。</summary>
    public string? AppId { get; init; }

    /// <summary>飞书应用 AppSecret（可空——不需要租户身份的模式不校验）。</summary>
    public string? AppSecret { get; init; }

    /// <summary>从配置文件读取统一连接配置（节 <see cref="SectionName"/>）。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填模型配置项。</exception>
    public static FeishuDemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new FeishuDemoSettings
        {
            Model = ChatModelSettings.FromSection(configuration, SectionName),
            AppId = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:{KeyAppId}"]),
            AppSecret = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:{KeyAppSecret}"]),
        };
    }

    /// <summary>校验模型三参数合法（fail-fast：缺必填项 / 非 HTTPS 端点）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate() => Model.Validate(SectionName);
}

/// <summary>
/// 工具冒烟模式的配置（全域只读工具 + 飞书租户身份）。
/// </summary>
/// <remarks>
/// 默认项开关与 <c>StreamChatId</c> 从配置节 <c>FeishuToolsDemo</c> 读取；
/// 模型三项与飞书凭证从统一节 <c>FeishuDemo</c> 读取（<see cref="FeishuDemoSettings"/>）。
/// </remarks>
internal sealed record ToolsDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuToolsDemo";

    /// <summary>默认项开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>默认项开关（<b>不决定是否进入本模式</b>——进入由启动菜单选择；它只决定菜单的默认项）。</summary>
    public required bool Enabled { get; init; }

    /// <summary>模型三参数。</summary>
    public required ChatModelSettings Model { get; init; }

    /// <summary>飞书应用 AppId。</summary>
    public required string AppId { get; init; }

    /// <summary>飞书应用 AppSecret。</summary>
    public required string AppSecret { get; init; }

    /// <summary>流式演示的目标群 chat_id（可选；空 = 关闭流式）。</summary>
    public string? StreamChatId { get; init; }

    /// <summary>从配置文件读取工具冒烟模式配置。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static ToolsDemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var shared = FeishuDemoSettings.FromConfiguration(configuration);

        return new ToolsDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            Model = shared.Model,
            AppId = Require(shared.AppId, FeishuDemoSettings.KeyAppId),
            AppSecret = Require(shared.AppSecret, FeishuDemoSettings.KeyAppSecret),
            StreamChatId = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:StreamChatId"]),
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        Model.Validate(FeishuDemoSettings.SectionName);

        if (string.IsNullOrWhiteSpace(AppId))
        {
            throw new InvalidOperationException($"请先设置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppId}");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException($"请先设置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppSecret}");
        }
    }

    private static string Require(string? value, string key)
        => value ?? throw new InvalidOperationException(
            $"请先设置 {FeishuDemoSettings.SectionName}:{key}");
}

/// <summary>
/// P2D-5a「零自定义接入」IM 会话处理器演示的配置（注册面冒烟，只读模型三项）。
/// </summary>
/// <remarks>默认项开关从配置节 <c>FeishuImHandlerDemo</c> 读取；模型三项从统一节 <c>FeishuDemo</c> 读取。</remarks>
internal sealed record ImHandlerDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuImHandlerDemo";

    /// <summary>默认项开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>默认项开关（<b>不决定是否进入本模式</b>——进入由启动菜单选择；它只决定菜单的默认项）。</summary>
    public required bool Enabled { get; init; }

    /// <summary>模型三参数。</summary>
    public required ChatModelSettings Model { get; init; }

    /// <summary>从配置文件读取 IM 事件接入模式配置。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static ImHandlerDemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var shared = FeishuDemoSettings.FromConfiguration(configuration);

        return new ImHandlerDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            Model = shared.Model,
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate() => Model.Validate(FeishuDemoSettings.SectionName);
}

/// <summary>
/// <b>R-13</b>：领域事件处理器 + 流式通道降级链的演示配置。
/// </summary>
/// <remarks>
/// <para>
/// 与工具冒烟同构：默认项开关与 <c>StreamChatId</c> 来自本节，模型三项与飞书凭证来自统一节
/// <c>FeishuDemo</c>（<see cref="FeishuDemoSettings"/>）。
/// </para>
/// <para>
/// <b>为什么必须要求飞书凭证</b>：本模式会真投递回复（审批事件发给操作人单聊、任务事件发给负责人），
/// 凭证缺失时在装配期 fail-fast 比"跑到回复那一步才失败"更早暴露配置问题。
/// </para>
/// </remarks>
internal sealed record DomainEventsDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuDomainEventsDemo";

    /// <summary>默认项开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>默认项开关（<b>不决定是否进入本模式</b>——进入由启动菜单选择；它只决定菜单的默认项）。</summary>
    public bool Enabled { get; init; }

    /// <summary>模型三参数。</summary>
    public required ChatModelSettings Model { get; init; }

    /// <summary>飞书应用 AppId。</summary>
    public required string AppId { get; init; }

    /// <summary>飞书应用 AppSecret。</summary>
    public required string AppSecret { get; init; }

    /// <summary>流式演示的目标群 chat_id（可选；留空 = 只跑三个领域处理器，跳过流式一节）。</summary>
    public string? StreamChatId { get; init; }

    /// <summary>从配置文件读取领域事件演示模式配置。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static DomainEventsDemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var shared = FeishuDemoSettings.FromConfiguration(configuration);

        return new DomainEventsDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            Model = shared.Model,
            AppId = Require(shared.AppId, FeishuDemoSettings.KeyAppId),
            AppSecret = Require(shared.AppSecret, FeishuDemoSettings.KeyAppSecret),
            StreamChatId = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:StreamChatId"]),
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        Model.Validate(FeishuDemoSettings.SectionName);

        if (string.IsNullOrWhiteSpace(AppId))
        {
            throw new InvalidOperationException($"请先设置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppId}");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException($"请先设置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppSecret}");
        }
    }

    private static string Require(string? value, string key)
        => value ?? throw new InvalidOperationException(
            $"请先设置 {FeishuDemoSettings.SectionName}:{key}");
}

/// <summary>
/// R-3 / F-8：Skills 产物消费演示的配置（<b>不需要飞书凭证与模型配置</b>——本模式不连飞书）。
/// </summary>
/// <remarks>
/// <para>
/// 本模式是 <c>skills/**</c> 产物的<b>首个真实消费方</b>：把包内 <c>SKILL.md</c> 读出来并注入域 guidance，
/// 用来闭环验证"产物格式有效、内容可消费"——此前该产物树随 NuGet 包分发，但仓内零消费者，
/// "包内产物 == 读取内容"从未被任何路径验证过。
/// </para>
/// <para>
/// 因此它只需要一个默认项开关与一个域：<c>FeishuSkillsDemo:Enabled</c> / <c>FeishuSkillsDemo:Domain</c>。
/// </para>
/// </remarks>
internal sealed record SkillsDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuSkillsDemo";

    /// <summary>默认项开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>默认演示域（<c>im</c>：工具面最大、guidance 最长，最能暴露预算问题）。</summary>
    public const string DefaultDomain = "im";

    /// <summary>默认项开关（<b>不决定是否进入本模式</b>——进入由启动菜单选择；它只决定菜单的默认项）。</summary>
    public bool Enabled { get; init; }

    /// <summary>要展开的演示域（可空 ⇒ <see cref="DefaultDomain"/>）。</summary>
    public string Domain { get; init; } = DefaultDomain;

    /// <summary>从配置文件读取 Skills 消费演示配置。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    public static SkillsDemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new SkillsDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            Domain = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:Domain"]) ?? DefaultDomain,
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">域名为空。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Domain))
        {
            throw new InvalidOperationException($"请先设置 {SectionName}:Domain（如 {DefaultDomain}）");
        }
    }
}

/// <summary>
/// S-4：复合工作流演示的配置（<b>不调用模型</b>——直接经注册表驱动两个 <c>workflow.*</c> 工具）。
/// </summary>
/// <remarks>
/// <para>
/// 凭证复用统一节 <c>FeishuDemo</c> 的 <c>AppId</c> / <c>AppSecret</c>；本模式刻意<b>不读模型三项</b>：
/// 工作流是确定性工具链，不需要模型决策，因此模型凭据缺失时本模式仍可运行。
/// </para>
/// <para>
/// <c>CalendarId</c> / <c>UserId</c> 可选：两者齐备时才真跑 <c>workflow.standup_report</c>
/// （第二步 <c>task.list_my_tasks</c> 是 user 身份，还要求宿主侧已有该用户的用户令牌）；
/// 否则只输出可执行的跳过提示，并保留 dry_run 步骤计划。
/// </para>
/// </remarks>
internal sealed record WorkflowDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuWorkflowDemo";

    /// <summary>默认项开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>默认会议搜索词（11 字符，满足 <c>minutes.search</c> 的 10~50 字符平台约束）。</summary>
    public const string DefaultMeetingQuery = "产品评审会纪要汇总整理";

    /// <summary>默认项开关（<b>不决定是否进入本模式</b>——进入由启动菜单选择；它只决定菜单的默认项）。</summary>
    public bool Enabled { get; init; }

    /// <summary>飞书应用 AppId。</summary>
    public required string AppId { get; init; }

    /// <summary>飞书应用 AppSecret。</summary>
    public required string AppSecret { get; init; }

    /// <summary>会议纪要搜索词（可空 ⇒ <see cref="DefaultMeetingQuery"/>）。</summary>
    public string MeetingQuery { get; init; } = DefaultMeetingQuery;

    /// <summary>妙记创建时间窗起点（可选，RFC3339；与 <see cref="MeetingEnd"/> 成对）。</summary>
    public string? MeetingStart { get; init; }

    /// <summary>妙记创建时间窗终点（可选，RFC3339；与 <see cref="MeetingStart"/> 成对）。</summary>
    public string? MeetingEnd { get; init; }

    /// <summary>站会日报读取的日历 ID（可选；与 <see cref="UserId"/> 齐备才真跑）。</summary>
    public string? CalendarId { get; init; }

    /// <summary>站会日报的当前用户 open_id（可选；任务读取需要用户身份与用户令牌）。</summary>
    public string? UserId { get; init; }

    /// <summary>站会日报是否包含已完成任务。</summary>
    public bool IncludeCompletedTasks { get; init; }

    /// <summary>从配置文件读取复合工作流演示配置。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static WorkflowDemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new WorkflowDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            AppId = Require(DemoConfigGuards.NullIfBlank(
                configuration[$"{FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppId}"]),
                FeishuDemoSettings.KeyAppId),
            AppSecret = Require(DemoConfigGuards.NullIfBlank(
                configuration[$"{FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppSecret}"]),
                FeishuDemoSettings.KeyAppSecret),
            MeetingQuery = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:MeetingQuery"])
                ?? DefaultMeetingQuery,
            MeetingStart = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:MeetingStart"]),
            MeetingEnd = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:MeetingEnd"]),
            CalendarId = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:CalendarId"]),
            UserId = DemoConfigGuards.NullIfBlank(configuration[$"{SectionName}:UserId"]),
            IncludeCompletedTasks = configuration.GetValue<bool?>($"{SectionName}:IncludeCompletedTasks") is true,
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppId))
        {
            throw new InvalidOperationException($"请先设置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppId}");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException($"请先设置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppSecret}");
        }

        var queryLength = MeetingQuery.Trim().Length;
        if (queryLength is < 10 or > 50)
        {
            throw new InvalidOperationException(
                $"{SectionName}:MeetingQuery 长度须在 10~50 字符之间（minutes.search 平台约束），实际 {queryLength} 字符");
        }

        if (string.IsNullOrWhiteSpace(MeetingStart) != string.IsNullOrWhiteSpace(MeetingEnd))
        {
            throw new InvalidOperationException(
                $"{SectionName}:MeetingStart 与 {SectionName}:MeetingEnd 须成对配置（或都留空）");
        }
    }

    private static string Require(string? value, string key)
        => value ?? throw new InvalidOperationException(
            $"请先设置 {FeishuDemoSettings.SectionName}:{key}");
}
