# Mud.Feishu Change Log

All notable changes to Mud.Feishu are documented in this file. The format follows [Keep a Changelog],
and versioning follows [Semantic Versioning].

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/
[Semantic Versioning]: https://semver.org/

## [Unreleased]

> Theme: **adapting to `Mud.HttpUtils` 3.0.9** (on top of the 3.0.8 `BC-30` member removal already
> shipped in 3.1.0: migrate the token-recovery registration to the official entry point and correct
> stale statements).
> This is a **breaking dependency change**: `Mud.HttpUtils` / `Mud.HttpUtils.Generator` move from
> `3.0.8` to `3.0.9` (the 3.0.9 batch is mainly "code that can now be deleted" plus corrected
> statements — no new breaking change).
> The v3.1-line batches are merged in as well: Webhook semantics fixes, the multi-app management
> hardening batch, and `Mud.AI.*` 1.0.2 (see the sections below).
> **Release prerequisite (satisfied)**: upstream `3.0.9` is **published to nuget.org**; the local source
> in `nuget.config` (pointing at the Release artifacts) has been **removed**, the packages are restored
> from the public feed, and the full quality gate was re-run.

### Dependency Upgrade

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`: `3.0.8` → **`3.0.9`** (must stay on the same version),
  unified across the 10 `Mud.HttpUtils*` declarations of 7 source projects; the README dependency
  tables and `AGENTS.md` are synced; the `Mud.HttpUtils.Attributes` / `Mud.HttpUtils.Abstractions`
  references of the `Mud.Feishu.AI*` and demo projects are bumped in the same batch.
- During validation the packages are restored from the local **Release** channel
  (`mudhttputils-local` → `D:/Repos/MudHttpUtils/artifacts`, produced by `pack.ps1 Release` with
  byte-for-byte DLL verification); the `artifacts-debug` Debug pack **must not** be consumed. After
  3.0.9 shipped on nuget.org the local source was **removed** and the packages were re-restored from
  the public feed following the "clear cache → `restore --no-cache` → `clean` → `build`" discipline
  (0 errors, 190 warnings identical code-by-code to the pre-upgrade baseline), and the full gate passed again.
- `Mud.AI.Abstractions` / `Mud.AI.Agent` / `Mud.AI.Generator`: 1.0.1 → **1.0.2** (neutral tool telemetry
  port `IAgentToolTelemetry`, `AmbientScope<T>` runtime assembly extensions, `TurnScopeContext.Facts`,
  the `DelegatingToolFunction` decorator base, session-store facade and fallback error-truncation fixes).
  1.0.2 is published on nuget.org; the local `mudai-local` folder source used for joint debugging was
  removed from `nuget.config` (restore re-verified afterwards).

### Changed

- **Token-recovery configuration now uses the component's official entry point**: the 38-line
  downstream block (DTO projection + `Apply` + hand-rolled `ConfigurationChangeTokenSource` + validator
  + resolvable instance) is replaced by `AddMudHttpTokenRecoveryFromConfiguration(configuration)`.
  Binding semantics (DTO projection, defaults preserved when the section is absent, invalid values
  rejected at option resolution) and hot-reload semantics are equivalent and covered by the existing
  equivalence / hot-reload tests. The code-only registration path without `IConfiguration` keeps the
  options pipeline, validator and resolvable instance (without section binding).
- **JSON contexts are merged instead of registered one by one**:
  `AddMudHttpClientJsonContext(FeishuApiResultJsonContext.Default)` became
  `AddMudHttpClientJsonContexts(FeishuJsonContext.Default, FeishuApiResultJsonContext.Default)`
  (net8+), so the DI resolver chain covers event/webhook types as well as response wrappers and auth
  DTOs. The static `FeishuJsonDefaults` merge chain used by non-DI call sites is **unchanged**.
- **Comments and tests corrected**: the stale claim that `TokenRecoveryEnhancedClient` does not override
  `WithBaseAddress` (and that a cast throws `InvalidCastException`) is gone — since 3.0.9 the method
  returns the same type. The hot-reload comment now states that the configuration delegate is re-run on
  every `CreateClient` rather than cloning an instance. The `PurgeGateTokenStoreDecorator` read-failure
  note now says the component degrades reads as well and the decorator keeps its own degradation as
  defense in depth (behavior unchanged).
- **New regression tests**: `TokenRecoveryClientCapabilityTests` (`WithBaseAddress` keeps the type and
  applies the new base address) and `AddFeishuApp_ShouldRegisterFeishuDomains_SoNoBlankAllowlistWarning`
  (the registration entry point registers the Feishu domains, so strict-mode clients never emit the
  blank-allowlist startup warning).

### Fixed

- `scripts/verify-build.ps1` step 0 (CACHE-1) now compares against a local component source **only when
  `nuget.config` explicitly declares a local folder source**, and prints `[SKIP]` otherwise:
  once the component ships on nuget.org the local source is gone and the cache comes from the public
  feed, so comparing against the machine's pack folder would report "public package differs from a local
  same-version repack" as a stale downstream cache. The comparison semantics for local development
  cycles (source declared) are unchanged.
- **Corrected a wrong statement in the old docs**: `GetWebApi<T>(appKey)` used to be described as
  returning an "independent instance". It actually shares a single `AsyncLocal`-backed
  `IAppContextHolder` with `UseAppScope` / the legacy `UseApp()` — there is **no "independent
  instance" semantics**; the correct pattern is to switch first and then resolve from DI.
- Decision archive updated with the execution status: `弃用成员移除方案.md` (§2 gate A, §5 risk 7
  resolved), `迁移与弃用史.md`, `编号索引.md`, `上游契约适配史.md`, `注释精简归档.md`.

### Fixed — Mud.Feishu.Webhook

- **"Signature validation unavailable" is no longer reported as "signature mismatch" (P0)**: an `IEncryptKeyProvider`
  failure (e.g. KeyVault returning empty), an internal validator exception, or a post-hot-reload options build failure
  now throws `FeishuWebhookUnavailableException` and maps to **HTTP 503 + `Retry-After`** so Feishu retries.
  Previously these returned `false` → 403 (terminal), losing the event permanently with no failed-event record.
  - `HandleEventAsync(request, body)` returning `false` now means **only** "the signature was judged invalid".
  - All four 503 write sites collapse into `WriteServiceUnavailable` (always emits `Retry-After`).
- **Hot-reload callbacks can no longer terminate the process**: every `IOptionsMonitor.OnChange` callback goes through
  `SafeOptionsChangeExtensions.OnChangeSafe` (exceptions are logged, never propagated to the reload thread) and no
  callback reads `CurrentValue` any more (it diffs against a snapshot seeded in the constructor). The rate-limit
  middleware's `System.Threading.Timer` is gone — window cleanup is now an opportunistic sweep on the request path.
- **Request-body budget**: `EnableBuffering()` removed (no more temp-file spill for bodies >30KB), 64KB chunked reads
  with `ContentLength` pre-allocation and `RequestAborted` propagation, and one less full copy.
  **`MaxRequestBodySize` default 10MB → 1MB** (413 beyond it; configure explicitly for oversized card payloads).
- **`UseFeishuWebhook()` fails fast when `Apps` is empty** (previously: starts fine, endpoints silently 404, Feishu keeps
  retrying). Hosts that only drive `IFeishuWebhookService` manually are unaffected (they do not mount the middleware).
- **`GlobalRoutePrefix` normalization**: `"/feishu"`, `"feishu/"`, `" /feishu/ "` no longer break routing
  (normalized once in `PostConfigure`, so every consumer sees the effective value).
- **Instance registrations are singletons**: `AddHandler(instance)` / `AddInterceptor(instance)` used to register the
  shared instance as Scoped, so DI disposed it at the end of the **first request**; now Singleton (disposed with the
  container). XML documents the thread-safety requirement and points to `AddHandler<T>()` for per-request state.
- **Removed the middleware's container registrations** (`UseMiddleware` always builds its own pipeline instance, so the
  registration produced a phantom instance with a duplicate change subscription).
- **`DecryptEventAsync` now forwards the `CancellationToken`** to the key provider (previously `DecryptionTimeoutMs`
  could not interrupt a hanging key lookup).
- **`InMemoryFailedEventStore.GetPendingRetryEventsAsync` honours `beforeTime`**.
- **Request-body pre-allocation is now capped, removing memory amplification and integer overflow**: it used to cast
  the declared `ContentLength` straight to `int` (`MaxRequestBodySize` is a `long` with no upper bound) — a >2GB
  declaration overflowed to a negative length (500) and an unauthenticated request could force an immediate
  equal-sized allocation. The effective budget is now capped at `min(MaxRequestBodySize, int.MaxValue)` (the result
  is a `string`, whose length is `int`-bounded) and the capacity is `min(declared, effective budget, 1MB)`; growth
  beyond that is on demand and the byte budget is still enforced per chunk.
- **Retries no longer refresh `FailedAt`**: each retry attempt used to overwrite it with "now", which made
  `InMemoryFailedEventStore.GetOldestPendingAge()` (feeding the `oldest_pending_retry_age_seconds` health dimension,
  `Degraded` above 5 minutes) blind to a handler that keeps failing. `FailedAt` now keeps the **first** failure time;
  backoff progress lives only in `NextRetryAt`. ⚠️ Operator-visible: a persistently failing event now really does turn
  the health check `Degraded`.
- **Health-check severity no longer inverted**: "utilization ≥80% → `Degraded`" used to be evaluated before
  "failed-event backlog ≥500 → `Unhealthy`", so a genuinely `Unhealthy` state was reported as the milder `Degraded`.
  Severity is now decided strictly as **`Unhealthy` > `Degraded`** (slots exhausted → backlog hard threshold →
  utilization → oldest pending age → backlog soft threshold), and every data item is always populated.

### Changed — Mud.Feishu.Webhook

- Decryption hot path: dropped `Task.Run` (AES is micro-second range) and cached the derived key per `EncryptKey`.
- Successful security-audit logging downgraded to `Debug` (failures stay `Warning`); middleware request/response logs
  downgraded to `Debug`, cutting successful-event logging from 4 lines to 2.
- Startup self-check log now prints effective `MaxRequestBodySize`, `DecryptionTimeoutMs` and rate-limit settings.

### Removed — Mud.Feishu.Webhook

- Four public exception types with **no runtime throw site** (misleading public surface):
  `FeishuWebhookDecryptionException`, `FeishuWebhookProcessingException`, `FeishuWebhookSecurityException`,
  `FeishuWebhookSerializationException`. Kept: `FeishuWebhookException` (base),
  `FeishuWebhookValidationException` (413), `FeishuWebhookUnavailableException` (503).

### Docs

- `documents/ErrorHandling.md`: added the Webhook ACK decision table (same source as
  `WebhookAckSemanticsContractGuards`) and the deployment preconditions (Redis dedup for multi-instance, in-memory
  failed-event loss on restart, plaintext events rejected, nonce consumption order, body size cap, prefix format).
- `documents/index.md`: added a "Webhook troubleshooting" section — 503 means the key service is unavailable (**not** a
  signature mismatch), plus 403 / 404 / 413 triage.

### Fixed — multi-app management and event dispatching

Batch-wide fix after the multi-app management and event dispatching review. Except for
**connection-level app binding** (a behavior change for multi-app deployments: a missing
`FeishuWebSocket:AppKey` now fails startup) every item leaves single-app semantics unchanged.

- **WebSocket — connection-level app binding**: `FeishuWebSocketOptions.AppKey` is promoted from an
  "metrics dimension" to the connection's **app binding key**. Single-app deployments may leave it
  empty (resolved to that app automatically); multi-app deployments must set it explicitly —
  missing or unregistered keys fail startup with `InvalidOperationException`. Previously a
  multi-app deployment silently bound the connection to the default app, so events were sent and
  received under the wrong identity. The binding is resolved once at client construction
  (`FeishuWebSocketClient` / `FeishuWebSocketManager`) and drives metrics, deduplication and the
  failed-event store; configuration reload never rebinds a live connection (rebind = restart or
  rebuild the client).
- **WebSocket — app dimension through the unified deduplication middleware**: new
  `IAppScopedUnifiedDeduplicationMiddleware` (implemented by `UnifiedDeduplicationMiddleware`)
  forwards `appKey` to the event deduplicator; the legacy interface methods are equivalent to
  `appKey = null`, so the change is binary compatible. Previously WebSocket events lost the app
  dimension when deduplicated through the middleware, so identical EventIds leaked across apps in
  multi-app deployments. The check / complete / rollback call sites in `FeishuEventMessageHandler`
  now pass the resolved app key.
- **Webhook — dispatch policy between app-scoped and global handlers**: new
  `FeishuWebhook:HandlerDispatchMode` (`AppScopedOnly` default / `MergeWithGlobal`). The default
  keeps the previous semantics (app-scoped handlers suppress the global ones); `MergeWithGlobal`
  lets global handlers run as well. When no app-scoped handler is registered the global factory is
  always used. In merge mode an event already handled by an app-scoped handler is no longer
  reported as `unhandled`. Dispatch is split into app-scoped / global paths; the existing warning
  and metric for "all app-scoped handlers skipped the event type" are preserved, and invalid enum
  values are rejected in `Validate()`.
- **Webhook / WebSocket — post-interceptor resilience**: `AfterHandleAsync` now runs per
  interceptor inside try/catch and always receives `CancellationToken.None`. Previously one failing
  post-interceptor skipped the remaining ones and turned a successful business result into a
  failure (on the webhook side it also replaced the original processing exception); on external
  cancellation the cancelled caller token made the cleanup logic short-circuit immediately.
  Interceptor failures now only log an error (interceptor type / event type / EventId) and do not
  affect the original processing outcome.
- **Multi-app — `RemoveApp` lifecycle closure**: registry removal, configuration snapshot sync,
  tombstoning, generation increment and purge-gate marking now happen atomically under
  `_registryLock`; the purge runs asynchronously outside the lock and releases the lease in
  `finally`. Three fixes: ① **tombstone** — an explicitly removed key can no longer be silently
  resurrected by a hot reload that started before the removal and commits after it (cleared only
  when the configuration source stops declaring the key or `AddApp` is called); ② **ABA** — if the
  app is re-added right after removal, the late purge is skipped by generation check and cannot
  delete tokens the new context just wrote; ③ **snapshot sync** — `RemoveApp` previously left the
  configuration snapshot untouched, so later hot reloads treated the removed app as still online.
  Default-app promotion is therefore deterministic: removed-key-free snapshot order, then ordinal
  AppKey ordering.
- **Multi-app — per-app failure isolation in hot reload**: the add / remove / update commit
  phases each isolate exceptions, so one failing app no longer aborts the remaining changes. The
  commit order is corrected to "register first, retire the old context after `RegisterApp`
  succeeds" — previously the old context went into the retirement queue before registration, so a
  failed registration left it to be disposed after the grace period while still referenced by the
  registry. A failed update rolls back its `Lazy` reference swap; a failed add is excluded from the
  configuration snapshot (otherwise the next reload sees "unchanged" and never retries), and a
  failed removal keeps its snapshot entry. The credential purge plan is now reduced to
  `WriteBackRiskKeys` (the "pre-purge timed out" branch is unreachable for a fire-and-forget
  purge).
- **Multi-app — unified validation across construction / hot reload / `AddApp`**: duplicate
  `AppKey`, empty `AppKey` and multiple `IsDefault = true` entries now fail hard. Previously a
  duplicate key only logged a warning and let the later registration silently overwrite the
  earlier one, and multiple defaults made the default identity drift with registration order and
  flip on every reload.
- **User tokens — persisted access-token invalidation**: new public capability interface
  `IUserTokenStoreAccessTokenInvalidation` (deletes the access token only, keeps the refresh
  token), implemented by `FeishuUserTokenStore` / `RedisUserTokenStore` and conditionally
  forwarded by `EncryptedUserTokenStore` (no-op when the inner store lacks the capability; it must
  **not** fall back to `RemoveAsync`, which would delete the refresh token too).
  `UserTokenManager.InvalidateUserTokenAsync` now writes through to the store after clearing the
  local mirror, and logs a warning instead of throwing on failure (invalidation stays
  best-effort). The capability probe uses the **raw undecorated store**; purge-gate decorators
  deliberately do not declare it so a probe cannot false-hit. Previously invalidation only cleared
  the local cache, so multi-instance deployments and cold starts restored an access token that
  should have been invalidated.
- **Critical-section self-check and scope documentation**: the `_lazyRebuildLock` commit section of
  the hot reload now emits a Debug self-check when held longer than 50 ms (hinting at IO or heavy
  computation inside the lock); `TokenStorePurgeGate` documents its scope (read paths only; writes,
  deletes and purges pass through; process-local); `TokenKeyBuilder` documents the plaintext-userId
  trade-off. New ADRs: ADR-0009 (user identity and storage-key privacy), ADR-0010 (hot-reload lock
  order and critical-section budget), ADR-0011 (purge-gate scope).

## [3.1.0] - 2026-10-19

**Major feature release — AI capabilities.** Three new packages ship for the first time with this
version: `Mud.Feishu.AI` (agent runtime), `Mud.Feishu.AI.Tools` (tool surface) and `Mud.Feishu.AI.Mcp`
(MCP server) — embed a Feishu agent in your .NET service, let the model call Feishu APIs safely, or
expose Feishu tools over MCP to any agent framework. The AI packages are first releases with **no
breaking changes**; the core SDK packages (`Mud.Feishu`, `Mud.Feishu.Abstractions`, `Mud.Feishu.Redis`,
etc.) can be upgraded directly — the only migration point is the three context-switching members
removed with `Mud.HttpUtils` 3.0.8 (see "Changed").

### Highlights

- **Feishu agent runtime**: Feishu messages / events → conversation → model → reply, ready to go with
  a single line of assembly; safety semantics such as bot self-trigger filtering and group
  @-mention filtering are built in; replies support streaming via message cards.
- **319 curated tools (31 business domains)**: high-frequency Feishu operations — IM, mail, tasks,
  Bitable, Sheets, Docs, Wiki, Calendar, Approval, Contacts, Minutes and more — exposed to the model
  with compile-time schemas; reads and writes separated, 36 high-risk write tools disabled by default
  and require host authorization before enabling.
- **Human-in-the-loop (HITL)**: write operations can pause in "awaiting confirmation" and continue
  after being released through the host approval channel; nothing runs automatically without
  confirmation.
- **MCP server** (optional package): stdio integration with Claude Code / Cursor / custom harnesses;
  the tool allow-list is shared with the in-process agent.
- **Conversation memory and knowledge retrieval**: count + token dual-window memory with over-window
  summarization; built-in knowledge ingestion and self-hosted vector retrieval (RAG), with cited
  results.

### Added

- **`Mud.Feishu.AI` — agent runtime**
  - `FeishuAgent` (built on `Microsoft.Agents.AI`): conversation management, model invocation, tool
    loop, streaming output;
  - `AddFeishuImConversationHandler` wires up the built-in IM conversation handler in one line
    (group @-mention and direct messages both configurable);
  - conversation serialization gate: the same conversation queues while different conversations run
    in parallel; in-process and Redis implementations;
  - streaming reply channels: message-card stream (create → update → final), with automatic fallback
    to message editing on failure;
  - event context assembly: approvals, Bitable record changes and other events enter the conversation
    context (`ConversationRequest` gains event facts);
  - delivery reliability: redelivery of replies that were "generated but not confirmed delivered"
    re-sends the existing reply instead of re-running the model.
- **`Mud.Feishu.AI.Tools` — tool surface (319 curated tools / 31 domains; 167 read-only + 152 write)**
  - strongly-typed parameter schemas, unified pagination envelope and error codes; writes support
    `dry_run` preview; file downloads land under controlled paths and the result carries no bytes
    (keeping the context clean); truncated tool results are flagged to the model honestly;
  - permission model: per-tool declared calling identity (tenant / user) and OAuth scopes, with the
    permission matrix shipped alongside the package;
  - per-tool affordance guidance (when to use / when not to / prerequisites / tips), visible to the
    model;
  - structured audit sink (`IToolExecutionAuditSink`) and OpenTelemetry metrics (tool execution,
    latency, model-call duration);
  - self-hosted knowledge retrieval (chunking / indexing / vector search) and text tools
    (`ai.translate_text`, `ai.detect_language`);
  - Skills artifacts (`skills/feishu-*`) ready to distribute to agent harnesses.
- **`Mud.Feishu.AI.Mcp` — MCP server (optional package)**
  - stdio JSON-RPC exposes allow-listed Feishu tools and resources to out-of-process agents; it shares
    the same tool definitions and execution pipeline as the in-process agent, with writes still going
    through human approval.
- **Human-in-the-loop (HITL) pipeline**: host approval channel (`IFeishuToolApprovalChannel`),
  persisted pending-approval snapshots (`IFeishuPendingApprovalStore`), idempotent resumption after
  approval; without an approval channel registered, write tools stop at "awaiting confirmation".
- **Demo app**: `Demos/Mud.Feishu.Agent.Demo` picks a run mode from a startup menu (raw model / IM
  conversation / document agent / tool smoke / domain events, etc.), defaulting to the configured
  mode in non-interactive environments.

### Changed

- **Unified OpenTelemetry composition kernel**: invalid configuration now throws
  `OptionsValidationException` at registration instead of being silently ignored; register exactly one
  OpenTelemetry composition (combining it with `AddMudHttpOpenTelemetry` duplicates exports).
- **Context-switching members removed with `Mud.HttpUtils` 3.0.8**: `FeishuAppManager.GetWebApi`,
  `GetDefaultWebApi` and `RegisterSwitcherFactory` are gone (upstream 3.0.8 physically removed the
  base members; the downstream implementations are deleted in the same batch).
  **Migration**: resolve `IAppScopeSwitcher` from DI and call `UseAppScope(appKey)` /
  `UseDefaultAppScope()` (context returned automatically), or use
  `IAppContextHolder.SwitchToApp(appKey, appManager, sp)` (guarded switch-and-stay).
- **Docs and samples rewritten**: call sites uniformly use "switch the context first, then resolve the
  API client from DI" (README zh / en, `Mud.Feishu` README, multi-app demo controller).

### Fixed

- Unanswered human approvals no longer block subsequent conversations (self-healing); the streaming
  fallback path no longer drops pending approvals.
- Failed-delivery redelivery now re-sends the existing reply instead of re-running the model and
  double-billing; empty replies are no longer sent.
- In multi-app hosts, replies are no longer mis-sent to other tenants under the default app identity.
- `minutes.get_artifacts` transcript windowing parameters were ignored (always returned a preview) —
  now honored.
- Knowledge recall answers are no longer dropped because `has_answer` was reset by intermediate events.
- Token counting is now precise (previously the missing vocabulary always fell back to character
  estimation).
- Security: CR/LF injection in mail headers is rejected; the IPv6 loopback allow-list check is
  enforced; all `IL2026` / `IL3050` trimming warnings are eliminated in AOT strict mode.

### Dependencies

- New: `Microsoft.Agents.AI` **1.25.0** (agent runtime foundation), with `Microsoft.Extensions.*`
  **10.0.12**, `Microsoft.Extensions.AI.Abstractions` **10.10.1**, `System.Text.Json` **10.0.12**.
- New: `Microsoft.ML.Tokenizers.Data.O200kBase` for precise token counting.
- `Mud.HttpUtils*` unified version bumped **3.0.7** → **3.0.8** (`Mud.HttpUtils` and
  `Mud.HttpUtils.Generator` must stay on the same version; includes the `Mud.HttpUtils.Resilience`
  policy package, passed through to consumers).

## [3.0.4] - 2026-10-18

> Theme: **source-comment governance** (comment discipline spec + `audit-comments` audit script +
> L3 decision archive) and **WebSocket heartbeat / observability fixes**.
> This release has **no external contract changes**: public API signatures, runtime behavior and
> package dependency versions are unchanged; changes concentrate on the comment surface, internal
> docs and demo project dependency alignment.

### Added

**Comment discipline**

- New normative document `documents/代码注释规范.md`: three comment criteria, the L0–L4 layering
  model, hard rules R-01–R-12, `[Obsolete]` message templates, glossary, per-line exemption
  mechanism, the "constraints that must be kept" allow-list (Appendix A) and the rule → script
  pattern mapping (Appendix B).
- New audit script `scripts/audit-comments.ps1` (report / `-Strict` gate / `-ListAll` modes), wired
  into `scripts/verify-build.ps1` step 1.5 and the CI pipeline.
- New stock baseline `scripts/comment-baseline.txt`: hits frozen by **content hash** (reformatting
  and line drift do not invalidate the baseline), **shrink-only** (`-UpdateBaseline` exits 2 when the
  baseline grows). After this round the baseline is **zeroed** (initial 980 entries / 1020 hits →
  **0 entries / 0 hits**).
- New per-line exemption marker `// comment-allow: <reason>` (reason required; whole-file exemption
  forbidden).
- New **L3 decision archive** `documents/决策档案/`: `README.md` (placement conventions),
  `编号索引.md` (internal id lookup), `上游契约适配史.md`, `迁移与弃用史.md`, `注释精简归档.md` —
  version adaptation history, defect reviews and acceptance records all moved out of source comments.
- `AGENTS.md` gains a "Comment discipline" section (criteria, gate wiring, decision archive
  placement).

### Changed

- **Repository-wide hand-written comments converged to "contract + constraint"**: 8 shipped projects,
  **172 source files** (2037 lines deleted, net −482). Moved out: internal branch/task ids (`BC-*`,
  `U-*`, `C-*`, `F-*`, `TMA*`, `TMF*`, `TMR*`, `WHF-*`, `WS-*`, `P0-/P1-/P2-*`, etc.),
  "before the fix / old implementation" defect retrospectives, `documents/…` and `.docs/…` internal
  doc paths, decorative `// ─────` section rules; constraints not derivable from code (lock order,
  atomicity, idempotency, ordering dependencies, AOT suppression rationale, security semantics) are
  kept. Covers `Mud.Feishu.Abstractions` (multi-app management, token storage & codec, service
  registration, event parsing, metrics), `Mud.Feishu.Redis`, `Mud.Feishu.Webhook`,
  `Mud.Feishu.WebSocket`, `Mud.Feishu.Authentication`, `Mud.Feishu.DataModels` (query DTOs) and
  `Mud.Feishu/Interfaces/**` (interface contract comments). Token-storage constraints (stored form
  only encoded/decoded via `TokenStoreHelper`; CAS capability probing must target the undecorated
  raw store) are reworded but semantically intact.
- **Comment audit script fix**: single-line `<remarks>` blocks that open and close on the same line no
  longer leave `$remarksOpen` dangling — previously the 8-line block below got merged into the run and
  misreported as "82 lines" long (25 such blocks; affected hit line numbers and lengths).
- **`scripts/verify-build.ps1` empty-local-source fix**: with no local source present it now checks
  emptiness before `Test-Path`, eliminating the "Path parameter is null or empty collection" throw
  (hit by CI and by dev machines with the local source removed).

### Fixed

- **Heartbeat loop captured the caller's `SynchronizationContext`, delaying Pings** (`HeartbeatManager`):
  every `await` in the loop now uses `ConfigureAwait(false)` — background loop continuations must not
  post back to the caller's context (UI thread / legacy ASP.NET request context / in-process xUnit
  concurrency gates); when that context is saturated, the Ping continuation queues for a slot (observed:
  a 5s interval still not delivered within a 10s budget). Adds the regression guard
  `HeartbeatManagerProtoBufTests.StartHeartbeatAsync_ShouldNotCaptureCallerSynchronizationContext`
  and relaxes the WebSocket integration handshake budget.
- **Removed the always-`null` `ProcessingTask` branch** (`BinaryMessageProcessor`): the property was
  never assigned and the branch never executed — pure dead code; binary routing results are handled
  via the return value of `RouteBinaryMessageWithResultAsync`.

### Build

- Demo project `Mud.Feishu.*` package references aligned: `FeishuFileServer` / `FeishuOAuthDemo` /
  `FeishuWikiManager` / `TaskManageDemo` → **3.0.3**; `Mud.Feishu.Webhook.Demo` → **3.0.1**.
- `FeishuWebhookServiceBuilder` gains per-line `audit-allow` markers for removed config keys and
  transitional `[Obsolete]` switches, so `audit-config-keys.ps1` can exempt them precisely without a
  whole-file exemption.

## [3.0.3] - 2026-10-15

> Theme: **adapting to `Mud.HttpUtils` 3.0.7** (upstream BC-30 deprecation propagation + F-9 meta-package
> decoupling). No public contract removals; `FeishuAppManager.GetWebApi` / `GetDefaultWebApi` inherit the
> upstream `[Obsolete]` marking (3.x warning, removal in 4.0) — signatures and behavior unchanged.

### Dependency Upgrade

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`: `3.0.5` → **`3.0.7`** (must stay on the same version),
  unified across the 8 `Mud.HttpUtils*` declarations of 6 source projects; README / AGENTS.md dependency
  tables synced.
- **New explicit `Mud.HttpUtils.Resilience` 3.0.7 dependency** (F-9): the upstream meta-package no longer
  carries the resilience package; the `AddFeishuApp` wiring (per-app retry / timeout / circuit breaker)
  requires it, so it is installed explicitly and flows to consumers (no `PrivateAssets`).

### Added

- **C-2 dual-read token-store codec**: new public `FeishuTokenStoreCodec` (implements the upstream
  `ITokenStoreCodec`). Writes now use the official relative-seconds `{expiresInSeconds}|{token}` form;
  reads dual-read legacy absolute-millisecond values (legacy entries converge by natural expiry, no
  backfill); consumers keep the absolute-ms contract. The Redis refresh-TTL "no TTL info" boundary is
  corrected (leading value ≤ 0 falls back to 30 days instead of being treated as expired). **Rollback
  note**: rolling back to ≤ 3.0.2 treats new-format values as expired (tokens re-fetched; Redis
  refresh keys purged by the old logic → re-authorization) — no corrupted tokens, but the upgrade /
  rollback is asymmetric.
  **U-1 retirement (upstream 3.0.10 Batch 1)**: dual-read discrimination has been lifted into the
  official `MigrationTokenStoreCodec`; `FeishuTokenStoreCodec` is now a thin wrapper over it
  (explicit Feishu threshold 10^10) and the private dual-read implementation is removed.
- **F-4 external-ID reverse lookup**: `FeishuAppContext` implements `IAppExternalIds`
  (`app_id → appKey`); `FeishuAppManager` resolves appKey via the built-in index plus a DI-injected
  host `IAppLookupProvider` (fail-closed, results must hit registered apps).
- **U-3 cross-process refresh-dedup lease (upstream 3.0.10 Batch 1)**: all three token managers
  (Tenant/App/User) now honor the `TokenManagerBase.RefreshLease` hook. `Mud.Feishu.Redis` registers
  the official `RedisRefreshLease` (`SET NX PX` acquire + Lua compare-and-del release, fail-open)
  with key prefix `{TokenKeyPrefix}:lease` following the environment segment; the token-manager
  factory resolves and injects it via DI. Without Redis the behavior is bit-identical to
  single-instance; multi-instance losers of the lease poll read-through (250ms interval / 10s
  fail-open timeout) instead of re-refreshing.
- **U-4 per-app token purge capability (upstream 3.0.10 Batch 2)**: `Mud.Feishu.Redis.RedisTokenStore`
  implements the official `ITokenStoreAppPurge` (`EnumerateKeysAsync` physical-key snapshot /
  `PurgeAppAsync` returns the actual deleted count; node failures propagate); the purge-gate and
  encrypted decorators pass the capability through — hosts can uniformly call
  `TokenStorePurgeExtensions.PurgeAppTokensAsync(store, appKey)` (capability probing crosses the
  decorator stack).
- **U-5 multi-hit external-ID reverse lookup (upstream 3.0.10 Batch 2)**:
  `FeishuAppManager.TryResolveAllAppKeys` — when the host provider implements
  `IMultiAppLookupProvider`, all registered appKeys are returned (unregistered entries filtered,
  duplicates removed, lazy contexts never materialized); single-result providers fall back to the
  F-4 semantics.
- **U-7 user-dimension native CAS (upstream 3.0.10 Batch 3)**: `RedisUserTokenStore` implements
  `IUserTokenStoreCompareExchange` — a single Lua script performs "read stored value → compare
  plaintext → write/delete", removing the read-compare-delete race window from the TMF-05 purge path;
  `UserTokenManager` dispatches on the **raw** store's capability (stores without it keep the C-2
  decoded-plaintext comparison path, semantically unchanged).
- **U-2 v2 envelope read compatibility (upstream 3.0.10 Batch 3)**: `TokenStoreHelper` now reads the
  v2 versioned envelope (`v2:{n}|{token}` three-state normalization, `v2:expired|` projected as
  "no token", Redis TTL derivation included). The write side still emits the v1 relative-seconds form
  (C-2: the default form must not drift).

### Changed

- **BC-30 propagation**: `FeishuAppManager.GetWebApi` / `GetDefaultWebApi` are marked `[Obsolete]`
  in lockstep with the upstream base members (also clears CS0672). Migration: resolve
  `IAppScopeSwitcher` via DI + `UseAppScope` / `UseDefaultAppScope`, or
  `IAppContextHolder.SwitchToApp(appKey, appManager, sp)` (guarded switch-and-stay, semantically
  equivalent). Guard-test call sites use precise `#pragma warning disable CS0618`.
- **Credential-change purge CAS now compares the plaintext token** (a consequence of the relative-seconds
  storage form): under the relative-seconds form the stored encoded value drifts with wall-clock time, so
  whole-value re-encoded comparison is guaranteed to miss. Comparing decoded plaintext is time-independent
  and also removes the old implementation's conservative cost (skipping the purge when the same
  `refresh_token` carried a different persisted timestamp).
- **`Mud.Feishu.Redis` now consumes the official `Mud.HttpUtils.Redis` package**: the official store and
  the Feishu pipeline follow two different `ITokenStore` usage conventions (plaintext in + internal codec
  vs. writing the encoded value directly), so they are not interchangeable drop-ins;
  `FeishuTokenStoreCodec` is the interop anchor for official components that must read Feishu-shaped
  stored data.

### Docs & Tests

- New `.docs/MudFeishu-MudHttpUtils-3.0.7-适配说明.md` (per-item reconciliation with the upstream
  contract-change chapter and the downstream constraint ledger, non-migration decisions, pre-release
  checklist). `AGENTS.md` documents the upstream contract-change channel plus BC-30 / F-9 notes.
- `scripts/verify-build.ps1` step 0 now picks the local component source by "actually contains the
  declared version package" (fixes the stale-cache check being skipped when `artifacts` and
  `artifacts-debug` coexist).
- Verified: full quality gate green (0 build errors, diagnostic whitelist all 0, AotStrictMode net8.0
  0 violations, unit tests 6298/6348 — skips are missing net6.0 runtimes only); zero new warnings
  versus the pre-upgrade baseline; 29 new tests (C-2 dual-read matrix, F-4 lookup, Redis TTL behavior).

## [3.0.2] - 2026-10-12

> Theme: **mass event-callback expansion** (business domains 10 → 24, 205 events in total) and **adapting to
> `Mud.HttpUtils` 3.0.5**. No public contract removals; three behavior fixes (allowed-domain union
> registration, the token-recovery config hot-reload chain, user-token removal semantics).

### Dependency Upgrade

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`: `3.0.0` → **`3.0.5`** (must stay on the same version), unified
  across the 8 `Mud.HttpUtils*` declarations of 6 source projects; `README.md` / `README_EN.md` /
  `Mud.Feishu/README.md` / `Mud.Feishu.OpenTelemetry/README.md` and `AGENTS.md` dependency tables synced.
- Demo project `Mud.Feishu.*` package references: `3.0.0` / `3.0.0-rc3` → **`3.0.1`**
  (`Demos/Directory.Build.props` + 8 demo projects).

### Added

**Event callbacks (`Mud.Feishu.EventCallback`)**

- **Business domains 10 → 24, 205 events in total**: 14 new domains — `Application` (app), `Auth`,
  `MeetingRoom`, `Moments`, `ELearning`, `SecurityCompliance`, `Compensation`, `Payroll`,
  `CoreHr` / `CoreHrV2` (core HR v1 / v2), `Hire` (recruitment), `Performance`, `Acs` (smart access
  control) and `Helpdesk`.
- **219 new event-model files**, **124 new `FeishuEventTypes` constants**; the 18 `Generated/*JsonContext.g.cs`
  files incrementally register 443 `[JsonSerializable]` entries (the AOT source-generation pipeline stays in
  sync, no AOT005 leftovers).
- Main event families:
  - **Recruitment**: `hire.application.deleted_v1` / `stage_changed_v1`, `hire.offer.status_changed_v1`,
    `hire.ehr_import_task.imported_v1` and `.for_internship_offer.imported_v1`, `hire.talent.deleted_v1` /
    `tag_subscription_v1`, `hire.eco_account.created_v1`, `hire.eco_background_check.created_v1` / `canceled_v1`,
    `hire.eco_exam.created_v1`, `hire.referral_account.assets_update_v1`.
  - **Core HR**: `corehr.*` v1 (department / position / employment record / contract / pre-onboarding /
    transfer / offboarding / permissions) and v2 (location / company / cost center / custom org unit /
    job family-grade-position / probation / onboarding tasks / offboarding checklist / process nodes and
    approval CC / signed documents, 40+ events).
  - **Approval & tasks**: `approval.task.status_changed_v4`, `task.task.comment.updated_v1`, etc.
  - **Contacts & chat**: `contact.scope.updated_v3`, `im.chat.access_event.bot_p2p_chat_entered_v1`, chat-member
    changes.
  - **Meetings & meeting rooms**: `vc.bot.meeting_started_v1`, meeting-room create / update / delete.
  - **Security compliance**: device registration and device-change events. Also mail, moments (post / comment /
    emoji reaction), eLearning (course enrollment), compensation & payroll activities, performance
    (`performance.stage_task.open_result_v2`, `performance.review_data.changed_v2`), access control
    (`acs.user.updated_v1`, `acs.access_record.created_v1`), and helpdesk (ticket create / update / comment /
    approval notification).
  - **Card event callbacks (new `Card` business domain)**: `card.action.trigger` (the user clicked the card's
    "request callback" interactive component) — request body model `CardActionTriggerResult`
    (`operator` / `token` / `action` / `host` / `delivery_type` / `context`, covering `value`, `form_value`,
    `input_value`, `option`, `options`, `timezone`, `name` and other interaction fields), plus response models
    `CardCallbackResponse` / `CardCallbackToast` (16-language `i18n`) / `CardCallbackCard` (both `raw` and
    `template` forms) + `CardCallbackTemplateData`; the "respond within 3 seconds, no 3xx redirect" constraint
    is enforced accordingly.

**Upstream adaptation (`Mud.HttpUtils` 3.0.5)**

- **`FeishuHttpClientFactory` now uses the upstream `EnhancedHttpClientOptions.Clone()`**: the hand-written
  field-list Clone in this repo is deleted, so new upstream properties (e.g. 3.0.5's `JsonEncoder`) no longer
  need to be mirrored here — eliminating this class of "new property silently dropped" drift.
- **`UrlValidator` domain registration is now an incremental union**: changed from the wholesale-replace
  semantics of `ConfigureAllowedDomains` to per-item `AddAllowedDomain` (the list extracted into
  `FeishuAllowedDomains`), so host custom domains registered before `AddFeishuApp` are no longer silently
  discarded.

### Changed

- **User-token removal semantics are now handled by the base class**: with `Mud.HttpUtils` ≥ 3.0.4 the bridge's
  `RemoveAsync` unconditionally writes through to the persistence layer, so `UserTokenManager` **no longer
  overrides** `RemoveTokenAsync`; removal / logout now reaches the persistence layer in multi-instance and
  cold-start scenarios as well (`PurgeGate` semantics unchanged), with cold-start comparison tests and
  constraint comments added.
- **`TokenRecoveryOptions` binding switched to the single-level "capture section + bind in place + Apply"
  scheme**: no longer read indirectly through the `IOptionsMonitor<TDep>` secondary pipeline (which was
  observed to return stale DTOs after a configuration reload); hot reload now depends on a single
  `ChangeTokenSource`. The AOT constraint "no `Configure<T>(IConfiguration)` reflection binding" is unchanged.
- The "default app rejected" message of `FeishuAppManager` is replaced by a registration-list allow-list
  authorizer (`AppKeyAllowListAuthorizer.FromPredicate` + `IFeishuAppManager.HasApp`); only the message wording
  and comments changed.

### Fixed

- **8 event models renamed where the file name did not match the primary class name**: `ApprovalResult` →
  `ApprovalTaskResult`, `DriveFilePermissionMemberAdded` → `DriveFilePermissionMemberAddedResult`,
  `ChatMemberUserAddedResult` → `ChatMemberUserDeletedResult` / `ChatMemberUserWithdrawnResult`, the misplaced
  `MeetingMeetingEndedResult` under `Mail` → `MessageReceivedResult`, `DepartmentCreatedEventResult` →
  `DepartmentCreatedResult`, `DepartmentDeleteResult` → `DepartmentUpdateResult`, `RoomCreatedResult` →
  `RoomUpdatedResult`. Class-name drift decouples the `[JsonSerializable]` entries in
  `Generated/*JsonContext.g.cs` from the real classes; a contract guard now blocks this.
- **Demo `FeishuDriveService` delete file / folder**: `cancellationToken` is now passed explicitly (previously
  the argument positions were off, so the cancellation token was passed to the wrong parameter).
- `.gitignore` ignores the `/.scratch` local staging directory.

### Docs & Tests

- `Mud.Feishu.EventCallback/README.md` rebuilt as a full event table (205 events / 24 domains, 54 → 205 rows),
  aggregated directly from the source `[GenerateEventHandler]` annotations; new events must be added to this
  table.
- New contract guard `EventCallbackContractGuards` (5 rules): constants must be wired by
  `[GenerateEventHandler]` (no dead constants); file names must match the primary class name; every domain must
  have a `Generated/{Domain}JsonContext.g.cs` already hooked into the resolver chain; every `*Result` must be
  reflectively resolvable to a generated handler; wired event types must appear in the README event table.
- New event-payload smoke tests `EventPayloadSmokeTests` + `TestData/EventCallback/event-examples.json`
  (verbatim replay of the official "event body examples", asserting 1:1 model coverage for every non-empty key
  — a field-name mismatch fails on the spot).
- `FeishuHttpClientFactoryTests` / `UserTokenManagerTests` / `FeishuServiceCollectionExtensionsTests` extended
  alongside the upstream adaptation (field equivalence, config hot-reload semantics and cold-start comparison
  regressions).

## [3.0.1] - 2026-10-07

> Theme: **adapting to `Mud.HttpUtils` 3.0.0** (upstream removal of `UseApp` / `UseDefaultApp` /
> `BeginScope(string)`, replaced by scope-based switching) and **event v1.0 / v2.0 dual-format parsing fixes**.
> This repo has **no public contract removals** (the three legacy members are re-declared by
> `IFeishuAppContextSwitcher` and marked `[Obsolete]`); a few behavior fixes follow (`EventData.CreateTime` unit,
> `EventData.Event` write-side type) — read "Changed" and "Migration Notes" before upgrading.

### Dependency Upgrade

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`: `2.0.9` → **`3.0.0`** (must stay on the same version).
- From upstream 3.0.0 on, generated classes in non-HttpClient mode **no longer emit**
  `UseApp(string)` / `UseDefaultApp()` / `BeginScope(string)` by default, and `IAppContextSwitcher` no longer
  declares them; generated classes **automatically attach** `IAppScopeSwitcher` (`UseAppScope` /
  `UseDefaultAppScope`).

### Added

- **`IFeishuAppContextSwitcher` now inherits `IAppScopeSwitcher`**: downstream code can program directly against
  the recommended surface (scope-based switching, context automatically returned on dispose).
- **`IFeishuAppContextSwitcher` re-declares the three legacy members with `[Obsolete]`**: the public contract is
  preserved, and 188+ generated implementation classes **keep emitting** those three members with behavior
  identical to 2.0.9. They will be removed together with upstream in the next major version of this SDK; migrate
  to `UseAppScope` / `UseDefaultAppScope`.
- **Shared event parser `FeishuEventDataParser`** (Abstractions, single source of truth): v2.0 / official v1.0
  shape / `data`-wrapped fallback shape are parsed uniformly; the WebSocket and Webhook channels removed their
  parallel implementations.
- **Failed-event persistence**: the WebSocket channel can persist handling failures via `AddFailedEventStore<T>()`
  / `AddFailedEventStore(instance)` registering `IFailedEventStore` (TryAdd semantics, not registered by
  default, zero behavior change when absent; only the business-failure branch persists — cancellation /
  interception terminal states do not). Accompanied by the new
  `FeishuWebSocketOptions.FailedEventInitialRetryDelaySeconds` (default 10; non-positive falls back to the
  default).
- **Read extension `GetEventRawJson()`**: uniformly reads the raw JSON carried by `EventData.Event`.

### Changed

- **`FeishuAppManager.GetWebApi` / `GetDefaultWebApi` internals migrated**: from the generated
  `UseApp(appKey)` / `UseDefaultApp()` to `IAppContextHolder.SwitchToApp(appKey, this, serviceProvider)` /
  `SwitchToDefaultApp(this)` (**semantically equivalent**: full guard + immediate switch + no return + context
  returned). External behavior is unchanged (still returns the DI-resolved service instance whose `Current` is
  bound to the target app).
- **`EventData.Event` is now uniformly written as the raw JSON string** (the WebSocket channel used to write a
  `JsonElement`). The property type stays `object?`; read via the extension `GetEventRawJson()` — it converges
  to `string?` in the next major.
- **Behavior change**: `EventData.CreateTime` semantics corrected from "seconds" to **milliseconds** (aligned
  with the XML docs; the v1.0 `ts` is included in the unified heuristic). Hosts consuming seconds must
  multiply by 1000 or switch to `DateTimeOffset.FromUnixTimeMilliseconds`.
- **`IgnoreUnknownEventTypes` defaults intentionally differ across channels** (Webhook=true / WS=false, defaults
  unchanged): both Options XML docs state the reason, and a one-time alignment warning is emitted at startup
  when WS runs with `false`.

### Fixed

- **The Webhook channel mapped v1.0 event fields incorrectly**, so v1.0 events were rejected with 400 by the
  empty-identifier fail-closed gate; **the WebSocket channel silently dropped entire official v1.0 frames**
  (root-level `uuid`/`token`/`ts`, event fields inside `event`). Both channels now route v1.0 events normally
  (the event type is the raw `event.type` value — register handlers by that key).
- **The v1.0 root-level `token` is now parsed into a synthetic Header** (`Schema == null`; fields taken from the
  root-level `uuid`/`token`/`ts` and `event.*`): detect v1.0 with `Schema == null`, not `Header == null`;
  signature verification and Header-dependent idempotency logic work again for v1.0.

### Migration Notes

- Calling `UseApp` / `UseDefaultApp` / `BeginScope(string)` through an `IFeishuAppContextSwitcher`-typed
  reference produces a **`CS0618`** warning (intentional guidance). Hosts with `TreatWarningsAsErrors` should
  switch to `UseAppScope` / `UseDefaultAppScope` (identical guards, plus automatic context return on dispose),
  or temporarily set `NoWarn CS0618`.
- Semantic reminder: `UseAppScope` **switches back automatically** at the end of the `using`; `UseApp` keeps the
  context until the next switch (long-lived hosts must switch back explicitly).

## [3.0.0] - 2026-09-28

> 3.0 is a comprehensive upgrade focused on **production reliability and performance**: first-class Native AOT
> support, hardened token & multi-app management, a Webhook security baseline, Redis / WebSocket stability work,
> and a unified configuration structure. It contains many breaking changes — be sure to read "Upgrade
> Highlights" before upgrading; per-item details live in the rc2 / rc3 records below.

### 🌟 Core Upgrades

- **Full Native AOT support**: net8.0+ end-to-end source-generated JSON serialization and configuration binding;
  the AOT strict-mode gate guarantees zero reflection warnings; an end-to-end verification project ships with
  it (`Demos/Mud.Feishu.AotVerification`). Configuration DTOs no longer use `required` — validation is
  uniformly carried by `Validate()`.
- **Token & multi-app management rework**: multi-app config hot reload (`BaseUrl` / `TimeoutSeconds` effective
  at runtime, incremental application per AppKey), per-app auth clients and endpoint isolation, purge the store
  immediately on credential change, 401 token recovery actually taking effect (cascading store purge),
  Memory/Redis token key layout unified (`TokenKeyBuilder` as the single source of truth), retryability
  classification for OAuth failures, optional token-store encryption.
- **Webhook security baseline**: distributed dedup mandatory in production, multi-app deployments must declare
  `ExpectedAppId`, interception semantics and default-handler routing tightened, every recoverable failure
  answers 503 so Feishu redelivers — eliminating cross-app event bleed and permanent event loss.
- **WebSocket reliability work**: connection lifetime decoupled from the caller's cancellation token, handling
  failures ACK `code=500` so the server redelivers, concurrency gate and reconnect circuit breaker, zombie
  connections eliminated, messages metered uniformly by UTF-8 bytes, backpressure moved forward to the receive
  path.
- **Redis hardening**: `rediss://` really enables TLS, four key families uniformly constructed and escaped
  (`RedisKeyBuilder`), dedup races made atomic in Lua, full Cluster node coverage, classifiable exceptions
  (`FeishuRedisFailureKind`), SeqID dedup switched to a capacity window, plus a new ops-diagnostics facade and
  metrics.
- **Quality gate**: a brand-new `verify-build.ps1` gate (cache self-check, all-TFM build + diagnostic
  whitelist, AOT strict-mode smoke, TRX test-count assertions), wired into CI.

### ⚠️ Upgrade Highlights (Breaking Changes)

**Configuration structure**

- Configuration is unified into nested groups: `FeishuWebSocketOptions` → `Reconnect.*` / `Certificate.*`;
  `FeishuAppConfig` / `RedisOptions` → `HttpRetry.*` / `CircuitBreaker.*` / `Connection.*`. Legacy flat JSON keys
  are still auto-backfilled, but **C# code must move to the nested API**; logging switches are unified to
  `Logging:LogLevel:*`.
- `nuget.config` tightened to a package-source lock; the dependency is pinned to `Mud.HttpUtils` 2.0.7.

**Token & multi-app**

- Redis token key layout changed (`feishu:token:*` → `feishu:{appKey}:token*`); old keys are no longer read;
  `SingletonFeishuTokenStoreFactory` is deprecated — use `IFeishuTokenStoreFactory.Create(appKey)`;
  `AddFeishuRedisTokenStore` no longer registers `ITokenStore` / `IUserTokenStore` singletons.
- Token invalidation cascades into the persistence layer; tenant 401 recovery no longer falls back to
  user-level; `SetDefaultApp` genuinely takes effect at runtime.
- Old token keys for AppKeys containing special characters (`* ? [ ] : \`) are no longer reachable — affected
  deployments are advised to rotate the AppSecret after upgrading.
- The injected `IFeishuAppContext` becomes a stateless forwarding proxy (resolves the current default app on
  every access); for instance-snapshot semantics set `FeishuAppOptions.ForwardDefaultAppContext=false`.
- Config hot reload is on by default (`EnableConfigReload=true`); set it explicitly to `false` to keep the old
  "changes require a restart" semantics.
- OAuth refresh failures are classified by retryability: `invalid_grant` etc. purge the refresh token and force
  re-authorization.

**Webhook**

- Production startup fails when no distributed dedup is registered (single-instance deployments can set
  `FeishuWebhook:AllowInMemoryNonceDedupInProduction=true`); multi-app setups must configure `ExpectedAppId`;
  `Build()` requires at least one globally registered default handler.
- `BeforeHandleAsync` returning `false` changes from 500 (retryable) to 200 (consumed); set
  `InterceptionAckMode=Retryable` for "intercept then redeliver". The interceptor combination defaults to
  `Merge` (old behavior: `InterceptorFallbackMode=AppOnly`).
- Decryption timeout changes from 400 to 503 (recoverable failures let Feishu redeliver).

**WebSocket**

- Terminate connections via `DisconnectAsync()` / `DisposeAsync()` (the token passed to `ConnectAsync` only
  governs connect + authentication); `StartReceivingAsync` is deprecated; `MessageReceived` is now dispatched
  concurrently (possibly out of order) — use `IMessageHandler` when ordering matters.
- Synchronous `Dispose()` no longer stops the service; configuration upper bounds are tightened and validated
  at startup (reconnect budget, timeouts, message size, …); constructor signatures changed (dead parameters such
  as `seqIdDeduplicator` / `ProcessingTask` removed).
- Metrics API migration: static writable properties such as `FeishuMetrics.WebSocketConnectionObserver` are
  removed — use `RegisterWebSocketMetricsSource(...)`.
- Connections default to the host allowlist `*.feishu.cn;*.larksuite.com`; configure `AllowedHostSuffixes` for
  self-hosted endpoints.

**DTO renaming (fixes source-generation name collisions SYSLIB1031)**

- `DepartmentsV1.DepartmentLeader` → `DepartmentLeaderV1`, `DepartmentDetail` → `DepartmentDetailV1`,
  `ApprovalExternal.ApprovalCreateViewers` → `ExternalCreateViewers`, etc. (7 groups; the full list is in
  3.0.0-rc2).

**Redis dedup**

- SeqID dedup keys gain a scope isolation dimension (default `AppKey|MachineName`); the window changes from TTL
  to capacity (`FeishuRedis:SeqIdWindowCapacity`, default 100000); `GetCacheCount()` etc. are narrowed to true
  in-window values.
- Invalid `RedisOptions` values now fail startup validation; `NonceFailureMode` applies only to Redis
  connection-class failures.

### 🐛 Key Fixes

- **Permanent event loss**: handling failures always ACKed 200, Nonce infrastructure failures disguised as 403,
  handler type mismatches silently dropped, decryption timeouts swallowed into 400 — all fixed; recoverable
  failures uniformly answer 503 so Feishu redelivers.
- **Token correctness**: 401 recovery reusing the rejected token, tenant retries being injected with a user
  token, Redis expired tokens getting TTL=0 and never expiring, credential-change purging being ineffective on
  the in-memory backend.
- **Stability**: the dedup factory self-resolving into `StackOverflowException`, an empty prefix purging the
  whole database, WebSocket zombie connections / deadlocks / `ObjectDisposedException` races, the Redis
  connection string leaking credentials in logs.
- 200+ secondary fixes and hardenings completed in the same round; details are in the rc2 / rc3 records below.

## [3.0.0-rc3] - 2026-09-23

> This release focuses on **Webhook multi-region security hardening, token & multi-app hot-reload stability, Redis dedup & token-store correctness, and WebSocket connection reliability**. It includes breaking changes — read "Upgrade Notes" before upgrading.

### 🌟 Highlights

- **Webhook replay protection & multi-app isolation**: Production now mandates distributed dedup, multi-app setups must declare `ExpectedAppId`, interception semantics and default-handler routing are tightened — eliminating cross-app event bleed and permanent event loss.
- **Token & multi-app hot-reload stability**: the default-app bridge switches to "resolve-on-access" (DI injection immediately follows runtime switching), per-app auth clients now actually take effect, Memory/Redis token key layouts are byte-identical, and `OnConfigurationChanged` no longer blocks the config callback thread.
- **Redis dedup & token-store correctness**: SeqID moves to a capacity window, `ClearCacheAsync` really deletes, `rediss://` really enables TLS, the token key prefix is configurable, and an ops diagnostics facade plus metrics are added.
- **WebSocket connection reliability**: connection lifetime is decoupled from the caller's `CancellationToken`, zombie connections are eliminated, fragmented-message boundaries are protected, and concurrent dispatch plus liveness/discard metrics are added.

### ⚠️ Upgrade Notes (Breaking Changes / Required Actions)

**Webhook (WHF-R3)**

- **In-memory Nonce dedup is now blocked in Production**: when no distributed deduplicator is registered (`AddFeishuRedisDeduplicators()`), the host **fails to start** in `ASPNETCORE_ENVIRONMENT=Production`. For confirmed single-instance deployments, set `FeishuWebhook:AllowInMemoryNonceDedupInProduction=true` (emits a risk warning).
- **`ExpectedAppId` is mandatory in multi-app setups**: with more than one entry under `FeishuWebhook:Apps`, a missing `ExpectedAppId` fails startup — it is the only guard against cross-app event bleed caused by an `EncryptKey` misconfiguration. Single-app deployments: still optional.
- **Interception semantics changed**: `BeforeHandleAsync` returning `false` now ACKs with `200` (consumed, dedup mark written, Feishu stops retrying) instead of the previous `500` (retryable). For "intercept then retry", set `FeishuWebhook:InterceptionAckMode=Retryable` (responds `503`, no dedup mark).
- **A globally registered default handler is now required**: `Build()` now requires at least one `AddHandler<T>()` without an appKey, otherwise startup fails (previously an app-specific handler could silently become the default and mis-route other apps' events).
- **App-scoped interceptors no longer silently shadow global interceptors**: the combination strategy now defaults to `Merge` (global first, then app-scoped). The old behaviour — one app-scoped interceptor discarding **all** global ones, silently disabling security/audit cross-cutting for that app — is preserved via `FeishuWebhook:InterceptorFallbackMode=AppOnly` (a startup Warning then lists the shadowed global interceptors). `AppThenGlobal` runs app-scoped first.
- **Decryption timeout now maps to 503 instead of 400**: it used to be swallowed into `null` → 400 (terminal, Feishu does not retry). Timeouts are recoverable, so they now request a redelivery; the decryption CTS is also linked to `RequestAborted`.
- **A missing `IEnvironmentService` now fails startup**: it is the sole source of the "production in-memory Nonce" decision, so without it the production lock would silently disappear.
- **Empty `appKey` no longer degrades the dedup key to a bare key** (cross-app collision surface); a fixed sentinel prefix is used instead.

**Token & Multi-App (TMR2 / TMF2)**

- **Default-app context bridging semantics changed (default on)**: the injected `IFeishuAppContext` is now a stateless forwarding proxy that resolves the current default app on every access. If your code casts it to the concrete `FeishuAppContext`, set `FeishuAppOptions.ForwardDefaultAppContext=false` to restore the instance-snapshot semantics (requires a restart).
- **AppKey key layout changes for special characters**: `TokenKeyBuilder` now escapes glob metacharacters (`* ? [ ] : \`). Before upgrading, make sure your AppKey contains none of these characters; otherwise old token keys will no longer match after a hot reload.
- **Custom `UserTokenStoreBase` subclasses**: if you override `KeyPrefix`, change it to delegate to `TokenKeyBuilder.BuildKeyPrefix(appKey)` to stay consistent with the Memory/Redis backends.
- **`OnConfigurationChanged` no longer waits synchronously for the purge to finish** (previously up to ~10s); hosts that must wait should poll for the purge gate to be removed as the completion signal. The D10 semantic (old tokens are not restored until the purge completes) is unchanged.
- **Runtime-added apps can now be correctly taken over by configuration**: once an app added via `AddApp` is declared in config, its "runtime-added" flag is now revoked, so later removing it from config is no longer permanently ignored.

**Redis (R2 series)**

- **New `FeishuRedis:SeqIdWindowCapacity`** (default 100000; non-positive fails startup): the SeqID dedup window changed from a "TTL time window" to a "capacity window"; `GetCacheCount()` / `GetMaxProcessedSeqId()` are narrowed to the true values inside the window and must not be used to infer remaining dedup headroom.
- **New `FeishuRedis:TokenKeyPrefix`** (default `feishu`): the token key prefix is now configurable, so multiple environments sharing a Redis can isolate their key spaces.
- For AppKeys containing `:` / `\`, the old token keys are no longer reachable — rotate the AppSecret on affected deployments (the first token fetch after upgrade re-populates the store).

**WebSocket**

- **Connection lifetime no longer follows the caller's `CancellationToken`**: the token passed to `ConnectAsync` only governs the "connect + authenticate" phase. To terminate, call `DisconnectAsync()` / `DisposeAsync()`; to recover, call `IFeishuWebSocketManager.ReconnectAsync()`.
- **`IFeishuWebSocketClient.StartReceivingAsync` is deprecated**: the receive loop is managed by `ConnectAsync`; it is an idempotent no-op when a loop is already running, and throws `InvalidOperationException` when not connected.
- **`MessageReceived` threading contract changed**: from "synchronous, serialized on the receive-loop thread, frame-ordered" to "dispatched inside the concurrency lease — may run concurrently and out of order". Use `IMessageHandler` when ordering or timeout protection is required.
- **`PingPongMessageHandler` / `HeartbeatMessageHandler` constructors no longer accept `FeishuWebSocketOptions`**.
- Configuration upper bounds tightened (fail-fast at startup): `Reconnect.TotalBudget <= 7 days`, `Reconnect.BaseDelayMs/MaxDelayMs <= 1 hour`, `MessageSizeLimits.MaxTextMessageSize <= 10MB`, `ConnectionTimeoutMs/AuthTimeoutMs/AuthGateTimeoutMs <= 5 minutes`.
- Inbound payloads are no longer logged verbatim (now "length + 200-char redacted preview"); connection URL logging strips the query string entirely.

### ✨ Added

- **Webhook**: `FeishuWebhook:AllowInMemoryNonceDedupInProduction`, `FeishuWebhook:InterceptionAckMode`, `FeishuWebhook:InterceptorFallbackMode` (default `Merge`), `FeishuWebhook:NonceTtlSeconds` (when set, enforces the replay-window invariant `> TimestampToleranceSeconds`); startup-time options validation (host fails to start instead of first-request 500); `intercepted` / `intercepted_retryable` metric tags; unmatched event type and soft-timeout observability (Warning + `unhandled` / `timeout_overshoot` metrics); health-check data item `nonceDedup` (Production + in-memory = `Degraded`); a startup summary log (dedup form, timestamp tolerance, Nonce TTL, replay-window invariant, per-app handler/interceptor registration self-check).
- **Token / Multi-App**: purge-chain observability `PurgeTokenStoreFailureEvent` (EventId 5601, hosts can build alerts on it); hot-reload race harness `HotReloadRaceHarness` test infrastructure.
- **Redis**: ops diagnostics facade `IRedisDeduplicationDiagnostics.GetSnapshotAsync` → `RedisDeduplicationDiagnosticsSnapshot`; Redis metrics `feishu.redis.operation` / `feishu.redis.operation.duration` / `feishu.redis.scan.keys` (on the existing `Mud.Feishu` Meter); optional health-check registration (`registerHealthCheck=false`).
- **WebSocket**: connection liveness probe `ConnectionLiveness` (`ReceiveLoopAlive` / `LastReceiveUtc` / `IdleMs` / `IsZombie`); liveness/discard metrics `feishu.websocket.receive.idle_ms` / `.loop_alive` / `.zombie` / `feishu.websocket.frames.discarded` (all four controlled discard points are now counted); health-check `data` adds `receive_loop_alive` / `last_receive_utc` / `idle_ms` / `is_zombie`; architecture invariants I13–I16 and 7 contract guards; `FeishuWebSocketServiceBuilder` no longer silently drops `IFeishuEventInterceptor` registered directly in DI.

### 🐛 Fixed

- **Process crash (P0)**: with `FeishuDeduplication:Mode=Distributed` and no Redis registered, the dedup factory resolved itself → `StackOverflowException`; now it builds normally and warns that "event dedup is still in-memory".
- **Permanent event loss**: Nonce/dedup infrastructure failures (Redis connection/timeout) are no longer disguised as a 403 signature failure; they now surface as **503** via `FeishuDeduplicationFatalException` so Feishu retries; client disconnects (`OperationCanceledException`) are no longer swallowed into a 403 written to an already-aborted connection; handler `SupportedEventType` mismatches are no longer silent; decryption timeouts are no longer swallowed into a 400.
- **Webhook tech debt**: in-memory Nonce dedup now has a capacity bound (was 0 = unbounded growth); multiple concurrently failing handlers now report **all** exceptions (`await Task.WhenAll` rethrows only the first, so the aggregate branch was unreachable); an unreachable compensation `catch` in `AcquireAsync` was removed (self-contradictory `when` filter); registry `Freeze`/`Register` are evaluated under the same lock, closing the check-then-write race; the failed-event retry service clears the AppKey context at the end of each poll round.
- **Token / Multi-App**: default-app DI bridge switched to resolve-on-access (fixed using stale credentials / a disposed context); per-app auth client now references the generated implementation at compile time and fails fast on assembly error (fixed "credentials sent to the wrong region"); Redis token key prefix de-escaping + SCAN glob literal escaping fixed (fixed purge/enumeration never matching); OAuth refresh-failure classification tightened (transient faults no longer wrongly purge the refresh token); runtime-added apps can be correctly taken over by config; credential-change purge now also catches non-instantiated apps.
- **Redis**: `rediss://` now really enables TLS (previously plaintext to a TLS port); `RedisFeishuEventDistributedDeduplicator` now declares `IDisposable`; event Lua guards `tonumber(timestamp)`; `GetStatusAsync` uses server `TIME`; token SCAN APIs use async batched deletes (500/batch); health check is `Healthy` on a successful PING; `FeishuRedisFailureKind.InvalidArgument` is now actually produced; 4 compiler warnings cleared.
- **WebSocket**: zombie connection when the receive loop exited on cancellation (P0-1); `StartReceivingAsync` idempotency guard could start a second loop (P1-1); oversize fragment discard did not drain, desyncing the WS boundary (P1-2); unclamped reconnect window disabled auto-reconnect (P1-3); inbound full payload logged verbatim (P1-4); lock-release order caused `ObjectDisposedException` (P1-5); `Connected`/`Disconnected` raised while the lock was held self-deadlocked; socket type narrowed to the abstract `WebSocket`; `ResolveMaxTextMessageBytes()` integer overflow (P2-1); `IsConnected` dual source of truth (P2-2).

### 📝 Docs & Tests

- `Mud.Feishu.Redis/README.md`: measured key samples (double colon and `\:` escaping), new configuration rows for `SeqIdWindowCapacity` / `TokenKeyPrefix`, new "Operations diagnostics" and "Observability" sections.
- `Tests/Mud.Feishu.Redis.Tests/README.md`: removed the non-existent `RedisFeishuEventDistributedDeduplicatorWithFallback` section, refreshed to the actual structure/stack.
- Added 7 contract guards validated by "intentional-violation canary" tests; WebSocket gained liveness / fragment-drain / wiring-reconnect / config-bound / stress cases; `verify-build.ps1` step 4 appends `--filter "Category!=Stress"`.
- `AGENTS.md` / README D8/D10/D13 wording and AppKey naming constraints synced.

## [3.0.0-rc2] - 2026-09-18

### ⚡ Full Native AOT Adaptation

- All source projects enable `IsAotCompatible` / `EnableAotAnalyzer` / `EnableTrimAnalyzer` /
  `TrimMode=full` on net8.0+ (not enabled on netstandard2.0 / net6.0).
- Source-generated configuration binding via `EnableConfigurationBindingGenerator`; config DTOs avoid
  the `required` modifier in favor of `Validate()` methods, removing the runtime reflection binding path.
- New `FeishuJsonAot` AOT-safe serialization helper (net8+ resolves `JsonTypeInfo` from options, with a
  reflection fallback only when no resolver is present) plus custom-context entry points such as
  `FeishuJsonDefaults.ConfigureUserResolver`.
- Protobuf static (reflection-based) serialization calls replaced with compile-time models
  (`FeishuWebSocketProtoModel`); rd.xml trim roots ship with Abstractions / DataModels.
- `verify-build.ps1` now includes an AOT strict-mode smoke gate: per-project `AotStrictMode` +
  `--no-incremental` builds asserting a successful build and 0 `AOT00x` / `IL2026` / `IL3050` diagnostics.
- New `Demos/Mud.Feishu.AotVerification` end-to-end verification project (win-x64 / linux-x64 dual RID).
- READMEs and NuGet package descriptions (including `AOT;NativeAOT` tags) updated to document AOT support.

### 🔧 Dependency Upgrade (Mud.HttpUtils 2.0.67)

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator` pinned to **2.0.7** in 5 projects: the generator-side
  `JsonContextScaffolder` now emits `TypeInfoPropertyName` for duplicate type-info names, fixing the
  9 `SYSLIB1031` warnings from 7 duplicate DTO names in `Mud.Feishu.DataModels`; the contract guard
  `TokenMultiAppContractGuards.ExpectedVersion` is synced to 2.0.6.

### ⚠️ Breaking Changes / Behavior Changes (Token & Multi-App R2)

- **User tokens remain renewable after the access token expires (P0-1 / TMA2-01)**: previously
  `RefreshUserTokenAsync` read the refresh token through `GetTokenInfoAsync`, which returns `null`
  once the access token is expired — after a restart or multi-instance takeover the refresh token was
  unreachable and the user got a permanent 401 until re-authorization. A dedicated
  `LoadRefreshCandidateAsync` now reads the refresh token independently of access-token validity and
  validates the refresh token's own expiry.
- **Token key layout unified into a single source of truth (P0-2 / TMA2-02)**: the Memory and Redis
  backends previously built keys independently (Redis escaped `:` inside `tokenType`, Memory did not),
  so the same token type landed on different physical keys and enumerated values could not be fed back
  to `RemoveAsync`. All keys are now produced by `TokenKeyBuilder` and are byte-identical on both
  backends.
- **Restore threshold is now the same as the cache-validity threshold (P1-1 / TMA2-04)**: the restore
  deprecation threshold previously used `threshold/2`, which caused a "restore → immediately
  invalidated → restore again" self-loop; `TokenRefreshThreshold` is now the single threshold.
- **Credential changes purge stored tokens immediately (P1-2 / TMA2-05)**: `RebuildAppContext` now
  detects `(AppId, AppSecret)` changes and clears the app's persisted tokens; non-credential changes
  (e.g. `BaseUrl`/`TimeoutSeconds`) still keep the token hot-migration.
- **OAuth refresh failures are classified (TMA2-06)**: non-retryable errors (`invalid_grant`,
  revoked/expired refresh token, scope mismatch) purge the stored refresh token and return `null`
  (entering component back-off); retryable errors throw `FeishuException` for visibility.
- **`AppInstantiated` event (TMA2-07)**: fired on first access of an app to incrementally register
  its token managers with the background refresh service (netstandard2.0 has no HostedService host —
  see README for its background-refresh coverage).
- **`AddApp` default-app write is now locked (TMA2-08)**, **hot reload is two-phase transactional
  (TMA2-09)**, **`TryGet*` never throws (TMA2-10)**, **assembly-failure scope leak and registered-context
  disposal fixed (TMA2-11)**, **exception filter narrowed to a transient whitelist (TMA2-12)**,
  **dead switches/constants removed (TMA2-13/14)**, **Redis expired refresh tokens are deleted instead
  of written with an endless TTL (TMA2-15)**, **restore backfills OpenId (TMA2-16)**,
  **Redis token store registration-order guard (TMA2-17)**, **docs/version consistency (TMA2-18)**,
  **Redis connection string masked in logs (TMA2-19)**, **multi-tenant deployment guide (TMA2-20)**.

### ⚠️ Breaking Changes / Behavior Changes

- **Redis token key layout changed (TOK-1)**: `PerAppRedisTokenStoreFactory` replaces
  `SingletonFeishuTokenStoreFactory`; keys move from `feishu:token:*` to `feishu:{appKey}:token*`,
  matching the in-memory layout. Previously all apps shared one key space and overwrote each other's
  tokens (random 401s / crossed tokens).
- **`FeishuAppConfig` no longer uses `required`**: configuration binding is now source-generated
  (AOT-safe) and cannot satisfy `required` members. Validation is fully covered by
  `FeishuAppConfig.Validate()`.
- **Enhanced HttpClient assembly is now baseline-driven (ARC-2 Step 1)**: the client assembly in
  `FeishuAppManager` starts from `IOptions<EnhancedHttpClientOptions>`, so the 10 fields that silently
  fell back to defaults (plus `AppAccessAuthorizer` added in 2.0.5) now take effect.
- **Configuration hot reload is enabled by default (ARC-1)**: `FeishuAppOptions.EnableConfigReload`
  defaults to `true`. Set it to `false` to restore the previous "restart required" semantics.
  Hot reload now also covers `BaseUrl` / `TimeoutSeconds` (ARC-7): an extra DI-aware
  `IHttpClientBuilder.ConfigureHttpClient(IServiceProvider, HttpClient)` action reads the current
  values from `IOptionsMonitor<List<FeishuAppConfig>>` on every `CreateClient`, so multi-region
  switching (`open.feishu.cn` ↔ `open.larksuite.com`) no longer requires a process restart.
- **NuGet package source locking (SEC-1)**: `nuget.config` now uses `<clear />` plus
  `packageSourceMapping` (`Mud.HttpUtils*` mapped to both the local folder source and nuget.org,
  local first). Note: `Mud.HttpUtils 2.0.5` is not published on nuget.org (highest is 2.0.2), so a
  hosted CI runner cannot satisfy the exact version constraint (`NU1603` is promoted to an error in
  this repo). This is a pre-existing issue - see COMP-5.

### ✨ Added

- `IFeishuHttpClientFactory` for unified per-app client assembly.
- Token store encryption (ENH-1): `EncryptedTokenStore` / `EncryptedUserTokenStore` /
  `EncryptedFeishuTokenStoreFactory` + `FeishuAppOptions.EnableTokenEncryption` (off by default).
- Multi-app configuration hot reload (ARC-1).
- AOT-safe JSON entry point `FeishuJsonAot`.
- Build/quality gate script `scripts/verify-build.ps1` and CI wiring for cache freshness + diagnostics.
- Encrypted-store marker contract (ENH-2): `EncryptedTokenStore` / `EncryptedUserTokenStore` now also
  implement `IEncryptedTokenStore`, and accept an optional `ILogger?` (source/binary compatible).
- Docs: `documents/ErrorHandling.md` (download-method error contract + the "HTTP 200 with JSON error
  body" residual risk) and `documents/ResponseCaching.md` (`[Cache]` usage, mandatory per-app key
  isolation for multi-app deployments).
- Tests: `FeishuClientEndpointHotReloadTests`, `DownloadErrorSemanticsTests`, plus 4 new
  `EncryptedTokenStoreTests` cases (marker contract and throttled decrypt-failure logging).

### 🐛 Fixed

- net8.0+/net10.0 threw `NotSupportedException` for types not covered by a source-generated
  `JsonSerializerContext` (cross-TFM inconsistency); the resolver chain now ends with a reflection
  fallback and `ConfigureUserResolver` is idempotent and thread-safe.
- `IFeishuAuthentication` was never registered, making the multi-app context path unreachable.
- Multi-app Redis token keys had no `appKey` dimension; `GetTokenTypesAsync` truncated token types
  containing `:` (TM-04).
- `[HttpJsonSerializable]` on open generics caused `AOT006` / `SYSLIB1030` on net8.0+.
- 204 `NU1603` version-drift warnings (5 projects, 7 `PackageReference` entries now pinned to 2.0.5).

### ⚠️ Breaking Changes / Behavior Changes (Redis Review Remediation R-series)

> Corresponds to M0–M4 of `.docs/MudFeishu-Redis-Review-Remediation-Plan.md`.

- `AddFeishuRedisTokenStore` no longer registers `ITokenStore`/`IUserTokenStore` singletons; use
  `IFeishuTokenStoreFactory.Create(appKey)` instead.
- `RedisOptions` `NonceTtl`/`SeqIdCacheExpiration`/key prefix illegal values (0/negative/empty) now
  **fail at startup** (previously silently ineffective or runtime server errors).
- Event dedup `ttl` sub-second values throw `ArgumentOutOfRangeException`; `MarkAsCompletedAsync` no
  longer creates permanent records for non-existent keys.
- SeqID dedup keys now include a `scopeKey` isolation dimension (default `AppKey|MachineName`);
  multi-instance deployments no longer cross-deduplicate. `GetCacheCount`/`GetMaxProcessedSeqId`
  semantics narrowed to "within TTL window".
- `NonceFailureMode` only applies to Redis connection failures; server/config errors propagate directly.
- All four Redis key families (event/nonce/seqid/token) now use `RedisKeyBuilder` for unified
  construction with segment escaping (`:` → `\:`) to prevent cross-segment collisions.

### 🐛 Fixed (Redis Review Remediation R-series)

- Fixed `ClearCacheAsync` degrading to full-database deletion when `SeqIdKeyPrefix` is empty (R-01/P0).
- Fixed `redis://`/`rediss://` addresses not connecting (switched to `ConfigurationOptions.Parse`;
  `rediss://` auto-enables TLS) (R-13).
- Fixed concurrent race in `RollbackProcessingAsync` and `MarkAsCompletedAsync` (Lua atomicity) (R-05/R-06).
- Fixed processing timeout relying on client clock (now uses Redis `TIME`) (R-15).
- Fixed SeqID Sorted Set unbounded growth (TTL refresh + pruning on write) (R-08).
- Fixed `CancellationToken` not propagated (explicit cancellation checks in loops) (R-11).
- Fixed `InvalidOperationException` on sync disposal (added `IDisposable`) (R-14).
- Fixed Cluster-scoped `ClearAsync`/`GetTokenTypesAsync` only covering a single node (R-10).
- Fixed token key cross-segment collision from raw string concatenation (R-20/R-21).

### ✨ Added (Redis Review Remediation R-series)

- `RedisKeyBuilder`: unified key construction (segment escaping, 256-byte length limit, empty-prefix guard).
- `FeishuRedisException` + `FeishuRedisFailureKind`: categorized Redis failure contract
  (Connection/Timeout/Server).
- `Tests/Mud.Feishu.Redis.IntegrationTests`: real Redis (Testcontainers) integration tests covering
  P0/P1 defects.
- `RedisOptions.ValidateOnStart()` (net6+): illegal config values fail at host startup.
- `RedisStoreHelper.GetServers()`: Cluster-wide primary node aggregation for SCAN.

### 📝 Documentation (Redis Review Remediation R-series)

- Removed non-existent `RedisFeishuEventDistributedDeduplicatorWithFallback` and related fallback claims.
- Added "capability ↔ implementation ↔ test" mapping table and plaintext token storage security disclosure
  (README appendix).

## [2.0.7] - 2026-04-07

### ✨ Added

- **WebSocket Enhancement**: Comprehensive enhancement of WebSocket connection management and error handling
  - Added message processing state tracking functionality
  - Added reconnection state reset functionality
  - Implemented reconnection coordinator to optimize reconnection logic and heartbeat intervals
  - Enhanced connection management with more comprehensive error handling mechanisms
- **Webhook Enhancement**: Added asynchronous validation methods and IP whitelist support
  - Added asynchronous validation methods to improve performance
  - Supported IP whitelist configuration to enhance security
- **Distributed Deduplication Service Refactoring**: Refactored distributed deduplication service with state machine and event processing lifecycle support
  - Implemented state machine pattern supporting Processing → Completed state transitions
  - Added event processing timeout and rollback mechanisms
  - Supported processing state tracking and exception recovery
- **Metrics Collection Optimization**: Optimized metrics collection performance using concurrent collections
  - Adopted `ConcurrentDictionary` to replace traditional lock mechanisms
  - Improved performance in high-concurrency scenarios
- **Utility Class Extension**: Added HttpClient extension methods for JSON serialization configuration
  - Added `ConfigureJsonSerializerOptions` extension method
  - Simplified JSON serialization configuration workflow

### 🔧 Changed

- **Token Management Refactoring**: Extracted token formatting logic to TokenUtils utility class
  - Unified `FormatBearerToken` and `RemoveBearerPrefix` methods
  - Eliminated code duplication and improved code maintainability
- **Signature Validation Optimization**: Removed deprecated methods and optimized signature validation logic
  - Cleaned up obsolete validation methods
  - Simplified validation workflow and improved performance
- **WebSocket Refactoring**: Refactored namespaces of WebSocket related classes
  - Unified namespace structure
  - Optimized code organization
- **Test Refactoring**: Refactored subscription validation tests to use encryption key provider
  - Updated test cases to adapt to new validator architecture
  - Improved test coverage and code quality

### 🐛 Fixed

- Fixed numerous null reference warnings to improve code robustness
- Fixed issue where `FeishuWebhookService` async methods were not awaited
- Fixed heartbeat interval issues in reconnection logic

### 🧪 Tests

- Added WebSocket core functionality tests
  - Added bug fix validation tests
  - Added exponential backoff reconnection strategy tests
  - Added reconnection coordinator tests
- Added deduplication service tests
  - Added unified deduplication middleware tests
  - Added configuration options tests
  - Added fallback alert service tests
- Added metrics collection tests
  - Optimized concurrent performance test cases

### 📦 Build & Config

- **CI/CD Enhancement**: Enhanced release process and supported multi-version .NET builds
  - Supported parallel builds for .NET 6.0, 8.0, 10.0
  - Optimized build and test configurations

## [2.0.6] - 2026-04-05

### ✨ Added

- **Bitable Advanced Permissions**: Added Feishu bitable advanced permission management functionality
  - Added advanced permission interfaces and related data models
  - Added role member management related interfaces and models
  - Added batch add collaborators interface and request models
  - Added custom role query, update, and delete interfaces
- **Bitable Dashboard**: Added Feishu bitable dashboard functionality
  - Added dashboard interface definitions
  - Added dashboard copy functionality and related data models and interfaces
- **Bitable Forms**: Added form management functionality
  - Added form field update related models and interfaces
  - Added update form metadata interface and related models
  - Added form upgrade interface and related data models
- **Bitable Fields**: Added field management functionality
  - Added create field group functionality interface and models
  - Added delete field interface and response models
  - Added update field interface
- **Bitable Records**: Added record batch operation functionality
  - Added batch create records interface and related data models
  - Added batch get records interface and related models
  - Added batch delete records interface and related models
  - Added delete record interface and response models
- **User Authentication**: Added Feishu user authentication middleware and related service configuration

### 🔧 Changed

- **Authorization Mechanism Refactoring**: Unified Token property declaration for authorization
  - Changed Token property from TokenType enum to string type
  - Removed Header property from interfaces, unified Token property for authorization settings
- **Bitable Refactoring**: Optimized model and interface structure
  - Refactored role related request model file structure
  - Refactored form field related interfaces and models
  - Refactored field operation request models and optimized directory structure
  - Refactored record operation request models and updated interfaces
- **Document Interface**: Moved document block interfaces to main namespace
- **Demo Project**: Changed demo project from NuGet package reference to local project reference

### 🐛 Fixed

- Fixed Bitable registration method name
- Updated dependency versions and added request body encryption method

### 📚 Documentation

- Updated NuGet package download badge style

### 🧪 Tests

- Added unit tests for bitable interfaces

### 📦 Build & Config

- Updated project version to 2.0.6 and adjusted token manager parameters
- Updated demo project dependencies and added Feishu authentication package

## [2.0.5] - 2026-03-27

### ✨ Added

- **Spreadsheets**: Added Feishu spreadsheet management module support
  - Worksheet management: create, query, update, delete worksheets
  - Row/Column operations: insert, update, move, delete rows and columns
  - Cell features: merge, find, style settings, data read/write
  - Filter features: filter view creation and management, filter condition settings
  - Data protection: protection range settings and management
  - Data validation: validation rule settings and management
  - Conditional formatting: format rule creation and management
  - Floating images: image insert, query, delete
- **Bitable**: Added Feishu bitable management module support
  - Bitable metadata get and update
- **Authentication**: Added Feishu user authentication module
  - User authentication middleware and context service
  - User authentication test project
- **Demo Project**: Added Feishu task management demo project

### 🔧 Changed

- **Spreadsheet Refactoring**: Refactored Feishu spreadsheet related models and interfaces
  - Refactored spreadsheet related models and interfaces
  - Adjusted spreadsheet interface namespaces and added tenant and user interfaces
  - Refactored cell operation related models and interfaces
  - Renamed cell related classes for better clarity
  - Refactored filter condition related models and interfaces
  - Unified filter view response models and added get interface
  - Refactored spreadsheet data validation interfaces
- **Authentication Refactoring**: Refactored user token manager and dependencies
  - Moved token formatting method to TokenUtils utility class
- **Webhook Refactoring**: Refactored validator base class and utilities
  - Used environment service instead of direct environment variable access
- **Test Optimization**: Used environment service mock instead of environment variable settings
- **Project Optimization**: Removed redundant EnsureUserContext calls and adjusted project references

### 📚 Documentation

- Updated README documentation
- Updated dependency documentation and version notes
- Updated interface documentation comments and added data protection related models
- Improved XML comments

### 📦 Build & Config

- Updated frontend dependency versions
- Updated dependency package versions to 10.0.5 and added authentication tests

## [2.0.4] - 2026-03-19

### ✨ Added

- **Document Management**: Added Feishu document management module support
  - Added Feishu document interfaces and data models
  - Added document block models and interface support
  - Added interfaces and models for getting and batch updating document blocks
  - Added interface for getting paginated list of document block children
  - Added batch delete blocks functionality and related models
  - Added content conversion functionality support
- **Wiki**: Added wiki management functionality
  - Added knowledge space and node management interfaces
  - Added node title update and copy functionality
  - Added node management interfaces and tests
  - Added functionality to move cloud documents to knowledge space
- **Drive Management**: Added Feishu drive management API service support
  - Added drive space folder related interfaces and data models
  - Added drive space file interfaces and metadata models
  - Added file version management functionality
  - Added file upload functionality support
  - Added file import/export task related data models and interfaces
  - Added create file shortcut related models and interfaces
  - Added search cloud documents functionality and related data models
  - Added get cloud document likers list interface
- **Task Module**: Added task and card modules and refactored service registration logic
- **Authentication Enhancement**: Added recoverable flag constructor for FeishuAuthenticationException
- **Enum Configuration**: Extracted enums and configuration classes to independent files

### 🔧 Changed

- **Project Refactoring**: Refactored request and response model file structure
  - Refactored document block model file structure, moved block models to Common directory and deleted original Blocks directory
  - Refactored file operation interface and model naming
  - Refactored code to optimize file upload request models and HTTP client
- **Test Optimization**: Unified data model namespaces in tests and optimized test code
- **Database Optimization**: Added indexes for favorite nodes to improve query performance

### 🐛 Fixed

- **Message Sending**: Fixed issue with request.Content value when sending Multipart/form-data requests
- **Webhook**: Fixed issue where Feishu callback configuration couldn't pass, specified EventType in FeishuEventDecryptor

### 📦 Build & Config

- **Dependency Update**: Updated dependency versions for multiple projects

### 🧪 Tests

- **New Tests**: Added unit tests for Feishu Webhook module

## [2.0.3] - 2026-02-26

### ✨ Added

- **Attendance Management**: Added complete attendance management functionality
  - Leave issuance record related data models and interfaces
  - Clock-in flow record related models and interfaces
  - Attendance archived report related interfaces and data models
  - Correction application related data models and interfaces
  - User face photo upload interface
  - User settings query interface
  - Attendance approval related data models and interfaces
  - Approval status update interface and data models
  - Approval result writing functionality and data models
  - Attendance statistics related data models and interfaces
  - Query statistics data related models and interfaces
  - Query statistics header interface and request models
  - User face recognition information management related interfaces and models
  - Attendance group related interfaces and data models
- **Approval Functionality**: Refactored approval instance preview functionality
- **Card Functionality**: Updated card element request field names
- **Task Functionality**: Updated Feishu task custom field interface return types

### 🔧 Changed

- **Project Structure**: Refactored project structure, adjusted namespaces and file locations
- **Interface Specifications**: Unified interface method naming conventions
- **Attendance Interfaces**: Refactored attendance group interface inheritance structure, renamed attendance user management interface and added download functionality
- **Token Management**: Refactored token management module and updated dependency versions
- **Internationalization**: Refactored internationalization resource models and updated related references
- **Test Structure**: Refactored test file directory structure, moved related test files to corresponding functional module directories
- **Demo Project**: Updated demo project to use package references instead of project references
- **Response Models**: Refactored response models

### 🐛 Fixed

- **User Group Members**: Fixed class name spelling error and updated related references
- **Test Cases**: Fixed test cases and improved test data
- **Activity Subscriptions**: Fixed activity subscription interface return types and improved test cases

### 📚 Documentation

- **Code Comments**: Added XML comments for test classes and cache methods
- **README**: Updated README documentation structure and content

### 📦 Build & Config

- **Dependency Update**: Updated Mud.HttpUtils dependency to version 1.5.3
- **Build Configuration**: Ignored test project build output directories

### 🧪 Tests

- **Improved Test Cases**: Added and improved test cases for multiple modules
  - Chat group and tab tests
  - Feishu task unit tests
  - Feishu user and user group interface tests
  - Feishu position and job level interface tests
  - Employee type related interface tests
  - Feishu department interface tests
  - Task section related tests
  - Task list interface tests
  - Feishu task attachment and comment tests
  - Group announcement related interface tests
  - Card interface tests
  - Feishu approval task related tests
  - Approval query interface tests
  - Approval comment related tests
  - Feishu approval related interface tests
  - Approval message interface tests

## [2.0.2] - 2026-01-30

### ✨ Added

- **Performance Metrics Monitoring**: Added complete performance metrics collection functionality
  - Added MeterExtensions, FeishuMetrics, and FeishuMetricsHelper classes
  - Added metrics recording to key components like TokenManager and HttpClientUtils
- **WebSocket and Webhook Metrics**: Added specialized metrics monitoring for WebSocket and Webhook
  - WebSocket connection count statistics and authentication/event processing metrics
  - Webhook signature verification, event decryption, and processing metrics
  - Event deduplication hit metrics
  - Support for WebSocketConnectionCountProvider to get real-time connection counts
- **Debug Logging Enhancement**: Added response content debug logs and error handling
  - Added debug logging to record raw response content
  - Provided more detailed error messages when JSON deserialization fails
- **Test Controllers**: Added log test controller and network test controller

### 🔧 Changed

- **Dependency Update**: Updated Mud.HttpUtils dependency to version 1.5.2
- **HTTP Client Optimization**:
  - Enhanced exception handling and logging
  - Improved security configuration and connection pool settings
- **Authentication Optimization**: Optimized token acquisition logic and removed redundant metrics recording
  - Changed to record metrics when retrieving token directly from cache
- **Configuration Simplification**: Removed circuit breaker functionality and related code
  - Removed circuit breaker configuration items and documentation
  - Simplified code structure and Webhook service
- **Timestamp Verification**: Optimized timestamp verification logic to prioritize app-specific configuration
- **Demo Project**: Adjusted Demo project configuration and dependencies

### 🐛 Fixed

- **JobLevel Interface**: Changed name parameter of JobLevel interface to nullable type
- **JobFamilies Interface**: Allowed name parameter of GetJobFamilesListAsync to be null

### 📚 Documentation

- Updated README documentation to explain metrics usage
- Added log test related documentation

### 📦 Build & Config

- Updated project version to 2.0.2

## [2.0.0] - 2026-01-28

### 🚨 BREAKING CHANGE

- **Removed FeishuOptions class** - Completely removed old configuration class, all scenarios now use `FeishuAppConfig`
- **Multi-application architecture support** - API signatures and configuration methods changed, migration required
- **Configuration system refactoring** - Retry mechanism and Token management configuration methods changed

### ✨ Added

- **Multi-app support**: Support for configuring and managing multiple Feishu apps
- **App context switching**: New `IFeishuAppContextSwitcher` interface supports runtime app switching
- **Auto-default app inference**: Three auto-inference rules simplify configuration
- **App-level Token cache isolation**: Each app has independent Token cache
- **New configuration parameters**: Added `RetryDelayMs` and `TokenRefreshThreshold` configuration
- **Documentation**: Added configuration migration guide and multi-app configuration documentation

### 🔧 Changed

- **Refactored HttpClient configuration**: Polly retry strategy uses configuration parameters
- **Refactored Token retry logic**: Uses configured `RetryDelayMs` instead of hardcoded values
- **WebSocket retry**: Uses configured `RetryDelayMs` instead of hardcoded values
- **Dependency updates**: Replaced code generator with `Mud.HttpUtils`

### 🐛 Fixed

- **Fixed `RetryCount` configuration inconsistency**: Unified application to HttpClient and TokenManager
- **WebSocket retry hardcoding issue**: Uses configuration parameters instead of hardcoded values

---

## 1.2.2 (2026-01-19)

### 🚀 Feature Enhancements

#### Attendance Management API

- **New Attendance Management API Service Support**
  - Files: Attendance-related modules
  - Impact: Provides complete attendance shift management capabilities

- **Shift Management Interfaces**
  - Added: Shift query by name interface
  - Added: Shift details interface and related data models
  - Added: Shift deletion interface and corrected shift creation interface path
  - Added: Attendance shift related data models and interfaces
  - Impact: Supports complete shift operations including creation, query, deletion, etc.

#### Approval Function Extension

- **Approval Message API**
  - Added: Approval Bot message update interface and request models
  - Added: Approval Bot message related data models and interfaces
  - Impact: Supports real-time update and management of approval messages

- **Approval Task Query**
  - Added: Approval task query interface and related data models
  - Impact: Provides more comprehensive approval task management capabilities

#### Demo Project Enhancement

- **Feishu OAuth Login Demo**
  - Added: Feishu OAuth login demo project
  - Impact: Provides complete OAuth login integration example

### 🐛 Bug Fixes

- **Decryption Failure Handling**
  - Fixed: Null reference issue when handling verification requests during decryption failure
  - Impact: Improved stability of decryption exception handling

- **Token Management Fix**
  - Fixed: User token management and status cleanup issues
  - Impact: Ensures proper token management and release

- **Project File Fix**
  - Fixed: Duplicate closing issue with PackageTags tag in project files
  - Impact: Resolves project file format issues

- **Webhook Middleware Fix**
  - Fixed: Removed verification request attributes and fixed middleware indentation
  - Impact: Optimizes Webhook middleware code structure

### 🔧 Code Refactoring

#### Model and Interface Refactoring

- **Shift Model Refactoring**
  - Refactored: Shift-related models, extracted common base class
  - Impact: Improves code reusability and maintainability

- **Authentication Service Refactoring**
  - Refactored: Authentication service and approval query interfaces
  - Impact: Optimizes service architecture

- **Namespace and Interface Renaming**
  - Refactored: Renamed IFeishuV3AuthenticationApi to IFeishuV3Authentication
  - Impact: Unifies interface naming conventions

#### Project Structure Optimization

- **File Organization Improvement**
  - Refactored: Moved project files to Sources folder to improve organization structure
  - Impact: Optimizes project directory structure

- **Tool Class Refactoring**
  - Refactored: Moved ExceptionUtils and HttpClientUtils to Utilities namespace
  - Refactored: Restructured log desensitization functionality and centralized into common utility class
  - Impact: Unifies tool class management

#### Performance and Security Optimization

- **Redis Command Optimization**
  - Refactored: Uses SCAN instead of KEYS command to avoid blocking Redis service
  - Impact: Improves performance and security of Redis operations

- **Cache Management Optimization**
  - Refactored: Merged methods for generating cache keys and added support for user ID parameters
  - Refactored: Restructured token manager's caching and formatting logic
  - Refactored: Restructured token management module, introduced cache abstraction layer
  - Impact: Optimizes cache management logic

- **Code Cleanup**
  - Refactored: Removed unused variables and redundant using declarations
  - Refactored: Removed standalone health check extension class and simplified registration logic
  - Refactored: Renamed health check namespace and updated references
  - Impact: Improves code quality and maintainability

### 📝 Documentation Improvements

- **API Documentation Updates**
  - Improved: Comments for approval message data models
  - Impact: Enhances API documentation completeness

- **Project Documentation Updates**
  - Updated: Project clone links in README
  - Updated: Repository links and version numbers in documentation
  - Added: README documentation for Mud.Feishu demo project collection
  - Impact: Provides more accurate project information

- **Demo Documentation Optimization**
  - Removed: WebSocket interceptor documentation and updated Webhook documentation
  - Updated: README documentation for Feishu WebSocket demo project
  - Moved: Feishu OAuth demo README file location
  - Impact: Optimizes demo documentation structure

### 📦 Build and Configuration

- **Version Update**
  - Updated: Project version to 1.2.2 and synchronized dependencies
  - Impact: Releases new version

- **Dependency Management Optimization**
  - Updated: Moved HTTP-related dependencies from global to specific projects
  - Impact: Optimizes dependency management

- **Git Configuration Update**
  - Updated: .gitignore file to exclude sensitive configurations and publish directories
  - Impact: Improves version control security

## 1.2.1 (2026-01-16)

**Type**: Configuration enhancement, security hardening, code quality improvement

### 🔒 Security Enhancements

#### Required Field Validation Strengthening

- **FeishuAppConfig Configuration Validation**
  - File: `Mud.Feishu.Abstractions/Configuration/FeishuAppConfig.cs`
  - Added: AppId format validation (must start with `cli_` or `app_`)
  - Added: AppId length validation (minimum 20 characters)
  - Added: AppSecret length validation (minimum 16 characters)
  - Added: Data Annotations attributes (`[Required]`, `[RegularExpression]`, `[MinLength]`)
  - Impact: Configuration errors can be detected at startup, avoiding runtime exceptions

- **FeishuWebhookOptions Configuration Validation**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Added: EncryptKey length validation (must be 32 characters)
  - Added: Data Annotations attributes to required fields
  - Impact: Ensures correct Feishu event encryption key configuration

#### Sensitive Information Protection

- **Configuration Class Sensitive Information Masking**
  - Files:
    - `Mud.Feishu.Abstractions/Configuration/FeishuAppConfig.cs`
    - `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
    - `Mud.Feishu.Redis/Configuration/RedisOptions.cs`
  - Added: `ToString()` method override, automatically masks sensitive information
  - Implementation: Shows first and last 2 characters, replaces middle with `****`
  - Impact: Prevents accidental leakage of sensitive information in logs

- **Redis Configuration Validation**
  - File: `Mud.Feishu.Redis/Configuration/RedisOptions.cs`
  - Added: ServerAddress format validation
  - Added: Connection timeout parameter range validation
  - Added: Data Annotations attributes
  - Impact: Ensures correct Redis connection configuration

### 🔧 Configuration Optimization

#### Configuration Example File

- **appsettings.example.json**
  - File: `appsettings.example.json` (new)
  - Added: Complete configuration example file
  - Contains: Feishu, FeishuWebhook, FeishuWebSocket, Redis configurations
  - Impact: New users can quickly understand configuration structure

### 🧪 Test Coverage

#### Configuration Unit Tests

- **FeishuAppConfig Tests**
  - File: `Tests/Mud.Feishu.Abstractions.Tests/Configuration/FeishuAppConfigTests.cs`
  - Coverage: AppId/AppSecret validation, range restrictions, sensitive information masking, auto-inference logic
  - Test cases:
    - Valid configuration validation
    - AppId format validation (cli*/app* prefix)
    - AppId/AppSecret null value validation
    - AppId/AppSecret length validation
    - TimeOut/RetryCount range restrictions
    - BaseUrl format validation
    - ToString sensitive information masking
  - Impact: Core configuration logic test coverage

### 📝 Documentation Improvements

#### XML Documentation Comment Improvements

- **Configuration Class Documentation Updates**
  - Files: All Options configuration classes
  - Updated: Added complete parameter descriptions and example values
  - Added: Data Annotations error message descriptions
  - Impact: Improves code readability and IDE intellisense

### 🔨 Breaking Changes

- Modified `FeishuAppConfig.Validate()` method, now validates AppId/AppSecret format
- Added `FeishuWebhookOptions.EncryptKey` length validation (32 characters)
- Added `RedisOptions.Validate()` method, validates connection parameters

### 📦 Dependency Updates

- New dependencies:
  - `System.ComponentModel.DataAnnotations` (for Data Annotations)

## 1.1.2 (2025-11-17)

**Type**: Feature enhancement, code refactoring, bug fixes, documentation improvement

### 🚀 Feature Enhancements

- **API Service Support**
  - Added: Attendance management API service support
  - Added: Shift management related interfaces
  - Added: Approval message API and approval task query interfaces
  - Impact: Provides more comprehensive Feishu functionality integration

- **Project Structure Optimization**
  - Added: Moved project files to Sources folder
  - Impact: Improves project organization structure

### 🐛 Bug Fixes

- **Decryption Failure Handling**
  - Fixed: Null reference issue when handling verification requests during decryption failure
  - Impact: Improves exception handling stability

- **User Token Management**
  - Fixed: User token management and status cleanup issues
  - Impact: Ensures proper token management and release

### 🔧 Code Refactoring

- **Authentication Service Refactoring**
  - Refactored: Authentication service and approval query interfaces
  - Refactored: Renamed IFeishuV3AuthenticationApi to IFeishuV3Authentication
  - Impact: Unifies interface naming conventions

- **Redis Command Optimization**
  - Refactored: Uses SCAN instead of KEYS command to avoid blocking Redis service
  - Impact: Improves performance and security of Redis operations

- **Cache Management Optimization**
  - Refactored: Merged methods for generating cache keys and added support for user ID parameters
  - Refactored: Restructured token manager's caching and formatting logic
  - Impact: Optimizes cache management logic

### 📝 Documentation Improvements

- **API Documentation Updates**
  - Improved: Comments for approval message data models
  - Impact: Enhances API documentation completeness

- **Project Documentation Updates**
  - Updated: Project clone links in README
  - Updated: Repository links and version numbers in documentation
  - Impact: Provides more accurate project information

## 1.1.1 (2026-01-06)

**Type**: Feature enhancement, code refactoring, documentation improvement

### 🚀 Feature Enhancements

- **Approval Function Extension**
  - Added: Approval event type constants (WorkApproval and WorkApprovalRevert)
  - Added: Interface to get detailed data by approval definition Code
  - Added: Third-party approval definition creation functionality
  - Added: Approval instance comment pagination query interface
  - Added: RemoveCommentsAsync interface to clear all comments and approval replies under approval instance
  - Added: Approval comment deletion functionality and optimized comment creation parameters
  - Added: Approval task signature functionality
  - Added: Interface to get detailed data by approval definition Code for approval tasks
  - Added: Approval instance status pagination query interface
  - Impact: Provides more complete approval process management capabilities

- **Task Management Functionality**
  - Added: Custom field management interface
  - Added: Custom field option management
  - Added: Custom field resource binding
  - Added: Custom field list pagination query
  - Impact: Supports custom field management for tasks

### 🔧 Code Refactoring

- **API Response Type Unification**
  - Refactored: Updated all API response types to FeishuApiResult series
  - Impact: Unifies API response format

- **Message Sending Interface Refactoring**
  - Refactored: Unified message sending interface design
  - Impact: Improves consistency of message sending functionality

- **Service Registration API Unification**
  - Refactored: Unified service registration API and removed redundant UseMultiHandler method
  - Impact: Simplifies service registration process

### 📝 Documentation Improvements

- **README Documentation Optimization**
  - Updated: Removed architecture design and performance characteristics documentation
  - Updated: Added department event handler documentation and usage examples
  - Updated: Optimized project description and feature explanations
  - Impact: Improves documentation practicality and readability

## 1.1.2 (2026-01-10)

**Type**: Feature enhancement, code refactoring, bug fixes, documentation improvement

### 🚀 Feature Enhancements

- **Webhook and WebSocket Functionality**
  - Added: Webhook verification method changed to async implementation and refactored retry service
  - Added: WebSocket event retry functionality, supporting both distributed and in-memory modes
  - Added: Event processing state synchronization and reconnection mechanism
  - Added: Request body signature verification option
  - Impact: Improves reliability and stability of event processing

- **Approval Function Extension**
  - Added: Third-party approval instance verification functionality
  - Added: Third-party approval instance synchronization functionality
  - Added: Approval instance status pagination query interface
  - Impact: Enhances completeness of approval functionality

- **Configuration Verification Functionality**
  - Added: WebSocket and Feishu configuration options
  - Added: Webhook configuration verification functionality and optimized option documentation
  - Impact: Improves configuration correctness and security

### 🐛 Bug Fixes

- **Project Configuration Fix**
  - Fixed: WebSocket configuration-related issues
  - Impact: Ensures WebSocket functionality operates normally

### 🔧 Code Refactoring

- **Event Handler Improvement**
  - Refactored: Adjusted event handler base class design to support async processing
  - Refactored: Updated event handler base class to async handler
  - Impact: Improves efficiency of event processing

- **Redis Service Registration**
  - Refactored: Simplified Redis service registration method and updated documentation
  - Impact: Simplifies usage of Redis service

- **Service Registration Refactoring**
  - Refactored: Renamed FeishuWebhook service registration method name
  - Refactored: Refactored Feishu service registration method naming
  - Impact: Unifies service registration method naming conventions

### 📝 Documentation Improvements

- **English Documentation Restructuring**
  - Updated: Reorganized English documentation structure and restored feature characteristic explanations
  - Impact: Improves readability of English documentation

### 📦 Dependency Updates

- Updated: Project dependency package versions to 1.1.2
- Updated: Mud.ServiceCodeGenerator version
- Impact: Ensures compatibility and stability of dependency packages

## 1.1.0 (2025-11-12)

**Type**: Feature enhancement, code refactoring, documentation improvement

### 🚀 Feature Enhancements

- **User Management API**
  - Added: Complete user management interfaces including create, update, delete users
  - Added: Batch query user information interfaces
  - Added: Restore deleted user interfaces
  - Impact: Provides comprehensive user management capabilities

- **User Group Management API**
  - Added: User group creation, update, deletion interfaces
  - Added: Query user's group membership and user group details interfaces
  - Impact: Supports complete user group management

- **Department Management API**
  - Added: Department creation, update, deletion interfaces
  - Added: Query departments by parent ID, query parent departments by department ID interfaces
  - Impact: Provides complete department management functionality

- **Employee Type Management API**
  - Added: Employee type related interfaces
  - Impact: Supports employee type management

### 🔧 Code Refactoring

- **API Result Model Refactoring**
  - Refactored: ApiListResult list data result collection
  - Refactored: Renamed ApiResult to FeishuApiResult
  - Impact: Unifies API result model naming

- **Service Registration Optimization**
  - Refactored: Service registration code structure
  - Impact: Improves service registration efficiency

### 📝 Documentation Improvements

- **README Documentation Optimization**
  - Updated: README file content and structure
  - Added: Project feature descriptions and usage examples
  - Impact: Improves documentation readability

## 1.0.9 (2025-11-14)

**Type**: Feature enhancement, code refactoring, bug fixes

### 🚀 Feature Enhancements

- **Cross-platform Support**
  - Added: Support for .NET Standard 2.0
  - Impact: Improves framework compatibility

- **HTTP Client Enhancement**
  - Added: Feishu HTTP client extension methods
  - Added: Enhanced HTTP client configuration
  - Impact: Provides more flexible HTTP request configuration

### 🐛 Bug Fixes

- **HttpClient Configuration Fix**
  - Fixed: HttpClient configuration and API endpoint URL format issues
  - Impact: Ensures API requests are sent correctly

### 🔧 Code Refactoring

- **Message and Event API Refactoring**
  - Refactored: Message and event API implementation
  - Impact: Improves code maintainability

- **File Download Function Refactoring**
  - Refactored: File download functionality and optimized HTTP request methods
  - Impact: Improves file download performance

## 1.0.7 (2025-11-12)

**Type**: Feature enhancement, code refactoring, documentation improvement

### 🚀 Feature Enhancements

- **Task Management API**
  - Added: Task comment related interfaces
  - Added: Task attachment management interfaces
  - Added: Task activity subscription interfaces
  - Added: Task list member management interfaces
  - Impact: Provides complete task management capabilities

- **JsTicket API**
  - Added: JsTicket API interfaces
  - Impact: Supports front-end JavaScript development requirements

### 🔧 Code Refactoring

- **Code Generator Upgrade**
  - Upgraded: Mud.ServiceCodeGenerator version
  - Impact: Improves code generation efficiency and quality

- **Project Dependency Updates**
  - Updated: Project dependency configuration and removed test tools
  - Impact: Optimizes project dependency management

### 📝 Documentation Improvements

- **Task Related Documentation Updates**
  - Updated: Task member information comments
  - Impact: Improves API documentation accuracy

## 1.0.3-dev (2025-11-12)

**Type**: Initial version, feature enhancement

### 🚀 Feature Enhancements

- **Core Function Implementation**
  - Added: Feishu API basic framework setup
  - Added: Authentication service and token management
  - Added: Enterprise address book related interfaces
  - Added: Message sending and receiving functionality
  - Added: Webhook and WebSocket support
  - Impact: Establishes project basic architecture

- **Demo Projects**
  - Added: Webhook demo service
  - Added: WebSocket demo project
  - Impact: Provides usage examples

### 📝 Documentation Improvements

- **Project Documentation Initialization**
  - Added: README.md file
  - Added: Project structure description
  - Impact: Provides project basic documentation

## 1.2.0 (2026-02-15)

**Type**: Feature enhancement, security hardening, performance optimization

### 🚀 New Features

#### Security Enhancements

- **Content Type Validation**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Added: Request Content-Type validation, only accepts `application/json`
  - Prevention: Maliciously constructed non-JSON requests
  - Impact: Improved request security

- **JSON Depth Limitation**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - Added: `MaxDepth = 64` limit to prevent deeply nested JSON
  - Prevention: DoS attacks and stack overflow risks
  - Impact: Deserialization security

- **Event Processing Interceptor**
  - File: `Mud.Feishu.Abstractions/IFeishuEventInterceptor.cs` (new)
  - Added: Pre/post event processing interceptor mechanism
  - Support: Logging, performance monitoring, custom validation
  - Impact: Enhanced extensibility

- **Failed Event Retry Service**
  - File: `Mud.Feishu.Webhook/Services/FailedEventRetryService.cs` (new)
  - Added: Background automatic retry of failed events
  - Strategy: Exponential backoff (2^retryCount minutes, max 60 minutes)
  - Impact: Improved event processing reliability

- **Circuit Breaker Pattern**
  - File: `Mud.Feishu.Webhook/Services/FeishuWebhookService.cs`
  - Added: Circuit breaker pattern implemented with Polly
  - Configuration: Break after 5 consecutive failures, retry after 30 seconds
  - Impact: Improved system stability

#### Performance Optimizations

- **Streaming Request Body Reading**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Optimization: Stream reading of request body, real-time size validation
  - Prevention: DoS attacks with forged Content-Length
  - Impact: Memory usage optimization

- **Full Application of Source Generator Serialization**
  - File: All serialization/deserialization code
  - Optimization: Full use of `JsonSerializerContext`
  - Performance: ~20-30% improvement
  - Impact: All JSON operations

#### Observability Enhancement

- **Extended Metrics Collection**
  - File: `Mud.Feishu.Webhook/Models/MetricsCollector.cs`
  - Added:
    - `feishu_webhook_events_received_total`
    - `feishu_webhook_events_failed_total`
    - `feishu_webhook_event_processing_duration_seconds`
    - `feishu_webhook_active_requests`
    - `feishu_webhook_circuit_breaker_state`
  - Impact: Better monitoring capabilities

- **Log Sanitization Middleware**
  - File: `Mud.Feishu.Webhook/Utils/LogSanitizer.cs` (new)
  - Added: Automatic sanitization of sensitive fields (encrypt, signature, token, etc.)
  - Prevention: Sensitive information leakage to logs
  - Impact: Production log security

### 🔒 Security Fixes

#### High-Risk Issues Fixed

- **[CVE-2026-XXXX] Nonce Expiration Cleanup Mechanism**
  - File: `Mud.Feishu.Abstractions/IFeishuNonceDistributedDeduplicator.cs`
  - Issue: Nonce storage lacks automatic expiration cleanup, may cause memory leaks
  - Fix: Added timestamp-based TTL cleanup mechanism
  - Implementation: Clean expired Nonces during each query
  - Impact: All scenarios using Nonce deduplication

- **[CVE-2026-XXXX] Request Size Validation Bypass**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: Only checked Content-Length header, bypassable by forgery
  - Fix: Stream reading of request body, real-time size validation
  - Impact: Prevention of DoS attacks

- **[CVE-2026-XXXX] Production Environment Configuration Bypass**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Issue: Incomplete environment variable detection, may bypass security checks
  - Fix: Enhanced environment detection, supports multiple environment identifiers
  - Impact: Production environment security configuration

#### Medium-Risk Issues Fixed

- **JSON Deserialization Depth Limitation**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - Issue: Missing MaxDepth limit, may cause performance issues
  - Fix: Added MaxDepth = 64 limit
  - Impact: Deserialization security

- **Rate Limiter Memory Management Optimization**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuRateLimitMiddleware.cs`
  - Issue: Dictionary may grow infinitely, lacks capacity limit
  - Fix: Added maximum entry limit (100k) and LRU eviction
  - Impact: Rate limiter stability

- **Concurrency Control Resource Management**
  - File: `Mud.Feishu.Webhook/Services/FeishuWebhookConcurrencyService.cs`
  - Issue: Task.Run didn't pass cancellation token, may not release correctly
  - Fix: Implemented IHostedService, added application shutdown hooks
  - Impact: Resource release during service shutdown

### 🐛 Bug Fixes

#### Resource Management Fixes

- **AES/SHA256 Resource Leak**
  - Files:
    - `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
    - `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Fix: Use `using` statements to ensure resource release
  - Impact: Encryption/decryption operations

- **Concurrency Control Resource Leak**
  - File: `Mud.Feishu.Webhook/Services/FeishuWebhookConcurrencyService.cs`
  - Fix: Implemented IHostedService lifecycle management
  - Impact: Resource release during service shutdown

#### Logic Fixes

- **Configuration Change Event Notification**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Fix: Added OptionsChanged event
  - Impact: Real-time notification of configuration hot updates

### 📝 Documentation Improvements

#### New Documentation

- **docs/troubleshooting.md**
  - Troubleshooting guide
  - FAQ (Frequently Asked Questions)
  - Diagnostic flowchart

- **docs/performance-tuning.md**
  - Performance tuning guide
  - Concurrency configuration suggestions
  - Monitoring metric explanations

- **docs/security-best-practices.md**
  - Security best practices
  - Production environment configuration checklist
  - Risk assessment recommendations

#### Improved Documentation

- **README.md**
  - Updated configuration instructions
  - Added troubleshooting links
  - Supplemented performance optimization suggestions

- **SECURITY.md**
  - Updated security configuration requirements
  - Added CVE disclosure process
  - Supplemented security audit reports

### 🔄 Breaking Changes

#### API Changes

- **IFeishuEventHandlerFactory Extension**
  - New methods: `GetHandlerInfo()`, `ClearHandlers()`
  - Impact: Classes implementing the interface

- **FeishuWebhookOptions Extension**
  - New properties: `MaxRetryCount`, `CircuitBreakerEnabled`
  - Impact: Configuration files need updates

#### Configuration Changes

- **New Required Configuration**
  - `MaxRetryCount`: Default value 3
  - `CircuitBreakerEnabled`: Default value true
  - `MaxRequestBodySize`: Default value 10MB

### ⚠️ Migration Guide

If you are upgrading to v1.2.0, please update your configuration files:

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true,
    "TimestampToleranceSeconds": 60,
    "MaxConcurrentEvents": 10,
    "EventHandlingTimeoutMs": 30000,
    "EnableBackgroundProcessing": false,
    "MaxRetryCount": 3,
    "CircuitBreakerEnabled": true,
    "RateLimit": {
      "EnableRateLimit": true,
      "MaxRequestsPerWindow": 100,
      "WindowSizeSeconds": 60,
      "MaxIpEntries": 100000
    }
  }
}
```

If using interceptors, register them in `Program.cs`:

```csharp
builder.Services.AddFeishuWebhook(options =>
{
    // ... existing configuration ...
})
.AddEventInterceptor<LoggingEventInterceptor>()
.AddEventInterceptor<MetricEventInterceptor>();
```

#### Dependency Updates

New dependencies:

- `Polly`: for circuit breaker pattern

```bash
dotnet add package Polly
```

### 🧪 Testing

#### New Unit Tests

- Nonce deduplication cleanup tests
- Content-Type validation tests
- JSON depth limitation tests
- Streaming request body reading tests
- Interceptor execution order tests
- Circuit breaker state transition tests
- Exponential backoff retry tests
- Log sanitization tests
- Rate limiter LRU eviction tests
- Concurrency control resource release tests

#### New Integration Tests

- End-to-end Webhook request tests
- Failed event retry flow tests
- Circuit breaker fault recovery tests
- Distributed scenario tests (multi-instance)

#### Performance Tests

- 100k concurrent request tests
- 1MB request body processing tests
- Deeply nested JSON processing tests
- Memory leak stress tests

### 📦 Dependency Updates

#### New Dependencies

- `Polly`: 8.4.0
  - Purpose: Circuit breaker pattern implementation
  - License: BSD-3-Clause

#### Updated Dependencies

- `Microsoft.Extensions.Diagnostics.HealthChecks`: 8.0.x
- `Microsoft.Extensions.Logging.Abstractions`: 8.0.x
- `System.Text.Json`: 8.0.x

---

## 1.1.3 (2026-01-15)

**Type**: Bug fix and security enhancement

### 🔒 Security Fixes

#### High-Risk Issues Fixed

- **[CVE-2026-XXXX] Production Environment Signature Validation Check Restoration**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Issue: Production environment forced signature validation logic was commented out, posing serious security risk
  - Fix: Uncommented lines 197-202 to ensure production environment signature validation is enabled
  - Impact: All Webhook users

- **[CVE-2026-XXXX] Background Processing Failed Event Persistence**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: During background processing failure, condition judgment error caused failed events to not be persisted
  - Fix: Corrected line 583 condition judgment, implemented TODO comment (lines 601-602)
  - Impact: All users of background processing mode

- **[CVE-2026-XXXX] Demo Project Hardcoded Sensitive Keys**
  - File: `Mud.Feishu.Webhook.Demo/appsettings.json`
  - Issue: VerificationToken and EncryptKey hardcoded in configuration file
  - Fix: Removed hardcoded values, added SECURITY-WARNING.md documentation
  - Impact: Demo project users

#### Medium-Risk Issues Fixed

- **WebSocket Error Handling Enhancement**
  - File: `Mud.Feishu.WebSocket/FeishuWebSocketClient.cs`
  - Issue: Exception handling not detailed enough, lacking classification and recovery judgment
  - Fix: Added detailed exception classification handling, distinguishing recoverable and non-recoverable errors
  - Impact: WebSocket connection stability

- **Redis Connection Failure Fallback Handling**
  - File: `Mud.Feishu.Redis/Services/RedisFeishuEventDistributedDeduplicatorWithFallback.cs` (new)
  - Issue: No fallback strategy when Redis connection fails
  - Fix: Implemented automatic fallback to memory deduplication, supporting exponential backoff retry
  - Impact: Users of Redis distributed deduplication

- **Authentication Failure Handling Enhancement**
  - File: `Mud.Feishu.WebSocket/Core/AuthenticationManager.cs`
  - Issue: Authentication failure logs not detailed enough, lacking error code classification
  - Fix: Added detailed error code classification handling and statistical information
  - Impact: WebSocket authentication issue troubleshooting

- **AES/SHA256 Resource Leak Fix**
  - Files:
    - `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
    - `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: AES and SHA256 instances not properly released, may cause resource leaks
  - Fix: Use `using` statements to ensure resources are released correctly
  - Impact: All scenarios using event decryption

- **InMemoryFailedEventStore Thread Safety Issue**
  - File: `Mud.Feishu.Webhook/Services/InMemoryFailedEventStore.cs`
  - Issue: Cleanup method not locked, race condition with dictionary operations
  - Fix: Added lock protection in `CleanupExpiredEvents` method
  - Impact: Users of in-memory failed event storage

- **InMemoryFailedEventStore Timer Resource Leak**
  - File: `Mud.Feishu.Webhook/Services/InMemoryFailedEventStore.cs`
  - Issue: Timer instance not released in Dispose method
  - Fix: Implemented IDisposable interface, released Timer in Dispose
  - Impact: Users of in-memory failed event storage

- **FeishuSeqIDDeduplicator Cache Cleanup Logic**
  - File: `Mud.Feishu.Abstractions/Services/FeishuSeqIDDeduplicator.cs`
  - Issue: Maximum SeqID calculation logic incomplete, may cause duplicate processing
  - Fix: Unified calculation of maximum SeqID after cleanup completion
  - Impact: Users of SeqID deduplication

- **Log Sensitive Information Cleanup**
  - File: `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
  - Issue: Logs contain complete decrypted JSON data, may leak sensitive information
  - Fix: Only record event data length, not complete content
  - Impact: All log users

- **Log Emoji Symbol Removal**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: Logs use emoji symbols, may cause log parsing issues in production environments
  - Fix: Removed emoji symbols from logs
  - Impact: Production environment log parsing

- **Nonce Deduplication Logic Comment Optimization**
  - File: `Mud.Feishu.Webhook/Services/FeishuEventValidator.cs`
  - Issue: Code logic correct but lacks clear comments
  - Fix: Added detailed comments explaining return value semantics
  - Impact: Code readability

- **IFeishuEventHandlerFactory Interface Improvement**
  - File: `Mud.Feishu.Abstractions/IFeishuEventHandlerFactory.cs`
  - Issue: Interface missing `UnregisterHandler(IFeishuEventHandler handler)` method definition
  - Fix: Added interface method definition, consistent with implementation class
  - Impact: Users needing to unregister specific event handlers

- **Duplicate Service Registration Fix**
  - File: `Mud.Feishu.Webhook/Extensions/FeishuWebhookServiceBuilder.cs`
  - Issue: `IFeishuNonceDistributedDeduplicator` registered twice
  - Fix: Removed duplicate registration on line 296
  - Impact: All users of Webhook services

### 🐛 Bug Fixes

#### Core Functionality Fixes

- **Background Processing Mode Failed Event Storage**
  - Fixed issue where failed events couldn't be persisted in background processing mode
  - Correctly calls `IFailedEventStore.StoreFailedEventAsync`
  - Added detailed error logging

#### Resource Management Fixes

- **AES Encryption Resource Release**
  - Use `using var` statement to ensure AES instance is released correctly
  - Also release ICryptoTransform resources

- **SHA256 Hash Resource Release**
  - Use `using` statement to ensure SHA256 instance is released correctly

- **Timer Resource Release**
  - InMemoryFailedEventStore implements IDisposable
  - Ensure timed cleanup Timer is released correctly

#### Thread Safety Fixes

- **Failed Event Storage Cleanup Method**
  - Added lock protection for cleanup operations
  - Ensured concurrent safety with dictionary operations

#### Logic Fixes

- **SeqID Deduplication Cleanup**
  - Unified recalculation of maximum SeqID after cleanup completion
  - Avoided calculation errors caused by partial cleanup

#### New Features

- **Redis Fallback Deduplicator**
  - Added `RedisFeishuEventDistributedDeduplicatorWithFallback` class
  - Supports automatic fallback to memory deduplication
  - Exponential backoff retry mechanism
  - Status query and monitoring capabilities

- **WebSocket Error Classification**
  - Distinguish between recoverable and non-recoverable errors
  - Detailed error logs and error type identification
  - Helps locate issues quickly

- **Detailed Authentication Failure Tracking**
  - Classify authentication failure reasons by error code
  - Statistical total failure count and failure time
  - Provide targeted repair suggestions

### 📝 Documentation Improvements

#### New Documentation

- **SECURITY-WARNING.md**
  - Demo project security configuration guide
  - Production environment security checklist
  - Environment variable configuration explanation

#### Improved Documentation

- **README.md**
  - Updated security configuration suggestions
  - Added best practice links

### 🔄 Breaking Changes

#### Configuration Changes

- **Production Environment Signature Validation**
  - Previous: Production environment signature validation could be disabled
  - Now: Production environment forces signature validation, disabling will throw exception

- **Demo Project Configuration**
  - Previous: appsettings.json contained hardcoded example keys
  - Now: Configuration file is empty, must configure through environment variables or manually

### ⚠️ Migration Guide

#### Production Environment Configuration

If you use the following configurations in production environment, you need to make corresponding adjustments:

##### 1. Enable Signature Validation

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true
  }
}
```

##### 2. Configure Environment Variables

```bash
# Linux/macOS
export FeishuWebhook__VerificationToken="your_verification_token"
export FeishuWebhook__EncryptKey="your_32_byte_encryption_key"

# Windows PowerShell
$env:FeishuWebhook__VerificationToken="your_verification_token"
$env:FeishuWebhook__EncryptKey="your_32_byte_encryption_key"
```

##### 3. Verify Configuration

Before deployment, confirm:

- [ ] Signature validation enabled
- [ ] Timestamp tolerance range <= 60 seconds
- [ ] EncryptKey is 32-byte strong key
- [ ] IP validation enabled and whitelist configured
- [ ] Logs don't contain sensitive information

### 🧪 Testing

#### New Tests

- Background processing failed event storage tests
- AES/SHA256 resource release tests
- InMemoryFailedEventStore thread safety tests
- SeqID deduplication cleanup logic tests
- Timer resource release tests
- IFeishuEventHandlerFactory interface method tests
- WebSocket error classification tests
- Redis fallback strategy tests
- Authentication failure handling tests

---

## 1.1.0 (2025-12-31)

**FEATURES**

### 🔧 Core Optimization and Refactoring

- 📦 **Multi-Framework Support**: Supports .NET Standard 2.0, .NET 6.0, .NET 8.0, .NET 10.0
  - Provides cross-platform compatibility, supports from .NET Framework 4.6+ to .NET 10.0
  - Unified API interface, different framework versions use the same programming model
  - Automatic compile-time conditional processing, fully utilize platform features

- 🏗️ **Response Type Unification**: Update all API response types to `FeishuApiResult<T>` series
  - `FeishuApiResult<T>` - Generic response type
  - `FeishuApiPageListResult<T>` - Paginated list response
  - `FeishuApiListResult<T>` - List response
  - `FeishuNullDataApiResult` - Null data response

- 🔧 **Message Sending Interface Refactoring**: Unified message sending interface design
  - `SendMessageRequest` replaces `TextMessageRequest`, supports all message types
  - `MessageTextContent` replaces `TextContent`, maintains type consistency
  - Improved Content field serialization mechanism

### 📋 New Approval API Support

- ✅ **Approval Definition Management**
  - `IFeishuV4Approval` - V4 approval base interface
  - `IFeishuTenantV4Approval` - V4 tenant approval interface
  - Supports creating approval definitions, querying approval instances and other core functions

### 📝 Task Management Enhancement

- 🎯 **Custom Field Management**
  - Create, update, query custom fields
  - Custom field option management
  - Custom field resource binding
  - Paginated query of custom field lists

- 📊 **Task Group Management**
  - Create, update, delete task groups
  - Query task group lists
  - Task group resource binding

### 🔄 WebSocket Real-Time Event Subscription

- 🌐 **Feishu WebSocket Client** (`Mud.Feishu.WebSocket`)
  - Supports Feishu WebSocket real-time event subscription
  - Automatic reconnection mechanism, ensures connection stability
  - Heartbeat detection, promptly discovers connection anomalies
  - Binary message parsing, supports complete event types

### 📡 Webhook Event Processing

- 🎭 **Event Handler Abstraction Layer** (`Mud.Feishu.Abstractions`)
  - Strategy pattern architecture, flexibly extend event handlers
  - Factory pattern management, automatically discover and register handlers
  - Complete Feishu event type coverage:
    - User events: Creation, update, departure, status change
    - Department events: Creation, update, deletion
    - Employee events: Onboarding, departure, information change
    - Message events: Reception, send status, read status
    - Task events: Creation, update, deletion, status change
    - Approval events: Submission, approval, rejection, revocation

### 🎨 Message and Card Enhancement

- 📰 **Message Stream Card API**
  - Complete interface support for application message stream cards
  - Card entity component management
  - Card content update and deletion

- 💬 **Group Function Enhancement**
  - Group announcement management
  - Session tab management
  - Group custom menu setting

### 🛠️ Configuration and Tool Optimization

- ⚙️ **Configuration Enhancement**
  - Added `FeishuOptions` configuration class
  - Supports configuration file binding
  - Logging configuration options

- 🔒 **Security Enhancement**
  - URL verification function, prevents malicious requests
  - Authorization header constants unified management

**REFACTOR**

- 📁 **Namespace Unification**
  - Unified interface namespace structure
  - Global reference imports, simplified code

- 🧹 **Code Cleanup**
  - Removed obsolete TaskSectionsResult class
  - Removed invalid classes and interfaces
  - Unified service registration API, removed obsolete `UseMultiHandler` method

**BUG FIX**

- 🔧 **Fixed HttpClient configuration issues**
- 🔧 **Fixed API endpoint URL format issues**
- 🔧 **Fixed message and task attachment API implementations**

**DOCS**

- 📚 **Documentation Updates**
  - Updated README document structure
  - Removed redundant architecture design and performance characteristics documentation
  - Added department event handler documentation and usage examples
  - Optimized project description and feature explanation

---

## 1.0.2 (2025-11-26)

**FEATURES**

- 🏗️ **Refactoring Optimization**: Created `ChatGroupBase` base class, integrating chat group related common properties
  - Reduced 70+ duplicate properties, improved code reusability
  - Unified `GetChatGroupInfoResult`, `CreateUpdateChatResult`, `UpdateChatRequest`, `CreateChatRequest` class structure
  - Maintained complete JsonPropertyName attributes, ensured JSON serialization compatibility

- 📚 **Documentation Improvement**: Added complete XML documentation comments to all chat group and group member related classes
  - `ChatGroupModeratorPageListResult` - Chat group moderator paginated list result
  - `ChatItemInfo` - Basic chat item information
  - `ShareLinkDataResult` - Share link data result
  - `AddMemberResult` - Add member operation result
  - `GetMemberIsInChatResult` - Member group status query result
  - `GetMemberPageListResult` - Group member paginated list result
  - `RemoveMemberResult` - Remove member operation result
  - `GroupManagerResult` - Group manager operation result

- 🎯 **Code Quality**: Improved code readability and maintainability
  - All new comments follow C# XML documentation specification
  - Contains detailed business meaning and usage scenario explanations
  - Distinguishes actual effects of different parameter values

## 1.0.1 (2025-11-20)

**REFACTOR**

- Optimized dependency injection configuration structure
- Improved concurrency safety of token managers
- Refactored HTTP client factory configuration

**FEATURES**

- Enhanced error handling mechanisms
- Added detailed logging support
- Supports custom HTTP header configuration

**BUG FIX**

- Fixed concurrency issues during token refresh
- Resolved data loss issues in pagination queries
- Fixed status tracking errors in batch message sending

### 📱 Message Service

- **Multiple Message Types**: Rich message types including text, images, files, cards, etc.
- **Batch Sending**: Supports batch message sending and status tracking
- **Message Interaction**: Emojis reply, message recall, read receipt
- **Asynchronous Processing**: Fully asynchronous message processing mechanism

---

## 1.0.0 (2025-11-01)

### 🎉 First Release - Mud.Feishu Feishu API SDK

**FEATURES**

### 🔐 Authentication and Authorization System

- **Multiple Token Management**: Supports application tokens, tenant tokens, user tokens
- **Automatic Refresh Mechanism**: Intelligent token refresh, triggered 5 minutes in advance
- **High Concurrency Security**: Uses `ConcurrentDictionary` and `Lazy<Task>` to avoid cache breakdown
- **OAuth Authorization Process**: Complete support for Feishu OAuth 2.0 authorization

### 🏢 Organizational Structure Management

#### User Management (V1/V3)

- **User CRUD**: Create, query, update, delete users
- **Batch Operations**: Batch get user information, batch status updates
- **Department Association**: Many-to-many relationship management between users and departments
- **Search Filtering**: Supports various search criteria and pagination

#### Department Management (V1/V3)

- **Tree Structure**: Supports infinite level department tree
- **Recursive Query**: Recursively get sub-departments and members
- **Permission Inheritance**: Automatic department permission inheritance mechanism

#### Employee Management (V1)

- **Employee Information**: Employee detailed information management
- **Onboarding and Departure**: Employee onboarding and departure process support

#### User Group Management (V3)

- **User Group CRUD**: Create, query, update, delete user groups
- **Member Management**: Adding, removing, querying user group members
- **Permission Allocation**: Permission control based on user groups

### 🏢 Enterprise Management System

#### Personnel Type Management (V3)

- **Classification System**: Employee type classification and label management
- **Flexible Configuration**: Supports custom personnel type attributes

#### Job Level Management (V3)

- **Job Level System**: Complete job level promotion and management
- **Job Level Association**: Link with salary, permissions

#### Job Title Management (V3)

- **Career Path**: Employee career development path management
- **Sequence Definition**: Different sequence position definitions

#### Position Management (V3)

- **Position Definition**: Specific position responsibilities and authority definition
- **Position Assignment**: Employee position allocation and changes

#### Role Management (V3)

- **Permission Roles**: Role-Based Access Control (RBAC)
- **Role Inheritance**: Role permission inheritance and combination
- **Member Management**: Adding, removing role members operations

#### Unit Management (V3)

- **Organizational Units**: Enterprise organizational unit management
- **Unit Hierarchy**: Hierarchy between units

#### Work City Management (V3)

- **Office Locations**: Work city and location management
- **Location Association**: Association with departments, employees

### 🔧 Core Technical Features

#### Attribute-Driven Design

- **[HttpClientApi] Attribute**: Automatically generate HTTP client code
- **Strong Type Support**: Compile-time type checking, reduce runtime errors
- **Unified Response**: Unified response handling based on `FeishuApiResult<T>`

#### Dependency Injection Friendly

- **Service Registration**: `AddFeishuApiService()` extension method
- **Flexible Configuration**: Supports configuration files and code configuration
- **Lifecycle Management**: Automatic service lifecycle management

#### High-Performance Caching

- **Smart Caching**: Automatic token caching and refresh
- **Concurrency Control**: Solve cache issues under high concurrency
- **Resource Management**: Implements `IDisposable` interface

#### Exception Handling

- **Unified Exception**: `FeishuException` unified exception handling
- **Error Classification**: Classification handling of different error types
- **Log Integration**: Integrated with .NET log system

### 🌐 API Coverage

#### Authentication and Authorization API

- `IFeishuV3AuthenticationApi` - V3 authentication and authorization interface

#### Message Service API

- `IFeishuV1Message` - V1 message base interface
- `IFeishuTenantV1Message` - V1 tenant message interface
- `IFeishuUserV1Message` - V1 user message interface
- `IFeishuTenantV1BatchMessage` - V1 batch message interface

#### Organizational Structure API (V1)

- `IFeishuV1ChatGroup` - V1 chat group base interface
- `IFeishuTenantV1ChatGroup` - V1 tenant chat group interface
- `IFeishuUserV1ChatGroup` - V1 user chat group interface
- `IFeishuV1ChatGroupMember` - V1 chat group member base interface
- `IFeishuTenantV1ChatGroupMember` - V1 tenant chat group member interface
- `IFeishuUserV1ChatGroupMember` - V1 user chat group member interface
- `IFeishuV1Departments` - V1 department management base interface
- `IFeishuTenantV1Departments` - V1 tenant department management interface
- `IFeishuUserV1Departments` - V1 user department management interface
- `IFeishuV1Employees` - V1 employee management base interface
- `IFeishuTenantV1Employees` - V1 tenant employee management interface
- `IFeishuUserV1Employees` - V1 user employee management interface

#### Enterprise Management API (V3)

- `IFeishuV3Departments` - V3 department management base interface
- `IFeishuTenantV3Departments` - V3 tenant department management interface
- `IFeishuUserV3Departments` - V3 user department management interface
- `IFeishuTenantV3EmployeeType` - V3 tenant personnel type management interface
- `IFeishuTenantV3JobFamilies` - V3 tenant job family management interface
- `IFeishuTenantV3JobLevel` - V3 tenant job level management interface
- `IFeishuV3JobTitle` - V3 position management base interface
- `IFeishuTenantV3JobTitle` - V3 tenant position management interface
- `IFeishuUserV3JobTitle` - V3 user position management interface
- `IFeishuTenantV3RoleMember` - V3 tenant role member management interface
- `IFeishuTenantV3Role` - V3 tenant role management interface
- `IFeishuTenantV3Unit` - V3 tenant unit management interface
- `IFeishuV3User` - V3 user management base interface
- `IFeishuTenantV3User` - V3 tenant user management interface
- `IFeishuUserV3User` - V3 user management interface
- `IFeishuTenantV3UserGroupMember` - V3 tenant user group member management interface
- `IFeishuTenantV3UserGroup` - V3 tenant user group management interface
- `IFeishuTenantV3WorkCity` - V3 tenant work city management interface
- `IFeishuV3WorkCity` - V3 work city base interface

### 📦 Technology Stack

#### Framework Support

- **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+
- **.NET 6.0** - LTS Long-term support version
- **.NET 8.0** - LTS Long-term support version
- **.NET 10.0** - LTS Long-term support version

#### Core Dependencies

- **Mud.ServiceCodeGenerator v1.4.5.3** - HTTP client code generator
- **System.Text.Json v10.0.1** - High-performance JSON serialization (.NET Standard 2.0)
- **Microsoft.Extensions.Http** - HTTP client factory
  - .NET 6.0 / .NET Standard 2.0: v8.0.1
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Http.Polly** - Resilience and transient fault handling
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Logging** - Logging
  - .NET 6.0 / .NET Standard 2.0: v8.0.3
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Configuration.Binder** - Configuration binding
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1

## 🔗 Related Links

- [Project Gitee Homepage](https://gitee.com/mudtools/MudFeishu)
- [Project Github Homepage](https://github.com/mudtools/MudFeishu)
- [NuGet Package](https://www.nuget.org/packages/Mud.Feishu/)
- [Documentation Site](https://www.mudtools.cn/documents/guides/feishu/)
- [Feishu Open Platform](https://open.feishu.cn/document/)
- [Issue Feedback](https://gitee.com/mudtools/MudFeishu/issues)

---

_Note: This CHANGELOG follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) specification._