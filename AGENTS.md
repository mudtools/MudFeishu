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
fail with `NETSDK1005`. The step asserts **0 build errors** *and* `AOT00x` / `IL2026` / `IL3050` = 0 —
asserting only the diagnostic counts is a false green when the build itself fails.
It must also pass `--no-incremental`: MSBuild's `CoreCompile` up-to-date check compares only input/output
timestamps and **not the csc command line**, so a strict-mode build issued right after the step-1 build
skips compilation entirely and reports 0 diagnostics (this was a long-standing false green; see
`documents/MudHttpUtils-2.0.7-升级验证报告.md` §F1).

Step 4 runs `dotnet test` **per (test project, TFM)** with a dedicated TRX per combination, and asserts both
"TRX exists" and `Counters.total > 0` (a testhost that fails to start still writes an empty TRX with exit
code 1). TFMs are resolved via `dotnet msbuild -getProperty:TargetFrameworks`, because several test projects
inherit `<TargetFrameworks>` from `Tests/Directory.Build.props`. Combinations whose .NET runtime is not
installed are reported as skipped (this machine only has .NET 8/9/10, so all `net6.0` test runs are skipped).

CI runs the same checks: `Restore dependencies` is preceded by `-CacheCheckOnly`, and the `Build` log is
asserted for the diagnostic whitelist afterwards (`.github/workflows/dotnet-publish.yml`).

## Dependency version policy (Mud.HttpUtils)

This repo consumes `Mud.HttpUtils` **2.0.6** (source-generator fix release: inherited-interface
clients forward `appAuthorizer` to the base generated class and no longer re-declare the field —
fixes a P0 where `UseApp`/`BeginScope` on inherited-interface clients always threw under the
MT-02 default-deny authorizer, plus ~1184 CS0108; the JsonContextScaffolder now emits
`TypeInfoPropertyName` for duplicate type-info names — SYSLIB1031). Until 2.0.6 is published to
nuget.org, `nuget.config` temporarily re-adds the local folder source
(`D:/Repos/MudHttpUtils/artifacts`); remove that entry once 2.0.6 is live and the consumption
returns to nuget.org-only. To consume a newer component version: bump the version in the
`PackageReference`s and sync `AGENTS.md` / README dependency table / `TokenMultiAppContractGuards.ExpectedVersion`.

> **Packaging rules (component repo `D:/Repos/MudHttpUtils`)**: release packages must be produced by
> `pack.ps1 Release` (writes to `artifacts/`) **and published to nuget.org**. `pack_debug.ps1` produces
> **Debug** builds into `artifacts-debug/` for local debugging only — never publish them or use them
> for release verification. `pack.ps1` verifies after packing that (a) the package set matches the
> expected list (10 packages, including `Mud.HttpUtils.Xml` / `Mud.HttpUtils.JsonContextScaffolder`)
> and (b) every DLL inside every package is SHA256-identical to its `bin/<Configuration>/…` build
> output (prevents "Debug posing as Release" and stale-cache mis-packs).
> Details: `documents/MudHttpUtils-2.0.7-升级验证报告.md` (the report filename keeps its pre-release
> iteration label).

**During local component development** (fix not yet on nuget.org): temporarily re-add the local
folder source to `nuget.config`
(`<add key="MudHttpUtils-local" value="D:/Repos/MudHttpUtils/artifacts" />`), and remember that NuGet
keys the global package cache by `id + version`, so re-packing under the *same* version does **not**
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
3. **D10 凭据变更清库**：`RebuildAppContext` 检测到 `(AppId, AppSecret)` 变更时必须清除该 appKey 的持久化令牌。仅 `BaseUrl`/`TimeOut` 等变更保留令牌热迁移。
4. **D11 OAuth 失败语义**：`RefreshUserTokenAsync` 必须通过 `FeishuOAuthErrorClassifier` 区分可重试/不可重试错误。`invalid_grant` 等不可重试错误清除 refresh token 并返回 null；可重试错误抛异常。
5. **D12 AppInstantiated 事件**：首次访问应用时必须触发 `AppInstantiated` 事件，后台令牌刷新订阅此事件实现增量注册。禁止启动期全量预热。
6. **D13 两阶段事务化**：`OnConfigurationChanged` 必须拆为两阶段——Phase-A 预装配（锁外）、Phase-B 提交（锁内）。禁止在锁内完成完整装配。
7. **D14 异常过滤白名单**：`CreateAppContext` / `TryGetApp` 的异常过滤必须使用 `IsTransientInitFailure` 白名单。禁止 `catch (Exception ex) when (ex is not OperationCanceledException)` 的宽过滤。

## MSBuild Configuration

`Directory.Build.props`: `LangVersion`: 13.0, `Nullable`: enable, `ImplicitUsings`: enable

## Target Frameworks

`netstandard2.0`, `net6.0`, `net8.0` (recommended), `net10.0`

Use conditional compilation: `#if NET7_0_OR_GREATER` for framework-specific code.
