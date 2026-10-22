// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Common;

/// <summary>
/// 演示配置：模型三项 + 单应用凭证 + 多维表格目标 + 策略模式。
/// </summary>
/// <remarks>
/// <b>只读配置文件</b>（不读环境变量）；密钥只允许写在 <c>appsettings.local.json</c>。
/// 本类型只负责"能不能跑"的 fail-fast，白名单/风险/身份等行为事实全部在
/// <see cref="ToolSets"/> 与 <see cref="ActProfile"/>（代码即配置，见方案 §5.3）。
/// </remarks>
internal sealed record DemoSettings
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuCapabilityDemo";

    /// <summary>基础配置模板文件。</summary>
    public const string AppSettingsFile = "appsettings.json";

    /// <summary>本地覆盖文件（已 gitignore）。</summary>
    public const string LocalAppSettingsFile = "appsettings.local.json";

    /// <summary>SDK 多应用节名。</summary>
    public const string AppSectionName = "FeishuApps";

    /// <summary>受支持的键（<c>--verify</c> 的模板守卫据此比对）。</summary>
    public static IReadOnlyCollection<string> ConfigurationKeys { get; } =
    [
        KeyModelId, KeyApiKey, KeyEndpoint,
        KeyAppKey, KeyAppId, KeyAppSecret, KeyUserId,
        KeyBitTableAppToken, KeyBitTableTableId,
        KeyPolicy, KeyAuditExportPath, KeySkillDomain,
    ];

    public const string KeyModelId = "ModelId";
    public const string KeyApiKey = "ApiKey";
    public const string KeyEndpoint = "Endpoint";
    public const string KeyAppKey = "AppKey";
    public const string KeyAppId = "AppId";
    public const string KeyAppSecret = "AppSecret";
    public const string KeyUserId = "UserId";
    public const string KeyBitTableAppToken = "BitTableAppToken";
    public const string KeyBitTableTableId = "BitTableTableId";
    public const string KeyPolicy = "Policy";
    public const string KeyAuditExportPath = "AuditExportPath";
    public const string KeySkillDomain = "SkillDomain";

    /// <summary>策略：高风险写由授权器独立拒绝（默认）。</summary>
    public const string PolicyStrict = "strict";

    /// <summary>策略：授权器放行（写工具仍受框架审批约束）。</summary>
    public const string PolicyAsk = "ask";

    /// <summary>策略：拒绝一切写。</summary>
    public const string PolicyReadonly = "readonly";

    public required string ModelId { get; init; }
    public required string ApiKey { get; init; }
    public string? Endpoint { get; init; }
    public string AppKey { get; init; } = "cap-demo";
    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public string UserId { get; init; } = "ou_cap_demo_user";
    public string BitTableAppToken { get; init; } = string.Empty;
    public string BitTableTableId { get; init; } = string.Empty;
    public string Policy { get; init; } = PolicyStrict;
    public string AuditExportPath { get; init; } = string.Empty;
    public string SkillDomain { get; init; } = "bitable";

    /// <summary>从配置构造（缺键时留空，由 <see cref="Validate"/> 统一报错）。</summary>
    public static DemoSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string Read(string key) => configuration[$"{SectionName}:{key}"]?.Trim() ?? string.Empty;

        return new DemoSettings
        {
            ModelId = Read(KeyModelId),
            ApiKey = Read(KeyApiKey),
            Endpoint = NullIfBlank(Read(KeyEndpoint)),
            AppKey = NullIfBlank(Read(KeyAppKey)) ?? "cap-demo",
            AppId = Read(KeyAppId),
            AppSecret = Read(KeyAppSecret),
            UserId = NullIfBlank(Read(KeyUserId)) ?? "ou_cap_demo_user",
            BitTableAppToken = Read(KeyBitTableAppToken),
            BitTableTableId = Read(KeyBitTableTableId),
            Policy = NullIfBlank(Read(KeyPolicy)) ?? PolicyStrict,
            AuditExportPath = Read(KeyAuditExportPath),
            SkillDomain = NullIfBlank(Read(KeySkillDomain)) ?? "bitable",
        };
    }

    /// <summary>按幕的能力需求做 fail-fast（消息指明配置键）。</summary>
    public void Validate(bool requireModel, bool requireApp, bool requireData)
    {
        if (requireModel)
        {
            Require(ModelId, KeyModelId);
            Require(ApiKey, KeyApiKey);

            if (Endpoint is not null)
            {
                if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint))
                {
                    throw new InvalidOperationException($"{SectionName}:{KeyEndpoint} 不是合法绝对 URI：'{Endpoint}'");
                }

                if (endpoint.Scheme != Uri.UriSchemeHttps
                    && endpoint.Host is not ("localhost" or "127.0.0.1" or "::1" or "[::1]"))
                {
                    throw new InvalidOperationException(
                        $"{SectionName}:{KeyEndpoint} 必须为 HTTPS（环回例外），实际：{endpoint}");
                }
            }
        }

        if (requireApp)
        {
            Require(AppId, KeyAppId);
            Require(AppSecret, KeyAppSecret);
        }

        Require(AppKey, KeyAppKey);

        if (AppKey.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{SectionName}:{KeyAppKey} 不能包含 ':'（会话键以 ':' 分段）：'{AppKey}'");
        }

        if (!new[] { PolicyStrict, PolicyAsk, PolicyReadonly }.Contains(Policy, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{KeyPolicy} 取值非法：'{Policy}'——合法值为 strict / ask / readonly");
        }

        if (requireData)
        {
            Require(BitTableAppToken, KeyBitTableAppToken);
            Require(BitTableTableId, KeyBitTableTableId);
        }

        Require(SkillDomain, KeySkillDomain);
    }

    /// <summary>密钥掩码（前 4 位 + ***）。</summary>
    public static string Mask(string? secret)
        => string.IsNullOrEmpty(secret) ? "（未设置）" : secret.Length <= 4 ? "****" : $"{secret[..4]}***";

    private void Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"请先设置 {SectionName}:{key}（密钥只写在 {LocalAppSettingsFile}，模板留空）");
        }
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
