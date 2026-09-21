// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用配置
/// </summary>
/// <remarks>
/// 定义单个飞书应用的配置信息，包括应用凭证、网络设置、重试策略等。
/// 支持在系统中配置多个飞书应用，通过 AppKey 进行区分和管理。
/// R4：HTTP/熔断请使用嵌套 <see cref="HttpRetry"/> / <see cref="CircuitBreaker"/> / <see cref="TimeoutSeconds"/>。
/// </remarks>
public class FeishuAppConfig
{
    /// <summary>
    /// 应用唯一标识（用于在代码中引用此应用）
    /// </summary>
    // AOT-3：配置 DTO 不使用 required（ConfigurationBinder 以 new T() 构造）。
    public string AppKey { get; set; } = string.Empty;

    /// <summary>应用ID（cli_ / app_ 前缀）</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>应用密钥</summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// API基础地址
    /// </summary>
    /// <remarks>
    /// 默认飞书官方域名；自定义域名需 AllowCustomBaseUrl=true（SSRF 风险）。
    /// </remarks>
    public string BaseUrl { get; set; } = "https://open.feishu.cn";

    /// <summary>是否允许自定义基础 URL，默认 false</summary>
    public bool AllowCustomBaseUrl { get; set; } = false;

    /// <summary>HTTP 请求超时（秒）。默认 30，范围 1-300。</summary>
    /// <remarks>
    /// R5.3/X12 命名对齐：本属性为<b>秒</b>，而 <c>RedisOptions.Connection.ConnectTimeout/SyncTimeout</c>
    /// 为<b>毫秒</b>——单位混用是有意保留（不做 <c>TimeoutMs</c> 别名，避免同一配置树内秒/毫秒语义并存，
    /// 见方案 §2.11/RK15）；单位变更须作为独立 major 的破坏性迁移立项。
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>HTTP 重试嵌套配置（C2/R4）</summary>
    public HttpRetryOptions HttpRetry { get; set; } = new();

    /// <summary>HTTP 熔断嵌套配置（C2/R4）</summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// 令牌刷新阈值（秒）。默认 300，范围 60-3600。
    /// </summary>
    public int TokenRefreshThreshold { get; set; } = 300;

    /// <summary>
    /// 是否为默认应用。AppKey=default 或单应用时由扩展自动推断。
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 从配置节回填 R4 前的扁平 HTTP/熔断键（仅配置 JSON 兼容；代码 API 使用嵌套属性）。
    /// </summary>
    /// <param name="section">FeishuApps 数组中当前应用的配置节</param>
    public void ApplyLegacyFlatKeys(IConfigurationSection section)
    {
        if (section is null)
            return;

        HttpRetry ??= new HttpRetryOptions();
        CircuitBreaker ??= new CircuitBreakerOptions();

        static string? Raw(IConfigurationSection s, string key) => s[key];

        if (int.TryParse(Raw(section, "TimeOut"), out var to) && TimeoutSeconds == 30)
            TimeoutSeconds = to;
        if (int.TryParse(Raw(section, "RetryCount"), out var rc) && HttpRetry.MaxAttempts == Consts.DefaultHttpRetryCount)
            HttpRetry.MaxAttempts = rc;
        if (int.TryParse(Raw(section, "RetryDelayMs"), out var rd) && HttpRetry.DelayMs == Consts.DefaultRetryDelayMs)
            HttpRetry.DelayMs = rd;
        if (bool.TryParse(Raw(section, "CircuitBreakerEnabled"), out var cbe) && CircuitBreaker.Enabled)
            CircuitBreaker.Enabled = cbe;
        if (int.TryParse(Raw(section, "CircuitBreakerFailureThreshold"), out var cft)
            && CircuitBreaker.FailureThreshold == Consts.DefaultCircuitBreakerFailureThreshold)
            CircuitBreaker.FailureThreshold = cft;
        if (int.TryParse(Raw(section, "CircuitBreakerSamplingDurationSeconds"), out var csd)
            && CircuitBreaker.SamplingDurationSeconds == Consts.DefaultCircuitBreakerSamplingDurationSeconds)
            CircuitBreaker.SamplingDurationSeconds = csd;
        if (int.TryParse(Raw(section, "CircuitBreakerBreakDurationSeconds"), out var cbd)
            && CircuitBreaker.BreakDurationSeconds == Consts.DefaultCircuitBreakerBreakDurationSeconds)
            CircuitBreaker.BreakDurationSeconds = cbd;
        if (int.TryParse(Raw(section, "CircuitBreakerMinimumThroughput"), out var cmt)
            && CircuitBreaker.MinimumThroughput == Consts.DefaultCircuitBreakerMinimumThroughput)
            CircuitBreaker.MinimumThroughput = cmt;
        // EnableLogging 已移除；兼容读取但忽略。
    }

    /// <summary>
    /// 验证配置项的有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置项无效时抛出</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
            throw new InvalidOperationException("AppKey 不能为空");

        if (string.IsNullOrWhiteSpace(AppId))
            throw new InvalidOperationException("AppId 不能为空");

        if (!AppId.StartsWith("cli_") && !AppId.StartsWith("app_"))
            throw new InvalidOperationException("AppId 格式无效，应以 'cli_' 或 'app_' 开头");

        if (AppId.Length < 20)
            throw new InvalidOperationException("AppId 长度无效");

        if (string.IsNullOrWhiteSpace(AppSecret))
            throw new InvalidOperationException("AppSecret 不能为空");

        if (AppSecret.Length < 16)
            throw new InvalidOperationException("AppSecret 长度必须至少为 16 字符");

        HttpRetry ??= new HttpRetryOptions();
        CircuitBreaker ??= new CircuitBreakerOptions();

        if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
            throw new InvalidOperationException("TimeoutSeconds (原 TimeOut) 必须在 1-300 秒之间");

        if (HttpRetry.MaxAttempts < 0 || HttpRetry.MaxAttempts > 10)
            throw new InvalidOperationException("HttpRetry.MaxAttempts (原 RetryCount) 必须在 0-10 次之间");

        if (HttpRetry.DelayMs < 100 || HttpRetry.DelayMs > 60000)
            throw new InvalidOperationException("HttpRetry.DelayMs (原 RetryDelayMs) 必须在 100-60000 毫秒之间");

        if (CircuitBreaker.FailureThreshold < 1 || CircuitBreaker.FailureThreshold > 100)
            throw new InvalidOperationException("CircuitBreaker.FailureThreshold (原 CircuitBreakerFailureThreshold) 必须在 1-100 之间");

        if (CircuitBreaker.SamplingDurationSeconds < 10 || CircuitBreaker.SamplingDurationSeconds > 300)
            throw new InvalidOperationException("CircuitBreaker.SamplingDurationSeconds (原 CircuitBreakerSamplingDurationSeconds) 必须在 10-300 秒之间");

        if (CircuitBreaker.BreakDurationSeconds < 10 || CircuitBreaker.BreakDurationSeconds > 300)
            throw new InvalidOperationException("CircuitBreaker.BreakDurationSeconds (原 CircuitBreakerBreakDurationSeconds) 必须在 10-300 秒之间");

        if (CircuitBreaker.MinimumThroughput < 2 || CircuitBreaker.MinimumThroughput > 1000)
            throw new InvalidOperationException("CircuitBreaker.MinimumThroughput (原 CircuitBreakerMinimumThroughput) 必须在 2-1000 之间");

        if (TokenRefreshThreshold < 60 || TokenRefreshThreshold > 3600)
            throw new InvalidOperationException("TokenRefreshThreshold 必须在 60-3600 秒之间");

        if (!string.IsNullOrEmpty(BaseUrl) && !Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("BaseUrl 必须是有效的 URI 格式");

        if (!string.IsNullOrEmpty(BaseUrl))
        {
            var uri = new Uri(BaseUrl);
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("BaseUrl 仅允许 HTTPS 协议");

            if (!AllowCustomBaseUrl)
            {
                var host = uri.Host.ToLowerInvariant();
                var allowedDomains = new[] { "open.feishu.cn", "open.larksuite.com", "feishu.cn", "larksuite.com" };
                bool isAllowed = allowedDomains.Any(domain =>
                    host == domain || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    throw new InvalidOperationException(
                        $"域名 '{uri.Host}' 不在飞书官方白名单中。如需使用自定义域名，请设置 AllowCustomBaseUrl=true（注意安全风险）。");
                }
            }
        }

        // IsDefault 自动推断见 FeishuMultiAppExtensions.ValidateAndSetDefaultApp()。
    }

    /// <summary>
    /// 返回配置的字符串表示（敏感信息已掩码）
    /// </summary>
    public override string ToString()
    {
        return $"FeishuAppConfig {{ AppKey: {AppKey}, AppId: {AppId}, AppSecret: {SensitiveDataUtils.MaskSensitiveData(AppSecret)}, BaseUrl: {BaseUrl}, TimeoutSeconds: {TimeoutSeconds}s, HttpRetry.MaxAttempts: {HttpRetry?.MaxAttempts}, HttpRetry.DelayMs: {HttpRetry?.DelayMs}ms, CircuitBreaker.Enabled: {CircuitBreaker?.Enabled}, CircuitBreaker.FailureThreshold: {CircuitBreaker?.FailureThreshold}%, CircuitBreaker.SamplingDurationSeconds: {CircuitBreaker?.SamplingDurationSeconds}s, CircuitBreaker.BreakDurationSeconds: {CircuitBreaker?.BreakDurationSeconds}s, CircuitBreaker.MinimumThroughput: {CircuitBreaker?.MinimumThroughput}, TokenRefreshThreshold: {TokenRefreshThreshold}s, IsDefault: {IsDefault} }}";
    }
}
