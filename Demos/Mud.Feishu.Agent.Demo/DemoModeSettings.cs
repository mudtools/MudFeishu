// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 模型客户端三参数（裸模型 / IM 事件接入 / 工具冒烟三个模式共用的模型配置面）。
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

    /// <summary>从给定配置节读模型三参数（键：<c>{section}:ModelId / ApiKey / Endpoint</c>）。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <param name="sectionName">配置节名（如 <c>FeishuChatDemo</c> / <c>FeishuToolsDemo</c> / <c>FeishuImHandlerDemo</c>）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">缺少必填配置项。</exception>
    public static ChatModelSettings FromSection(IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        string? Read(string key) => NullIfBlank(configuration[$"{sectionName}:{key}"]);

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

            EnsureHttpsOrLoopback(sectionName, endpoint);
        }
    }

    private static string Require(string? value, string sectionName, string key)
        => value ?? throw new InvalidOperationException($"请先设置 {sectionName}:{key}");

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static void EnsureHttpsOrLoopback(string sectionName, Uri endpoint)
    {
        if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var host = endpoint.Host.Trim('[', ']');
        var isLoopback = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || (System.Net.IPAddress.TryParse(host, out var address)
                && System.Net.IPAddress.IsLoopback(address));

        if (!isLoopback)
        {
            throw new InvalidOperationException(
                $"{sectionName}:Endpoint 必须为 HTTPS（环回地址例外，对齐 SDK 的 EnsureHttpsEndpoint 安全默认），实际：{endpoint}");
        }
    }
}

/// <summary>
/// Phase 1/2 工具冒烟模式的配置（全域只读工具 + 飞书租户身份）。
/// </summary>
/// <remarks>
/// 只从配置节 <c>FeishuToolsDemo</c> 读取；模型三项经 <see cref="ChatModelSettings"/> 复用。
/// 飞书多应用由 <c>AppId</c>/<c>AppSecret</c> 在进程内合成（不落盘、不入日志）。
/// </remarks>
internal sealed record ToolsDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuToolsDemo";

    /// <summary>模式开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>模式开关。</summary>
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

        return new ToolsDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            Model = ChatModelSettings.FromSection(configuration, SectionName),
            AppId = Require(configuration[$"{SectionName}:AppId"], "AppId"),
            AppSecret = Require(configuration[$"{SectionName}:AppSecret"], "AppSecret"),
            StreamChatId = NullIfBlank(configuration[$"{SectionName}:StreamChatId"]),
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        Model.Validate(SectionName);

        if (string.IsNullOrWhiteSpace(AppId))
        {
            throw new InvalidOperationException($"请先设置 {SectionName}:AppId");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException($"请先设置 {SectionName}:AppSecret");
        }
    }

    private static string Require(string? value, string key)
        => value is null
            ? throw new InvalidOperationException($"请先设置 {SectionName}:{key}")
            : value;

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}

/// <summary>
/// P2D-5a「零自定义接入」IM 会话处理器演示的配置（注册面冒烟，只读模型三项）。
/// </summary>
/// <remarks>只从配置节 <c>FeishuImHandlerDemo</c> 读取。</remarks>
internal sealed record ImHandlerDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuImHandlerDemo";

    /// <summary>模式开关键。</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>模式开关。</summary>
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

        return new ImHandlerDemoSettings
        {
            Enabled = configuration.GetValue<bool?>(EnabledKey) is true,
            Model = ChatModelSettings.FromSection(configuration, SectionName),
        };
    }

    /// <summary>校验配置合法（fail-fast）。</summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate() => Model.Validate(SectionName);
}

/// <summary>
/// Phase 0 裸模型模式（默认模式，无开关）的配置节名与读取入口。
/// </summary>
/// <remarks>只从配置节 <c>FeishuChatDemo</c> 读取。</remarks>
internal static class ChatDemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuChatDemo";

    /// <summary>从配置文件读取裸模型三参数。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    public static ChatModelSettings FromConfiguration(IConfiguration configuration)
        => ChatModelSettings.FromSection(configuration, SectionName);
}