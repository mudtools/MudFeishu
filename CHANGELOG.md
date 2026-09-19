# Mud.Feishu 更新日志

## [3.0.0-rc2] - 2026-09-18

### 🌟 亮点

- ⚡ **原生 AOT 全面适配**：net8.0+ 一等公民支持 Native AOT 发布——全链路源生成 JSON 序列化与
  配置绑定、AOT 严格模式质量门禁保证 0 反射告警，附带端到端验证工程与完整文档。
- 🔐 **令牌与多应用管理三轮专项加固**：用户令牌可续期、401 恢复真正生效、
  凭据变更即清库、Memory/Redis 令牌键统一、配置热更新事务化、OAuth 失败语义分类等 60+ 项修复。
- 🪝 **WebSocket 可靠性**：事件处理失败不再静默吞异常（飞书将重发）、重连熔断、并发闸门、健康检查并发指标。
- 🗄️ **Redis 去重加固**：四类键统一构造与转义、Cluster 全节点覆盖、竞态 Lua 原子化、失败可分类、新增 Testcontainers 集成测试。
- 🛡️ **质量门禁**：`verify-build.ps1` 全新门禁（缓存自检、全 TFM 构建 + 诊断白名单、AOT 严格模式冒烟、TRX 测试计数断言），CI 同步接入。
- 📦 **依赖升级**：`Mud.HttpUtils` / `Mud.HttpUtils.Generator` 统一钉住 **2.0.7** （组件首个正式版系列，生成器修复源生成同名类型冲突 SYSLIB1031）。

### ⚠️ 升级须知（破坏性变更 / 行为变更）

**令牌与多应用**
- 令牌失效现在级联清除持久层存储——401 恢复才真正生效（此前重试仍用被拒旧令牌）。
- Redis 令牌键布局变更：`feishu:token:*` → `feishu:{appKey}:token*`，升级后旧键不再读取；
  `SingletonFeishuTokenStoreFactory` 已废弃，请改用 `PerAppRedisTokenStoreFactory`。
- `AddFeishuRedisTokenStore` 不再注册 `ITokenStore`/`IUserTokenStore` 单例，
  改用 `IFeishuTokenStoreFactory.Create(appKey)`。
- 租户请求的 401 恢复不再回退为用户级恢复（重试不会被注入用户令牌）。
- `GetAllApps()` 不再隐式实例化全部应用；后台令牌刷新默认仅覆盖默认应用，其余应用首次访问时增量注册。
  需要启动期预热请设 `WarmUpAllAppsOnStartup = true`。
- `SetDefaultApp` / `TrySetDefaultApp` / `DefaultAppKey` 现在真正生效，请复核运行期切换默认应用的调用点。
- `TryGet*` 令牌管理器解析器无默认应用时返回 null（不再抛异常）。
- 认证/取令牌请求改用本应用的命名客户端（多区域部署不再取错平台端点）；
  必要时以 `EnablePerAppAuthenticationClient = false` 降级。
- 配置热更新默认开启（`EnableConfigReload = true`），`BaseUrl`/`TimeOut` 变更无需重启；
  如需「配置变更需重启」的旧语义，显式设为 `false`。
- OAuth 刷新失败按可重试/不可重试分类：`invalid_grant` 等将清除 refresh token 并要求重新授权。
- 凭据变更即清库：热更新检测到 (AppId, AppSecret) 变更，立即清除该应用全部持久化令牌。
- 已删除死配置项：`EnableContextRetirement` / `PurgeStoreOnTokenInvalidation`（默认安全行为保留）。

**配置与 HTTP**
- `FeishuAppConfig` 移除 `required`：AOT 源生成绑定要求，校验统一由 `Validate()` 承担。
- 增强 HttpClient 装配基线化：此前手工 `new` 静默取默认值的 10 个字段现与注册路径同源。
- `nuget.config` 收紧为包来源锁定（`<clear />` + `packageSourceMapping`）。

**WebSocket**
- 事件处理失败不再静默吞异常：ACK 返回 `code=500`，飞书服务端将重发——**请确保业务处理器幂等**。
- 同步 `Dispose()` 不再尝试停止服务；确定性停止请 `await StopAsync()` 或 `await DisposeAsync()`。
- 服务端 Pong 下发的 ClientConfig 不再覆盖本地重连策略（`PingInterval` 仍生效，钳制 5–30 秒）。
- 构造签名变更：`FeishuEventMessageHandler` 移除 `seqIdDeduplicator` 参数；
  `WebSocketBinaryMessageEventArgs.ProcessingTask` 移除。
- 重连协调器不再在持锁期间触发事件：订阅者在回调内**同步阻塞等待**重入 `TryReconnectAsync` 不再死锁，
  并发重连请求改为立即返回 `false`。**注意：回调内不得同步阻塞**（会占住重连闸门导致后续重连被跳过），
  耗时操作请自行 `Task.Run` 或改由 `ReconnectFailed`/`ReconnectLimitReached` 触发异步补偿。
- 旧连接接收循环的迟到异常不再误报为"新连接断开"（断线声明绑定 socket 身份）；
  "旧连接已关闭 + 新连接握手失败"场景下 `Disconnected` 事件不再丢失。
- `Dispose()` 之后调用连接/发送 API 现在确定性抛出 `ObjectDisposedException`（此前为随机抛出或偶发成功）；
  内部 `SemaphoreSlim` 不再随 `Dispose` 释放（消除在途 `Release()`/租约归还的 `ObjectDisposedException` 竞态，
  未访问 `AvailableWaitHandle`，无 OS 句柄泄漏）。
- `MaxReconnectDelayMs` setter 不再自动抬升到 `ReconnectDelayMs`：非法组合改由 `Validate()` 在启动期报错
  （此前赋值结果依赖配置绑定顺序）。
- 文本消息发送与接收统一按 **UTF-8 字节**计量（新增 `MessageSizeLimits.MaxTextMessageBytes`，
  0 = 3 × `MaxTextMessageSize` 自动推导）。默认值下属**放宽**：旧"字符语义"的合法消息全部继续通过；
  二进制发送补齐此前完全缺失的 `MaxBinaryMessageSize` 校验。
- 背压前移到接收路径：并发槽位耗尽（`MaxConcurrentHandlers`，默认 32）时接收循环被阻塞以施加 TCP 反压
  （排队任务数与消息副本数一并受上界约束）。需要旧行为可设 `MaxConcurrentHandlers = 0`。
- 分片文本消息的接收上限由"1MB 字节"改为与发送侧同源（默认 3MB 字节），不再误拒"1MB 字符级"合法消息。
- 关闭握手回显服务端下发的关闭码/描述（RFC 6455 §5.5.1），不再固定 `NormalClosure`；同步 `Dispose()` 的
  关闭握手超时后会强制中止并观察残留任务异常（不再遗留无人观察的任务）。
- **指标 API 变更**：`FeishuMetrics.WebSocketConnectionObserver` / `WebSocketBacklogObserver` 两个静态可写属性**已移除**，
  改为 `FeishuMetrics.RegisterWebSocketMetricsSource(appKeyProvider, activeConnectionsProvider, pendingMessagesProvider)`
  （返回注销令牌，`Dispose` 后停止采集）。原因：静态单值属性在多应用场景互相覆盖，且长期持有已释放的服务实例；
  新形态按注册实例聚合，AppKey 由提供器每次采集时读取（支持热更新）。自定义集成请迁移到新 API。

**DTO 重命名（修复 SYSLIB1031，AOT 源生成要求）**
- `DepartmentsV1.DepartmentLeader` → `DepartmentLeaderV1`、`DepartmentDetail` → `DepartmentDetailV1`、
  `DepartmentPathInfo` → `DepartmentPathInfoV1`
- `ApprovalExternal.ApprovalCreateViewers` → `ExternalCreateViewers`；
  `Drive.Folder.FileShortcutInfo` → `FileShortcutTargetInfo`；`TasksSections.TaskSummary` → `TaskSectionSummary`
- 移除嵌套类型 `AppTableViewProperty.AppTableViewPropertyHierarchyConfig`；
  `IFeishuV2TaskSections.GetTaskSectionsPageListByIdAsync` 返回类型变更为
  `FeishuApiPageListResult<TaskSectionSummary>`

**Redis 去重**
- `RedisOptions` 非法值（0/负 TTL/空前缀）改为启动期校验失败；事件去重亚秒 `ttl` 抛异常；
  `MarkAsCompletedAsync` 不再为不存在的键创建永久记录。
- SeqID 去重键增加 `scopeKey` 隔离维度（默认 `AppKey|MachineName`），多实例不再互相判重；
  `GetCacheCount`/`GetMaxProcessedSeqId` 语义收窄为 TTL 窗口内。
- `NonceFailureMode` 仅对 Redis 连接类故障生效；服务端/配置类错误直接抛出。
- 四类 Redis 键（事件/Nonce/SeqID/令牌）统一走 `RedisKeyBuilder` 构造（分段转义 `:`，杜绝跨段碰撞）。

### ✨ 新增

- **令牌存储加密**：`EncryptedTokenStore` 系列 + `FeishuAppOptions.EnableTokenEncryption`
  （默认关闭；未注册加密提供程序时降级明文并告警，解密失败按缓存未命中处理）。
- **多应用配置热更新**：`appsettings.json` 变更按 AppKey 增量应用；`BaseUrl`/`TimeOut` 运行期热更新，
  多区域切换无需重启。
- **AOT 安全 JSON 入口**：`FeishuJsonAot`（`JsonTypeInfo` 重载）+
  `FeishuJsonDefaults.ConfigureUserResolver` 自定义 Context 注册。
- **多应用 API**：`IFeishuAuthenticationFactory` / `PerAppFeishuAuthenticationFactory`、
  `IFeishuAppManager.ConfiguredAppKeys`、`AppInstantiated` 事件、`FeishuAppContextRetirement` 退休队列、
  `TokenKeyBuilder`、`FeishuOAuthErrorClassifier`。
- **WebSocket**：并发闸门 `FeishuWebSocketConcurrencyService`（`MaxConcurrentHandlers` 默认 32）、
  真实 `backlog` 指标、按 app_key 分组连接指标、`AllowCertificateNameMismatch`、
  `ProtocolKeepAliveInterval`（默认 20s）、统一去重中间件接入、健康检查并发指标与重连熔断态、
  `AckResponse`/`SubscriptionRequest` 强类型 DTO。
- **WebSocket（本轮加固）**：`MessageSizeLimits.MaxTextMessageBytes`（字节维度上限，0=自动推导）；
  已解析帧在处理异常时补 ACK `code=500`（服务端即时重投，不再等超时）；`WebSocketBinaryMessageEventArgs.ReceiveStartTime`
  现在真实赋值（`ReceiveDurationMs` 可用）；`FeishuWebSocketClient` 的 `AppKey`/认证闸门读取支持
  `IOptionsMonitor` 热更新（其余配置项需重启，已在 XML 注释中口径化）。
- **Redis**：`RedisKeyBuilder`、`FeishuRedisException` + `FeishuRedisFailureKind` 可分类失败契约、
  `RedisOptions.ValidateOnStart()`、Cluster 全节点聚合 `GetServers()`、Testcontainers 集成测试工程。
- **文档**：`documents/ErrorHandling.md`（下载方法错误契约与「HTTP 200 + JSON 错误体」残余风险）、
  `documents/ResponseCaching.md`（多应用缓存键隔离约束）；README 多租户部署指引。

### 🐛 修复（按模块摘要）

- **多应用/令牌**：401 恢复拿到同一被拒令牌（P0）；租户重试被注入用户令牌；默认应用状态分裂；
  Lazy 异常缓存自愈失效致应用永久毒化；热更新快照并发修改 / 节流漏比对字段 / 缺 IsDefault 校验；
  旧上下文令牌维护 Timer 泄漏；HostedService 启动期强制实例化阻断宿主启动；稳态 store 恢复永不命中；
  无过期存储值编造有效期；重复 `AddFeishuApp` 双重加解密；凭据变更清库在内存后端无效（TMF-01）；
  热更新锁内同步阻塞 IO（TMF-02）；恢复路径 IssuedAt 缺失；指标缺 appKey 维度；
  Redis 过期令牌 TTL=0 永不过期；Redis 连接串日志含口令等。
- **JSON / AOT**：net8+ 未覆盖类型抛 `NotSupportedException`（补链尾反射兜底 + 幂等 + 加锁）；
  开放泛型误标 `[HttpJsonSerializable]`（AOT006 / SYSLIB1030）；DataModels 7 组同名 DTO 触发
  SYSLIB1031（重命名修复）；低 TFM 7486 条 AOT006 噪音豁免。
- **HTTP / 配置**：`IFeishuAuthentication` 未注册；`BaseUrl`/`Timeout` 不参与热更新；
  per-app 弹性策略固化注册期配置；解密失败完全静默（补节流告警）；
  `FailedEventRetryService` 反序列化选项失配；204 条 NU1603 版本漂移。
- **WebSocket**：健康检查并发指标缺失、重连无熔断、配置热更新不一致、协议保活硬编码、
  未接入统一去重中间件、ACK 恒 200 致事件永久丢失。
- **Redis**：空前缀 `ClearCacheAsync` 误删全库（P0）；`redis://`/`rediss://` 地址无法连接；
  Rollback/Mark 并发竞态（Lua 原子化）；超时判定依赖客户端时钟；Sorted Set 无界增长；
  `CancellationToken` 全链路失效；Cluster 单节点覆盖；键 `:` 跨段碰撞。

<details>
<summary><strong>📎 附录：逐任务工程明细（面向维护者，点开展开）</strong></summary>

> 完整方案与逐条验证记录见 `.docs/` 各方案文档（MudHttpUtils-2.0.4-Repair-and-Enhancement-Plan、
> MudHttpUtils-2.0.5-Review-Remediation-Plan、MudFeishu-Token-MultiApp-Review-Remediation-Plan-R1/R2）
> 与 `AGENTS.md` 质量门禁章节。

**AOT 适配**
- net8.0+ 全部源工程启用 `IsAotCompatible` / `EnableAotAnalyzer` / `EnableTrimAnalyzer` / `TrimMode=full`
  （netstandard2.0 / net6.0 按 TFM 精确豁免噪音 AOT006）；启用 `EnableConfigurationBindingGenerator`，
  配置 DTO 不用 `required`、改由 `Validate()` 校验。
- WebSocket 二进制链路替换 Protobuf 静态（反射式）序列化为编译期 `FeishuWebSocketProtoModel`
  （禁止 `ProtoBuf.Serializer` 静态门面）；Abstractions / DataModels 附 rd.xml 裁剪兜底。
- `verify-build.ps1` 步骤 3 以 `AotStrictMode` + `--no-incremental` 冒烟（修复 CoreCompile 增量判定
  不比较 csc 命令行导致的假绿，暴露并补齐 WebSocket 4 条标注链），断言 `AOT00x` / `IL2026` / `IL3050` 为 0。
- 新增 `FeishuJsonAot`（net8+ 从 options 解析 `JsonTypeInfo`，无 resolver 时回退反射）；
  `FeishuJsonDefaults.ConfigureUserResolver` 自定义 Context 注册；`Demos/Mud.Feishu.AotVerification`
  双 RID 端到端验证（JSON 序列化 / HTTP 客户端 / 事件处理 / WebSocket 协议消息）。

**SYSLIB1031（同名 DTO 冲突）**
- STJ 源生成器按类型简单名生成元数据，DataModels 7 组同名 DTO 编译期报 9 条 SYSLIB1031
  （net8.0/net10.0 各一遍），且仅为其一生成元数据——其余运行时静默退化为反射兜底（AOT 下失效），
  派生集合类型（`ListDepartmentLeader`、`ApprovalCreateViewersArray`）同理被丢弃。
  修复：按命名空间语义重命名（见升级须知）并移除重复嵌套定义。

**Mud.HttpUtils 2.0.6 组件侧**
- 打包防呆（Release 打包 + 包内 DLL 与 `bin/<Configuration>` 逐字节校验）与清单漂移防护
  （补齐 `Mud.HttpUtils.Xml` / `JsonContextScaffolder`）；修复三处 DI 构造歧义
  （`TokenRecoveryDelegatingHandler` / `StandardOAuth2TokenManager` / `PollyResiliencePolicyProvider`）
  与 `TokenRefreshHealthCheck` 构造歧义；`RequiresDynamicCodeAttribute` polyfill 边界与 net7.0 资产缺口；
  `UserTokenInfo` 复制入口丢失 `IssuedAt`（TMX-22：TTL 感知过期提前量 `min(阈值, ttl/2)` 生效）。
  组件侧新增 10 条机器护栏用例。
- `AGENTS.md` / README 依赖表 / 契约守卫 `TokenMultiAppContractGuards.ExpectedVersion` 同步为 2.0.6。

**门禁（verify-build.ps1 / CI）**
- 步骤 4 重写：TRX 计数器路径修复（原写错恒 `$null` 且回退中文正则）、逐 `(工程, TFM)` 运行并逐组合
  断言 TRX 存在且 `total > 0`、按 `dotnet --list-runtimes` 显式播报跳过组合；门禁不再依赖中文输出。
- CI（`dotnet-publish.yml`）：Restore 前 `-CacheCheckOnly`；Build 日志断言 `NU1603` / `CS1750` /
  `HTTPCLIENT0xx` / `MUD001-002` / `FORM0xx` / `AOT001-007` 全零；移除 filter 使
  `AotJsonSerializableCoverageTests` 等架构守卫真实生效。

**TMA2 / TMF（令牌多应用 R2 复审 + 后续修复）逐任务**
- TMA2-P0-1：用户令牌续期可达——新增 `LoadRefreshCandidateAsync` 独立读取 refresh token，
  不依赖 access token 有效性。
- TMA2-P0-2：`TokenKeyBuilder` 统一 Memory / Redis 键布局（逐字节一致）。
- TMA2-P0-3：门禁去中文依赖（TRX + locale-independent）。
- TMA2-P1-1：恢复弃用阈值与缓存失效阈值同源（`TokenRefreshThreshold`）。
- TMA2-P1-2：凭据变更即清库（检测 (AppId, AppSecret) 变更）。
- TMA2-06：`FeishuOAuthErrorClassifier` OAuth 失败分类；-07：`AppInstantiated` 增量注册 +
  `FeishuTokenRegistrationHelper`；-08：`AddApp` 默认键加锁；-09：热更新两阶段事务化
  （Phase-A 预装配锁外 / Phase-B 提交锁内）；-10：`TryGet*` 改用 `DefaultAppKey` 属性不抛异常；
  -11：装配失败 scope 释放 + Dispose 遍历在册上下文；-12：异常过滤收敛为瞬时白名单
  （`IsTransientInitFailure`）；-13/-14：删除死配置项与死常量；-15：Redis 过期刷新令牌改
  `KeyDeleteAsync`；-17：`EnsureFeishuAppNotRegistered` 调用顺序守卫；-18/-19/-20：文档版本一致性 /
  Redis 连接串脱敏 / 多租户部署指引；-21：六条契约守卫测试（键布局单一真相源、包版本唯一、
  配置属性集契约、认证 DTO 覆盖、ConfigDto 禁 required、无未消费开关）。
- TMF-01（P0）：凭据变更清库在内存后端无效——记账改为按 KeyPrefix 的进程级共享注册表，
  新增 `IFeishuUserTokenStorePurge.ClearAllUsersAsync`（Redis 走 SCAN 模式
  `TokenKeyBuilder.AllUsersScanPattern`），Memory 与 Redis 清库语义统一；
  `GetTokenTypesAsync` 语义放宽为「本进程本前缀已知」。
- TMF-02（P1）：热更新 Phase-P——凭据检测与清库（IO）移到 `_configApplyLock` 之外，
  消除锁内同步阻塞；`ResolveExistingContext` 收敛三份旧上下文解析副本。
- TMF-03（P2）：`PurgeTokenStoreAsync` 收口 `OperationCanceledException`，取消信号不再使整次热更新被放弃。
- TMF-04（P2）：删除产品代码零调用的 `RebuildAppContext` 死代码（含第二份清库副本）。
- TMF-05（P2）：刷新失败清库前 CAS 比对 store 现值，防止误删并发刷新刚持久化的新令牌。
- TMF-06（P2）：恢复令牌 IssuedAt 不可知按阈值保守判定，文档化短 TTL 部署不应依赖 store 恢复路径。
- TMF 验收收口：Memory 双存储 `SetRefreshTokenAsync` 补共享记账（仅写 refresh 的 tokenType
  不再成为凭据变更清库盲区）；`FeishuUserTokenStore.ClearUserAsync` 改为「保留外层记账条目、
  仅清内容」，消除与并发写令牌交错的孤儿字典注册丢失；补齐 Redis `ClearAllUsersAsync` SCAN 用例、
  加密装饰器透传用例；重建 `Demos/Mud.Feishu.Webhook.Demo` 缺失的 `WebhookDemoJsonContext`
  （3c099cf4 引用未定义类型导致 Demo 构建失败）。

**TMA（R1）逐任务**
- P0-1：令牌失效级联清 store，401 恢复真正生效。
- P1-1：租户 401 恢复不回退用户级（凭据类别由显式上下文决定）；P1-2：`SetDefaultApp` 状态分裂修复；
  P1-3：Lazy 异常缓存自愈覆盖 `InvalidOperationException`（DI 解析失败不再永久毒化应用）；
  P1-4：配置快照 `List<T>` 锁内改锁外读；P1-5：热更新节流漏比对字段（`TokenRefreshThreshold` /
  重试 / 熔断 / `AllowCustomBaseUrl`）；P1-6：旧上下文 Timer 泄漏 + 后台刷新字典驻留旧实例；
  P1-7：`GetAllApps()` 语义回归 + 后台刷新仅默认应用 + `WarmUpAllAppsOnStartup`；
  P1-8：per-app 认证客户端（`EnablePerAppAuthenticationClient` 可降级）。
- P2-1/-2：store 恢复边界修正（缓存 TTL 全量化、弃用阈值 `Math.Max(60, threshold/2)`）/
  无过期存储值不再编造 1800 秒有效期；P2-5：`TryGet*` Try 语义；P2-7：重复 `AddFeishuApp`
  双重加解密（加密装饰器幂等）；P2-11：`RemoveApp` 默认应用提升确定性；P2-13：热更新先校验后应用
  （AppKey / IsDefault 唯一性 + 逐条 `Validate()`）。
- TMA-13：`FeishuAppManager` Captive Dependency（改注入 `IServiceScopeFactory`）；
  TMA-19/-21：加密幂等 / 非内存存储未加密启动告警；TMA-22：refresh token TTL 不再硬编码 30 天；
  TMA-23：指标补 appKey 维度（`token_manager_key` 可区分）；TMA-24：注释纠偏
  （退休队列 / `StopAsync` / 单应用注册提示）。
- 新增：`FeishuAppContextRetirement`（宽限期后 Dispose，停止令牌维护 Timer）、
  `IFeishuAuthenticationFactory` / `PerAppFeishuAuthenticationFactory`（per-app 端点与弹性策略）、
  `IFeishuAppManager.ConfiguredAppKeys`（不触发懒加载）、
  `ContextRetireDelaySeconds` / `RemoveRuntimeAddedAppsOnReload` 选项。

**ARC / SEC / ENH / TOK**
- ARC-1：多应用配置热更新（`IOptionsMonitor<List<FeishuAppConfig>>.OnChange` Diff 增量 + 快照节流）；
  ARC-2：增强 HttpClient 装配基线化（`RequestBodySerialization` / `ExceptionRedactor` / `HttpVersion` /
  `JsonTypeInfoResolver` 等 10 字段与注册路径同源，`AppAccessAuthorizer` 同步）；
  ARC-7/-7b：`BaseUrl`/`TimeOut` 运行期热更新（`ConfigureHttpClient(IServiceProvider, HttpClient)` 按 diff
  覆盖，未变化短路）与 per-app 弹性策略读 `IOptionsMonitor` 当前配置（残余：组件侧已解析策略缓存
  需重启，COMP-4）。
- SEC-1：`nuget.config` 包来源锁定（`<clear />` + `packageSourceMapping`）；组件 2.0.6 发布到
  nuget.org 前，托管 CI 需注入组件源（COMP-5）。
- ENH-1：令牌存储加密（`EncryptedTokenStore` / `EncryptedUserTokenStore` /
  `EncryptedFeishuTokenStoreFactory`）；ENH-2：加密 marker 契约（`IEncryptedTokenStore`，组件 v2
  前向兼容）+ 解密失败节流告警（首次 + 每 100 次一次，不含明文密钥/密文）。
- TOK-1：Redis 令牌键 `feishu:{appKey}:token*`（与 Memory 对齐，旧键不再读取）；
  TM-04：`GetTokenTypesAsync` 对含 `:` 的 tokenType 截断修复。
- 其他修复：`IFeishuAuthentication` 从未注册（改调源生成 `AddAuthenticationWebApiHttpClient()`）；
  热更新下 `UpdateApp` 报「未找到应用」（改 `RegisterApp`）；`FailedEventRetryService` 反序列化
  选项失配（统一 `FeishuJsonDefaults.DeserializerOptions`）；204 条 NU1603 漂移（钉版本 +
  修正 Tests 中不存在的包引用）。
- 文档与测试：`documents/ErrorHandling.md`（统一响应模型、9 个 `Task<byte[]?>` 下载方法错误契约与
  「HTTP 200 + JSON 错误体」残余风险）、`documents/ResponseCaching.md`（`[Cache]` 接入与
  多应用缓存键隔离约束）；`FeishuClientEndpointHotReloadTests`（6）/ `DownloadErrorSemanticsTests`（3）/
  `EncryptedTokenStoreTests` +4；`FeishuJsonDefaults.Reset()` 供测试隔离。

**WebSocket（WS 系列）**
- WS-01（P0）：事件处理失败向上传播、回 `code=500` 让飞书重发（原 ACK 恒 200 致事件永久丢失）；
  WS-02（P0）：同步 `Dispose()` 不再持锁等待 `StopAsync` 死锁（约 3 秒超时且服务未停止）。
- 新增：WS-03 并发闸门 `FeishuWebSocketConcurrencyService`（`MaxConcurrentHandlers` 默认 32，热更新）；
  WS-06 `AckResponse` / `SubscriptionRequest` 强类型 DTO（消除 AOT 反射依赖）；
  WS-12 `AllowCertificateNameMismatch`（与 `AllowSelfSignedCertificates` 解耦）。
- 修复：WS-07 服务端 Pong 不覆盖本地重连策略（`PingInterval` 钳制 5–30 秒）；WS-17 连接指标按
  app_key 分组；WS-21/-25 移除死参数 `seqIdDeduplicator` / 死属性 `ProcessingTask`；
  F1 `backlog` 指标真实化；F2 健康检查并发指标（槽位耗尽 Unhealthy / ≥90% Degraded）；
  F3 重连熔断器（达上限打开、连接成功清除，健康检查返回 Degraded）；F4 客户端配置热更新一致
  （注入 `IOptionsMonitor`）；F5 `ProtocolKeepAliveInterval` 可配置（默认 20s，5–300s）；
  F7 接入 `IUnifiedDeduplicationMiddleware` EventId + SeqID 双重去重（可用时优先，否则回退）。

**Redis（审查整改 R 系列）**
- R-01（P0）：`SeqIdKeyPrefix` 为空时 `ClearCacheAsync` 退化为全库删除；R-05/R-06：
  Rollback/Mark 并发竞态 Lua 原子化；R-08：SeqID Sorted Set 无界增长（写入时刷新 TTL 并裁剪）；
  R-10：Cluster 下 `ClearAsync`/`GetTokenTypesAsync` 全主节点覆盖（`RedisStoreHelper.GetServers()`）；
  R-11：`CancellationToken` 全链路失效；R-13：`redis://`/`rediss://` 地址支持
  （`ConfigurationOptions.Parse`，自动 TLS）；R-14：同步释放补 `IDisposable`；
  R-15：超时判定改用 Redis `TIME`；R-20/R-21：令牌键裸拼接 `:` 跨段碰撞。
- 新增：`RedisKeyBuilder`（分段转义、长度上限 256、空前缀防护）、`FeishuRedisException` +
  `FeishuRedisFailureKind`、`RedisOptions.ValidateOnStart()`（net6+）、
  `Tests/Mud.Feishu.Redis.IntegrationTests`（Testcontainers 真实 Redis）。
- 文档：移除不存在的 `RedisFeishuEventDistributedDeduplicatorWithFallback` 及降级承诺；
  README 新增「能力 ↔ 实现 ↔ 测试」映射表与令牌明文存储安全披露。

</details>

## [2.1.5] - 2026-06-25

### ✨ Added

- **AI 文档处理**: 新增飞书 AI 文档处理模块（智能文档处理能力，支持 17 种证件识别）
  - 添加简历信息解析接口及数据模型
  - 添加名片识别接口及数据模型
  - 添加合同字段识别接口及数据模型
  - 添加营业执照识别接口及数据模型
  - 添加增值税发票识别接口及数据模型
  - 添加驾驶证识别接口及数据模型
  - 添加食品经营许可证识别接口及数据模型
  - 添加食品生产许可证识别接口及数据模型
  - 添加身份证识别接口及数据模型
  - 添加出租车发票识别接口及数据模型
  - 添加火车票识别接口及数据模型
  - 添加车辆行驶证识别接口及数据模型
  - 添加银行卡识别接口及数据模型
  - 添加中国护照识别接口及数据模型
  - 添加台湾居民来往大陆通行证识别接口及数据模型
  - 添加港澳居民来往内地通行证识别接口及数据模型
  - 添加机动车发票识别接口及数据模型
  - 添加健康证识别接口及数据模型
  - 支持租户级别和用户级别调用
- **AI 文字识别 (OCR)**: 新增飞书基础图片 OCR 识别功能
  - 添加基础图片 OCR 识别接口及数据模型
- **AI 语音转文字**: 新增飞书语音转文字（Speech-to-Text）功能
  - 添加飞书语音文件识别接口及数据模型
  - 添加流式语音识别接口及数据模型
- **AI 翻译**: 新增飞书翻译相关接口（语言检测、文本翻译等）
- **搜索模块**: 新增飞书搜索（Search）模块
  - 添加飞书 V2 文档 Wiki 搜索接口及数据模型
  - 添加飞书 V2 套件搜索接口及数据模型
  - 添加飞书 V2 搜索数据源（DataSource）全套接口及数据模型
  - 添加搜索数据源索引管理接口（创建、批量创建等）及数据模型
  - 添加数据模式（Schema）相关 API 接口及数据模型

### 🔧 Changed

- **AI 接口命名空间**: 调整 AI 接口命名空间，统一归入 `Interfaces/AI` 目录结构
- **邮件组**: 移除邮件组接口定义（功能下架，相关 API 不再对外暴露）
- **机动车发票识别**: 重构机动车发票识别数据模型结构，新增健康证识别支持

### 🐛 Fixed

- 修复飞书消息序列化与测试用例相关问题
- **SpeechToText**: 将 `SpeechStreamConfig` 移动到独立文件，便于维护

### 🧪 Tests

- 调整测试项目目标框架与依赖配置，统一测试项目环境

### 📝 Docs

- 修正 README 示例代码中错误的配置变量名

## [2.1.4] - 2026-06-03

### ✨ Added

- **邮箱别名管理**: 新增飞书邮箱别名管理功能
  - 添加邮箱别名创建接口及数据模型
  - 添加邮箱别名查询接口及数据模型
  - 添加邮箱别名删除接口
  - 添加邮箱地址状态查询接口
- **公共邮箱管理**: 新增飞书公共邮箱管理功能
  - 添加公共邮箱创建接口及数据模型
  - 添加公共邮箱查询、更新、删除接口
  - 添加公共邮箱成员管理全套API接口与数据模型
  - 添加公共邮箱别名管理API接口与数据模型
- **邮件组管理**: 新增飞书邮件组管理功能
  - 添加邮件组创建、删除、更新、查询、列表接口及数据模型
  - 添加邮件组成员管理API（创建、删除、获取、分页列表、批量创建、批量删除）
  - 添加邮件组权限成员管理API（创建、删除、获取、批量查询）
  - 添加邮件组别名管理API（创建、删除、获取列表）
  - 添加邮件组管理员批量操作API（批量创建、批量删除、分页列表）
- **邮箱联系人管理**: 新增飞书邮箱联系人管理功能
  - 添加邮箱联系人增删改查分页管理功能
  - 支持租户级别和用户级别的调用
- **邮箱收信规则管理**: 新增飞书邮箱收信规则管理功能
  - 添加收信规则创建、删除、更新、列表、排序接口及数据模型
- **用户邮箱事件订阅**: 新增用户邮箱事件订阅功能
  - 添加订阅、获取订阅状态、取消订阅用户邮箱事件接口
- **用户邮箱邮件管理**: 新增用户邮箱邮件管理功能
  - 添加发送用户邮箱邮件接口及数据模型
  - 添加获取用户邮箱邮件详情接口及数据模型
  - 添加分页列表用户邮箱邮件接口（支持按文件夹、未读状态、标签等查询）
  - 添加按卡片获取用户邮箱邮件接口
- **邮箱文件夹管理**: 新增用户邮箱文件夹管理功能
  - 添加创建、删除、更新、获取、列表邮箱文件夹接口及数据模型
  - 添加批量删除邮箱文件夹接口
- **邮箱标签管理**: 新增用户邮箱标签管理功能
  - 添加创建、删除、更新、获取、列表邮箱标签接口及数据模型
  - 添加批量删除邮箱标签接口
- **邮件会话管理**: 新增用户邮箱会话管理功能
  - 添加获取、更新、列表、移动邮箱会话接口及数据模型
  - 添加批量删除邮件会话接口
- **邮件模板管理**: 新增飞书邮件模板管理功能
  - 添加邮件模板创建、删除、更新、获取、列表接口及数据模型
  - 添加列出可发信邮箱接口及数据模型
  - 添加邮件附件下载相关数据模型
- **日历模块**: 新增飞书日历管理功能
  - 新增租户级/用户级日历管理API
  - 新增日历ACL管理API
  - 新增日程管理API
- **视频会议**: 新增视频会议租户告警记录查询功能
  - 添加告警联系人和告警记录数据模型
  - 添加分页查询告警记录接口

### 🔧 Changed

- **模块注册**: 新增邮箱模块支持，注册邮件模块服务
- **接口命名规范**: 修正邮件组接口方法命名，统一为驼峰命名规范
  - 部分更新接口重命名为UpdateMailGroupPartialAsync
  - 全量更新接口重命名为UpdateMailGroupAsync
- **数据模型结构**: 调整邮件模板相关数据模型结构，移动到对应子目录

### 📝 Docs

- 新增飞书邮箱全套API文档
- 更新飞书视频会议API文档
- 更新日历相关API文档
- 统一API文档标题中的令牌术语
- 修正Exchange绑定接口文档与注释

## [2.1.3] - 2026-05-22

### ✨ Added

- **视频会议导出**: 新增飞书视频会议 V1 导出功能
  - 添加飞书视频会议 V1 导出接口定义
  - 添加会议列表导出 API 及数据模型
  - 添加参会人明细导出接口及数据模型
  - 添加参会人会议质量数据导出接口
  - 添加会议预约数据导出和查询 API
  - 添加下载导出文件 API 接口
- **会议室层级**: 新增飞书会议室层级管理功能
  - 添加会议室层级创建接口及数据模型
  - 添加删除会议室层级接口
  - 添加更新会议室层级接口及请求模型
  - 添加获取会议室层级详情接口
  - 添加批量查询会议室层级详情 API
  - 添加分页查询会议室层级列表 API
  - 添加搜索会议室层级 API 及数据模型
- **会议室**: 新增飞书会议室管理功能
  - 添加飞书租户视频会议室 API 接口定义
  - 添加会议室管理 API 及数据模型
  - 添加删除会议室接口
  - 添加更新会议室接口
  - 添加获取会议室详情接口及数据模型
  - 添加批量获取会议室 API 支持
  - 添加分页查询会议室列表 API
  - 添加搜索会议室 API
- **会议室配置**: 新增飞书会议室配置相关功能
  - 添加飞书会议室配置相关接口和数据模型
  - 添加创建范围配置 API 及请求模型
  - 添加获取范围配置 API 及数据模型
  - 添加获取预约配置表单 API 及数据模型
  - 添加更新预约配置表单 API 支持
  - 添加获取预约配置管理员 API 及数据模型
  - 添加更新预约配置管理员 API 支持
  - 添加获取停用状态变更通知配置 API
  - 添加更新停用通知配置 API 及数据模型
- **视频会议数据查询**: 新增飞书视频会议数据查询功能
  - 添加飞书会议数据查询 API 及数据模型
  - 添加会议质量和预约数据模型及 API

### 🔧 Changed

- **模块注册重构**: 重构模块注册逻辑，新增视频会议和认证授权模块
- **会议室预定配置**: 整理会议室预定配置的数据模型结构

### 📝 Docs

- 修复视频会议文档注释并更新接口参数

## [2.1.2] - 2026-05-11

### ✨ Added

- **视频会议录制**: 新增飞书会议录制 API 接口和相关数据模型
  - 添加会议录制 API 接口及请求模型
  - 添加会议录制 API 和相关数据模型
  - 添加设置会议录制权限 API 和相关模型
  - 添加租户级别和用户级别的会议录制接口
- **视频会议报告**: 新增视频会议报告功能
  - 添加视频会议报告相关模型和接口
  - 添加获取每日报告 API 及响应模型
  - 添加获取 Top 用户报告 API 及响应模型

### 🔧 Changed

- **HTTP 客户端增强**: 增强 FeishuHttpClient 的 HTTP 客户端选项支持

### 🧹 Chore

- 更新 Mud.HttpUtils 相关包到 1.7.1 版本


## [2.1.1] - 2026-05-11

### ✨ Added

- **视频会议**: 新增飞书视频会议 V1 模块支持
  - 新增飞书视频会议接口及实现
  - 添加预约会议相关数据模型和接口
  - 添加删除预约接口并完善预约文档
  - 添加更新预约接口及相关数据模型
  - 添加获取预约详情功能
  - 添加获取活跃会议相关数据模型和接口
  - 添加会议相关数据模型和接口
  - 添加会议分页列表接口及相关模型
  - 添加会议搜索功能及相关模型
  - 添加邀请参会人接口及相关数据模型
  - 添加移除会议用户接口及相关模型
  - 添加设置主持人接口并重构参会人管理

### 🔧 Changed

- **数据模型重构**: 将 Meeting 类重构为继承 MeetingBaseInfo
- **多应用配置重构**: 重构飞书多应用配置注册逻辑
- **视频会议重构**: 重命名预约会议相关类和方法

### 🧹 Chore

- 移除冗余的会议信息代码

## [2.1.0] - 2026-05-01

### ✨ Added

- **日历管理**: 新增飞书日历管理模块支持
  - 添加飞书日历 V4 接口定义
  - 添加日历创建相关数据模型和接口
  - 添加主日历查询接口及相关数据模型
  - 添加批量获取主日历信息功能
  - 添加获取日历信息的接口方法
  - 添加批量查询日历信息接口
  - 添加更新日历接口及数据模型
  - 增加查询日历列表接口及默认分页大小调整
  - 添加删除共享日历接口
  - 添加搜索日历接口
  - 添加日历订阅和取消订阅接口
- **日历 ACL**: 新增日历访问控制列表功能
  - 添加飞书日历 ACL 接口定义
  - 添加日历访问控制列表功能并重构相关类
  - 添加日历 ACL 创建和删除事件支持
- **日历日程**: 新增日历日程管理功能
  - 添加日历日程相关数据模型和接口
  - 添加获取日程接口及相关数据模型
  - 添加获取日程分页列表功能
  - 添加日历日程搜索功能及相关模型
  - 添加更新日程功能并重构响应模型
  - 添加租户和用户日历事件接口
  - 添加回复日程接口及请求模型
  - 添加取消订阅日程变更事件接口
  - 添加获取重复日程实例接口及响应模型
  - 添加获取日程实例视图相关数据模型和接口
- **日程参与人**: 新增日程参与人管理功能
  - 添加日程参与人相关功能接口及数据模型
  - 添加获取日程参与人列表接口
  - 添加获取日程参与群成员列表接口
- **会议群**: 新增会议群管理功能
  - 添加创建会议群接口及响应模型
  - 添加解绑会议群接口方法
  - 添加创建会议纪要和会议群响应模型及接口
- **请假日程**: 新增请假日程管理功能
  - 添加创建请假日程接口及模型
  - 添加删除请假日程接口
- **会议室**: 新增会议室相关功能
  - 添加会议室忙闲查询功能及相关数据模型
  - 添加会议室日程查询相关数据模型和接口
  - 添加会议室日程实例回复接口
  - 新增查询日历忙闲信息接口
  - 添加批量查询日历忙闲信息接口
- **Exchange 集成**: 新增 Exchange 账户绑定功能
  - 添加 CalDAV 配置生成功能
  - 添加 Exchange 账户绑定到飞书账户功能
  - 添加删除 Exchange 绑定接口
- **日历事件回调**: 新增日历相关事件回调支持
  - 添加日历变更事件类型和处理类
  - 添加日程变更事件支持
  - 添加第三方会议室日程变动事件支持
  - 添加会议室状态变更事件支持
- **多维表格**: 添加飞书多维表格工作流接口定义

### 🔧 Changed

- **日历模块重构**: 重构日历模块文件结构，将数据模型按功能分类整理
- **Exchange 重构**: 重命名 Exchange 绑定响应模型并添加查询接口
- **云文档重构**: 重命名评论接口以匹配驱动模块命名规范

### 📦 Build & Config

- 更新所有 demo 项目中的 Mud.Feishu 相关包至 2.1.3 版本
- 更新项目版本号至 2.1.3

### 📝 Docs

- 新增多维表格、电子表格和画板模块文档
- 添加飞书云文档相关接口文档
- 更新多个 README 文档，完善功能说明和配置示例

## [2.0.9] - 2026-04-24

### ✨ Added

- **云文档权限管理**: 新增飞书云文档权限管理功能
  - 添加云文档协作者权限相关模型和接口
  - 添加批量增加协作者权限接口
  - 添加删除云文档协作者权限接口
  - 添加云文档所有者转移功能
  - 添加判断用户云文档权限接口
  - 添加获取云文档权限设置接口
- **云文档密码**: 新增云文档密码功能支持
  - 添加云文档密码设置功能
  - 添加云文档密码刷新和停用接口
- **云文档订阅**: 新增云文档文件订阅功能
  - 添加文件订阅功能相关接口和模型
  - 添加更新文件订阅状态的接口
  - 更新用户订阅接口
- **云文档评论**: 新增飞书云文档评论功能
  - 添加飞书文档评论相关数据模型和接口
  - 支持批量获取评论
  - 添加解决/恢复评论接口
  - 添加文件评论表情回复支持
  - 添加全文评论接口及模型
  - 添加创建文件评论回复的接口
  - 添加更新和删除文件评论回复的接口
  - 添加评论表情回应接口
- **画板 (Board)**: 新增飞书画板功能模块
  - 添加画板主题接口及相关数据模型
  - 添加画板缩略图下载和语法解析接口
  - 添加创建画板节点接口及响应模型
  - 添加画板 V1 租户和用户接口
- **事件回调**: 更新事件处理器以使用新的事件回调模块

### 🔧 Changed

- **画板重构**: 重构画板相关数据模型和接口，优化模型文件结构
- **评论重构**: 统一云文档评论响应模型类名以 Result 结尾，重构评论接口支持批量获取
- **去重器重构**: 重构缓存清理逻辑并添加异步清空方法

### 🐛 Fixed

- 修复 RedisFeishuSeqIDDeduplicator 同步方法阻塞问题

### 📦 Build & Config

- 更新多个演示项目的依赖包版本至 2.0.8
- 更新项目版本号至 2.0.9

### 📝 Docs

- 更新云文档评论接口文档，添加分页获取回复方法和租户/用户实现说明

## [2.0.8] - 2026-04-12

### ✨ Added

- **EventCallback 项目**: 新增独立的事件回调处理项目
  - 重构数据模型结构，将原有抽象层的数据模型迁移至 EventCallback 项目
  - 添加项目文档说明
- **飞书 V2 事件头支持**: 添加对飞书 v2.0 事件 Header 的强类型支持
  - 支持飞书 V2 事件头解析
  - 统一事件处理器默认 Header 类型
- **云文档事件订阅**: 新增飞书云文档事件订阅功能
  - 添加云文档事件订阅接口
  - 添加云文档事件订阅状态查询功能
  - 添加取消用户云文档事件订阅接口
  - 添加查询用户云文档事件订阅状态功能
- **云文档事件回调**: 新增云文档相关事件支持
  - 添加文件已读和文件编辑事件支持
  - 添加文件协作者权限申请和添加事件支持
  - 添加文档协作者移除事件支持
  - 添加文件删除和回收站事件支持
  - 添加文件评论新增事件支持
- **多维表格自动化流程**: 新增自动化流程相关功能
  - 添加自动化流程相关接口及数据模型
  - 添加更新自动化流程状态接口支持
  - 添加获取工作流列表接口
  - 添加多维表格字段和记录变更事件支持
- **审批订阅**: 添加飞书 V4 审批订阅接口
- **多应用隔离**: 实现多应用隔离的事件处理机制
- **配置验证**: 添加配置选项验证器并集成到 DI 容器
- **WebSocket 增强**:
  - 添加配置验证器并优化消息处理性能
  - 添加心跳超时事件并优化连接管理
  - 重构 WebSocket 客户端并添加心跳管理和消息队列功能
- **中间件**: 添加请求耗时日志记录并优化签名验证配置

### 🔧 Changed

- **命名空间重构**: 重构命名空间结构并更新发布脚本
  - 重命名事件处理命名空间并迁移事件类型常量
  - 更新 HandlerNamespace 路径
- **事件处理器重构**:
  - 更新事件处理类并迁移 EventData 文件
  - 更新事件结果类的 HeaderType 配置
  - 更新事件处理类并修正文档链接
  - 移除冗余构造函数
- **Webhook 重构**:
  - 重构签名验证逻辑并扩展应用配置选项
  - 重构多应用键上下文传播机制
  - 移除 ISetAppKeyAware 接口及相关实现
- **配置验证重构**: 重构配置验证逻辑并移除数据注解
- **Drive 重构**: 重构云文档订阅相关模型文件路径
- **代码清理**: 清理冗余代码，移除未使用的命名空间引用

### 🐛 Fixed

- 修复并发测试中的变量捕获问题并简化 .gitignore

### 🧪 Tests

- 添加 FeishuWebhook 配置验证的集成测试和单元测试
- 调整重试延迟测试范围以适应 CI 环境

### 📦 Build & Config

- 更新依赖包版本至 1.7.0 并调整项目引用结构
- 更新 Mud.HttpUtils 依赖至 1.6.6 版本
- 删除 appsettings.example.json 配置文件

### 📝 Docs

- 添加类和方法注释以提升代码可读性
- 更新接口文档链接

## [2.0.7] - 2026-04-07

### ✨ Added

- **WebSocket 增强**: 全面增强 WebSocket 连接管理和错误处理
  - 新增消息处理状态跟踪功能
  - 添加重连状态重置功能
  - 实现重连协调器，优化重连逻辑和心跳间隔
  - 增强连接管理，支持更完善的错误处理机制
- **Webhook 增强**: 新增异步验证方法和 IP 白名单支持
  - 添加异步验证方法以提升性能
  - 支持 IP 白名单配置，增强安全性
- **分布式去重服务重构**: 重构分布式去重服务，支持状态机和事件处理生命周期
  - 实现状态机模式，支持 Processing → Completed 状态转换
  - 添加事件处理超时和回滚机制
  - 支持处理状态跟踪和异常恢复
- **指标收集优化**: 使用并发集合优化指标收集性能
  - 采用 `ConcurrentDictionary` 替代传统锁机制
  - 提升高并发场景下的性能表现
- **工具类扩展**: 添加 HttpClient 扩展方法用于 JSON 序列化配置
  - 新增 `ConfigureJsonSerializerOptions` 扩展方法
  - 简化 JSON 序列化配置流程

### 🔧 Changed

- **Token 管理重构**: 将令牌格式化逻辑提取到 TokenUtils 工具类
  - 统一 `FormatBearerToken` 和 `RemoveBearerPrefix` 方法
  - 消除代码重复，提升代码可维护性
- **签名验证优化**: 移除废弃方法并优化签名验证逻辑
  - 清理过时的验证方法
  - 简化验证流程，提升性能
- **WebSocket 重构**: 重构 WebSocket 相关类的命名空间
  - 统一命名空间结构
  - 优化代码组织
- **测试重构**: 重构订阅验证测试以使用加密密钥提供者
  - 更新测试用例以适配新的验证器架构
  - 提升测试覆盖率和代码质量

### 🐛 Fixed

- 修复大量空引用警告，提升代码健壮性
- 修复 `FeishuWebhookService` 异步方法未等待的问题
- 修复重连逻辑中的心跳间隔问题

### 🧪 Tests

- 新增 WebSocket 核心功能测试
  - 添加 Bug 修复验证测试
  - 新增指数退避重连策略测试
  - 添加重连协调器测试
- 新增去重服务测试
  - 添加统一去重中间件测试
  - 新增配置选项测试
  - 添加降级告警服务测试
- 新增指标收集测试
  - 优化并发性能测试用例

### 📦 Build & Config

- **CI/CD 增强**: 增强发布流程并支持多版本 .NET 构建
  - 支持 .NET 6.0、8.0、10.0 多版本并行构建
  - 优化构建和测试配置

## [2.0.6] - 2026-04-05

### ✨ Added

- **多维表格高级权限**: 新增飞书多维表格高级权限管理功能
  - 新增高级权限接口及相关数据模型
  - 添加角色成员管理相关接口及模型
  - 新增批量添加协作者接口及请求模型
  - 添加自定义角色查询、更新和删除接口
- **多维表格仪表盘**: 新增飞书多维表格仪表盘功能
  - 新增仪表盘接口定义
  - 添加仪表盘复制功能及相关数据模型和接口
- **多维表格表单**: 新增表单管理功能
  - 添加表单字段更新相关模型和接口
  - 新增更新表单元数据接口及相关模型
  - 添加表单升级接口及相关数据模型
- **多维表格字段**: 新增字段管理功能
  - 添加创建字段编组功能接口及模型
  - 新增删除字段接口及响应模型
  - 添加更新字段接口
- **多维表格记录**: 新增记录批量操作功能
  - 添加批量新增记录接口及相关数据模型
  - 新增批量获取记录接口及相关模型
  - 添加批量删除记录接口及相关模型
  - 新增删除记录接口及响应模型
- **用户认证**: 添加飞书用户认证中间件及相关服务配置

### 🔧 Changed

- **授权机制重构**: 统一使用 Token 属性声明授权方式
  - 将 Token 属性从 TokenType 枚举改为字符串类型
  - 移除接口中的 Header 属性，统一使用 Token 属性设置授权
- **多维表格重构**: 优化模型和接口结构
  - 重构角色相关请求模型文件结构
  - 重构表单字段相关接口及模型
  - 重构字段操作请求模型并优化目录结构
  - 重构记录操作请求模型并更新接口
- **文档接口**: 将文档块接口移动到主命名空间
- **演示项目**: 将演示项目从 NuGet 包引用改为本地项目引用

### 🐛 Fixed

- 修正 Bitable 注册方法名称
- 更新依赖版本并添加请求体加密方法

### 📚 Documentation

- 更新 NuGet 包下载量徽章样式

### 🧪 Tests

- 添加多维表格接口的单元测试

### 📦 Build & Config

- 更新项目版本至 2.0.6 并调整 token 管理器参数
- 更新演示项目依赖项，添加飞书认证包

## [2.0.5] - 2026-03-27

### ✨ Added

- **电子表格**: 新增飞书电子表格管理模块支持
  - 工作表管理：创建、查询、更新、删除工作表
  - 行列操作：插入、更新、移动、删除行列
  - 单元格功能：合并、查找、样式设置、数据读写
  - 筛选功能：筛选视图创建与管理、筛选条件设置
  - 数据保护：保护范围设置与管理
  - 数据验证：验证规则设置与管理
  - 条件格式：格式规则创建与管理
  - 浮动图片：图片插入、查询、删除
- **多维表格**: 新增飞书多维表格管理模块支持
  - 多维表格元数据获取与更新
- **用户认证**: 新增飞书用户认证模块
  - 用户认证中间件和上下文服务
  - 用户认证测试项目
- **演示项目**: 新增飞书任务管理演示项目

### 🔧 Changed

- **电子表格重构**: 重构飞书电子表格相关模型和接口
  - 重构飞书表格相关模型和接口
  - 调整飞书电子表格接口命名空间并新增租户和用户接口
  - 重构单元格操作相关模型和接口
  - 重命名单元格相关类以更准确描述功能
  - 重构筛选条件相关模型和接口
  - 统一筛选视图响应模型并添加获取接口
  - 重构飞书电子表格数据验证接口
- **认证重构**: 重构用户令牌管理器和依赖项
  - 将 token 格式化方法移至 TokenUtils 工具类
- **Webhook 重构**: 重构验证器基类与工具类
  - 使用环境服务替代直接环境变量访问
- **测试优化**: 使用环境服务模拟替换环境变量设置
- **项目优化**: 移除冗余的 EnsureUserContext 调用并调整项目引用

### �📚 Documentation

- 更新 README 文档说明
- 更新依赖项文档和版本说明
- 更新接口文档注释并添加数据保护相关模型
- 完善 xml 注释

### 📦 Build & Config

- 更新前端依赖版本
- 更新依赖包版本至 10.0.5 并添加认证测试

## [2.0.4] - 2026-03-19

### ✨ Added

- **文档管理**: 新增飞书文档管理模块支持
  - 添加飞书文档接口及数据模型
  - 新增文档块模型及接口支持
  - 添加获取和批量更新文档块接口及模型
  - 新增获取文档块子块分页列表接口
  - 添加批量删除块功能及相关模型
  - 新增内容转换功能支持
- **知识库**: 新增知识库管理功能
  - 添加知识空间和节点管理接口
  - 新增节点标题更新和复制功能
  - 添加节点管理接口和测试
  - 新增移动云文档至知识空间功能
- **云盘管理**: 新增飞书云盘管理 API 服务支持
  - 添加云空间文件夹相关接口及数据模型
  - 新增云空间文件接口及元数据模型
  - 添加文件版本管理功能
  - 新增文件上传功能支持
  - 添加文件导入导出任务相关数据模型和接口
  - 新增创建文件快捷方式相关模型和接口
  - 添加搜索云文档功能及相关数据模型
  - 新增获取云文档点赞者列表接口
- **任务模块**: 添加任务和卡片模块并重构服务注册逻辑
- **认证增强**: 为 FeishuAuthenticationException 添加可恢复标志构造函数
- **枚举配置**: 将枚举和配置类提取到独立文件

### 🔧 Changed

- **项目重构**: 重构请求和响应模型的文件结构
  - 重构文档块模型文件结构，将块模型移动到 Common 目录并删除原 Blocks 目录
  - 重构文件操作接口及模型命名
  - 重构代码以优化文件上传请求模型和 HTTP 客户端
- **测试优化**: 统一测试中数据模型命名空间并优化测试代码
- **数据库优化**: 为收藏节点添加索引优化查询性能

### 🐛 Fixed

- **消息发送**: 修复发送 Multipart/form-data 请求时 request.Content 的值问题
- **Webhook**: 修复飞书回调配置通不过的问题，在 FeishuEventDecryptor 中指定 EventType

### 📦 Build & Config

- **依赖更新**: 更新多个项目的依赖版本

### 🧪 Tests

- **新增测试**: 添加飞书 Webhook 模块的单元测试

## [2.0.3] - 2026-02-26

### ✨ Added

- **考勤管理**: 新增完整的考勤管理功能
  - 休假发放记录相关数据模型和接口
  - 打卡流水记录相关模型和接口
  - 考勤归档报表相关接口及数据模型
  - 补卡相关数据模型和接口
  - 用户人脸照片上传接口
  - 用户设置查询接口
  - 考勤审批相关数据模型及接口
  - 审批状态更新接口及数据模型
  - 审批结果写入功能及数据模型
  - 考勤统计相关数据模型和接口
  - 查询统计数据相关模型和接口
  - 查询统计表头接口及请求模型
  - 用户人脸识别信息管理相关接口和模型
  - 考勤组相关接口及数据模型
- **审批功能**: 重构审批实例预览功能
- **卡片功能**: 更新卡片元素请求字段名
- **任务功能**: 飞书任务自定义字段更新接口返回类型

### 🔧 Changed

- **项目结构**: 重构项目结构，调整命名空间和文件位置
- **接口规范**: 统一接口方法命名规范
- **考勤接口**: 重构考勤组接口继承结构，重命名考勤用户管理接口并添加下载功能
- **令牌管理**: 重构令牌管理模块并更新依赖版本
- **国际化**: 重构国际化资源模型并更新相关引用
- **测试结构**: 重构测试文件目录结构，将相关测试文件移动到对应功能模块目录下
- **Demo 项目**: 更新 demo 项目以使用包引用而非项目引用
- **响应模型**: 重构响应模型

### 🐛 Fixed

- **用户组成员**: 修正类名拼写错误并更新相关引用
- **测试用例**: 修复测试用例并完善测试数据
- **活动订阅**: 修正活动订阅接口返回类型并完善测试用例

### 📚 Documentation

- **代码注释**: 为测试类和缓存方法添加 XML 注释
- **README**: 更新 README 文档结构和内容

### 📦 Build & Config

- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.3 版本
- **构建配置**: 忽略测试项目的构建输出目录

### 🧪 Tests

- **完善测试用例**: 为多个模块添加和完善测试用例
  - 聊天群组和标签页测试
  - 飞书任务单元测试
  - 飞书用户和用户组接口测试
  - 飞书职位和职级接口测试
  - 员工类型相关接口测试
  - 飞书部门接口测试
  - 任务板块相关测试
  - 任务列表接口测试
  - 飞书任务附件和评论测试
  - 群公告相关接口测试
  - 卡片接口测试
  - 飞书审批任务相关测试
  - 审批查询接口测试
  - 审批评论相关测试
  - 飞书审批相关接口测试
  - 审批消息接口测试

## [2.0.2] - 2026-01-30

### ✨ Added

- **性能指标监控**: 添加完整的性能指标收集功能
  - 新增 MeterExtensions、FeishuMetrics 和 FeishuMetricsHelper 类
  - 在 TokenManager、HttpClientUtils 等关键组件中添加指标记录
- **WebSocket 和 Webhook 指标**: 为 WebSocket 和 Webhook 添加专项指标监控
  - WebSocket 连接数统计和认证/事件处理指标
  - Webhook 签名验证、事件解密和处理指标
  - 事件去重命中指标
  - 支持 WebSocketConnectionCountProvider 获取实时连接数
- **调试日志增强**: 增加响应内容调试日志和错误处理
  - 添加调试日志记录原始响应内容
  - 在 JSON 反序列化失败时提供更详细的错误信息
- **测试控制器**: 新增日志测试控制器和网络测试控制器

### 🔧 Changed

- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.2 版本
- **HTTP 客户端优化**:
  - 增强异常处理和日志记录
  - 改进安全配置和连接池设置
- **认证优化**: 优化令牌获取逻辑并移除冗余指标记录
  - 改为直接从缓存获取令牌时记录指标
- **配置简化**: 移除断路器功能及相关代码
  - 移除断路器配置项和文档说明
  - 简化代码结构和 Webhook 服务
- **时间戳验证**: 优化时间戳验证逻辑，优先使用应用特定配置
- **Demo 项目**: 调整 Demo 项目的配置和依赖

### 🐛 Fixed

- **JobLevel 接口**: 将 JobLevel 接口的 name 参数改为可空类型
- **JobFamilies 接口**: 允许 GetJobFamilesListAsync 的 name 参数为 null

### 📚 Documentation

- 更新 README 文档说明指标使用方式
- 添加日志测试相关文档

### 📦 Build & Config

- 更新项目版本至 2.0.2

## [2.0.1] - 2026-01-28

### 🚨 BREAKING CHANGE

- **移除 FeishuOptions 类** - 完全移除旧的配置类，所有场景统一使用 `FeishuAppConfig`
- **多应用架构支持** - API 签名和配置方式发生变化，需要迁移
- **配置系统重构** - 重试机制和 Token 管理配置方式改变

### ✨ Added

- **多应用支持**: 支持配置和管理多个飞书应用
- **应用上下文切换**: 新增 `IFeishuAppContextSwitcher` 接口支持运行时切换应用
- **自动推断默认应用**: 三种自动推断规则简化配置
- **应用级 Token 缓存隔离**: 每个应用拥有独立的 Token 缓存
- **新配置参数**: 新增 `RetryDelayMs` 和 `TokenRefreshThreshold` 配置
- **文档**: 新增配置迁移指南和多应用配置文档

### 🔧 Changed

- **重构 HttpClient 配置**: Polly 重试策略使用配置参数
- **重构 Token 重试逻辑**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **WebSocket 重试**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **依赖更新**: 替换代码生成器为 `Mud.HttpUtils`

### 🐛 Fixed

- **修复 `RetryCount` 配置不一致问题**: 统一应用到 HttpClient 和 TokenManager
- **WebSocket 重试硬编码问题**: 使用配置参数替代硬编码值

### 📚 Documentation

- 新增配置迁移指南
- 更新示例配置文件
- 新增多应用配置文档

---

## [1.2.2] - 2026-01-19

### ✨ Added

- **考勤管理 API**: 新增完整的考勤班次管理能力
- **审批功能**: 新增审批消息 API 和审批任务查询接口
- **演示项目**: 新增飞书 OAuth 登录演示项目

### 🐛 Fixed

- **解密失败处理**: 修复解密失败时空引用问题
- **令牌管理**: 修复用户令牌管理及状态清理问题
- **项目文件**: 修复 PackageTags 标签重复闭合问题
- **Webhook 中间件**: 修复验证请求属性和缩进问题

### 🔧 Changed

- **模型重构**: 班次模型提取公共基类
- **认证服务**: 重构认证服务和接口命名
- **项目结构**: 移动项目文件到 Sources 文件夹
- **工具类**: 重组异常和 HTTP 客户端工具
- **Redis 性能**: 使用 SCAN 替代 KEYS 命令
- **缓存管理**: 优化令牌缓存和格式化逻辑
- **代码清理**: 移除未使用变量和多余引用

### 📚 Documentation

- API 文档更新和完善
- 项目文档链接和版本号更新
- 演示项目文档优化和重组

### 📦 Build & Config

- 版本更新至 1.2.2
- 依赖管理优化
- Git 配置安全加固

---

## [1.2.1] - 2026-01-16

### ✨ Added

- **配置验证**: 新增 AppId、AppSecret、EncryptKey 格式和长度验证
- **敏感信息保护**: 配置类添加敏感信息掩码功能
- **示例配置**: 新增完整的 appsettings.example.json 文件

### 🔒 Security

- **数据注解验证**: 添加 Data Annotations 属性验证必填字段
- **Redis 配置验证**: 新增 ServerAddress 格式和连接参数验证

### 🧪 Tests

- **配置单元测试**: 全面覆盖 AppId/AppSecret 验证、敏感信息掩码等

### 📚 Documentation

- **XML 文档**: 完善配置类参数说明和示例值

### ⚠️ BREAKING CHANGE

- 修改了 `FeishuAppConfig.Validate()` 方法验证规则
- 添加了 `FeishuWebhookOptions.EncryptKey` 长度验证（32 字符）
- 添加了 `RedisOptions.Validate()` 方法验证连接参数

---

## [1.1.2] - 2026-01-10

### ✨ Added

- **Webhook/WebSocket**: 异步验证、重试功能和请求体签名验证
- **审批功能**: 第三方审批实例验证、同步和状态分页接口
- **配置验证**: WebSocket 和 Feishu 配置选项验证

### 🐛 Fixed

- **WebSocket 配置**: 修复配置相关问题

### 🔧 Changed

- **事件处理器**: 调整为异步处理器设计
- **Redis 服务**: 简化服务注册方法
- **服务注册**: 统一方法命名规范

### 📚 Documentation

- **英文文档**: 重构文档结构和内容组织

### 📦 Dependencies

- 更新项目依赖包至 1.1.2 版本

---

## [1.1.1] - 2026-01-06

### ✨ Added

- **审批功能**: 新增多种审批相关接口和常量
- **任务管理**: 自定义字段管理和选项管理功能
- **API 响应**: 统一为 FeishuApiResult 格式

### 🔧 Changed

- **接口设计**: 统一消息发送接口
- **服务注册**: 统一服务注册 API

### 📚 Documentation

- **README**: 优化项目描述和功能说明
- **架构文档**: 移除过时的架构设计文档

---

## [1.1.0] - 2025-11-12

### ✨ Added

- **用户管理**: 完整的用户 CRUD 操作接口
- **用户组管理**: 用户组创建、更新、删除接口
- **部门管理**: 完整的部门管理功能
- **员工类型管理**: 员工类型相关接口

### 🔧 Changed

- **API 结果模型**: 重构为 FeishuApiResult 命名
- **服务注册**: 优化服务注册代码结构

### 📚 Documentation

- **README**: 更新内容和结构
- **功能说明**: 新增项目功能说明和使用示例

---

## [1.0.9] - 2025-11-14

### ✨ Added

- **跨平台支持**: 支持.NET Standard 2.0
- **HTTP 客户端**: 新增飞书 HTTP 客户端扩展方法

### 🐛 Fixed

- **HttpClient 配置**: 修复配置和 API 端点 URL 格式问题

### 🔧 Changed

- **消息和事件 API**: 重构实现以提高可维护性
- **文件下载**: 优化 HTTP 请求方法提高性能

---

## [1.0.7] - 2025-11-12

### ✨ Added

- **任务管理**: 任务评论、附件、活动订阅和成员管理接口
- **JsTicket API**: 新增 JsTicket API 接口支持前端开发

### 🔧 Changed

- **代码生成器**: 升级 Mud.ServiceCodeGenerator 版本
- **依赖管理**: 优化项目依赖配置

### 📚 Documentation

- **任务文档**: 更新任务成员信息注释

---

## [1.0.3-dev] - 2025-11-12

### ✨ Added

- **基础框架**: 飞书 API 基础框架搭建
- **认证服务**: 认证服务和令牌管理
- **通讯录**: 企业通讯录相关接口
- **消息功能**: 消息发送和接收功能
- **事件支持**: Webhook 和 WebSocket 支持
- **演示项目**: Webhook 和 WebSocket 演示项目

### 📚 Documentation

- **项目文档**: 初始 README 和项目结构说明
