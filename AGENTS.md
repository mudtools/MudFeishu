# AGENTS.md

Guidance for AI coding agents working on the MudFeishu codebase.

## Project Overview

MudFeishu is an enterprise-grade .NET SDK for Feishu (Lark) API integration, providing HTTP API clients, WebSocket real-time events, and Webhook handling.

## Build Commands

```bash
dotnet build Mud.Feishu.slnx                      # Build solution
dotnet build Mud.Feishu.slnx -c Release           # Release build
dotnet build Mud.Feishu/Mud.Feishu.csproj         # Build specific project
```

## Test Commands

```bash
dotnet test Mud.Feishu.slnx                       # Run all tests
dotnet test Mud.Feishu.slnx --logger "console;verbosity=detailed"
dotnet test Tests/Mud.Feishu.Abstractions.Tests   # Run specific test project
dotnet test Tests/Mud.Feishu.Abstractions.Tests --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests"  # Single test class
dotnet test Tests/Mud.Feishu.Abstractions.Tests --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests.HandleAsync_ShouldCallProcessBusinessLogic_WhenEventDataIsValid"  # Single test
dotnet test Mud.Feishu.slnx --collect:"XPlat Code Coverage"
```

## Lint and Format

```bash
dotnet format Mud.Feishu.slnx
```

Project uses `.editorconfig` for code style. `TreatWarningsAsErrors` is disabled.

## Quality Gate

```bash
pwsh ./scripts/verify-build.ps1                  # Full gate (build + diagnostics + tests + format)
pwsh ./scripts/verify-build.ps1 -ClearStaleCache # Also auto-clear a stale Mud.HttpUtils package cache
pwsh ./scripts/verify-build.ps1 -CacheCheckOnly  # Step 0 only (used by CI before restore)
pwsh ./scripts/verify-build.ps1 -StrictFormat    # Promote format diffs to a gate failure
```

The gate enforces: 0 build errors, 0 `CS1750`, 0 `NU1603` (version drift), and 0
`HTTPCLIENT0xx` / `MUD001-002` / `FORM0xx` / `AOT001-007` diagnostics.

Step 3 (`AotStrictMode` smoke, net8.0) builds the **9 source projects one by one** — never the whole
solution with `-f net8.0`: `Demos/` contains single-TFM projects (net9.0 / net10.0) that make MSBuild
fail with `NETSDK1005`. The step asserts **0 build errors** _and_ `AOT00x` / `IL2026` / `IL3050` = 0 —
asserting only the diagnostic counts is a false green when the build itself fails.
It must also pass `--no-incremental`: MSBuild's `CoreCompile` up-to-date check compares only input/output
timestamps and **not the csc command line**, so a strict-mode build issued right after the step-1 build
skips compilation entirely and reports 0 diagnostics (this was a long-standing false green; see
§F1 of the historical upstream report `documents/MudHttpUtils-2.0.7-升级验证报告.md`, which is **not
committed to this repo** — the conclusion is now enforced by the `--no-incremental` flag in
`scripts/verify-build.ps1`).

Step 4 runs `dotnet test` **per (test project, TFM)** with a dedicated TRX per combination, and asserts both
"TRX exists" and `Counters.total > 0` (a testhost that fails to start still writes an empty TRX with exit
code 1). TFMs are resolved via `dotnet msbuild -getProperty:TargetFrameworks`, because several test projects
inherit `<TargetFrameworks>` from `Tests/Directory.Build.props`. Combinations whose .NET runtime is not
installed are reported as skipped (this machine only has .NET 8/9/10, so all `net6.0` test runs are skipped).

CI runs the same checks: `Restore dependencies` is preceded by `-CacheCheckOnly`, and the `Build` log is
asserted for the diagnostic whitelist afterwards (`.github/workflows/dotnet-publish.yml`).

## Dependency version policy (Mud.HttpUtils)

This repo consumes `Mud.HttpUtils` **2.0.7** — the release line that carries the generator-side fixes:
inherited-interface clients forward `appAuthorizer` to the base generated class and no longer
re-declare the field (fixes a P0 where `UseApp`/`BeginScope` on inherited-interface clients always
threw under the MT-02 default-deny authorizer, plus ~1184 `CS0108` — neither for the
`_appAuthorizer` field nor for `[Query]`/`[Path]`/`[Header]` interface properties re-declared in
derived classes), the `CS0472` value-type array filter and the `CS8604` nullable path-parameter
escaping fixes, and the JsonContextScaffolder now emits `TypeInfoPropertyName` for duplicate
type-info names — SYSLIB1031. 2.0.7 is published on nuget.org, so `nuget.config` declares
**nuget.org only** (the temporary `MudHttpUtils-local` folder source pointing at
`D:/Repos/MudHttpUtils/artifacts` has been removed). To consume a newer component version: bump the
version in the `PackageReference`s and, optionally, sync `AGENTS.md` / the README dependency tables
(`README.md` / `README_EN.md` / `Mud.Feishu/README.md`) — the docs are **not** gated (a wrong version
number there has no runtime impact and must not block an upgrade). The contract guard
`TokenMultiAppContractGuards.MudHttpUtils_PackageReference_ShouldBeSingleVersion` only asserts that
every `Mud.HttpUtils*` declaration across the repo agrees on **one** version (guarding the
mixed-assembly `TypeLoadException` class of failure); it never hard-codes the version, so it needs no
edit on upgrade.

> **Packaging rules (component repo `D:/Repos/MudHttpUtils`)**: release packages must be produced by
> `pack.ps1 Release` (writes to `artifacts/`) **and published to nuget.org**. `pack_debug.ps1` produces
> **Debug** builds into `artifacts-debug/` for local debugging only — never publish them or use them
> for release verification. `pack.ps1` verifies after packing that (a) the package set matches the
> expected list (10 packages, including `Mud.HttpUtils.Xml` / `Mud.HttpUtils.JsonContextScaffolder`)
> and (b) every DLL inside every package is SHA256-identical to its `bin/<Configuration>/…` build
> output (prevents "Debug posing as Release" and stale-cache mis-packs).
> Details: `documents/MudHttpUtils-2.0.7-升级验证报告.md` (the report filename keeps its pre-release
> iteration label; the file itself is **not committed to this repo** — the rules above are enforced
> by `pack.ps1`'s post-pack validation and by `scripts/verify-build.ps1` step 0).

**During local component development** (fix not yet on nuget.org): temporarily re-add the local
folder source to `nuget.config`
(`<add key="MudHttpUtils-local" value="D:/Repos/MudHttpUtils/artifacts" />`), and remember that NuGet
keys the global package cache by `id + version`, so re-packing under the _same_ version does **not**
invalidate the downstream cache. Symptom: the component source is already fixed, but the build still
fails with `CS1750`. This anti-pattern occurred three times during 2.0.4
(23:16 / 20:59 / 21:23 re-packs), each polluting every downstream cache.
**Always bump the component version when packing.**

When you must refresh the cache manually (local component development), do it as a
three-step sequence:

```bash
dotnet nuget locals global-packages --clear
dotnet clean  Mud.Feishu.slnx -c Release
dotnet build  Mud.Feishu.slnx -c Release
```

The clean step is **mandatory**. An incremental build after a cache refresh leaves old
component assemblies next to new ones, which surfaces at runtime as:

```
System.TypeLoadException : Method 'set_Current' in type '...' does not have an implementation.
```

(Observed: 54 test failures from a stale incremental build; all green after `dotnet clean`.)

`scripts/verify-build.ps1` detects the stale cache (step 0, SHA256 comparison) and runs
`dotnet clean` automatically when it clears the cache (`-ClearStaleCache`).

## Target Frameworks

`netstandard2.0`, `net6.0`, `net8.0` (recommended), `net10.0`

Use conditional compilation: `#if NET7_0_OR_GREATER` for framework-specific code.

Notes:

- `IsAotCompatible` / AOT analyzers are enabled only for `net8.0+` (`Directory.Build.props`).
  `AOT006` is suppressed for `netstandard2.0` / `net6.0`, where the source-generated
  `JsonSerializerContext` files are not compiled.
- `IL2026` / `IL3050` must stay at **0** (`-p:AotStrictMode=true` is part of the quality gate).
  Rules for keeping it that way:
  - **Never** call the reflection `JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` /
    `Deserialize<TValue>(string, JsonSerializerOptions)` overloads. Use
    `Mud.Feishu.Abstractions.Utilities.FeishuJsonAot.Serialize/Deserialize` instead — it resolves
    `JsonTypeInfo` from the options on net8+ (AOT-safe) and transparently falls back to the
    reflection overload only when `options.TypeInfoResolver` is `null` (where the AOT path is
    impossible anyway).
  - Configuration binding is source-generated
    (`EnableConfigurationBindingGenerator=true`). Consequence: **configuration DTOs must not use
    `required`** — the generator constructs via `new T()` and emits `CS9035` otherwise. Validate in a
    `Validate()` method instead (see `FeishuAppConfig`).
  - `UnconditionalSuppressMessageAttribute` is `internal` on `net10.0` and cannot be referenced from
    user code; use `#pragma warning disable IL2026, IL3050` with a justification comment.
- `Tests/Directory.Build.props` and `Demos/Directory.Build.props` **shadow** the root
  `Directory.Build.props`. Any governance property (e.g. `WarningsAsErrors`) must be
  repeated there, otherwise tests/demos become gate blind spots.

## Project Structure

```
MudFeishu/
├── Mud.Feishu/               # Core HTTP API client
├── Mud.Feishu.Abstractions/  # Event handling abstractions
├── Mud.Feishu.Authentication/# User authentication middleware
├── Mud.Feishu.Redis/         # Redis distributed deduplication
├── Mud.Feishu.Webhook/       # Webhook event handling
├── Mud.Feishu.WebSocket/     # WebSocket real-time events
├── Tests/                    # Test projects (mirror source structure)
├── Demos/                    # Example applications
├── Directory.Build.props     # Global MSBuild properties
└── Mud.Feishu.slnx           # Solution file
```

## Code Style Guidelines

### File Header

All source files must start with the copyright header:

```csharp
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
```

### Namespaces and Imports

- Use **file-scoped namespaces** (C# 10+)
- Use `GlobalUsings.cs` for common imports
- Import order: System → Microsoft → Third-party → Project namespaces

### Naming Conventions

| Element                   | Convention       | Example               |
| ------------------------- | ---------------- | --------------------- |
| Classes, Records, Structs | PascalCase       | `FeishuEventHandler`  |
| Interfaces                | `I` + PascalCase | `IFeishuEventHandler` |
| Methods                   | PascalCase       | `HandleAsync`         |
| Properties                | PascalCase       | `SupportedEventType`  |
| Private fields            | `_camelCase`     | `_logger`             |
| Parameters                | camelCase        | `eventData`           |

### Types and Nullability

- **Nullable reference types enabled** - use `?` for nullable types
- Use `required` keyword for required properties (NET7+)

```csharp
public class FeishuAppConfig
{
    public required string AppKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

### Async/Await

- All async methods must end with `Async` suffix
- Include `CancellationToken` parameter with default value

```csharp
public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default);
```

### Error Handling

- Use `ArgumentNullException` for null parameters, `InvalidOperationException` for invalid state
- Use `ExceptionUtils.ThrowIfNull()` helper for validation

### Dependency Injection

- Use constructor injection, mark dependencies as `readonly`

```csharp
public class DefaultFeishuEventHandler : IFeishuEventHandler
{
    protected readonly ILogger _logger;
    public DefaultFeishuEventHandler(ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### Documentation

- Use XML documentation for public APIs with `<summary>`, `<param>`, `<returns>`, `<exception>` tags
- 文件下载类接口（返回 `Task<byte[]?>`）必须声明 `<exception cref="ApiException">`，
  并说明「HTTP 200 + JSON 错误体」这一残余风险（参见 `documents/ErrorHandling.md`）

### 接口查询参数规范（API-2）

**仅对新增接口生效，不迁移存量接口**（改造 259 个接口的公共签名收益不成比例）。

- 查询参数 ≥ 6 个时，优先采用**查询对象模式**：定义一个实现 `Mud.HttpUtils.IQueryParameter`
  的 DTO，接口签名写 `[Query] MyQuery query`。
  - 生成器会识别 `IQueryParameter` 实现并调用 `ToQueryParameters()` 整体展开
    （`Mud.HttpUtils.Generator/Generators/Implementation/Binders/QueryParameterBinder.cs`），
    不会把 DTO 序列化成单个查询值；该类型也不会触发 AOT005（不涉及 JSON 序列化）。
  - 收益：飞书新增可选参数时只改 DTO，接口签名与调用方不变。
- 参数 < 6 个时继续使用逐参 `[Query("name")]` 绑定，保持与存量接口一致的风格。
- 接口文档（`documents/`）需同步给出 DTO 的字段与对应查询参数名。

## Test Guidelines

- xUnit with FluentAssertions and Moq
- Tests mirror source folder structure
- Test class: `{ClassName}Tests`
- Test method: `{MethodName}_Should{Behavior}_When{Condition}`

```csharp
public class TokenUtilsTests
{
    private readonly TokenUtils _sut;
    public TokenUtilsTests() => _sut = new TokenUtils(new Mock<ILogger>().Object);

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenValidInput()
    {
        // Arrange
        var eventData = new EventData { /* ... */ };
        // Act
        var result = await _sut.HandleAsync(eventData);
        // Assert
        result.Should().NotBeNull();
    }
}
```

## Security Guidelines

- Never log or expose `AppSecret` or tokens
- Use `MaskSensitiveData()` when logging configuration
- Validate URLs to prevent SSRF

## Token & Multi-App Management (TMA Series)

- `FeishuAppManager` is a **Singleton**; it injects `IServiceScopeFactory` and creates a scope per `FeishuAppContext` to avoid Captive Dependency (TMA-13). Never resolve Scoped services directly from the root `IServiceProvider` inside `CreateAppContext`.
- Old `FeishuAppContext` instances enter the **retirement queue** (`FeishuAppContextRetirement`) and are Disposed after a grace period (default 300s). Never assume GC will reclaim them—Timer roots the object graph (TMA-07/TMA-24).
- `GetAllApps()` returns only **instantiated** apps; it does not trigger lazy initialization (TMA-08). Use `ConfiguredAppKeys` for all configured app keys without instantiation.
- `InvalidateTokenAsync` cascades to `ITokenStore` (memory + store double-clear, TMA-01). The `PurgeStoreOnTokenInvalidation` option (default `true`) can disable store purge.
- Store values must include an expiry timestamp (`{expireTimestampMs}|{token}` format via `TokenStoreHelper.EncodeStoredToken`). Values without expiry are treated as miss (TMA-15).
- `PerAppFeishuAuthenticationFactory` uses `ActivatorUtilities.CreateInstance<IFeishuAuthentication>`; if the registered type is an interface/abstract, it falls back to the DI singleton (TMA-09).
- New `IDisposable` members on `FeishuAppContext` **must** be disposed in `FeishuAppContext.Dispose()` to prevent leaks through the retirement queue (TMA-24 maintenance constraint).

## Multi-Tenant Deployment (TMA2-20)

Multi-tenant scenarios use the same multi-app infrastructure: each tenant maps to a `FeishuAppConfig` with its own `AppId`/`AppSecret`. Context switching via `UseApp`/`BeginScope` switches token and endpoint, but does **not** enforce tenant-level authorization. Business layers must implement custom authorization (e.g., Claim-based tenant validation middleware) to restrict access. The component-side `IAppAccessAuthorizer` provides error prompts when missing, but this SDK does not bundle an authorization implementation. For Redis deduplication, set distinct key prefixes (`EventKeyPrefix`/`NonceKeyPrefix`/`SeqIdKeyPrefix`) per tenant to prevent cross-tenant event conflicts.

## Contract Addenda (TMA2-22 / D8–D14)

1. **D8 键布局**：令牌键只允许由 `TokenKeyBuilder` 构造；变更须同步两后端 + 等价与回灌测试。禁止在各 Store 中内联键拼接逻辑。
2. **D9 阈值同源**：恢复阈值必须等于缓存有效性阈值（`TokenRefreshThreshold`）；禁止第二套阈值（如 `threshold/2`）。
3. **D10 凭据变更清库**：配置热更新（`ApplyConfigurationChanges`；TMF-04：原 `RebuildAppContext` 死代码已删除）检测到 `(AppId, AppSecret)` 变更时必须清除该 appKey 的持久化令牌——租户经 `ClearAsync`、用户经 `IFeishuUserTokenStorePurge.ClearAllUsersAsync` 能力探测（TMF-01 共享记账保证工厂新实例可清干净）。仅 `BaseUrl`/`TimeoutSeconds` 等变更保留令牌热迁移。
4. **事件去重分层所有权**：传输层（WebSocket `BinaryMessageProcessor`）归 **SeqID/序列验证器**；事件层（`FeishuEventMessageHandler` / Webhook）归 **EventId**。失败时两侧各自回滚本层幂等状态；Idempotent 业务键必须自带命名空间（禁止裸 `EventId`）。投递语义为 at-least-once，不追求严格一次。

## 配置面约定（R4）

MudFeishu 配置面以**嵌套 Options** 为唯一公共 API（旧扁平属性已删除）：

| 区域                | 权威形状                                                                                     | 配置节示例                                                                                                                  |
| ------------------- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| 应用 HTTP/熔断      | `FeishuAppConfig.TimeoutSeconds` / `HttpRetry` / `CircuitBreaker`                            | `FeishuApps:0:TimeoutSeconds`、`HttpRetry:MaxAttempts`、`CircuitBreaker:Enabled`                                            |
| 事件去重            | `FeishuDeduplicationOptions`（Mode/Profile/Event/Nonce/SeqId 三前缀）                        | `FeishuDeduplication:*`；双读期旧 `FeishuRedis:Event*` 等仍可绑                                                             |
| Redis 连接          | `RedisOptions.Connection` / `Advanced`                                                       | `FeishuRedis:Connection:ServerAddress`；旧扁平键由 `ApplyLegacyFlatConnectionKeys` 回填                                     |
| WebSocket 重连/证书 | `FeishuWebSocketOptions.Reconnect` / `Certificate`                                           | `FeishuWebSocket:Reconnect:*`、`Certificate:Mode`                                                                           |
| WebSocket 事件防线  | `FeishuWebSocketOptions.RejectEmptyEventIds` / `IgnoreUnknownEventTypes`                     | `FeishuWebSocket:RejectEmptyEventIds`（默认 true）、`IgnoreUnknownEventTypes`（默认 false，推荐 true；支持热更新，R2-P1-3） |
| Webhook 令牌刷新    | `EnableTokenBackgroundRefresh`（bool?；null=不干预基座）                                     | `FeishuWebhook:EnableTokenBackgroundRefresh`                                                                                |
| Webhook 超时        | 全局 `EventHandlingTimeoutMs` + 应用级同名键（正整数覆盖）；过渡闸 `LegacyGlobalTimeoutOnly` | —                                                                                                                           |

**日志**：**禁止**再新增任何「日志开关」类配置属性（历史上曾以 `Enable*Logging` / `Enable*Monitoring` 之类的名字出现，其中若干从未被运行时读取，属死配置）。日志级别统一由 `Logging:LogLevel:{Category}` 控制，例如：

```jsonc
"Logging": {
  "LogLevel": {
    "Mud.Feishu.WebSocket": "Information",
    "Mud.Feishu.Webhook": "Information",
    "Mud.Feishu.Abstractions.TokenManager": "Warning"
  }
}
```

**去重主路径仅消费**：`CacheExpiration` / `ProcessingTimeout` / `CleanupInterval` / `KeyPrefix` / `MaxCacheSize`。事件失败重试用 `FailedEventRetryOptions`，勿与去重键混淆。

**多租户**：`FeishuDeduplication` / Redis 去重键 **Event/Nonce/SeqId 三个 KeyPrefix 必须互异**（TMA2-20）。

**包安全跨校验**：`NonceTtl >= TimestampToleranceSeconds`（WHF-03）由宿主在同时引用 Webhook+Redis 时通过 `AddFeishuConfigurationConsistencyChecks` 注入 Webhook 容差后校验；Redis/Webhook 单包不做跨包类型耦合。

**配置审计**：`scripts/audit-config-keys.ps1` 扫描仓库内是否出现已删除的旧配置键/属性名，
分两档：`$strictPatterns`（精确的「已删除键名」，`-Strict` 与 CI 门禁以此为判据）与
`$warnPatterns`（R4 遗留的宽口径启发式，含已知误报，仅告警）。
脚本排除 `Demos/`（演示刻意保留旧形态）；`allowlist` **不再整文件豁免**，改为行内
`// audit-allow: <reason>`（匹配行或其上一行）。路径片段**必须用 `/` 书写**（脚本会对被扫描路径
做分隔符归一化）——用 `\` 时在 ubuntu-latest runner 上会全部失配，使排除/豁免在 CI 静默失效。
**新增死键模式必须与「删除该键」同阶段落地**，否则门禁会对仍受支持的配置面误报。

### 配置面治理（R5）

1. **禁止新增「日志开关」类配置属性**：日志级别只能由 `Logging:LogLevel:{Category}` 控制。
   历史上 `Enable*Logging` / `Enable*Monitoring` 之类开关多次出现「配了但运行时从不读取」，
   属死配置，已按 R5 逐步删除。
2. **每个公开配置属性必须有真实消费点**：新增属性必须同时接线（README 表格里的每一项都要能
   在源码中找到读取处，`Validate` / `ToString` **不算**）。由
   `Tests/**/ContractGuards/ConfigSurfaceContractGuards.cs` 锁定。
3. **配置 DTO 必须有配置节绑定**：仅 `services.AddOptions<T>()` **不会**绑定任何节。
   绑定必须走 `Configure<T>(o => section.Bind(o))`（**不要**用 `Configure<T>(IConfiguration)`
   重载——其反射绑定调用点无法被配置绑定源生成器拦截，会破坏 `IL2026`/`IL3050` 净零，见
   `FeishuServiceCollectionExtensions.cs` 中 `TokenRecoveryOptions` 的 AOT-3 记录），
   并按需显式注册 `IOptionsChangeTokenSource` 以保留热更新语义。
4. **两处「同名配置」不得并存**：同一个语义只能有一个配置节（真相源）。新增别名必须同时提供
   字段级回填与迁移表，并标注 `[Obsolete]` 的下线版本。
5. **行为变更必须可回滚**：使某项此前静默无效的配置**首次生效**，与「删除从未生效的开关」
   同属对外可见行为变更，必须写入 `documents/Configuration/CHANGELOG-Config.md` 与发布说明。
6. **审计脚本与契约守卫同批更新**：删除键 → 同一批次把模式加入 `$strictPatterns`；
   新增配置属性 → 同一批次补契约守卫登记。`audit-config-keys.ps1 -Strict` 是 CI 门禁的一部分。

**安全默认不得削弱**：wss、Strict 证书、`EnforceHeaderSignatureValidation`、`RejectEmptyIdentifiers`、三键前缀隔离、BaseUrl HTTPS 白名单。4. **D11 OAuth 失败语义**：`RefreshUserTokenAsync` 必须通过 `FeishuOAuthErrorClassifier` 区分可重试/不可重试错误。`invalid_grant` 等不可重试错误清除 refresh token 并返回 null；可重试错误抛异常。5. **D12 AppInstantiated 事件**：首次访问应用时必须触发 `AppInstantiated` 事件，后台令牌刷新订阅此事件实现增量注册。禁止启动期全量预热。6. **D13 两阶段事务化**：`OnConfigurationChanged` 必须拆为 Phase-P（`_configApplyLock` 外预清库——凭据变更是 IO，禁止锁内执行；TMF-02）、Phase-A 预构造（纯内存装配）、Phase-B 提交（`_lazyRebuildLock` 内仅引用交换）。禁止在锁内执行清库 IO 或完整装配。7. **D14 异常过滤白名单**：`CreateAppContext` / `TryGetApp` 的异常过滤必须使用 `IsTransientInitFailure` 白名单。禁止 `catch (Exception ex) when (ex is not OperationCanceledException)` 的宽过滤。8. **D15 补偿性去重操作**：通道层对 `IFeishuEventDeduplicator` / `IUnifiedDeduplicationMiddleware` 的 `Rollback*` / `Mark*` 调用属补偿/终态操作——落至后端调用点必须使用 `CancellationToken.None`（调用链可接收调用方 token，但补偿不得被取消中断），且补偿失败不得替换/吞没原始业务异常（记日志后继续，键停留 processing 由 ProcessingTimeout/TTL 兜底）。`TryMark*`（进入处理态）不受此约束，取消应即时传播。依据：R2-P0-1（Redis 后端入口 ThrowIfCancellationRequested + 已取消 token = 回滚失效 → 服务端重发被判重跳过并 ACK 200 → 事件丢失）。

## WebSocket 模块强制约束（R2：I13–I16 / D5 / I9）

适用于 `Mud.Feishu.WebSocket`。**新增/修改该模块代码必须逐条自检**；违反者由
`Tests/Mud.Feishu.WebSocket.Tests/ContractGuards/WebSocketContractGuards.cs`（7 条源码守卫）拦截。
权威语义见 `documents/WebSocket/架构与并发模型.md` §7。

1. **I13 连接终止路径穷尽占位**：任何"使 socket 不再被读取"的路径（服务端关闭帧、客户端主动断开、
   接收循环异常、**接收循环因取消退出**、`Dispose`/`DisposeAsync`）都必须经过
   `WebSocketConnectionManager.TryClaimDisconnected`；**主动断开必须先占位、后关闭握手**。
2. **I14 接收循环唯一性**：启动必须使用**原子占位**（`Interlocked.CompareExchange`），
   **禁止**"先检查后使用"地读 `_receiveTask`；循环 Task 必须登记到 `_receiveTask`
   （否则停机等待与存活判定都失效）。
3. **I15 令牌归属**：调用方 `CancellationToken` **只约束建连阶段**（握手 + 认证），
   不得链接为连接生命周期令牌。连接生命周期由客户端自持 CTS 控制。
4. **I16 配置双向约束**：数值配置必须同时校验下界与上界；任何
   `new CancellationTokenSource(<TimeSpan>)` / `Task.Delay(<TimeSpan>)` 的**非字面量**参数
   必须先经 `Core/TimeSpanGuards.cs` 钳制。`netstandard2.0` **禁用 `Math.Clamp`**（用 `Math.Min/Max`）。
5. **D5 日志最小暴露**：入站报文只能记"结构化字段 + `LogSanitizer.CleanMessage` 脱敏截断预览"；
   连接 URL 日志**整体剥离 query**（`Uri.GetLeftPart(UriPartial.Path)`）。
6. **I9 信号量释放**：一律不随 `Dispose` 释放；例外必须在类型 XML 注释中登记。
   当前**唯一例外**：`FeishuWebSocketConcurrencyService` 热更新时延迟释放**旧**信号量，
   阈值为 `max(60s, 2 × MessageHandlerTimeoutMs)`（见 `ResolveLegacySemaphoreRetention`）。
7. **F5 受控丢弃必须可计数**：任何"主动丢弃帧/消息"的新增路径都必须调用
   `FeishuMetricsHelper.RecordWebSocketFramesDiscarded(appKey, reason)`，
   且 `reason` **必须**取 `FeishuMetrics.DiscardReasons` 常量（告警规则按该维度聚合，裸字符串会静默改变指标序列）。
   新增原因时同步登记常量、`Readme.md` 告警表与守卫清单。
8. **I1 覆盖"两层锁"**：`WebSocketConnectionManager._connectionLock` **与** `FeishuWebSocketClient._connectLock`
   的持有期内都**不得**派发用户事件。客户端层的做法是"入队 + 出锁后按原顺序冲刷"
   （`BeginDeferConnectionEvents` / `FlushDeferredConnectionEvents`）——
   新增用户事件或改派发点时，必须确认它不在上述任一锁的持有期内，且冲刷路径自带异常隔离。
9. **FU-2 隐性耦合**：SeqID 去重键是裸 `SeqID`（无应用维度），前提是"同进程单客户端 + 只绑定默认应用"。
   **若引入多应用 WebSocket 装配，必须同时给去重键加应用维度**，否则会造成静默事件丢失。
   该耦合由守卫 `SeqIdDeduplication_ShouldStaySingleAppScoped_OrGainAppDimension` 守护，
   口径见 `documents/WebSocket/架构与并发模型.md` §8。

> 压力/长稳用例（`Category=Stress`）**不进全量门禁**：xUnit 的 `Trait` 不会自动排除用例，
> `scripts/verify-build.ps1` 步骤 4 已显式传入 `--filter "Category!=Stress"`。
> 手工执行：`dotnet test Tests/Mud.Feishu.WebSocket.Tests -c Release -f net8.0 --filter "Category=Stress"`。

## MSBuild Configuration

`Directory.Build.props`: `LangVersion`: 13.0, `Nullable`: enable, `ImplicitUsings`: enable

## Target Frameworks

`netstandard2.0`, `net6.0`, `net8.0` (recommended), `net10.0`

Use conditional compilation: `#if NET7_0_OR_GREATER` for framework-specific code.
