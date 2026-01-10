// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 飞书 Webhook 事件处理配置
/// </summary>
public class FeishuWebhookOptions
{
    /// <summary>
    /// 应用验证 Token，用于飞书事件订阅验证
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// 事件加密 Key，用于解密飞书推送的事件数据
    /// </summary>
    public string EncryptKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook 路由前缀
    /// </summary>
    public string RoutePrefix { get; set; } = "feishu/Webhook";

    /// <summary>
    /// 是否自动注册 Webhook 端点
    /// </summary>
    public bool AutoRegisterEndpoint { get; set; } = true;

    /// <summary>
    /// 是否启用请求日志记录
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// 是否启用事件处理异常捕获
    /// </summary>
    public bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// 事件处理超时时间（毫秒）
    /// 注意：此配置项暂未实际使用，预留用于未来超时控制功能
    /// </summary>
    public int EventHandlingTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 并行处理事件的最大并发数
    /// </summary>
    public int MaxConcurrentEvents { get; set; } = 10;

    /// <summary>
    /// 是否启用事件处理性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;

    /// <summary>
    /// 支持的 HTTP 方法
    /// </summary>
    public HashSet<string> AllowedHttpMethods { get; set; } = new(new[] { "POST" });

    /// <summary>
    /// 最大请求体大小（字节）
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 是否验证请求来源 IP
    /// </summary>
    public bool ValidateSourceIP { get; set; } = false;

    /// <summary>
    /// 允许的源 IP 地址列表
    /// </summary>
    public HashSet<string> AllowedSourceIPs { get; set; } = new();

    /// <summary>
    /// 是否在服务层再次验证请求体签名
    /// 如果 Middleware 中已验证 X-Lark-Signature 请求头，可禁用此选项以避免重复验证
    /// </summary>
    public bool EnableBodySignatureValidation { get; set; } = true;

    /// <summary>
    /// 验证配置项的有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置项无效时抛出</exception>
    public void Validate()
    {
        // 验证 Token 和 Key 时，允许占位符值用于演示环境
        // 仅在生产环境才需要严格的非空验证
        // 这里只验证格式，不验证实际值的有效性

        if (string.IsNullOrEmpty(RoutePrefix))
            throw new InvalidOperationException("RoutePrefix不能为空");

        if (EventHandlingTimeoutMs < 1000)
            throw new InvalidOperationException("EventHandlingTimeoutMs必须至少为1000毫秒");

        if (MaxConcurrentEvents < 1)
            throw new InvalidOperationException("MaxConcurrentEvents必须至少为1");

        if (MaxRequestBodySize < 1024)
            throw new InvalidOperationException("MaxRequestBodySize必须至少为1024字节");

        if (AllowedHttpMethods == null || !AllowedHttpMethods.Any())
            throw new InvalidOperationException("AllowedHttpMethods不能为空");
    }
}

/// <summary>
/// 飞书事件订阅验证请求
/// </summary>
public class EventVerificationRequest
{
    /// <summary>
    /// 事件类型：url_verification
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 挑战码
    /// </summary>
    [JsonPropertyName("challenge")]
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// 验证 Token
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// 飞书事件订阅验证响应
/// </summary>
public class EventVerificationResponse
{
    /// <summary>
    /// 挑战码，原样返回
    /// </summary>
    [JsonPropertyName("challenge")]
    public string Challenge { get; set; } = string.Empty;
}

/// <summary>
/// 飞书 Webhook 请求模型
/// </summary>
public class FeishuWebhookRequest
{
    /// <summary>
    /// 加密的事件数据
    /// </summary>
    [JsonPropertyName("encrypt")]
    public string? Encrypt { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// 随机数
    /// </summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// 签名
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 是否为验证请求
    /// </summary>
    [JsonIgnore]
    public bool IsVerificationRequest => Type == "url_verification";

    /// <summary>
    /// 请求类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 挑战码（验证请求时使用）
    /// </summary>
    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }
}