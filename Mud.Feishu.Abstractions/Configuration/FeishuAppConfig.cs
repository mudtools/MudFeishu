// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用配置
/// </summary>
/// <remarks>
/// 定义单个飞书应用的配置信息，包括应用凭证、网络设置、重试策略等。
/// 支持在系统中配置多个飞书应用，通过 AppKey 进行区分和管理。
/// </remarks>
public class FeishuAppConfig
{
    /// <summary>
    /// 应用唯一标识（用于在代码中引用此应用）
    /// </summary>
    /// <remarks>
    /// 示例值: "default", "hr-app", "approval-app"
    /// 用于在代码中通过名称引用特定应用，不与飞书平台关联。
    /// </remarks>
    public
#if NET7_0_OR_GREATER
        required
#endif
  string AppKey
    { get; set; } = string.Empty;

    /// <summary>
    /// 飞书应用ID
    /// </summary>
    /// <remarks>
    /// 示例值: "cli_a1b2c3d4e5f6g7h8"
    /// 在飞书开放平台创建应用后获得，用于标识你的飞书应用。
    /// </remarks>
    public
#if NET7_0_OR_GREATER
        required
#endif
  string AppId
    { get; set; } = string.Empty;

    /// <summary>
    /// 飞书应用密钥
    /// </summary>
    /// <remarks>
    /// 示例值: "dskLLdkasdjlasdKK"
    /// 在飞书开放平台创建应用后获得，用于应用身份验证。
    /// 请妥善保管，不要在代码中硬编码或提交到版本控制系统。
    /// </remarks>
    public
#if NET7_0_OR_GREATER
        required
#endif
  string AppSecret
    { get; set; } = string.Empty;

    /// <summary>
    /// API基础地址
    /// </summary>
    /// <remarks>
    /// 默认值: "https://open.feishu.cn"
    /// 用于自定义飞书服务的访问地址，通常在生产环境中使用默认值即可。
    /// <para>
    /// 安全提示: 默认情况下仅允许飞书官方域名。如需使用自定义域名，
    /// 请将 AllowCustomBaseUrl 设置为 true（存在 SSRF 风险，仅用于特殊场景）。
    /// </para>
    /// </remarks>
    public string BaseUrl { get; set; } = "https://open.feishu.cn";

    /// <summary>
    /// 是否允许自定义基础 URL
    /// </summary>
    /// <remarks>
    /// 默认值: false
    /// <para>
    /// 当设置为 true 时，允许使用非飞书官方域名的基础 URL。
    /// 此选项仅用于特殊场景（如内网代理、测试环境），生产环境不建议启用。
    /// </para>
    /// <para>
    /// 安全警告: 启用此选项存在 SSRF（服务端请求伪造）攻击风险。
    /// 请确保自定义域名可信且可审计。
    /// </para>
    /// </remarks>
    public bool AllowCustomBaseUrl { get; set; } = false;

    /// <summary>
    /// HTTP请求超时时间（秒）
    /// </summary>
    /// <remarks>
    /// 默认值: 30秒
    /// 范围: 1-300秒
    /// 用于设置API调用的超时时间，网络环境较差时可适当增加此值。
    /// </remarks>
    public int TimeOut { get; set; } = 30;

    /// <summary>
    /// 失败重试次数
    /// </summary>
    /// <remarks>
    /// 默认值: 3次
    /// 范围: 0-10次
    /// 当API调用失败时的自动重试次数，提高请求的成功率和稳定性。
    /// </remarks>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 重试延迟时间（毫秒）
    /// </summary>
    /// <remarks>
    /// 默认值: 1000毫秒（1秒）
    /// 范围: 100-60000毫秒
    /// 重试之间的基础延迟时间，实际延迟会采用指数退避策略。
    /// </remarks>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// 令牌刷新阈值（秒）
    /// </summary>
    /// <remarks>
    /// 默认值: 300秒（5分钟）
    /// 范围: 60-3600秒
    /// 在令牌过期前提前刷新的时间间隔，避免因网络延迟等原因导致令牌失效。
    /// </remarks>
    public int TokenRefreshThreshold { get; set; } = 300;

    /// <summary>
    /// 是否启用日志记录
    /// </summary>
    /// <remarks>
    /// 默认值: true
    /// 控制是否记录飞书API调用的详细日志信息。
    /// 生产环境建议开启，便于问题排查和监控。
    /// </remarks>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 是否为默认应用
    /// </summary>
    /// <remarks>
    /// 默认值: false
    /// 当系统中配置了多个应用时，可以指定一个默认应用。
    /// 在未明确指定应用的情况下，将使用默认应用的配置。
    /// <para>注意：当 AppKey 为 "default" 时，会自动设置为 IsDefault = true</para>
    /// <para>当只配置一个应用时，会自动设置为 IsDefault = true</para>
    /// </remarks>
    public bool IsDefault { get; set; } = false;

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

        if (TimeOut < 1 || TimeOut > 300)
            throw new InvalidOperationException("TimeOut 必须在 1-300 秒之间");

        if (RetryCount < 0 || RetryCount > 10)
            throw new InvalidOperationException("RetryCount 必须在 0-10 次之间");

        if (RetryDelayMs < 100 || RetryDelayMs > 60000)
            throw new InvalidOperationException("RetryDelayMs 必须在 100-60000 毫秒之间");

        if (TokenRefreshThreshold < 60 || TokenRefreshThreshold > 3600)
            throw new InvalidOperationException("TokenRefreshThreshold 必须在 60-3600 秒之间");

        if (!string.IsNullOrEmpty(BaseUrl) && !Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("BaseUrl 必须是有效的 URI 格式");

        if (!string.IsNullOrEmpty(BaseUrl))
        {
            var uri = new Uri(BaseUrl);
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("BaseUrl 必须是 HTTP 或 HTTPS 协议");
        }

        if (AppKey.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            IsDefault = true;
        }
    }

    /// <summary>
    /// 返回配置的字符串表示（敏感信息已掩码）
    /// </summary>
    /// <returns>配置字符串</returns>
    public override string ToString()
    {
        return $"FeishuAppConfig {{ AppKey: {AppKey}, AppId: {AppId}, AppSecret: {MaskSensitiveData(AppSecret)}, BaseUrl: {BaseUrl}, TimeOut: {TimeOut}s, RetryCount: {RetryCount}, RetryDelayMs: {RetryDelayMs}ms, TokenRefreshThreshold: {TokenRefreshThreshold}s, EnableLogging: {EnableLogging}, IsDefault: {IsDefault} }}";
    }

    private static string MaskSensitiveData(string? data)
    {
        if (string.IsNullOrEmpty(data) || data!.Length <= 4)
            return "****";
        return $"{data.Substring(0, 2)}****{data.Substring(data.Length - 2)}";
    }
}
