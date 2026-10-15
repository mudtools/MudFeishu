# AGENTS.md

Guidance for AI coding agents working on the MudFeishu codebase. Enterprise-grade .NET SDK for
Feishu (Lark): HTTP API clients, WebSocket real-time events, Webhook handling.

## Commands

```bash
dotnet build Mud.Feishu.slnx                       # Build solution
dotnet build Mud.Feishu/Mud.Feishu.csproj          # Build specific project
dotnet test  Tests/Mud.Feishu.Abstractions.Tests   # Test project (add --filter "FullyQualifiedName~X" for one test)
dotnet format Mud.Feishu.slnx                      # Lint/format (.editorconfig; TreatWarningsAsErrors off)
```

## Quality Gate

```bash
pwsh ./scripts/verify-build.ps1                  # Standard: full build + diagnostics + tests + format
pwsh ./scripts/verify-build.ps1 -Fast            # Daily loop: incremental build + tests only
pwsh ./scripts/verify-build.ps1 -Full            # Release/nightly: + Timing tests + strict format
pwsh ./scripts/verify-build.ps1 -CacheCheckOnly  # Step 0 only (CI, before restore)
```

| Profile | Build | Diagnostics (steps 2–3) | Tests | Format |
|---|---|---|---|---|
| `-Fast` | incremental | **skipped** (generators skipped ⇒ counts structurally 0 = false green) | `Category!=Stress&Category!=Timing` | skipped |
| Standard | `--no-incremental` | full (MUDFT/SDKT/RS/AOT baselines) | `Category!=Stress&Category!=Timing` | warn-only |
| `-Full` | `--no-incremental` | full | all incl. `Timing` | strict (`-StrictFormat`) |

Enforced: 0 build errors, 0 `CS1750`, 0 `NU1603`, 0 `HTTPCLIENT0xx` / `MUD001-002` / `FORM0xx` /
`AOT001-007`. Two false-green rules are load-bearing (AGENTS.md has recorded the
"只断言诊断计数而构建失败即假绿" lesson): (1) the standard build **must** use
`--no-incremental` (incremental skips source generators ⇒ zero diagnostics); (2) AOT strict-mode
step builds the **source projects one by one** (glob `Mud.*`; `Demos/` has single-TFM projects ⇒
`NETSDK1005` on `-f net8.0`) and asserts **both** 0 errors **and** `AOT00x`/`IL2026`/`IL3050` = 0 — diagnostic
counts alone are a false green when the build itself failed. Step 4 runs per (project, TFM) with a
dedicated TRX and asserts `total > 0` (a dead testhost writes an empty TRX with exit code 1).
TFMs resolve via `dotnet msbuild -getProperty:TargetFrameworks` (test projects inherit from
`Tests/Directory.Build.props`). CI mirrors this: `-CacheCheckOnly` before restore, then the build
log is asserted against the same whitelist (`.github/workflows/dotnet-publish.yml`).

**Source-project discovery is glob-based, never a hardcoded count** (v2.3): `verify-build.ps1`
step 3 enumerates root directories matching `Mud.*` (was `Mud.Feishu*`), so the new `Mud.AI.*`
package family is AOT-smoked automatically. Any "N source projects" count in docs/CI is
**stale by construction** — do not reintroduce one.

## Dependency version policy (Mud.HttpUtils)

- `Mud.HttpUtils` + `Mud.HttpUtils.Generator` **must stay on the same version** — the
  「同一组件单版本」contract, enforced by
  `TokenMultiAppContractGuards.MudHttpUtils_PackageReference_ShouldBeSingleVersion` (asserts one
  version repo-wide, never hardcodes the number — bumps need no guard edit).
- **Pinned: `3.0.6`** — published on nuget.org (2026-10-11). `nuget.config` restores from
  **nuget.org only**; the intake-time local source was deleted. Never pre-bump without a resolvable
  source (nuget.org **or** a temporary local folder source) — restore fails outright otherwise.
  Intake trail (Release-pack verification, cache-clear discipline):
  `documents/AIAgent/MudFeishu-AI-per-tool-affordance-组件侧生成器改造方案.md`.
- ⚠️ **Never reuse `3.1.0`** (prepared but never released; occupied in the component CHANGELOG).
- **BC-27 (3.0.x line)**: `IAppContextSwitcher` no longer declares `UseApp`/`UseDefaultApp`/
  `BeginScope`. Adaptation: `IFeishuAppContextSwitcher` **re-declares** the three legacy members
  `[Obsolete]` — do not delete before the next major, keep signatures verbatim (a mismatch drops the
  upstream "self-declared ⇒ exempt" status ⇒ `HTTPCLIENT024` + `NotSupportedException` placeholders;
  `GeneratedAppSwitchContractTests` asserts the five switch members reflectively). New code uses
  `IAppScopeSwitcher.UseAppScope`/`UseDefaultAppScope`, or
  `IAppContextHolder.SwitchToApp(appKey, appManager, sp)` for switch-and-stay semantics.
- `Demos/` still consume the published `Mud.Feishu 3.0.0` package (depends on `Mud.HttpUtils 2.0.8`),
  so demo `BeginScope(...)` calls are intentionally untouched until the rebuilt package ships.
- Packaging (component repo): release = `pack.ps1 Release` → nuget.org only. `pack_debug.ps1` output
  is local-debug only, never for release baselines. NuGet caches by `id + version`: re-packing the
  same version does **not** invalidate downstream caches (symptom: source fixed but build still
  `CS1750`) — **always bump when packing**. Manual refresh = clear global-packages → `dotnet clean`
  (mandatory; stale incremental builds surface as `TypeLoadException`) → rebuild.
- Tool surface is generated **only** by the in-repo engine `Mud.AI.Generator` (v2.3 P1-E: migrated
  from the component-side `Mud.HttpUtils.Generator`; tuning window is repo-closed, re-upstreaming
  is P3). `SingleToolSurfaceEngineContractGuards` locks that: (1) the only source-level
  `[Generator]` host is `Mud.AI.Generator` and it is registered in `Mud.Feishu.slnx`; (2) no
  project except that engine **and its test project** references `Microsoft.CodeAnalysis.CSharp`
  (the test project drives the generator via `CSharpGeneratorDriver` — driving ≠ hosting a second
  engine); (3) **no project may reference both** the in-repo engine and the component package
  (same hintName products ⇒ CS8785/CS0101). Each assertion has a `[Theory]` self-test.
  `FeishuToolProfile.cs` (`Mud.Feishu.AI.Tools/SdkProfile/`) is the single source of Feishu naming
  facts, linked into `Tests/Mud.Feishu.AI.Tests` (profiles are per-compilation).
  `[AgentTool]`/`[ToolParameter]`/`[ToolHandler]` live in **`Mud.AI.Abstractions`**
  (`Mud.AI.Abstractions.Tools[.Handlers]`) — the bottom of the dependency graph, so non-owner
  declaring assemblies (e.g. AI.Tests samples) can see them without referencing the owner
  assembly (avoids CS0433 ambiguity).

## AI runtime layering (P1-H1/H2/H3, 2026-10-14)

The AI family is **unpublished**, so the reusable mechanism moved to the neutral packages in one
hard cut (no `[Obsolete]` forwards, no shims). Four invariants are mechanically locked:

- **MAF allow-list = `{Mud.Feishu.AI, Mud.AI.Agent}`** (relaxed from "only `Mud.Feishu.AI`" in
  P1-H3, because `Mud.AI.Agent` *is* the AI runtime and its session facade needs `AIAgent`).
  Relaxation is **convergent, not open**: `Mud.AI.Abstractions` must stay MAF-free (compile-time
  contract layer, consumed by the source generator) and every non-AI `Mud.Feishu*` project still
  must not declare MAF directly. Guarded by
  `AgentContractGuards.AgentsAi_PackageReferences_ShouldBeDeclaredOnlyInAllowListedProjects` +
  `MudAiAbstractions_ShouldNotDeclareMafPackages`; MAF version stays single-point 1.20.0.
- **Bidirectional neutrality lock**: core SDK ↛ `Mud.AI.*` (existing
  `CoreSdkProjects_ShouldNotDependOnMudAiFamily`) **and** `Mud.AI.*` ↛ any `Mud.Feishu*`
  (`MudAiFamily_ShouldNotDependOnFeishuProjects`). The session-storage port exists precisely
  because the "obvious" shortcut (let `Mud.AI.Agent` reference
  `Mud.Feishu.Abstractions.IConversationStore`) compiles, passes tests, and makes P3 upstreaming
  impossible. Instead: neutral port `Mud.AI.Abstractions.Conversations.ISessionStore` +
  `Mud.Feishu.AI.Conversations.ConversationStoreSessionAdapter` (internal). The published SDK
  packages (`Mud.Feishu.Abstractions`, `Mud.Feishu.Redis`) are **byte-untouched**.
  The lock has a **third axis** (2026-10-14): `Mud.AI.*` must not declare Mud **components**
  either — `AgentContractGuards.MudAiFamily_ShouldNotDeclareMudHttpUtilsComponent` (scope = the exact
  `Mud.HttpUtils` component id; sub-packages `.Attributes`/`.Abstractions` would need an explicit
  scope widening + justification). Same "ports, not the obvious shortcut" reasoning:
  `Mud.AI.Agent.ToolErrorClassifier` used to pattern-match `Mud.HttpUtils.ApiException`, which made
  P3 upstreaming impossible and chained the neutral package to the component's single-version line.
  Now: neutral port `Mud.AI.Abstractions.Tools.Errors.ITransportFault` (`int? StatusCode` only) +
  `Mud.Feishu.AI.AgentTools.FeishuTransportFaultAdapter` (internal), with the wiring locked
  end-to-end by `ToolErrorContractGuards.TransportForbidden_ShouldBeClassifiedAsAuthorization`
  (behaviour, not source text — a missing adapter silently degrades 403 to `retryable/transient`).
  The **same cut covers product business codes**: `ToolErrorClassifier.ClassifyCode` +
  `ForbiddenApiCodes`/`RetryableApiCodes`/`ToolErrorCodeMeta` left the neutral package; the code table
  now lives in `Mud.Feishu.AI.AgentTools.FeishuApiErrorVocabulary` and reaches the neutral layer as a
  *reading* (`ToolApiFailure`, carried by `ToolApiOutcome<T>.Failure` →
  `ToolPagination.PagedFetchResult.Failure` → payload/`suggestions`/hint). Locked by
  `ToolErrorContractGuards.ApiCodeReading_ShouldTravelFromReaderToPayload` (end-to-end: reader → payload)
  plus the tripwire `NeutralPackageProductFactGuards` (no known product code literals in
  `Mud.AI.Agent` source; a sentinel, not a proof).
- **`FeishuAgent : DelegatingAIAgent`** (MAF-provided). Do not hand-write `IdCore` / the three
  session hooks / `GetService` again — `FeishuAgent_ShouldDelegateIdentityToInnerAgent` and
  `FeishuAgent_GetService_ShouldReturnOuterInstance` lock the semantics (reflection + behaviour,
  not source text).
- **Product facts never enter the neutral packages**: state keys (`feishu.agent.*`) and the
  guidance budget (`FeishuGuidanceBudget.MaxGuidanceLength`) stay on the Feishu side;
  `GuidanceComposer.Compose(..., maxGuidanceLength)` and `AgentSessionStore(..., historyStateKey, ...)`
  take them as *parameters*.

`Mud.AI.Agent` now carries `ChatTokenCounter` (public), `GuidanceComposer` and `AgentSessionStore`;
`Tests/Mud.AI.Agent.Tests` is their owning test project (registered in the slnx, auto-discovered by
`verify-build.ps1` step 4). Guard evidence must live with the implementation — moving code without
moving its guard anchors is how a guard silently starts asserting something else.

## AI neutralization N4–N6 closure (2026-10-14)

The neutralization plan (`.docs/AI/MudFeishu-AI-公共功能中立化重构方案-v2.md`, status: ✅ complete) finished
its remaining waves. Decisions recorded here so they are not re-litigated:

- **Turn pipeline is neutral-owned**: `ConversationalTurnPipeline` (`Mud.AI.Agent/Events/`) owns the
  11-stage turn state machine + the 4 compensation paths (streaming-cancel/exception compensating
  Flush MUST use `CancellationToken.None`; save-after-stream/replay uses non-cancellable tokens).
  `ConversationalFeishuEventHandler` is a thin shell (~330 lines) keeping product policy
  (`AllowMissingAppKey`/`AllowToolsWithoutAppKey`, injected as `ValidateRequestPolicies` delegates —
  the pipeline must contain zero product policy; guarded by `TurnPipeline_ShouldNotHardcodeProductPolicy`).
- **Neutral vocabulary guard** (`NeutralVocabularyContractGuards`): `Mud.AI.*` non-comment lines must
  not contain `feishu`/`appKey` (camelCase). The single whitelist is the `ConversationRequest.AppKey`
  property name — do not "fix" it.
- **Owning-tests guard** (`NeutralPublicTypesOwningContractGuards` in Agent.Tests): every public type
  of `Mud.AI.Agent` needs a `<Type>Tests` class or an `OwningEvidence` entry; the map is self-verifying
  (pointers to non-existent test classes fail). Adding a neutral public type without an owning test
  fails the build — that is intended (Q4 anti-drift).
- **PublicAPI ledger files were deleted repo-wide** (user ruling: "对开发者没屁用"; D5). Do NOT recreate
  `PublicAPI.Shipped/Unshipped.txt` or re-add `Microsoft.CodeAnalysis.PublicApiAnalyzers`. The
  consumer-rationale mechanism survives as reflection-based
  `PublicSurfaceRationaleContractGuards` (baseline 326 = reflection count over the three Feishu
  assemblies; bump the constant in the same batch as any public-surface change).
- **Golden snapshots moved to `Tests/Mud.AI.Agent.Tests/Golden`** (D2; 6 snapshots byte-identical).
  The summarizer golden uses neutral-default instruction/idKeys; the retrieval golden replicates the
  product header/footer literals — keep them byte-identical when touching either side.

## Target Frameworks & MSBuild

`netstandard2.0`, `net6.0`, `net8.0` (recommended), `net10.0`; `#if NET7_0_OR_GREATER` for
framework-specific code. AOT analyzers on net8.0+ only (`AOT006` suppressed on ns2.0/net6.0 where
source-generated `JsonSerializerContext` files are not compiled). `Tests/Directory.Build.props` and
`Demos/Directory.Build.props` **shadow** the root props — governance properties (e.g.
`WarningsAsErrors`) must be repeated there or tests/demos become gate blind spots.

## AOT rules (IL2026 / IL3050 must stay 0 — `-p:AotStrictMode=true` is in the gate)

1. **Never** call the reflection `JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` /
   `Deserialize<TValue>(string, JsonSerializerOptions)` overloads. Use
   `Mud.Feishu.Abstractions.Utilities.FeishuJsonAot.Serialize/Deserialize` (resolves `JsonTypeInfo`
   from options on net8+; falls back to reflection only when `TypeInfoResolver` is null).
2. Configuration DTOs **must not use `required`** — binding is source-generated
   (`EnableConfigurationBindingGenerator=true`) and constructs via `new T()` (`CS9035`). Validate in
   a `Validate()` method instead (see `FeishuAppConfig`).
3. `UnconditionalSuppressMessageAttribute` is `internal` on net10.0 — use
   `#pragma warning disable IL2026, IL3050` with a justification comment. Demos are exempt by
   design (that is the AGENTS.md **pragma 模式**); see `Demos/*/Program.cs`.

## Code style

- **File header** (every source file, enforced by `SourceConventionTests`): the copyright block below;
  **filename == type name** (`文件头规范`).

```csharp
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
```

- File-scoped namespaces; `GlobalUsings.cs`; import order System → Microsoft → third-party → project.
- Naming: types/methods/properties PascalCase; interfaces `I`+PascalCase; private fields `_camelCase`;
  parameters camelCase. Nullable enabled (`?`); async methods end in `Async` with a
  `CancellationToken ct = default`. Constructor injection, `readonly` fields.
- Errors: `ArgumentNullException` / `InvalidOperationException`; `ExceptionUtils.ThrowIfNull()`.
- XML docs on public APIs; file-download interfaces (`Task<byte[]?>`) must declare
  `<exception cref="ApiException">` and note the "HTTP 200 + JSON error body" residual risk.

### Comment discipline — 注释纪律 (`CommentConventionContractGuards`, 2026-10-14 audit)

Comments are code's subordinate and every fact has exactly one home. Three mechanical rules apply
to the hand-written runtime layers (`Mud.Feishu.AI`, `Mud.AI.Agent`, `Mud.AI.Abstractions`,
`Mud.Feishu.AI.Mcp`); the ledger is `documents/AIAgent/comment-budget-baseline.json`:

- **Anchors must resolve**: a review id in a comment (round ids like `R2-01`, `P2D-3b`, `AT-F14`…)
  must be findable in `documents/**` or `CHANGELOG.md` — otherwise state the constraint itself.
  ⚠️ **Never enumerate round ids in a CHANGELOG entry** (e.g. while describing a cleanup): the
  CHANGELOG is a resolution source, so listing them *legitimises* the same ids sitting in code —
  measured twice during the 2026-10-14 rounds. Describe batches by content, not by id.
- **Process narration needs a live link**: 「此前 / 原先 / 已删除 / 旧实现…」belongs in the commit
  message; kept in code, the block must cite an existing `documents/…` or `ADR-nnnn` path.
- **Budgets are tighten-only**: per-file comment lines and longest comment block are frozen;
  new files: block ≤ 20 lines, ≤ 120 comment lines. The block metric counts **prose runs only**
  (`/// <param`/`<typeparam>`-led lines separate runs — otherwise a 10-parameter member could never
  be documented within budget), and narration excludes runtime states (`任务已删除`). Decisions → `documents/AIAgent/decisions/`,
  contract rules → a `ContractGuards` test (with a `[Theory]` self-proof), traps that cannot be
  read off the code (platform quirks, ordering) stay inline. Re-freeze with
  `pwsh ./scripts/comment-audit.ps1 -UpdateBaseline`; it **refuses to freeze while any violation
  stands** (re-freezing must never launder a regression) and its diff is reviewed like a golden.
  The script's report is the phased-cleanup ledger (`-Project` / `-LongBlocks` to slice it).

### SDK interface conventions (`Mud.Feishu/Interfaces/{Module}/`)

Two parallel naming systems — file name ≠ interface name:

| Token form | File names | Interface names |
|---|---|---|
| Dual | `IFeishuV{n}{D}{R}.cs` + `_Tenant.cs` + `_User.cs` | `IFeishuV{n}{D}{R}` (base, `IsAbstract=true`, methods here) + empty `IFeishuTenantV{n}{D}{R}` / `IFeishuUserV{n}{D}{R}` (`RegistryGroupName`, `InheritedFrom`) |
| Tenant only | `IFeishuV{n}{D}{R}_Tenant.cs` | `IFeishuTenantV{n}{D}{R}` (`: IFeishuAppContextSwitcher`, no `IsAbstract`/`InheritedFrom`) |
| User only | `IFeishuV{n}{D}{R}_User.cs` | `IFeishuUserV{n}{D}{R}` (also `: ICurrentUserId`) |

Rules: `Tenant`/`User` sit after `IFeishu` (before `V{n}`) in the **interface name**, but as
`_Tenant`/`_User` **file suffix**; single-token files without a suffix are forbidden.
`{n}{D}{R}` maps the OpenAPI path (`hire/v1/jobs` → `HireJob`); `{Module}` matches the
`FeishuModule` enum. Generated client = interface name minus `I`; docs at
`documents/{Module}/FeishuTenantV{n}{D}{R}.md`; test class `{Interface}Tests`. New interface ⇒ update
in the same batch: `FeishuModule`, `FeishuServiceBuilder` + `Add{Module}Api()`,
`FeishuJsonResolverExtensions`, regenerate via `scripts/GenerateJsonContext.ps1`.

**Sub-domain merging** (no one-interface-per-OpenAPI-resource): interfaces organize by
module × token × sub-domain, **3–6 sub-domain interfaces per (module × token)** (Hire = 8,
reference implementation); method names carry the resource prefix and stay unique within the
interface; `DataModels/{Module}` folders mirror the sub-domains (namespace stays module-level);
shared base types go to `Common/`.

**Query parameters (API-2)** — new interfaces only (no migration of the 259 existing ones):
≥ 6 query params ⇒ a DTO implementing `Mud.HttpUtils.IQueryParameter` with `[Query] MyQuery query`
(generator expands it via `ToQueryParameters()`; no AOT005). < 6 ⇒ per-parameter
`[Query("name")]` binding. Interface docs must list the DTO fields ↔ query names.

## Token & multi-app management (TMA series)

- `FeishuAppManager` is a **Singleton**; it injects `IServiceScopeFactory` and scopes per
  `FeishuAppContext` — never resolve Scoped services from the root provider inside `CreateAppContext`.
- Retired contexts go to the retirement queue and are Disposed after a grace period (300s default);
  Timer roots the graph — never rely on GC. New `IDisposable` members **must** be disposed in
  `FeishuAppContext.Dispose()`.
- `GetAllApps()` returns only **instantiated** apps; use `ConfiguredAppKeys` for all configured keys.
- `InvalidateTokenAsync` cascades to `ITokenStore` (memory + store double-clear; disable via
  `PurgeStoreOnTokenInvalidation`). Stored values must carry an expiry
  (`{expireTimestampMs}|{token}` via `TokenStoreHelper.EncodeStoredToken`).
- `PerAppFeishuAuthenticationFactory` uses `ActivatorUtilities.CreateInstance`; interface/abstract
  registrations fall back to the DI singleton.

### Contract addenda (D8–D15)

- **D8** token keys only via `TokenKeyBuilder`; never inline key concatenation in stores.
- **D9** refresh threshold == cache validity threshold (`TokenRefreshThreshold`); no second set.
- **D10** credential change (`ApplyConfigurationChanges`) must purge that appKey's persisted tokens
  (tenant via `ClearAsync`, user via `IFeishuUserTokenStorePurge.ClearAllUsersAsync`); BaseUrl/
  Timeout changes keep tokens.
- **D11** `RefreshUserTokenAsync` classifies via `FeishuOAuthErrorClassifier`: non-retryable
  (`invalid_grant`) clears the refresh token and returns null; retryable throws.
- **D12** first app access must raise `AppInstantiated` (background refresh subscribes to it);
  no startup-wide prewarming.
- **D13** `OnConfigurationChanged` is two-phase: Phase-P pre-purge outside `_configApplyLock`
  (credential change is IO), Phase-B reference swap inside `_lazyRebuildLock`; never run purge IO
  or full assembly inside a lock.
- **D14** exception filtering in `CreateAppContext`/`TryGetApp` must use the
  `IsTransientInitFailure` whitelist; no `catch (Exception ex) when (ex is not OperationCanceledException)`.
- **D15** compensating dedup calls (`Rollback*`/`Mark*`) must use `CancellationToken.None` at the
  backend call site (a cancelled rollback = server resend judged duplicate and ACKed = event loss;
  R2-P0-1), must not swallow the original business exception; `TryMark*` propagates cancellation.

### Multi-tenant (TMA2-20)

Each tenant = its own `FeishuAppConfig`; `UseApp`/`BeginScope` switch token + endpoint but do **not**
enforce tenant authorization (implement custom authorization in the business layer). Redis
deduplication: distinct `EventKeyPrefix`/`NonceKeyPrefix`/`SeqIdKeyPrefix` per tenant.

## WebSocket module invariants (I13–I16 / D5 / I9 / F5 / I1 / FU-2)

Enforced by `Tests/Mud.Feishu.WebSocket.Tests/ContractGuards/WebSocketContractGuards.cs`; semantics
in `documents/WebSocket/架构与并发模型.md` §7. New/changed code in this module self-checks each:

- **I13** every path that stops socket reading (server close, client disconnect, receive-loop exit
  incl. cancellation, Dispose) must go through `TryClaimDisconnected`; active disconnect claims
  **before** the close handshake.
- **I14** the receive loop claims its slot atomically (`Interlocked.CompareExchange`); never
  check-then-use `_receiveTask`; the loop task must be registered in `_receiveTask`.
- **I15** the caller's `CancellationToken` covers handshake + auth only, never the connection
  lifetime (client-owned CTS).
- **I16** numeric configs validate both bounds; any non-literal `new
  CancellationTokenSource(TimeSpan)` / `Task.Delay(TimeSpan)` must pass `Core/TimeSpanGuards.cs`
  clamping (fail-fast discipline; `Math.Clamp` is banned on netstandard2.0 — use Min/Max).
- **D5** inbound frames log only structured fields + `LogSanitizer.CleanMessage` preview; connection
  URL logs strip the query entirely.
- **I9** semaphores are never released on Dispose (sole exception:
  `FeishuWebSocketConcurrencyService` hot-update retains the old semaphore for
  `max(60s, 2 × MessageHandlerTimeoutMs)`).
- **F5** every new "actively discard" path must call `RecordWebSocketFramesDiscarded(appKey,
  reason)` with `reason` from `FeishuMetrics.DiscardReasons`.
- **I1** no user events may be dispatched while holding `_connectionLock` or `_connectLock`
  (defer + flush after unlock: `BeginDeferConnectionEvents`/`FlushDeferredConnectionEvents`).
- **FU-2** SeqID dedup keys are app-less (assumes single-app default binding); introducing
  multi-app WebSocket assembly **must** add the app dimension to the key.

## Configuration surface — 配置面治理 (R4 / R5)

Nested Options are the only public config shape (flat legacy properties deleted):

| Area | Shape | Section |
|---|---|---|
| App HTTP/circuit-breaker | `FeishuAppConfig.TimeoutSeconds` / `HttpRetry` / `CircuitBreaker` | `FeishuApps:0:*`, `HttpRetry:*`, `CircuitBreaker:*` |
| Event dedup | `FeishuDeduplicationOptions` | `FeishuDeduplication:*` |
| Redis connection | `RedisOptions.Connection` / `Advanced` | `FeishuRedis:Connection:*` |
| WebSocket reconnect/cert | `FeishuWebSocketOptions.Reconnect` / `Certificate` | `FeishuWebSocket:Reconnect:*`, `Certificate:Mode` |
| WebSocket event guards | `RejectEmptyEventIds` (default true) / `IgnoreUnknownEventTypes` (default false, recommend true) | `FeishuWebSocket:*` |
| Webhook token refresh | `EnableTokenBackgroundRefresh` (bool?) | `FeishuWebhook:*` |
| Webhook timeout | global `EventHandlingTimeoutMs` + app-level override | — |

Governance (R5, locked by `ConfigSurfaceContractGuards`): **no new logging switches** — levels come
only from `Logging:LogLevel:{Category}`; every public config property must have a real consumer
(`Validate`/`ToString` don't count); binding must use `Configure<T>(o => section.Bind(o))`, never
the `IConfiguration` overload (breaks IL2026/IL3050); one config section per semantic (aliases need
field-level fallback + migration table + `[Obsolete]`); behavior changes go to
`documents/Configuration/CHANGELOG-Config.md`; delete a key ⇒ add it to `audit-config-keys.ps1`
`$strictPatterns` in the same batch. Multi-tenant: the three dedup KeyPrefixes must be distinct;
`NonceTtl >= TimestampToleranceSeconds` (WHF-03). Audit script: `Demos/` excluded; allowlist is
inline `// audit-allow: <reason>`; paths use `/`.

## Test guidelines — 测试规范

xUnit + FluentAssertions + Moq; tests mirror source folders; `{ClassName}Tests`;
`{Method}_Should{Behavior}_When{Condition}`. No `Thread.Sleep` in test harnesses — use gate
signals. Stress/long-running cases carry
`[Trait("Category","Stress")]` (excluded from the gate; run manually with `--filter
"Category=Stress"`); wall-clock assertions carry `[Trait("Category","Timing")]` (`-Full` only).

### Test Suite Governance (2026-10-13; analysis: 《单元测试体系过度测试分析与解决方案》; doc locations: `Tests/Shared/Docs/FeishuDocs.cs`)

- **Test TFMs are net8.0-only** (`Tests/Directory.Build.props`). No per-project
  `<TargetFrameworks>` overrides; sole exception `Mud.Feishu.Agent.Demo.Tests` (net10.0 demo).
- **Guard admission**: a new `ContractGuards/` test must cite the real regression it intercepts
  (commit/incident id) in its XML docs, else it is demoted to `-Full` or rejected. Self-proof
  ("guard the guard") tests: one `[Theory]` per parser, not standalone `[Fact]`s.
- **Single-point closure equality**: exactly one guard asserts "artifact ↔ code" equality per closed
  set; count-only assertions fully subsumed by an equality guard must not be added.
- **Guard-read files must be tracked and registered**: any file read via
  `File.Exists/ReadAllText` lives under `documents/…` — never `.docs/` (gitignored ⇒ structurally
  red on CI) — **and its path is registered in `FeishuDocs`** (`Tests/Shared/Docs/`, test-only
  asset, zero production references), so a
  doc move is a one-line change verified by `FeishuDocsRegistryContractGuards`. Runtime/public
  constants (e.g. MCP `ErrorContractSummary`) reference docs **by title only**, never by path.
- **Budgets**: ≤ ~5 test files touched per feature commit (baseline ≈ 10); `ContractGuards/` ≤ 30%
  of a test project; a test that flakes twice in the full gate gets a threshold review or a Timing
  trait — never a blind retry loop.

## Security

Never log/expose `AppSecret` or tokens (`MaskSensitiveData()`); validate URLs (SSRF); never weaken
the secure defaults (wss, Strict certificate, `EnforceHeaderSignatureValidation`,
`RejectEmptyIdentifiers`, three-prefix isolation, BaseUrl HTTPS allowlist).

## Project structure

```
MudFeishu/
├── Mud.Feishu/                # Core HTTP API client (Interfaces/, DataModels/)
├── Mud.Feishu.Abstractions/   # Event handling abstractions, token/multi-app
├── Mud.Feishu.Authentication/ # User authentication middleware
├── Mud.Feishu.Redis/          # Redis distributed deduplication
├── Mud.Feishu.Webhook/        # Webhook event handling
├── Mud.Feishu.WebSocket/      # WebSocket real-time events
├── Mud.Feishu.AI/             # Agent tools ([FeishuTool]/[ToolParameter] in AgentTools/)
├── Mud.Feishu.AI.Tools/       # Tool surface (SdkProfile/, Curation/, Guidance/, Affordance/)
├── Mud.Feishu.AI.Mcp/         # MCP server + resources
├── Tests/  Demos/  scripts/  skills/  documents/
├── Directory.Build.props      # Global MSBuild properties (shadowed by Tests/ and Demos/)
└── Mud.Feishu.slnx
```
