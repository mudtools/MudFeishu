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
    /// 是否自动注册 Webhook 端点。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R5/X4：该开关无运行时效果。</b>路由始终由宿主显式调用
    /// <c>app.UseFeishuWebhook()</c> 注册；设为 <c>false</c> 既不会自动注册、也不会停止接收事件
    /// （历史上同样从未产生过效果，仅被 <c>FeishuMultiAppMiddleware</c> 的配置变更日志引用）。
    /// </para>
    /// <para>
    /// 若不需要 Webhook 处理，请**不要**调用该中间件。本属性为源码级兼容保留，
    /// 将在下个 major 删除。
    /// </para>
    /// </remarks>
    [Obsolete("该开关无运行时效果：路由由 app.UseFeishuWebhook() 显式注册。若不需要 Webhook 处理，请不要调用该中间件。将在下个 major 移除。")]
    public bool AutoRegisterEndpoint { get; set; } = true;

    /// <summary>
    /// 是否启用事件处理异常捕获
    /// </summary>
    public bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// 事件处理超时时间（毫秒）
    /// 超过此时间仍未完成的请求将被取消并返回超时错误
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>这是"软超时"，不是硬超时</b>：到达该时限时 SDK 只取消
    /// <see cref="CancellationToken"/>，<b>不会</b>强制中断处理器
    /// （强制中断会制造"状态/去重已释放而任务仍在跑"的双重执行，见方案 D4）。
    /// </para>
    /// <para>
    /// 因此：仅当处理器<b>协作式响应</b>取消令牌时，本值才等于实际耗时的上界。
    /// 不响应取消的处理器会把本次请求的实际耗时顶到远超本值，并在此期间持续占用
    /// 并发闸槽位与去重 <c>processing</c> 态。此类情况会以 <c>timeout_overshoot</c>
    /// 指标与 Warning 日志暴露（R3-P1-3），请据此排查处理器实现。
    /// </para>
    /// </remarks>
    public int EventHandlingTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// true 时事件超时仅使用全局 <see cref="EventHandlingTimeoutMs"/>，忽略应用级
    /// <see cref="FeishuAppWebhookOptions.EventHandlingTimeoutMs"/> 覆盖。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 用于 B1 修复升级过渡：修复前应用级超时键可绑定但运行时不生效；修复后开始生效。
    /// 若生产 appsettings 中已配置了过小的应用级超时且需保持旧全局语义，临时设为 <c>true</c>。
    /// </para>
    /// <para><b>默认 <c>false</c></b>（应用级配置生效）。建议在核对配置后移除本开关。</para>
    /// </remarks>
    [Obsolete("B1 修复的过渡开关：仅用于在升级后临时保持「全局超时」的旧语义。核对配置后请移除，将在下个 major 删除。")]
    public bool LegacyGlobalTimeoutOnly { get; set; }

    /// <summary>
    /// Nonce 有效期（秒）；<c>null</c> 表示未显式配置（由去重实现自身的 TTL 决定）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// R3-P2-4：**重放窗口不变量** <c>NonceTtlSeconds ≥ TimestampToleranceSeconds</c>。
    /// 若 TTL 短于时间戳容差，则"Nonce 已过期、但时间戳仍在容差窗口内"的区间里重放攻击可行。
    /// 此前该不变量只存在于文档，无代码强制，且文档口径与默认实现（内存 Nonce TTL=300s）不一致。
    /// </para>
    /// <para>
    /// 显式配置后由 <see cref="Validate"/> 强制该不变量（启动期失败，见 R3-P0-5）；
    /// 留空时不阻断，仅在启动 Summary 日志中打印实际形态供人工核对。
    /// </para>
    /// </remarks>
    public int? NonceTtlSeconds { get; set; }

    /// <summary>
    /// 解析本次事件处理的有效超时（毫秒）。
    /// </summary>
    /// <param name="appConfig">当前应用的 Webhook 配置；无应用级配置时为 null</param>
    internal int ResolveEventHandlingTimeoutMs(FeishuAppWebhookOptions? appConfig)
    {
#pragma warning disable CS0618 // 过渡开关的唯一读取点：下个 major 随属性一并移除
        if (LegacyGlobalTimeoutOnly || appConfig is null)
            return EventHandlingTimeoutMs;
#pragma warning restore CS0618
        return appConfig.GetEffectiveEventHandlingTimeout(EventHandlingTimeoutMs);
    }

    /// <summary>
    /// 并行处理事件的最大并发数
    /// </summary>
    public int MaxConcurrentEvents { get; set; } = 10;

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
    /// 白名单/来源校验的可信代理（CIDR 或精确 IP）。
    /// <para>null = 继承 <see cref="RateLimitOptions.TrustedProxies"/>，避免两处配置漂移。</para>
    /// <para>配置后，白名单校验将使用与限流一致的 ADR-3 零信任 XFF 解析逻辑还原真实客户端 IP。</para>
    /// </summary>
    public HashSet<string>? SourceIpTrustedProxies { get; set; }

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
    /// <list type="bullet">
    /// <item><description>生产环境建议设置为 30 秒或更短，以减少重放攻击时间窗口</description></item>
    /// <item><description>开发环境可以适当放宽到 300 秒</description></item>
    /// <item><description>飞书官方建议的时间戳容错范围为 60 秒以内</description></item>
    /// </list>
    /// <para>
    /// 重放窗口不变量（WHF-03）：<b>NonceTtl 必须 &gt; 本值</b>——否则在 Nonce 过期后、
    /// 容差窗口结束前的区间内重放攻击可行。默认组合（NonceTtl=600s / 容差上限=300s）天然满足。
    /// 本值上限 300 秒由 <see cref="Validate"/> 强制；NonceTtl 侧声明见 Redis 工程的
    /// <c>RedisOptions.NonceTtl</c> XML 注释（跨工程 Options 无法在单一库内联断言）。
    /// </para>
    /// </remarks>
    public int TimestampToleranceSeconds { get; set; } = 30;

    /// <summary>
    /// 重放窗口上限（秒）：TimestampToleranceSeconds 的最大允许值（WHF-03）。
    /// </summary>
    public const int MaxTimestampToleranceSeconds = 300;

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
    /// 生产环境是否允许使用进程内内存 Nonce 去重（默认 <c>false</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 内存实现仅对<b>单实例</b>部署有效：多实例 / 负载均衡下各实例的 Nonce 表互不相通，
    /// 攻击者可将捕获的合法请求在时间戳容差窗口内重放给另一个实例并通过校验 → 事件被重复消费
    /// （跨实例重放<b>不可检测</b>）。这是一种<b>静默的安全退化</b>，默认必须由代码阻断而非文档建议
    /// （与 ADR-4「生产安全默认由代码强制」同口径，见 <c>documents/WebhookHardeningPlan.md</c>）。
    /// </para>
    /// <para>
    /// 生产环境（<c>IEnvironmentService.IsProduction</c>）且未注册分布式实现时，本值为 <c>false</c>
    /// 将导致启动失败；单实例部署可显式置 <c>true</c> 承担风险（仍会输出 Warning）。
    /// 多实例请调用 <c>AddFeishuRedisDeduplicators()</c> 注册 Redis 实现。
    /// </para>
    /// <para>
    /// <b>与 <see cref="NonceValidationFailureMode"/> 的区别（两个正交的轴，勿混淆）：</b>
    /// 本项约束的是「去重服务的<b>实现形态</b>」（内存 vs 分布式）；
    /// <see cref="NonceValidationFailureMode"/> 约束的是「去重服务<b>运行时故障</b>时的降级策略」。
    /// </para>
    /// </remarks>
    public bool AllowInMemoryNonceDedupInProduction { get; set; }

    /// <summary>
    /// 事件被前置拦截器中断（<c>BeforeHandleAsync</c> 返回 <c>false</c>）时对飞书表达的确认语义，
    /// 默认 <see cref="Configuration.InterceptionAckMode.Ack"/>（已消费 → 200）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// WHF/D2：此前拦截一律返回 <c>(false, …)</c>，中间件映射为 HTTP 500 → 飞书重推 → 再次被拦截，
    /// 事件永不 ack 且不落任何去重/失败存储记录，形成重推风暴。
    /// </para>
    /// <para>
    /// 设为 <see cref="Configuration.InterceptionAckMode.Retryable"/> 可表达「暂时不能处理、请稍后重投」
    /// （响应 503，不落去重标记）。详见 <see cref="InterceptionAckMode"/> 的语义说明。
    /// </para>
    /// </remarks>
    public InterceptionAckMode InterceptionAckMode { get; set; } = InterceptionAckMode.Ack;

    /// <summary>
    /// 应用专属拦截器与全局拦截器的组合策略，默认 <see cref="InterceptorFallbackMode.Merge"/>。
    /// </summary>
    /// <remarks>
    /// R3-P2-3：旧行为等价于 <see cref="InterceptorFallbackMode.AppOnly"/>——应用一旦注册专属拦截器，
    /// 全局拦截器就被<b>完全丢弃</b>，导致安全/审计横切在多应用下静默失效。
    /// 详见 <see cref="InterceptorFallbackMode"/>。
    /// </remarks>
    public InterceptorFallbackMode InterceptorFallbackMode { get; set; } = InterceptorFallbackMode.Merge;

    /// <summary>
    /// 空 EventId/Nonce 是否拒绝请求（true=fail-closed），默认 <c>true</c>（WHF-05）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 空 EventId 无法去重（每次都会按新事件处理，幂等性失效）；空 Nonce 无法防重放。
    /// 二者只会出现在畸形或恶意流量中（飞书正常事件必带 event_id 与 nonce），默认拒绝。
    /// </para>
    /// <para>
    /// 策略落点（评审修订 R-3）：本开关仅在 Webhook 层消费——<c>NonceValidator</c> 的
    /// Check/Mark 路径与 <c>FeishuMultiAppMiddleware</c> 的事件数据校验；去重器本身对空键
    /// 「防御性放行」的库级语义不受影响。
    /// </para>
    /// </remarks>
    public bool RejectEmptyIdentifiers { get; set; } = true;

    /// <summary>
    /// 未注册 eventType 的事件是否静默忽略（记 Debug + <c>unhandled</c> 指标），默认 <c>true</c>（WHF-09）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 设为 <c>false</c> 时回退为调用工厂默认处理器兜底——注意默认处理器是「第一个注册的处理器」
    /// （<c>DefaultFeishuEventHandlerFactory</c> 以 <c>_handlerTypes.First()</c> 充当），并非专用兜底实现，
    /// 未知事件被意外路由到首个处理器属于隐式意外语义，故默认改为显式忽略。
    /// </para>
    /// <para>
    /// 门控位于 <c>FeishuWebhookService.DispatchEventAsync</c> 的全局工厂路径（应用专属处理器路径不受影响）。
    /// </para>
    /// </remarks>
    public bool IgnoreUnknownEventTypes { get; set; } = true;

    /// <summary>
    /// 请求频率限制配置
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// 是否启用 Mud.HttpUtils 的令牌后台主动刷新服务（TokenRefreshBackgroundOptions.Enabled）的显式开关。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><c>null</c>（默认）：不干预基座 AddFeishuApp 的令牌刷新策略（有应用时基座默认开启）。</item>
    ///   <item><c>true</c>/<c>false</c>：显式覆盖。R4：原 EnableBackgroundProcessing 已移除。</item>
    /// </list>
    /// </remarks>
    public bool? EnableTokenBackgroundRefresh { get; set; }

    /// <summary>
    /// 失败事件重试配置
    /// </summary>
    public FailedEventRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// 解密阶段超时（毫秒），默认 1000。AES 毫秒级完成，此值为抗线程池饥饿的兜底（WHF-R2/B6）。
    /// </summary>
    public int DecryptionTimeoutMs { get; set; } = 1000;

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

        // R3-P2-4：重放窗口不变量——显式配置 Nonce TTL 时强制 TTL ≥ 时间戳容差。
        // 未配置（null）时不阻断：SDK 无法得知宿主所用去重实现的实际 TTL，仅由启动 Summary 日志提示。
        if (NonceTtlSeconds is { } ttl)
        {
            if (ttl <= 0)
                throw new InvalidOperationException("NonceTtlSeconds 必须为正整数（秒）");

            // 要求**严格大于**（不取 ≥）：TTL 与容差相等时余量为零，
            // 时钟抖动/网络延迟即可让"Nonce 已过期而时间戳仍被接受"的窗口真实存在。
            if (ttl <= TimestampToleranceSeconds)
                throw new InvalidOperationException(
                    $"重放窗口不变量不满足：NonceTtlSeconds({ttl}s) 必须严格大于 " +
                    $"TimestampToleranceSeconds({TimestampToleranceSeconds}s) 以保留余量。" +
                    "否则在 Nonce 已过期、时间戳仍被接受的区间内重放攻击可行");
        }

        if (TimestampToleranceSeconds < 0)
            throw new InvalidOperationException("TimestampToleranceSeconds 不能为负数");

        // WHF-03：重放窗口上限（详见 TimestampToleranceSeconds XML 注释的不变量说明）
        if (TimestampToleranceSeconds > MaxTimestampToleranceSeconds)
            throw new InvalidOperationException(
                $"TimestampToleranceSeconds 不能超过 {MaxTimestampToleranceSeconds} 秒（重放窗口上限，" +
                "飞书官方建议 ≤60 秒）。如需更长窗口请同步调大 NonceTtl（NonceTtl 必须 ≥ TimestampToleranceSeconds）。");

        // 验证重试配置
        Retry.Validate();

        // 验证限流配置（WHF-R2/A2：此前遗漏导致 RateLimitOptions.Validate() 永不执行——
        // RateLimitOptions 作为嵌套属性消费，未注册为独立 IOptions<RateLimitOptions>，
        // IValidateOptions<RateLimitOptions> 永远不会被框架调用。此处补线消除死配置。）
        RateLimit.Validate();

        // 验证解密超时配置（WHF-R2/B6）
        if (DecryptionTimeoutMs is < 100 or > 60_000)
            throw new InvalidOperationException("DecryptionTimeoutMs 必须在 100~60000 毫秒之间");

        // 验证多应用配置
        foreach (var appConfig in Apps)
        {
            var appKey = appConfig.Key;

            // 验证 AppKey 格式（防止路由歧义）
            if (!System.Text.RegularExpressions.Regex.IsMatch(appKey, @"^[a-zA-Z0-9_-]{1,64}$"))
                throw new InvalidOperationException($"应用键 '{appKey}' 格式无效，仅允许字母、数字、下划线和连字符，长度1-64");

            var config = appConfig.Value;

            // R5.2/X8：AppKey 一律由字典键**强制派生**（此前仅在为空时回填，允许配置值与其分叉，
            // 而路由只认字典键 → 诊断信息与实际路由不一致）。属性对宿主只读（internal set）。
            config.AppKey = appKey;

            if (string.IsNullOrEmpty(config.EncryptKey))
                throw new InvalidOperationException($"应用 {appKey} 的 EncryptKey 不能为空");

            if (string.IsNullOrEmpty(config.VerificationToken))
                throw new InvalidOperationException($"应用 {appKey} 的 VerificationToken 不能为空");

            if (config.EncryptKey.Length != 32)
                throw new InvalidOperationException($"应用 {appKey} 的 EncryptKey 长度必须为 32 字符");

            // D6/WHF-R2/C7：多应用下 ExpectedAppId 是 EncryptKey 误配的唯一兜底，必须强制。
            // 缺失时，把 B 应用的 EncryptKey 误填到 A 应用名下，发往 /feishu/appA 的 B 应用密文
            // 会验签+解密全通，并按 A 的处理器与去重键处理 → 跨应用事件串扰且无任何拦截。
            // 单应用部署保持可选（向后兼容）。
            if (Apps.Count > 1 && string.IsNullOrEmpty(config.ExpectedAppId))
                throw new InvalidOperationException(
                    $"多应用配置（FeishuWebhook:Apps 共 {Apps.Count} 项）下应用 {appKey} 必须配置 ExpectedAppId。" +
                    "否则 EncryptKey 误配（如把 B 应用的密钥填到 A）会导致跨应用事件串扰且无任何拦截。");

            // R5.2/X8：接线应用级校验。
            // 此前 FeishuAppWebhookOptions.Validate() 在生产代码中**零调用**（仅测试调用），
            // 于是 WHF-03 声明的「应用级 TimestampToleranceSeconds ≤ 300 秒」与
            // 「应用级 EventHandlingTimeoutMs ≥ 1000ms」两个约束**从未真正生效**。
            // 必须放在上面的 EncryptKey 回填/派生之后：Validate 读的是派生后的最终值。
            config.Validate();
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
