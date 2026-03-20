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
    /// 全局路由前缀（所有应用共享的基础路径）
    /// </summary>
    public string GlobalRoutePrefix { get; set; } = "feishu";

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
    /// 超过此时间仍未完成的请求将被取消并返回超时错误
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
    public HashSet<string> AllowedHttpMethods { get; set; } = ["POST"];

    /// <summary>
    /// 最大请求体大小（字节）
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 允许的源 IP 地址列表（白名单）
    /// 当此列表非空时，将自动启用 IP 验证
    /// 支持 CIDR 格式（如 192.168.1.0/24）和单个 IP 地址
    /// </summary>
    public HashSet<string> AllowedSourceIPs { get; set; } = [];

    /// <summary>
    /// 是否在服务层再次验证请求体签名
    /// 如果 Middleware 中已验证 X-Lark-Signature 请求头，可禁用此选项以避免重复验证
    /// </summary>
    public bool EnableBodySignatureValidation { get; set; } = true;

    /// <summary>
    /// 是否强制验证 X-Lark-Signature 请求头签名
    /// 当设置为 true 时，如果请求头中缺少签名将拒绝请求
    /// 生产环境建议设置为 true 以提高安全性
    /// </summary>
    /// <remarks>
    /// 安全警告：
    /// - 生产环境必须设置为 true，否则存在严重的安全漏洞
    /// - 仅在开发/测试环境且明确了解风险时设置为 false
    /// - 系统会在生产环境自动检测并拒绝禁用签名的配置
    /// </remarks>
    public bool EnforceHeaderSignatureValidation { get; set; } = true;

    /// <summary>
    /// 时间戳验证容错范围（秒）
    /// 用于验证请求时间戳是否在有效范围内，默认为 30 秒
    /// </summary>
    /// <remarks>
    /// 安全建议：
    /// - 生产环境建议设置为 30 秒或更短，以减少重放攻击时间窗口
    /// - 开发环境可以适当放宽到 300 秒
    /// - 飞书官方建议的时间戳容错范围为 60 秒以内
    /// </remarks>
    public int TimestampToleranceSeconds { get; set; } = 30;

    /// <summary>
    /// 请求频率限制配置
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// 是否启用异步后台处理模式
    /// 启用后会立即返回飞书成功响应，然后在后台处理事件
    /// 这可以避免飞书超时重试，适用于耗时较长的业务逻辑
    /// </summary>
    public bool EnableBackgroundProcessing { get; set; } = false;

    /// <summary>
    /// 失败事件重试配置
    /// </summary>
    public FailedEventRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// 应用配置集合（AppKey -> 应用配置）
    /// </summary>
    public Dictionary<string, FeishuAppWebhookOptions> Apps { get; set; } = new();


    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
        if (EventHandlingTimeoutMs < 1000)
            throw new InvalidOperationException("EventHandlingTimeoutMs 必须至少为 1000 毫秒");

        if (MaxConcurrentEvents < 1)
            throw new InvalidOperationException("MaxConcurrentEvents 必须至少为 1");

        if (MaxRequestBodySize < 1024)
            throw new InvalidOperationException("MaxRequestBodySize 必须至少为 1024 字节");

        if (TimestampToleranceSeconds < 0)
            throw new InvalidOperationException("TimestampToleranceSeconds 不能为负数");

        // 注意：CircuitBreaker 功能已被移除，不再验证相关配置

        // 验证重试配置
        Retry.Validate();

        // 验证多应用配置
        foreach (var appConfig in Apps)
        {
            var appKey = appConfig.Key;
            var config = appConfig.Value;

            if (string.IsNullOrEmpty(config.AppKey))
                config.AppKey = appKey;

            if (string.IsNullOrEmpty(config.EncryptKey))
                throw new InvalidOperationException($"应用 {appKey} 的 EncryptKey 不能为空");

            if (string.IsNullOrEmpty(config.VerificationToken))
                throw new InvalidOperationException($"应用 {appKey} 的 VerificationToken 不能为空");

            if (config.EncryptKey.Length != 32)
                throw new InvalidOperationException($"应用 {appKey} 的 EncryptKey 长度必须为 32 字符");
        }
    }

    /// <summary>
    /// 根据应用键获取应用配置
    /// </summary>
    public FeishuAppWebhookOptions? GetAppConfig(string appKey)
    {
        return Apps.TryGetValue(appKey, out var config) ? config : null;
    }

    /// <summary>
    /// 获取应用完整路由路径
    /// </summary>
    public string GetAppRoutePrefix(string appKey)
    {
        return $"{GlobalRoutePrefix}/{appKey}";
    }

    /// <summary>
    /// 返回配置的字符串表示（敏感信息已掩码）
    /// </summary>
    public override string ToString()
    {
        return $"FeishuWebhookOptions {{ GlobalRoutePrefix: {GlobalRoutePrefix}, EventHandlingTimeoutMs: {EventHandlingTimeoutMs}, MaxConcurrentEvents: {MaxConcurrentEvents}, EnforceHeaderSignatureValidation: {EnforceHeaderSignatureValidation}, Apps: {Apps.Count} }}";
    }


}
