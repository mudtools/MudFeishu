# Webhook 链路安全审查修复方案（WHF-R2）

> **文档版本**：v1.1（多维评审后修订）
> **编制日期**：2026-09-21
> **编制依据**：《Webhook 链路深度安全审查报告》（签名 / 防重放 / 分发 / 去重四域全量代码审查，2026-09-21）
> **适用范围**：`Mud.Feishu.Webhook`、`Mud.Feishu.Redis`、`Mud.Feishu.Abstractions`
> **结论基线**：审查未发现 P0 阻断性缺陷；确认 P1×4、P2×13。本方案为全部 17 项给出落地设计、测试与验收标准。

---

## 多维方案评审（v1.1 新增）

> 评审日期：2026-09-21，基于 v3 分支当前源码逐条核查。

### 一、高级程序员视角

#### A1. Nonce 去重多实例静默降级

- **问题真实性** ✅：`FeishuWebhookServiceBuilder.cs:644` 确认 `TryAddSingleton<IFeishuNonceDistributedDeduplicator, FeishuNonceDistributedDeduplicator>()` 注册内存实现；事件去重在 `:610-614` 有 `LogWarning` 兜底，Nonce 去重无等价告警——问题成立。
- **方案必要性** ✅：多实例部署下跨实例重放不可检测是真实安全风险。
- **逻辑完整性** ⚠️ **修订**：方案中 `NonceDedupModeGuard` 使用 `IConfigureOptions<FeishuDeduplicationOptions>` 注册方式不对——`IConfigureOptions` 的职责是配置绑定而非校验。且 `sp.GetRequiredService<IFeishuNonceDistributedDeduplicator>()` 在 `IConfigureOptions` 回调中触发会打破 DI 构建顺序。**改为**在 `FeishuWebhookServiceBuilder.RegisterCoreServices()` 末尾用 `PostConfigure<IServiceProvider>` 回调做检测（与文件中既有的 X2/X4 迁移探测同模式），与既有架构一致。
- **冗余排查** ✅：无重复代码。
- **最优解评估** ✅：fail-fast 生产 + warn 非生产是当前能力下最优解。

#### A2. RateLimitOptions 校验链接线

- **问题真实性** ✅：`FeishuWebhookOptions.Validate()`（`:207-261`）确认无 `RateLimit.Validate()` 调用；`IValidateOptions<RateLimitOptions>` 注册于 `:525` 但 `RateLimitOptions` 从未作为独立 `IOptions<RateLimitOptions>` 注册——死配置确认。
- **方案必要性** ✅：`MaxRequestsPerWindow=0` 或 `WindowSizeSeconds=0` 导致限流失效或全量 429，启动期无报错。
- **逻辑完整性** ⚠️ **修订**：`RateLimitOptions.Validate()` 当前已检查 `WindowSizeSeconds < 1` 和 `MaxRequestsPerWindow < 1`（`:75-79`），方案中新增的 `MaxRequestsPerWindow > 1_000_000` 护栏属于过度防御——飞书 Webhook QPS 极低，且 `WindowSizeSeconds` 上限未设护栏不对称。**删除该额外护栏**，只做 `Validate()` 接线，保持与 `Retry.Validate()` 同形的最小改动。
- **最优解评估** ✅：在 `FeishuWebhookOptions.Validate()` 补 `RateLimit.Validate()` 一行是最简方案。

#### A3. IP 白名单与限流中间件统一客户端 IP 认定

- **问题真实性** ✅：`FeishuMultiAppMiddleware.cs:166` 确认 `var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";`，而 `FeishuRateLimitMiddleware.GetClientIp`（`:169-197`）实现了完整的零信任 XFF 解析。
- **方案必要性** ✅：反代部署下白名单全误拒或形同虚设。
- **逻辑完整性** ✅：提取 `ClientIpResolver` 共享解析器、新增 `SourceIpTrustedProxies` 继承 `RateLimit.TrustedProxies`、白名单中间件接入——设计完整。
- **冗余排查** ✅：消除两份 IP 解析实现。
- **最优解评估** ✅。

#### A4. NonceTtl × TimestampTolerance 重放窗口不变量

- **问题真实性** ✅：`RedisOptions.Validate()`（`:124-126`）只查 `NonceTtl > 0`，无跨工程断言。
- **方案必要性** ✅。
- **逻辑完整性** ⚠️ **修订**：方案在 `ResolveUnifiedNonceTtl` 中做运行时断言，但该方法返回 `TimeSpan`——抛异常会导致 DI 容器构建失败，这与 Nonce 去重器可选使用的语义冲突。且 `ResolveUnifiedNonceTtl` 位于 `Mud.Feishu.Redis` 工程，引用 `IOptionsMonitor<FeishuWebhookOptions>` 会使 Redis 工程对 Webhook 工程产生编译期依赖。**修订**：改为在 `FeishuWebhookServiceBuilder.RegisterCoreServices` 的 `PostConfigure<IServiceProvider>` 中做跨工程断言（与 A1 同模式），Redis 工程不引入对 Webhook 工程的依赖。Webhook 侧通过 `IFeishuNonceDistributedDeduplicator` 的运行时类型检测（判断是否为内存实现）间接推断 Redis 缺失。
- **最优解评估** ✅（修订后）。

#### B1-B6、C1-C7 逐条核查

- **B1 日志注入** ✅：`FeishuMultiAppMiddleware.cs:355-358` 确认 `{Nonce}` 和 `{SignaturePrefix}` 未经 `LogSanitizer.Clean`；`CompositeFeishuEventValidator.cs:80` 已对 nonce 做了 Clean。
- **B2 Nonce Check 移除** ✅：`CompositeFeishuEventValidator.cs:91-96` 的 CheckNonceAsync 预检查确认存在；移除后 Mark 路径的 `SET NX EX` 原子性已核实。
- **B3 CancellationToken 贯穿** ✅：`IFeishuEventValidator.ValidateHeaderSignatureAsync` 当前无 ct 参数（`:41`），方案追加默认值重载不破坏接口。⚠️ **修订**：需同步修改 `ISignatureValidator` 和 `INonceValidator` 接口（方案中提到但未明确标注接口名）。
- **B4 重试计数竞态** ✅：`InMemoryFailedEventStore.cs:103` 的 `AddOrUpdate` update 工厂 `(_, _) => failedEvent` 确认覆盖 `RetryCount`。
- **B5 AsyncLocal AppKey 清理** ✅：`FailedEventRetryService.cs:188-189` 确认仅在 `!IsNullOrEmpty(failedEvent.AppKey)` 时设置，空 AppKey 不清理。
- **B6 解密超时可配置** ✅：`FeishuMultiAppMiddleware.cs:365` 确认硬编码 `TimeSpan.FromSeconds(1)`。
- **C1 限流白名单归一化** ✅：`FeishuRateLimitMiddleware.cs:114` 的 `Contains(clientIp!)` 确认无 IPv4-mapped 归一。
- **C2 Retry-After** ✅：`FeishuRateLimitMiddleware.cs:258` 确认 `Math.Max(1, rateLimitOptions.WindowSizeSeconds)` 返回整窗口而非剩余。
- **C3 中间件 DI 生命周期** ✅：`ApplicationBuilderExtensions.cs:23-26` 确认未注册到 DI。⚠️ **修订**：`FeishuRateLimitMiddleware` 也需同样处理（它也实现了 `IDisposable`，有 `_cleanupTimer` 需释放）。
- **C4 并发服务竞态** ✅：`FeishuWebhookConcurrencyService.cs:175` 确认 `WaitAsync` 后无 OCE 补还；`:144` 确认 60s 固定延迟。
- **C5 窗口边界文档** ✅：与 A4 合并。
- **C6 SeqID 异步化** ✅：`IFeishuSeqIDDeduplicator.cs:51,57` 确认同步方法；`RedisFeishuSeqIDDeduplicator.cs:266,290` 确认同步 Redis 调用。
- **C7 明文验证废弃 + ExpectedAppId** ✅：`IFeishuWebhookService.cs:30` 确认 `VerifyEventSubscriptionAsync` 存在。

### 二、系统架构师视角

- **前后端逻辑闭环** ✅：各条目的修复点均有明确的入口（中间件/验证器）和出口（日志/HTTP 响应），链路闭环。
- **架构兼容性** ✅：方案遵循"只追加可选成员或新增重载，不改签名"原则，与 AGENTS.md 的契约约束一致。
- **多 TFM 一致性** ✅：新 API 使用 `#if NET7_0_OR_GREATER` 隔离，配置 DTO 不使用 `required`。
- **AOT 红线** ✅：无反射式 JSON；新增 Options 属性均为简单类型。
- **跨工程依赖** ⚠️ **已修订**：A4 的跨工程断言改为在 Webhook 侧执行，Redis 工程不引入对 Webhook 工程的依赖。
- **R5 配置面治理** ⚠️：A3/B6/C7 新增配置属性需同步更新 `ConfigSurfaceContractGuards`。

### 三、质量保障视角

- **测试方案可落地性** ✅：每条目均有明确的测试用例命名（`{Method}_Should{Behavior}_When{Condition}`），遵循仓库规范。
- **测试覆盖度** ✅：预估约 30 例新增测试覆盖全部 17 项变更。
- **回归风险** ✅：各条目均声明"既有用例无回归"为验收条件。
- **门禁对齐** ✅：最终需通过 `verify-build.ps1 -StrictFormat` 全量门禁。

---

## 0. 执行摘要与分期策略

| 分期 | 内容 | 目标 | 预估工作量 |
|---|---|---|---|
| **第一期（P1，本迭代必须）** | A1–A4：四项 P1 修复 | 消除"配置错误静默劣化"类风险 | 1.5 ~ 2 人日 |
| **第二期（P2 安全与正确性）** | B1–B6 | 收敛日志注入、竞态、ct 断链 | 2 ~ 3 人日 |
| **第三期（P2 性能与加固）** | C1–C7 | RTT 削减、资源生命周期、API 收敛 | 2 ~ 3 人日 |

**总体原则**（贯穿全部条目）：

1. **Fail-fast 优先于静默降级**——凡"配置错误导致安全能力静默失效"的场景，一律在启动期（`IValidateOptions` / `PostConfigure` / 去重器注册路径）报错或告警，不在请求路径上兜底。
2. **不破坏既有契约**——公开接口（`IFeishuEventValidator`、`IFeishuNonceDistributedDeduplicator` 等）只做**追加可选成员**或**新增重载**，不改签名；破坏性变更（如移除 `VerifyEventSubscriptionAsync`）按仓库惯例走 `[Obsolete]` + 下个 major 移除。
3. **多 TFM 一致性**——netstandard2.0 / net6.0 / net8.0 / net10.0 分支行为必须一致；新 API 使用 `#if NET7_0_OR_GREATER` 隔离。
4. **AOT 红线**——禁止反射式 System.Text.Json；配置 DTO 不引入 `required`；校验一律放 `Validate()`。
5. **每个条目附带回归测试**，测试命名遵循 `{Method}_Should{Behavior}_When{Condition}`。

---

## 第一期：P1 修复（本迭代必须）

### A1. Nonce 去重多实例静默降级 → 启动期告警/阻断

- **对应审查项**：P1-1
- **根因**：`FeishuWebhookServiceBuilder.RegisterCoreServices` 中 `TryAddSingleton<IFeishuNonceDistributedDeduplicator, FeishuNonceDistributedDeduplicator>()` 默认注册内存实现；事件去重在 `Mode=Distributed` 未注册 Redis 实现时有 `LogWarning` 兜底，Nonce 去重没有等价告警。多实例部署下跨实例重放不可检测。
- **位置**：`Mud.Feishu.Webhook/Extensions/FeishuWebhookServiceBuilder.cs:644`（对照 `:599-615` 事件去重的既有告警）

#### 修复设计（v1.1 修订）

在 `RegisterCoreServices()` 末尾追加 `PostConfigure<IServiceProvider>` 回调（与文件中既有 X2/X4 迁移探测同模式）：

```csharp
_services.AddOptions<FeishuWebhookOptions>()
    .PostConfigure<IServiceProvider>((options, sp) =>
    {
        var unified = sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value;
        var isDistributedIntent = unified is { IsConfiguredFromConfiguration: true }
            && string.Equals(unified.Mode, FeishuDeduplicationOptions.ModeDistributed, StringComparison.OrdinalIgnoreCase);
        var nonceImpl = sp.GetService<IFeishuNonceDistributedDeduplicator>();
        var isMemoryNonce = nonceImpl is FeishuNonceDistributedDeduplicator or null;

        if (isDistributedIntent && isMemoryNonce)
        {
            var logger = sp.GetService<ILogger<FeishuWebhookOptions>>();
            var isProduction = sp.GetService<IEnvironmentService>()?.IsProduction == true;

            if (isProduction)
            {
                throw new InvalidOperationException(
                    "FeishuDeduplication:Mode=Distributed 但 Nonce 去重为进程内内存实现。" +
                    "多实例部署下跨实例重放攻击不可检测。请调用 AddFeishuRedisDeduplicators() 注册 Redis 实现。");
            }

            logger?.LogWarning(
                "FeishuDeduplication:Mode=Distributed 但 Nonce 去重为进程内内存实现。" +
                "多实例部署下跨实例重放攻击不可检测。请调用 AddFeishuRedisDeduplicators() 注册 Redis 实现。");
        }
    });
```

- **改动文件**：`FeishuWebhookServiceBuilder.cs`
- **测试**：
  - `NonceDedupGuard_ShouldThrow_WhenDistributedModeWithMemoryNonceInProduction`
  - `NonceDedupGuard_ShouldWarn_WhenDistributedModeWithMemoryNonceInNonProduction`
  - `NonceDedupGuard_ShouldNotFire_WhenRedisNonceRegistered`
- **验收**：新增单测 3 例全绿；`Tests/Mud.Feishu.Webhook.Tests` 既有用例无回归。

---

### A2. RateLimitOptions 校验链接线（消除死配置）

- **对应审查项**：P1-2
- **根因**：`RateLimitOptions` 作为 `FeishuWebhookOptions.RateLimit` 嵌套属性消费，从未注册为独立 `IOptions<RateLimitOptions>`，因此 `IValidateOptions<RateLimitOptions>`（`FeishuWebhookServiceBuilder.cs:525`）永远不会被框架调用；同时 `FeishuWebhookOptions.Validate()`（`:207-261`）遗漏了 `RateLimit.Validate()` 调用。后果：`MaxRequestsPerWindow=0` → 全量 429 自我 DoS；`WindowSizeSeconds=0` → 限流完全失效，均无启动期报错。
- **位置**：`Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs:207-261`

#### 修复设计（v1.1 修订：删除过度防御护栏）

在 `FeishuWebhookOptions.Validate()` 中补一行（与 `Retry.Validate();` 同形，`:228` 之后）：

```csharp
// 验证限流配置
RateLimit.Validate();
```

**同时**保留 `IValidateOptions<RateLimitOptions>` 注册不动——它是为宿主显式 `AddOptions<RateLimitOptions>()` 场景预留的，并在注释中说明双路径关系。

- **改动文件**：`FeishuWebhookOptions.cs`
- **测试**：
  - `Validate_ShouldThrow_WhenRateLimitWindowIsZero`
  - `Validate_ShouldThrow_WhenRateLimitMaxRequestsIsZero`
  - `Validate_ShouldPass_WhenRateLimitDisabledWithValidBounds`
- **验收**：3 例新增全绿；`FeishuWebhookOptionsBindingTests` 既有绑定用例（合法默认值）不回归。

---

### A3. IP 白名单与限流中间件统一客户端 IP 认定（零信任 XFF）

- **对应审查项**：P1-3
- **根因**：`FeishuMultiAppMiddleware` 白名单校验直接使用 `context.Connection.RemoteIpAddress`（`:166`），而 `FeishuRateLimitMiddleware.GetClientIp`（`:169-197`）已实现"可信代理 + XFF 从右向左取第一个非可信 IP"的 ADR-3 零信任逻辑。反代部署下白名单全误拒，或被迫放行代理 IP 后形同虚设。
- **位置**：`Mud.Feishu.Webhook/Middleware/FeishuMultiAppMiddleware.cs:166`、`FeishuRateLimitMiddleware.cs:169-197`

#### 修复设计

**第 1 步——提取共享解析器**（消除两份实现）：

```csharp
// Mud.Feishu.Webhook/Utils/ClientIpResolver.cs（新建，internal static class）
internal static class ClientIpResolver
{
    /// <summary>ADR-3 零信任：默认仅认 RemoteIpAddress；直连命中可信代理时解析 XFF。</summary>
    public static string? GetClientIp(
        HttpContext context,
        bool useForwardedHeaders,
        HashSet<string> trustedProxies)
    {
        // 实现体从 FeishuRateLimitMiddleware.GetClientIp 原样下沉（含从右向左算法）
    }
}
```

**第 2 步——白名单配置面**：`FeishuWebhookOptions` 新增可选项，与限流的 `TrustedProxies` 解耦但默认继承，避免两处配置漂移：

```csharp
/// <summary>白名单/来源校验的可信代理（CIDR 或精确 IP）。null = 继承 RateLimit.TrustedProxies。</summary>
public HashSet<string>? SourceIpTrustedProxies { get; set; }
```

**第 3 步——中间件接线**：

```csharp
var clientIp = ClientIpResolver.GetClientIp(
    context,
    Options.RateLimit.UseForwardedHeaders,
    Options.SourceIpTrustedProxies ?? Options.RateLimit.TrustedProxies);

if (Options.AllowedSourceIPs.Count > 0
    && !IpAddressHelper.IsIpAllowed(clientIp, Options.AllowedSourceIPs)) { ... 403 ... }
```

`FeishuRateLimitMiddleware.GetClientIp` 改为薄委托到 `ClientIpResolver`（删除重复实现，保留原 public 行为不变）。

- **改动文件**：`Utils/ClientIpResolver.cs`（新建）、`FeishuMultiAppMiddleware.cs`、`FeishuRateLimitMiddleware.cs`、`FeishuWebhookOptions.cs`
- **兼容性**：未配置 `TrustedProxies` 时行为与现状完全一致（零信任默认）；已配置场景下白名单从"只看代理 IP"升级为"还原真实客户端 IP"——属安全增强，写入 CHANGELOG 提示运维核对白名单内容。
- **测试**：
  - `ClientIpResolver_ShouldPreferRemoteIp_WhenTrustedProxiesEmpty`
  - `ClientIpResolver_ShouldResolveRightmostUntrusted_WhenDirectIpIsTrustedProxy`
  - `ClientIpResolver_ShouldIgnoreLeftmostSpoofed_WhenXffForged`
  - `MultiAppMiddleware_Should403_WhenRealClientIpNotInWhitelist`
- **验收**：4 例新增全绿；`FeishuRateLimitMiddlewareTests` 无回归。

---

### A4. NonceTtl × TimestampTolerance 重放窗口不变量运行时强制

- **对应审查项**：P1-4
- **根因**：WHF-03 不变量「NonceTtl 必须 ≥ TimestampToleranceSeconds」仅存在于 XML 注释（`FeishuWebhookOptions.cs:122-133`），`RedisOptions.Validate()`（`Mud.Feishu.Redis/Configuration/RedisOptions.cs:124-126`）只查 `NonceTtl > 0`。跨工程配置组合（NonceTtl=60s + Tolerance=300s）产生最长 240s 重放窗口。
- **位置**：`Mud.Feishu.Redis/Extensions/RedisFeishuServiceBuilderExtensions.cs:269`（去重器构造点）；`Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs:222-225`

#### 修复设计（v1.1 修订：跨工程断言改在 Webhook 侧执行）

**双层防御**：

1. **Webhook 侧（权威声明面 + 跨工程断言执行面）**——在 `FeishuWebhookServiceBuilder.RegisterCoreServices` 的 `PostConfigure<IServiceProvider>` 中（与 A1 合并到同一回调）检测：

```csharp
// WHF-03 跨工程断言：Nonce TTL 必须 ≥ Webhook TimestampToleranceSeconds
var nonceDedup = sp.GetService<IFeishuNonceDistributedDeduplicator>();
if (nonceDedup is not null)
{
    var tolerance = options.TimestampToleranceSeconds;
    // 从 Redis 实现的 NonceTtl 属性读取（需 IFeishuNonceDistributedDeduplicator 追加 NonceTtl 只读属性）
    // 内存实现跳过（内存 Nonce 无过期，不存在窗口问题）
}
```

⚠️ **进一步修订**：`IFeishuNonceDistributedDeduplicator` 当前无 `NonceTtl` 属性。追加接口成员会破坏所有实现方。**最终方案**：不在运行时做跨工程断言，改为在 `FeishuWebhookOptions.Validate()` 中做**声明性校验**——当 `TimestampToleranceSeconds > 0` 且 Nonce 去重为内存实现时，在日志中告警提示 NonceTtl 需手动确认。真正的跨工程硬断言留待 Nonce 去重器接口追加 `NonceTtl` 属性后实施。

2. **边界收紧（配合审查 P2-3）**——文档与默认值同步调整：`RedisOptions.NonceTtl` 默认从 5 分钟调整为 **600s（2 × 300s 上限容差）**，XML 注释"≥"改写为"建议 2 倍容差，至少严格大于"。默认组合变更属**向后兼容的安全增强**（旧默认恰好压线），写入 CHANGELOG。

- **改动文件**：`RedisOptions.cs`、`Redis/README.md`、CHANGELOG.md
- **测试**：
  - `Validate_ShouldPass_WhenTimestampToleranceWithinBounds`
  - `RedisOptions_DefaultNonceTtl_ShouldBe600Seconds`
- **验收**：2 例新增全绿；`Redis.Tests` 既有键隔离域用例不回归。

---

## 第二期：P2 安全与正确性

### B1. 日志注入三处漏网收敛

- **对应审查项**：P2-1
- **位置与改动**：
  1. `FeishuMultiAppMiddleware.cs:355-358`：Warning 中 `{Nonce}`、`{SignaturePrefix}` 包 `LogSanitizer.Clean(...)`（签名前缀先 Substring 再 Clean）。
  2. `RedisFeishuNonceDistributedDeduplicator.cs:195`：`RemoveAsync` 异常路径原始 nonce → `LogSanitizer.Clean(nonce)`。
  3. `Mud.Feishu.Abstractions/Services/FeishuNonceDistributedDeduplicator.cs:44`：内存实现 Warning 同样清洗（`LogSanitizer` 位于同工程 `Utilities/`，直接引用即可）。
- **测试**：`LogSanitizer` 侧新增 1 例输入含 `\r\n` 的 nonce 断言输出无换行；三处调用点各 1 例日志断言（可用 `ILogger` fake 捕获）。
- **验收**：`grep -n "nonce" 三文件` 确认所有 `{Nonce}` 占位实参均经 Clean。

### B2. Nonce 验证链移除验签前的 Check（省 1 RTT + 消除探测面）

- **对应审查项**：P2-2
- **设计**：`CompositeFeishuEventValidator.ValidateHeaderSignatureAsync` 删除第 2 步（`:92-96` 的 `CheckNonceAsync` 预检查），保留"时间戳 → 签名 → 原子 Mark"三步。防重放正确性由 `TryMarkNonceAsUsedAsync` 的 `SET NX EX` 单独保证（已核实原子），语义不变：
  - 签名失败 → 不触碰 nonce（现状亦然，Mark 在验签后）；
  - 签名通过 + nonce 已用 → Mark 返回 true → 拒绝（与现状 Check 命中同效，仅少一次 GET）。
- **契约注意**：`INonceValidator.CheckNonceAsync` **保留不删**（公开接口 + 独立测试引用），仅组合验证器不再调用；在接口 XML 注释标注"组合验证器自 vX 起不再调用，仅供诊断/自定义编排"。
- **测试**：`CompositeFeishuEventValidatorTests` 调整——删除"Check 先于签名失败"的用例，新增 `ValidateHeaderSignature_ShouldConsumeOnlyOneNonceCall_WhenSignatureFails`（断言验签失败时 Mark 未被调用）。
- **验收**：热路径 Redis RTT 由 4 降为 3；既有重放攻击测试全部改经 Mark 路径后仍绿。

### B3. CancellationToken 贯穿验证链

- **对应审查项**：P2-4
- **设计**（追加可选参数，不破坏接口）：

```csharp
// IFeishuEventValidator / ISignatureValidator / INonceValidator：新增带 ct 的重载，旧签名委托新签名
Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body,
    string? headerSignature, string encryptKey, CancellationToken cancellationToken = default);
```

贯通路径：`FeishuMultiAppMiddleware.cs:353` 传 `context.RequestAborted` → `FeishuWebhookService.HandleEventAsync(request, body, ct)` → `CompositeFeishuEventValidator` → `NonceValidator`（Check/Mark 内部 `_nonceDeduplicator.TryMarkAsUsedAsync(nonce, CurrentAppKey, cancellationToken: ct)`）。Redis 侧 `ToCommandFlags(ct)` 契约不变（取消仍靠 `ThrowIfCancellationRequested` 前置检查，`RedisStoreHelper.cs:20-28` 注释已声明）。
- **例外**：`MarkDeduplicationCompletedAsync` 保持无 ct（去重标记必须完成，WHF-16 已有注释声明，不动）。
- **测试**：`NonceValidator_ShouldForwardCancellationToken_ToDeduplicator`；`MultiAppMiddleware_ShouldAbortSignatureChain_WhenClientDisconnects`。
- **验收**：`dotnet build` 0 警告（新重载不产生 CS 异常）；多 TFM 编译一致。

### B4. 失败重试计数竞态与淘汰后更新丢失

- **对应审查项**：P2-8
- **设计**：
  1. `IFailedEventStore.StoreFailedEventAsync` 增加重试感知语义：实现内 `AddOrUpdate` 的 update 工厂**保留存量 `RetryCount`**（仅首次新增时为 0）：

```csharp
_failedEvents.AddOrUpdate(storeKey,
    _ => failedEvent,                                   // 新增
    (_, existing) => { failedEvent.RetryCount = existing.RetryCount; return failedEvent; });
```

  2. `InMemoryFailedEventStore.UpdateFailedEventAsync`（`:183-195`）：条目不存在时 `LogWarning("失败事件 {EventId} 已被淘汰，重试状态更新丢失")`（多实例下由 Redis 存储实现兜底，文档已声明内存实现的单机语义）。
  3. `FailedEventRetryService` 侧不动（其快照递增逻辑与第 1 点天然兼容）。
- **测试**：`InMemoryFailedEventStoreTests` 新增 `StoreFailedEvent_ShouldPreserveRetryCount_WhenKeyExists`；`UpdateFailedEvent_ShouldLog_WhenEntryEvicted`。
- **验收**：多实例重试次数不再超 `MaxRetryCount`（以新单测模拟两轮覆盖写验证）。

### B5. FailedEventRetryService 的 AsyncLocal AppKey 跨迭代污染

- **对应审查项**：P2-9
- **位置**：`FailedEventRetryService.cs:188-189`
- **设计**：循环体内改为无条件设置/清理：

```csharp
foreach (var failedEvent in failedEvents)
{
    // AsyncLocal 在同一 async 方法内向前流动：空 AppKey 条目必须显式清空，
    // 否则继承上一迭代的 AppKey 上下文（去重键/应用级配置错挂）
    webhookService.SetCurrentAppKey(failedEvent.AppKey ?? string.Empty);
    ...
}
```

`WebhookAppKeyAccessor.SetAppKey(string.Empty)` 使 `CurrentAppKey` 语义等价于"无应用"（各验证器对空串 `IsNullOrEmpty` 分支已正确处理——`SignatureValidator.cs:126`、`NonceValidator.cs` 均以 `IsNullOrEmpty` 判断，空串安全）。
- **测试**：`FailedEventRetryService_ShouldResetAppKeyContext_WhenNextEventHasNoAppKey`（连续两条：appA → null，断言第二条的 accessor 值为空）。
- **验收**：1 例新增全绿。

### B6. 解密超时可配置化

- **对应审查项**：P2-5
- **设计**：`FeishuWebhookOptions` 新增：

```csharp
/// <summary>解密阶段超时（毫秒），默认 1000。AES 毫秒级完成，此值为抗线程池饥饿的兜底。</summary>
public int DecryptionTimeoutMs { get; set; } = 1000;
```

`Validate()` 增加 `if (DecryptionTimeoutMs is < 100 or > 60_000) throw ...`；中间件 `:365` 改读配置。
- **测试**：`Validate_ShouldThrow_WhenDecryptionTimeoutOutOfRange`；中间件用例验证新默认值生效。
- **验收**：默认行为不变；非法配置启动期报错。

---

## 第三期：P2 性能与加固

### C1. IP 白名单归一化统一（限流白名单）

- **对应审查项**：P2-10②
- **设计**：`FeishuRateLimitMiddleware.InvokeAsync:114` 的 `WhitelistIPs.Contains(clientIp!)` 改为 `IpAddressHelper.IsIpAllowed(clientIp, rateLimitOptions.WhitelistIPs)`——复用 WHF-18 的 IPv4-mapped 归一与 CIDR 支持（白名单从此也支持 CIDR，属增强）。
- **测试**：`RateLimit_ShouldSkip_WhenMappedIpv6InWhitelist`（`::ffff:10.0.0.1` 命中 `10.0.0.1`）。
- **验收**：1 例新增 + 既有用例回归绿。

### C2. Retry-After 返回窗口剩余秒数

- **对应审查项**：P2-10①
- **设计**：`WriteTooManyRequestsResponse` 增加 `DateTime windowStart` 参数（调用方传入命中的 counter 的 `WindowStart`）：

```csharp
var remaining = Math.Max(1, (int)(rateLimitOptions.WindowSizeSeconds
    - (DateTime.UtcNow - windowStart).TotalSeconds));
context.Response.Headers["Retry-After"] = remaining.ToString();
```

- **测试**：`RateLimit_ShouldReturnRemainingSeconds_InRetryAfterHeader`。
- **验收**：RFC 9110 语义符合；既有 429 测试适配新断言。

### C3. 中间件纳入 DI 生命周期（Dispose 可达）

- **对应审查项**：P2-6
- **设计**（v1.1 修订：同时处理两个中间件）：`ApplicationBuilderExtensions` 两个方法内补注册（幂等）：

```csharp
public static IApplicationBuilder UseFeishuWebhook(this IApplicationBuilder builder)
{
    builder.ApplicationServices.TryAddSingleton<FeishuMultiAppMiddleware>();
    return builder.UseMiddleware<FeishuMultiAppMiddleware>();
}

public static IApplicationBuilder UseFeishuRateLimit(this IApplicationBuilder builder)
{
    builder.ApplicationServices.TryAddSingleton<FeishuRateLimitMiddleware>();
    return builder.UseMiddleware<FeishuRateLimitMiddleware>();
}
```

`UseMiddleware<T>` 检测到 DI 注册后从容器解析，`IHost` 关停时随容器 Dispose（`_onChangeSubscription`、`_cleanupTimer` 释放兑现 WHF-13 承诺）。
- **测试**：`MultiAppMiddleware_ShouldDisposeOnChangeSubscription_WhenHostStops`（Host 停止后触发 options change，断言回调不再执行）。
- **验收**：1 例新增；既有中间件测试不回归（ActivatorUtilities 解析路径兼容）。

### C4. 并发服务两处竞态加固

- **对应审查项**：P2-7
- **设计**：
  1. **WaitAsync 取消竞态槽位泄漏**——`AcquireAsync` 捕获 OCE 后探测补还：

```csharp
try
{
    await currentSemaphore.WaitAsync(combinedCts.Token);
    return new SemaphoreLease(currentSemaphore, _logger);
}
catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !_shutdownCts.IsCancellationRequested)
{
    // 取消与获取的竞态：WaitAsync 可能在已获取后抛 OCE，必须补还槽位
    currentSemaphore.Release();
    throw;
}
```

  2. **旧信号量延迟释放**——`UpdateSemaphoreAsync` 的 60s 固定延迟改为动态：`Task.Delay(Math.Max(60_000, 有效处理超时 × 2))`；`SemaphoreLease.Dispose` 包 try/catch `ObjectDisposedException` 记 Debug（兜底，不再向上传播）。
- **测试**：`Acquire_ShouldNotLeakSlot_WhenCancelledDuringWait`（高并发取消压测，断言 `AvailableCount` 最终恢复满额）；`Lease_ShouldNotThrow_WhenOldSemaphoreDisposed`。
- **验收**：并发压测 1000 轮无槽位漂移；既有 `FeishuWebhookConcurrencyServiceTests` 回归绿。

### C5. Nonce 窗口边界余量（默认值与文档）

- **对应审查项**：P2-3（与 A4 第 3 点合并实施）
- **设计**：已在 A4 落地默认 TTL 600s；此处补充校验措辞——`RedisOptions.Validate()` 的错误消息改为"NonceTtl 建议为 TimestampToleranceSeconds 的 2 倍且至少严格大于"；`FeishuWebhookOptions.TimestampToleranceSeconds` XML 注释更新为 `>`（严格大于）语义。
- **验收**：文档与校验一致；无代码行为变更（A4 已覆盖）。

### C6. SeqID 去重器同步调用异步化

- **对应审查项**：P2-11
- **设计**：`RedisFeishuSeqIDDeduplicator.GetCacheCount` / `GetMaxProcessedSeqId` 新增异步版本并保留同步版（`[Obsolete("诊断用途，WebSocket 热路径请使用异步版本")]`）：

```csharp
public async Task<int> GetCacheCountAsync(CancellationToken ct = default)
    => (int)await _database.SortedSetLengthAsync(_sortedSetKey);

public async Task<ulong> GetMaxProcessedSeqIdAsync(CancellationToken ct = default)
{
    var max = await _database.SortedSetRangeByScoreAsync(_sortedSetKey,
        take: 1, order: Order.Descending);
    return max.Length > 0 && ulong.TryParse(max[0], out var v) ? v : 0ul;
}
```

调用方（WebSocket 消息泵）迁移至异步版；`IFeishuSeqIDDeduplicator` 接口追加两个异步成员（追加不破坏实现方）。
- **测试**：`SeqIdDeduplicator_GetMaxProcessedSeqIdAsync_ShouldReflectWindow`。
- **验收**：接口追加后 Abstractions.Tests 全绿；WebSocket 热路径无同步 Redis 调用（架构测试断言可后续加门禁）。

### C7. 明文订阅验证 API 标记废弃 + AppId 交叉校验（加固项）

- **对应审查项**：P2-12 / P2-13
- **设计**：
  1. `IFeishuWebhookService.VerifyEventSubscriptionAsync` 加 `[Obsolete("明文验证协议无重放防护，中间件已强制加密验证。请使用加密 url_verification 链路。将在下个 major 移除。")]`（实现与接口同步标注，`#pragma` 处理内部调用 CS0618）。
  2. `FeishuAppWebhookOptions` 新增可选字段 `public string? ExpectedAppId { get; set; }`；`FeishuMultiAppMiddleware.HandleEncryptedVerificationAsync` 之后、事件分发之前校验：

```csharp
if (!string.IsNullOrEmpty(appConfig.ExpectedAppId)
    && !string.Equals(decryptedData.AppId, appConfig.ExpectedAppId, StringComparison.Ordinal))
{
    _logger.LogWarning("事件 AppId {AppId} 与应用 {AppKey} 声明的 ExpectedAppId 不匹配，拒绝处理",
        decryptedData.AppId, appKey);
    await WriteErrorResponse(context, 403, "Forbidden: AppId mismatch", requestId);
    return;
}
```

  防御 EncryptKey 复用误配置导致的跨应用事件串扰。未配置时行为不变（零迁移成本）。
- **测试**：`MultiAppMiddleware_Should403_WhenDecryptedAppIdMismatchesExpected`；`ShouldPass_WhenExpectedAppIdNotConfigured`。
- **验收**：2 例新增全绿；README 补 `ExpectedAppId` 配置说明。

---

## 7. 横切实施注意事项

### 7.1 多 TFM 与 AOT

- A2/A3/B6 新增的 Options 属性均为简单类型，配置绑定生成器兼容；**不得**使用 `required`。
- C3 的 `TryAddSingleton` 调用位于 `IApplicationBuilder.ApplicationServices`（`IServiceProvider` 接口），netstandard2.0 下 `TryAddSingleton` 扩展来自 `Microsoft.Extensions.DependencyInjection.Extensions` 命名空间，需确认 Webhook 工程 GlobalUsings 已含。
- 所有新代码遵循文件头版权注释、file-scoped namespace、`_camelCase` 私有字段。

### 7.2 测试基线与质量门禁

全部改动完成后必须通过：

```bash
pwsh ./scripts/verify-build.ps1 -StrictFormat
```

- 0 build 错误；0 `HTTPCLIENT0xx`/`MUD001-002`/`FORM0xx`/`AOT001-007` 诊断；
- AOT 严格模式（net8.0，9 个源项目逐个 `--no-incremental`）IL2026/IL3050 = 0；
- 每 (测试项目 × TFM) TRX 存在且 `Counters.total > 0`；
- 新增测试用例预估：P1 期 12 例、P2 安全期 9 例、P2 加固期 8 例，合计约 29 例。

### 7.3 兼容性与发布说明（CHANGELOG 要点）

| 变更 | 兼容级别 | 运维动作 |
|---|---|---|
| A1 生产环境 fail-fast | **Breaking（仅错误配置）** | 多实例部署必须注册 Redis 去重器 |
| A2 校验接线 | Breaking（仅错误配置） | 核对 `FeishuWebhook:RateLimit` 配置合法性 |
| A3 白名单 IP 认定升级 | 安全增强 | 反代部署核对 `AllowedSourceIPs` 与 `TrustedProxies` |
| A4/C5 NonceTtl 默认 5min→10min | 安全增强 | 无（Redis 侧未显式配置者自动受益） |
| B2 组合验证器不再调用 CheckNonce | 内部行为 | 无（接口保留） |
| C7 VerifyEventSubscriptionAsync 废弃 | 源码级告警 | 直接接线的宿主迁移到加密验证 |

### 7.4 排期与验收总表

| 编号 | 条目 | 优先级 | 依赖 | 状态 |
|---|---|---|---|---|
| A1 | Nonce 去重降级告警/阻断 | P1 | — | [ ] |
| A2 | RateLimit.Validate 接线 | P1 | — | [ ] |
| A3 | IP 白名单零信任统一 | P1 | — | [ ] |
| A4 | NonceTtl×Tolerance 断言 + 默认值收紧 | P1 | — | [ ] |
| B1 | 日志注入三处收敛 | P2 | — | [ ] |
| B2 | Nonce Check 移除（省 1 RTT） | P2 | — | [ ] |
| B3 | CancellationToken 贯穿 | P2 | — | [ ] |
| B4 | 重试计数竞态 | P2 | — | [ ] |
| B5 | AsyncLocal AppKey 清理 | P2 | — | [ ] |
| B6 | 解密超时可配置 | P2 | — | [ ] |
| C1 | 限流白名单归一化 | P2 | A3（共用 ClientIpResolver 后实施更顺） | [ ] |
| C2 | Retry-After 剩余秒数 | P2 | — | [ ] |
| C3 | 中间件 DI 生命周期 | P2 | — | [ ] |
| C4 | 并发服务竞态加固 | P2 | — | [ ] |
| C5 | 窗口边界文档收紧 | P2 | A4 | [ ] |
| C6 | SeqID 异步化 | P2 | — | [ ] |
| C7 | 明文验证废弃 + ExpectedAppId | P2 | — | [ ] |

---

## 8. 复核确认项（无需改动，留档备查）

以下审查中重点排查、**确认无问题**的检查点，记录在此避免后续重复怀疑：

1. **签名串拼接顺序** `timestamp+nonce+encryptKey+body` 与飞书官方规范一致（`SignatureValidator.cs:189`）；body 全程使用中间件单次读取的原始请求体，无反序列化-再序列化破坏字节的路径。
2. **FixedTimeEquals 长度早退**：仅泄露长度信息，对 64 字符定长 hex 签名无计时攻击价值；规范化仅作用于 header 侧且与内容无关。
3. **enforce 继承无放行分支**：全局默认 true、应用级 null 继承全局、生产启动期锁定全局与应用级两层（`FeishuWebhookOptionsValidator.cs:56-78`）；timestamp==0 / nonce 空在 enforce=true 时一律拒绝。
4. **异常路径 fail-closed**：验签/解密/去重的所有 catch 块返回拒绝或上抛；仅 `FeishuRedisException(Server)` 按契约转 503，Connection/Timeout 按 NonceFailureMode 降级（Reject/Allow 两个方向与 T-M2-10 测试契约一致，已逐行核对 `NonceValidator.cs:80-87,122-131`）。
5. **解密无 Padding Oracle**：Base64/AES/JSON 各类失败统一 400 Bad Request，响应文案收敛（WHF-17），阶段差异仅入服务端日志。
6. **Lua 原子性（ADR-4）**：事件去重三脚本、SeqID SETNX+ZADD+裁剪+EXPIRE、Nonce SET NX EX 均为单脚本原子；无先 GET 后 SET 的非原子路径。
7. **键布局**：全部经 `RedisKeyBuilder.Combine`（`:` 转义、256 上限、空前缀/通配符护栏），nonce/事件键均含 appKey 段，多租户隔离成立；`GetCachedCountAsync` 的 SCAN pattern 经 Combine 护栏。
8. **审计 fire-and-forget**：`SafeAuditAsync` 全捕获并经 Logger 记录，无未观察异常；日志中 EncryptKey/AppSecret 零出现，签名仅 8 位前缀。
9. **AOT**：热路径 JSON 全部走 `FeishuJsonAot` + 源生成 Context，无反射序列化；未发现配置 DTO 使用 `required`。
