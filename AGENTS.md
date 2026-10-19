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

## Release and version tags (`v*`)

Releases are tag-driven: **the tag name is the single source of truth for the package version**. The tag form
is `v<major>.<minor>.<patch>` (e.g. `v3.0.3`).

- `.github/workflows/dotnet-publish.yml` matches `tags: ["v*"]` **only**. A bare `3.0.3` tag (the historical
  form, used up to `3.0.2`) does not trigger the workflow at all — no build, no publish, no warning.
- `publish-nuget` resolves the version from the tag (`Resolve package version`) and feeds it to both `build`
  and `pack` via `-p:Version=`; `Assert package set` then hard-asserts that the produced `.nupkg` set is
  exactly the 9 source packages (the contract `publish.bat` encodes in its `PROJECTS` list) and that every
  file carries that version. Keep `IsPackable=false` for everything outside those 9 sources
  (`Demos/Directory.Build.props`, `Tests/Directory.Build.props`): a packable demo still matches the
  `Mud.Feishu*` glob of the nuget.org Trusted Publishing policy, so it would be published silently.
- `Directory.Build.props`'s `<Version>` is **not** the release version; a mismatch with the tag is reported
  as a `::warning::` only (so "tag = release" stays a one-step operation). `publish.bat` without a version
  argument still packs whatever `<Version>` currently holds.
- `workflow_dispatch` with an **empty** `version` input runs semantic-release (`tagFormat` pinned to
  `v${version}`). Because the pre-`3.0.3` history has no `v*` release tag, that path is guarded: it refuses
  to run unless the highest existing `v*` tag is >= the version recorded at the top of `CHANGELOG.md`.
  Prefer the explicit alternatives — pass `version` to `workflow_dispatch`, or push a `v*` tag.
- Publishing uses NuGet Trusted Publishing (OIDC), not an API key: job `publish-nuget` declares
  `environment: release` + `permissions: id-token: write`, and must match the nuget.org policy (repository
  `mudtools/MudFeishu`, workflow `dotnet-publish.yml`, environment `release`, glob `Mud.Feishu*`). The only
  secret involved is `NUGET_USER` (the NuGet.org profile name) — the `release` environment holds no secrets,
  so it must be a repository/organization secret.

## Dependency version policy (Mud.HttpUtils)

This repo consumes `Mud.HttpUtils` **3.0.9** (HttpUtils and Generator **must stay on the same
version**; adaptation notes: `.docs/MudFeishu-MudHttpUtils-3.0.7-适配说明.md`; 3.0.9 升级方案与实施结论:
`.docs/MudFeishu-MudHttpUtils-3.0.9-3.2.0-升级完善方案-v1.md`) — the 3.0.x release line carries
**BC-27**: `IAppContextSwitcher` no longer declares
`UseApp(string)` / `UseDefaultApp()` / `BeginScope(string)`, and the generator no longer emits them
by default. Adaptation (plan: `.docs/MudHttpUtils-3.0.0-破坏性变更改造计划.md`, §10):

- `IFeishuAppContextSwitcher` **re-declares** the three legacy members with `[Obsolete]`
  (upstream "self-declared ⇒ exempt" mechanism keeps 188+ generated classes emitting them).
  Do **not** delete them before the next major version, and keep their signatures verbatim —
  a mismatched signature drops the exemption and turns into `HTTPCLIENT024` + `NotSupportedException`
  placeholders. The contract guard `GeneratedAppSwitchContractTests` (in `Tests/Mud.Feishu.Tests`)
  asserts the five switch members reflectively on all generated classes.
- New code must use `IAppScopeSwitcher.UseAppScope` / `UseDefaultAppScope`; for the
  "guarded + switch-and-stay" semantics use `IAppContextHolder.SwitchToApp(appKey, appManager, sp)`
  (upstream `SW-15`).
  `Demos/` still consume the **published** `Mud.Feishu 3.0.0` package (which depends on
  `Mud.HttpUtils 2.0.8`), so demo `BeginScope("hr-app")` calls are intentionally untouched until the
  rebuilt package ships.
- **BC-30 已完成（上游 3.0.8 移除；下游同批适配）**：`IAppManager<T>` 的 `GetWebApi` / `GetDefaultWebApi` / `RegisterSwitcherFactory` 三成员已从上游 **物理移除**；`FeishuAppManager` 中
  对应的两个重写、`new` 遮蔽与显式实现**已同批删除**。**必须**保持「引用升级 3.0.8 与三成员删除
  同批落地」（历史不变量，针对 3.0.8 的 BC-30 批次；本仓当前依赖已升至 3.0.9）——若引用未指向 3.0.8
  而先删重写，基类实现会**静默回退到无守卫路径**（非编译错误，属安全回退）。迁移路径：DI 解析 `IAppScopeSwitcher` + `UseAppScope` / `UseDefaultAppScope`
  （自动归还），或 `IAppContextHolder.SwitchToApp(appKey, appManager, sp)`（受控切换并保持）。
- **F-9（3.0.7 起）元包解耦**：`Mud.HttpUtils` 元包**不再传递** `Mud.HttpUtils.Resilience`。
  `Mud.Feishu.Abstractions` 的 `AddFeishuApp` 装配链使用弹性策略（per-app 重试/超时/熔断），
  故已**显式安装** `Mud.HttpUtils.Resilience`（版本与全仓 `Mud.HttpUtils*` 单版本守卫一致，
  **不得**设 `PrivateAssets`——`AddFeishuApp` 运行期需要加载该程序集）。
- **C-2 已采纳（3.0.3 起；U-1 已回收，上游 3.0.10 Batch 1）**：令牌存储值**写入**为官方相对秒
  `{expiresInSeconds}|{token}`，**读取**双读兼容旧「绝对毫秒戳」存量值（`TokenStoreHelper` 投影回
  绝对毫秒戳，消费方契约不变）。双读判别已上收官方 `MigrationTokenStoreCodec`——
  `FeishuTokenStoreCodec` 自 U-1 起为**薄包装**（显式传飞书阈值 `AbsoluteMsThreshold`=10^10；
  注意上游默认常量实为 10^11，其 XML 注释写「10^10」属上游文档笔误；两阈值在两真实形态下等价），
  **不得**再在下游恢复私有双读实现。**禁止**把存储值格式改回私有形态，也**禁止**在新写入路径
  直接拼 `{expireTimestampMs}|`——一律经 `TokenStoreHelper.EncodeStoredToken / DecodeStoredToken /
  TryDecodeExpiry`。TMF-05 CAS 是**明文比较**（解码后比对），不要再改回编码值整串比较。
  过期态无法用相对秒新格式表达（剩余秒 ≤ 0 归一为 0 = 无 TTL 信息 → Redis 回落 30 天）；
  测试模拟过期须用旧格式字面量。
- **U-3 已采纳（上游 3.0.10 Batch 1）跨进程刷新去重租约**：`TokenManagerBase.RefreshLease` 钩子
  （默认 null = 零行为变化）。`Mud.Feishu.Redis` 注册官方 `RedisRefreshLease`
  （`SET NX PX` + Lua compare-and-del，键前缀 `{TokenKeyPrefix}:lease` 随环境段隔离）；
  `DefaultFeishuTokenManagerFactory` 经 DI 解析注入三个管理器（Tenant/App/User）。
  未启用 Redis 时 `RefreshLease` 恒 null，单实例行为逐位不变。自建去重器可经
  `ITokenRefreshLease` 薄包装接入，**勿**绕过钩子另起炉灶。租约键 = `{TokenKeyPrefix}:lease:{管理器键含AppKey}:{scopeKey}`（默认 `feishu:lease:...`）。
- **U-4 已采纳（上游 3.0.10 Batch 2）按应用清库能力面**：`Mud.Feishu.Redis.RedisTokenStore` 实现
  `ITokenStoreAppPurge`（实例键空间即应用边界，`PurgeAppAsync` 与 `ClearAsync` 共用清库核心）；
  `PurgeGateTokenStoreDecorator` / `EncryptedTokenStore` 透传该能力面。宿主清库统一走官方入口
  `TokenStorePurgeExtensions.PurgeAppTokensAsync(store, appKey)`（能力探测穿透装饰器栈）；
  **勿**在下游新写「SCAN + 键解析 + 删除」的私有清库路径。
- **U-5 已采纳（上游 3.0.10 Batch 2）多命中外部标识反查**：`FeishuAppManager.TryResolveAllAppKeys`
  重写——宿主提供者实现 `IMultiAppLookupProvider` 时委托多命中（`HasApp` 过滤 + 去重，不物化懒加载），
  仅单结果面时回退 F-4 语义。注意：基类多命中实现消费的是**基类构造注入**的提供者字段，而飞书构造 ①
  不向基类注入（提供者由本类字段持有），**勿**删除本重写、也**勿**把提供者改注到基类构造（会绕过本类兜底链）。
- **U-7 已采纳（上游 3.0.10 Batch 3）用户维度原生 CAS**：`RedisUserTokenStore` 实现
  `IUserTokenStoreCompareExchange`（Lua 单脚本内「取存储值 → 按首个 `|` 切出明文比较 → 写入/删除」，
  无读取-写入窗口）；`UserTokenManager` 的 TMF-05 清库路径按**原始 store** 能力分派：
  有原子能力走 CAS，无能力保留 C-2「解码明文比较 + `RemoveAsync`」。
  **三条约束**（勿破）：①能力探测必须落在**未装饰的原始 store** 上（`_compareExchangeStore`）——
  门装饰器**刻意不**声明 `IUserTokenStoreCompareExchange`，否则内层未实现时探测「假命中」，
  官方非原子回退按「存储原值 vs 期望明文」整值比较，对飞书编码值恒失配 ⇒ 清库静默失效；
  ②**禁止**把无能力 store 的路径改走官方非原子回退（同上恒失配）；③CAS 属写/删族，
  门只拦读，故直调原始 store 不违 D10 门语义。
- **U-2 读取侧已兼容 / 写侧不切换（上游 3.0.10 Batch 3）**：上游 v2 版本化信封
  （`v2:{expiresInSeconds}|{token}` / `v2:expired|`，`VersionedTokenStoreCodec`，默认关闭）——
  `TokenStoreHelper` 已支持 v2 读取（三态归一 + TTL 推导），宿主经官方 store +
  `VersionedTokenStoreCodec` 组合写入的 v2 值可被飞书消费侧正确消费。
  **写侧仍单写 v1 相对秒形态**（C-2：默认形态不得漂移）；切换写侧须走两阶段纪律
  （先升级全部读取方、再开写入开关），且需同步评估 `RedisUserTokenStore.SetRefreshTokenAsync`
  的 TTL 推导路径。
- **U-6/U-8 不采纳登记**：①U-6 分隔符键编码——飞书键布局（env 段 + glob 转义 +
  `{prefix}:{appKey}:token`）与官方归属键不同构，`TokenKeyBuilder` 保留单一来源（C-3 对账口径）；
  ②U-8 官方 AppInstantiated——与既有同名公共事件遮蔽冲突（双通道双发 + 公共 API 破坏），
  登记 4.0 择期迁移。**勿**在后续任务中「顺手」采纳这两项。
- **F-4 已采纳**：`FeishuAppContext.ExternalIds` 暴露 `app_id → appKey` 快照；
  `FeishuAppManager.TryResolveAppKey` = 基类内置索引（实例化后自动登记）+ DI 解析的宿主
  `IAppLookupProvider` 兜底，fail-closed。新增外部标识一律走 `ExternalIds` 快照，勿自建字典缓存。
- **C-6 分层共存**：`Mud.Feishu.Redis` 已引用官方 `Mud.HttpUtils.Redis`。官方
  `RedisTokenStore` 是「明文入参 + store 内部 codec 编解码」约定，飞书管线（桥接器）是
  「直存编码值」约定——**两者不可互换直插**（混用双重编码）。官方侧对接飞书存量数据的
  唯一受支持方式：注入 `FeishuTokenStoreCodec.Instance` 作其 codec。

**Upstream contract-change channel（契约变更传导通道）**：上游 `Mud.HttpUtils` 的
`CHANGELOG.md`「契约变更（Contract Changes）」专章（C-1 ~ C-7）与
`.docs/下游约束对账清单.md`（A/B/C 三系列逐条标注本仓 workaround 的已失效/仍有效/待裁决状态）
是本仓约束的唯一权威来源。涉及令牌存储、键构建、应用管理器继承、Redis 分层的约束调整，
**必须先查该两份文档再动代码**，防止约束漂移。3.0.7 适配时点对本仓的逐条结论见
`.docs/MudFeishu-MudHttpUtils-3.0.7-适配说明.md`。

Mud.HttpUtils **3.0.9 is consumed from nuget.org**: the `MudHttpUtils-local` folder source used during the
3.0.9 verification cycle (`D:/Repos/MudHttpUtils/artifacts` — **Release** output of `pack.ps1 Release`;
the `artifacts-debug` Debug pack must never be consumed) has been **removed** from `nuget.config` after
`3.0.9` shipped on nuget.org, and `dotnet restore` resolves the published package (after clearing the
`mud.httputils*` `3.0.9` cache entries: `restore --no-cache` → `clean` → `build`).
To consume a newer component version: bump the
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

### 接口文件与接口命名（Interfaces）

`Mud.Feishu/Interfaces/{Module}/` 下的 SDK 接口有**两套并行的命名**——文件名与接口名**不相同**，必须同时满足：

| 令牌形态   | 文件名（3 个或 1 个）                                                                                                | 接口名                                          | 结构                                                                                                                                                        |
| ---------- | -------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 双令牌     | `IFeishuV{n}{Domain}{Resource}.cs`<br>`IFeishuV{n}{Domain}{Resource}_Tenant.cs`<br>`IFeishuV{n}{Domain}{Resource}_User.cs` | `IFeishuV{n}{Domain}{Resource}`<br>`IFeishuTenantV{n}{Domain}{Resource}`<br>`IFeishuUserV{n}{Domain}{Resource}` | 基接口 `[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]`（**无** `RegistryGroupName`），方法全部写在基接口；`_Tenant`/`_User` 为**空**派生接口，标 `[HttpClientApi(..., RegistryGroupName = "{Module}", InheritedFrom = nameof(FeishuV{n}{Domain}{Resource}))]`，`_User` 额外 `: ICurrentUserId` |
| 仅租户令牌 | `IFeishuV{n}{Domain}{Resource}_Tenant.cs`                                                                             | `IFeishuTenantV{n}{Domain}{Resource}`           | 方法直接写在该接口，`: IFeishuAppContextSwitcher`；`[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "{Module}")]`（**不带** `IsAbstract`/`InheritedFrom`） |
| 仅用户令牌 | `IFeishuV{n}{Domain}{Resource}_User.cs`                                                                              | `IFeishuUserV{n}{Domain}{Resource}`             | 同上，但接口额外 `: ICurrentUserId`                                                                                                                         |

要点：

- 令牌词 `Tenant`/`User` 在**接口名**中紧随 `IFeishu`（位于 `V{n}` 之前），在**文件名**中改为**后缀** `_Tenant`/`_User`。**禁止**出现「无令牌后缀的单令牌接口文件」（即文件名与接口名只差首字母 `I` 且不构成双令牌基接口）。
- `{n}{Domain}{Resource}` 忠实映射 OpenAPI 路径（如 `hire/v1/jobs` → `HireJob`）；`{Module}` 与 `FeishuModule` 枚举、`Interfaces/` 目录名一致。**大模块例外按下方「接口子域合并」执行。**
- 令牌统一 `[Token(FeishuTokenTypes.TenantAccessToken 或 UserAccessToken, Name = Consts.Authorization)]`。
- 生成的客户端类名 = 接口名去掉首字母 `I`（`IFeishuTenantV1HireJob` → `FeishuTenantV1HireJob`）；`Add{Module}WebApiHttpClient` 由生成器按 `[HttpClientApi]` 自动产出。
- 接口文档文件名同样跟随接口名去 `I`：`documents/{Module}/FeishuTenantV{n}{Domain}{Resource}.md`；测试类名为 `{接口名}Tests`（如 `IFeishuTenantV1ApprovalMessageTests`）。
- 新增接口后需同批更新：`FeishuModule` 枚举、`FeishuServiceBuilder` 注册器 + `Add{Module}Api()`、`FeishuJsonResolverExtensions` 的 JsonContext 合并、`scripts/GenerateJsonContext.ps1` 再生成。

### 接口子域合并（方案 B，Hire 为参照实现）

大模块禁止按 OpenAPI 资源「一资源一接口」无限拆分（Hire 曾拆到 21 个接口、其中 15 个仅含 1 个方法；全仓库一度 307 个接口）。接口按 **「模块 × 令牌类型 × 功能子域」** 组织，每个（模块 × 令牌）组合保留 **3~6 个**子域接口（Hire 租户态现为 8 个，后续新增资源并入既有子域，分组以飞书文档章节 candidate-management / recruitment-related-configuration 等为准）：

| 模块 | 子域接口 | 合并范围 |
| ---- | -------- | -------- |
| Hire | `IFeishuTenantV1HireJob` | 职位、职位类别、职能分类、职位模板、职位发布记录、职位广告 |
| Hire | `IFeishuTenantV1HireJobRequirement` | 招聘需求（创建/更新/查询/删除/需求模板） |
| Hire | `IFeishuTenantV1HireInterview` | 面试轮次类型、面试反馈表、面试登记表模板、面试官 |
| Hire | `IFeishuTenantV1HireOffer` | Offer 申请表、Offer 自定义字段、Offer 审批模板 |
| Hire | `IFeishuTenantV1HireSetting` | 招聘流程、科目、信息登记表、人才标签、地点、角色、用户角色 |
| Hire | `IFeishuTenantV1HireCandidate` | 内推信息与内推官网职位、招聘官网（列表/推广渠道/官网用户/官网职位）、官网投递与投递任务、官网申请表模板、人才管理（人才池/文件夹/标签/黑名单/组合创建更新/入职状态）、投递流程管理（面试信息与评价记录、Offer、背调订单、三方协议、入职与员工）、人才备注、评估/阅卷/面试任务、简历来源 |

规则：

- 文件名仍遵循上表令牌后缀规则；子域接口的 `{Resource}` 对应**飞书文档的资源分组**（如 `interview-settings` → `Interview`、`offer-settings` → `Offer`），不再要求一个接口只映射单一 OpenAPI 路径；单资源接口仍按路径映射。
- 合并接口内方法名必须带资源前缀（`GetJobTypeListAsync` 而非 `GetListAsync`），并保证**全接口唯一**（合并前先做重名检查）。
- `Mud.Feishu.DataModels/{Module}` 按同一子域分文件夹组织（如 `DataModels/Hire/{Common|Job|JobRequirement|Interview|Offer|Setting}/`），**命名空间保持模块级不变**（`Mud.Feishu.DataModels.Hire`）；跨子域共享的基础类型（`I18n`、`IdNameObject`、`CommonSchema*` 等）放 `Common/`。
- 新模块创建时直接按此规则分组（每模块 3~6 个接口），不再逐资源建接口。

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

#### 注释纪律（Comment discipline）

源码注释只承载两件事：**对外契约**（消费者需要的 API 语义）与**不可从代码推断的约束**
（锁序、幂等、顺序依赖、AOT 抑制理由、安全语义）。**决策史一律不进源码**——上游版本适配
（`BC-27`/`U-7`）、缺陷复盘（"修复前…"/"原实现…"）、任务与阶段编号（`TMF-02`/`Phase-A`）、
仓库内部文档路径（`.docs/`、`AGENTS.md`）属于过程记录，应落在 `documents/决策档案/`。

判据：删掉这条注释，维护者会不会踩坑？回答"不会"即应删除。同一约束不得在 L1 与 L2 重复表述。
`/// <remarks>` 会进入 NuGet 包与 IDE 提示，属**对外文档面**。

- **完整规范（判据、R-01~R-12、正反例、必须保留的约束白名单）**：`documents/代码注释规范.md`
- **门禁**：`pwsh ./scripts/audit-comments.ps1 -Strict`（已接入 `scripts/verify-build.ps1` 与 CI）。
  存量冻结在 `scripts/comment-baseline.txt`，**只减不增**；新增命中即失败。确属必须保留的约束，
  在同行或上一行写 `// comment-allow: <原因>`（只允许逐行豁免）。
- **决策档案**：`documents/决策档案/`（编号全集见其 `编号索引.md`）

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
