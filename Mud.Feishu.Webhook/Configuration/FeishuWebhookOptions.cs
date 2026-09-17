// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
    /// 是否强制验证 X-Lark-Signature 请求头签名
    /// 当设置为 true 时，如果请求头中缺少签名将拒绝请求
    /// 生产环境建议设置为 true 以提高安全性
    /// </summary>
    /// <remarks>
    /// 安全警告：
    /// - 生产环境必须设置为 true，否则存在严重的安全漏洞
    /// - 仅在开发/测试环境且明确了解风险时设置为 false
    /// - 该约束由 FeishuWebhookOptionsValidator 在生产环境强制执行（详见 documents/WebhookHardeningPlan.md ADR-4）
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
    /// Nonce 验证异常时的降级策略
    /// 当 Nonce 去重服务（如 Redis）不可用时，决定如何处理请求
    /// </summary>
    /// <remarks>
    /// - <see cref="NonceFailureMode.Reject"/>（默认）：拒绝请求（安全优先，但可能影响可用性）
    /// - <see cref="NonceFailureMode.Allow"/>：允许请求通过（可用性优先，但存在重放攻击风险）
    /// 生产环境建议保持 Reject 模式；仅在去重服务短暂不可用且明确了解风险时切换为 Allow
    /// </remarks>
    public NonceFailureMode NonceValidationFailureMode { get; set; } = NonceFailureMode.Reject;

    /// <summary>
    /// 请求频率限制配置
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// 是否启用后台处理模式
    /// 启用后将激活 Mud.HttpUtils 组件的令牌自动刷新后台服务（TokenRefreshBackgroundService），
    /// 定期刷新即将过期的访问令牌，确保后台处理事件时令牌始终有效。
    /// 参见 <see cref="Mud.HttpUtils.TokenRefreshBackgroundOptions"/> 了解更多令牌刷新配置。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>与令牌后台刷新的关系（默认值语义）</b>：本属性会被映射到
    /// <see cref="Mud.HttpUtils.TokenRefreshBackgroundOptions.Enabled"/>。默认值 <c>false</c> 意味着
    /// <b>启用 Webhook 模块的宿主默认不会开启令牌后台主动刷新</b>（即使已注册了应用）——
    /// 令牌退化为首次请求时懒加载 + 过期前无预热。若宿主在关闭 Webhook 后台处理的同时仍需令牌预热，
    /// 请显式设置 <see cref="EnableTokenBackgroundRefresh"/> = <c>true</c>。
    /// </para>
    /// <para>
    /// 本属性仅描述 Webhook 模块自身的后台处理模式，运行时变更由
    /// <c>FeishuWebhookService</c> 响应；与令牌刷新解耦请改用
    /// <see cref="EnableTokenBackgroundRefresh"/>。
    /// </para>
    /// </remarks>
    public bool EnableBackgroundProcessing { get; set; } = false;

    /// <summary>
    /// 是否启用 Mud.HttpUtils 的令牌后台主动刷新服务（<see cref="Mud.HttpUtils.TokenRefreshBackgroundOptions.Enabled"/>）
    /// 的<b>显式覆盖开关</b>。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><c>null</c>（默认）：<b>不干预</b> —— 沿用 <see cref="EnableBackgroundProcessing"/> 的映射
    ///     （保持既有行为，不改变默认语义）。</item>
    ///   <item><c>true</c>/<c>false</c>：显式覆盖映射结果，用于把「令牌刷新」与「Webhook 后台处理」解耦。</item>
    /// </list>
    /// <para>
    /// 背景：此前 <c>TokenRefreshBackgroundOptions.Enabled</c> 被直接赋值为
    /// <see cref="EnableBackgroundProcessing"/>，当宿主已配置应用（基础注册判定为启用）而 Webhook 后台处理
    /// 未开启时，令牌后台刷新会被静默关闭且无任何显式开关可恢复。本属性补齐该逃生口。
    /// </para>
    /// </remarks>
    public bool? EnableTokenBackgroundRefresh { get; set; }

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

        // 验证重试配置
        Retry.Validate();

        // 验证多应用配置
        foreach (var appConfig in Apps)
        {
            var appKey = appConfig.Key;

            // 验证 AppKey 格式（防止路由歧义）
            if (!System.Text.RegularExpressions.Regex.IsMatch(appKey, @"^[a-zA-Z0-9_-]{1,64}$"))
                throw new InvalidOperationException($"应用键 '{appKey}' 格式无效，仅允许字母、数字、下划线和连字符，长度1-64");

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
