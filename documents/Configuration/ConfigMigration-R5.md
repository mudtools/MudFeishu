# 配置面迁移对照表（R5）

> 配套文档：`.docs/配置面可用性修复与收敛方案-R5.md`（方案 + 独立复核结论）、`CHANGELOG-Config.md`（变更日志）。
> 适用版本：R5.0 / R5.1 / R5.2 / R5.3。**每个键都标注了「升级后行为是否改变」**，请重点阅读标记为「行为变更」的行。

## 1. R5.0 — 直接删除（无行为影响）

| 已删除 | 替代 | 若你仍在配置它 |
| ------ | ---- | ---- |
| `FeishuWebhook:EnableRequestLogging` | `Logging:LogLevel:Mud.Feishu.Webhook` | 启动时输出一次 `Warning`，提示该键已移除且从未生效；**不会报错** |
| `FeishuWebhook:Apps:{key}:Description` | 无 | 静默忽略（仅曾用于 `ToString()`） |
| `Consts.DefaultDeduplicationRetryCount` 等 3 个常量 | 无 | 属 `internal`，宿主无法引用，无影响 |

## 2. R5.1 — 行为变更（**升级前请逐项核对**）

| 键 | 变更前 | 变更后 | 你需要做什么 |
| -- | ------ | ------ | ------------ |
| `FeishuAppOptions:*` | **完全无效**（从未绑定配置节） | 正常生效 | 若你此前「以为配了」但实际没生效，升级后它会真的生效——请核对 `EnableTokenEncryption` / `WarmUpAllAppsOnStartup` / `ContextRetireDelaySeconds` 等值是否符合预期 |
| `FeishuWebhook:Retry:MaxRetryCount` | 恒为 3 | 生效 | 核对你的期望重试次数 |
| `FeishuWebhook:Retry:InitialRetryDelaySeconds` | 恒为 10 | 生效 | 同上 |
| `FeishuWebhook:Retry:RetryDelayMultiplier` | 恒为 2.0 | 生效 | 指数退避曲线会变 |
| `FeishuWebhook:Retry:MaxRetryDelaySeconds` | 恒为 300 | 生效 | 退避上限会变 |
| `FeishuWebhook:Retry:RetryPollIntervalSeconds` | 恒为 30 | 生效 | 轮询频率会变 |
| `FeishuWebhook:Retry:MaxRetryPerPoll` | 恒为 10 | 生效 | 单轮处理量会变 |
| `FeishuWebhook:Retry:EnableRetry` | 生效（唯一生效项） | 生效（不变） | — |
| `FeishuWebSocket:Certificate:Mode=Dev` | **无运行时效果** | 生效：放行「自签名根」与「名称不匹配」 | **安全面放宽**——确认仅用于开发/测试环境；生产请保持 `Strict`。**R5.2.7 生产加固**：生产环境（`IHostEnvironment` 或 `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT`，缺省按 Production）下该旁路输出 `LogError`（不阻断启动，major 再评估是否 Validate 失败） |
| `FeishuWebSocket:Certificate:ValidateServerCertificate=false` | 完全关闭校验 | 完全关闭校验（**不变**，且优先于 `Mode=Dev`） | — 。**R5.2.7 生产加固**：生产环境输出 `LogError` |
| `FeishuWebSocket:Certificate:CustomCallback`（代码配置，`Mode` 未设为 `Custom`） | 使用该回调 | 使用该回调 + `Warning` | 建议显式设置 `Mode=Custom` 消除歧义 |
| `FeishuApps:{n}:TimeOut` 等旧扁平键 | 启动时回填 | 启动**与热更**均回填 | 无需处理；此前首次热更会回退默认值 |
| `FeishuDeduplicationOptions.IsConfiguredFromConfiguration`（代码赋值） | 可写 | **不可写**（`internal set`） | 删除该赋值——它本就不该由宿主控制，且可伪造「新节有效」 |

> **`FeishuWebSocket:Certificate` 优先级链**（R5.1 起唯一权威表述）：
> `CustomCallback`（`Mode=Custom`，或 `Mode≠Custom` 但回调非 null 的兼容分支）
> \> `ValidateServerCertificate=false`（完全关闭校验）
> \> `Mode=Dev`（仍校验，仅放宽自签名根与名称不匹配）
> \> `Mode=Strict`（默认）

## 3. R5.1 — 已标 `[Obsolete]`（仍可正常使用，下个 major 删除）

| 键 | 状态 | 替代 |
| -- | ---- | ---- |
| `FeishuWebhook:AutoRegisterEndpoint` | `[Obsolete]`，**无运行时效果**（路由由 `app.UseFeishuWebhook()` 决定） | 不需要 Webhook 处理时，请不要调用该中间件。配置 `false` 会在启动期输出一次 Warning |
| `FeishuWebhook:LegacyGlobalTimeoutOnly` | `[Obsolete]`，B1 的过渡闸（仍生效） | 核对应用级 `EventHandlingTimeoutMs` 后移除该键 |

> `FeishuWebhookServiceBuilder.EnableAutoEndpoint()` / `DisableAutoEndpoint()` 同步 `[Obsolete]`；
> 它们**并非 no-op**——会写入 `AutoRegisterEndpoint`（该写入本身无运行时效果）。

## 4. R5.2 — 计划中（本表随实现更新）

| 键 | 计划处置 | 替代 |
| -- | -------- | ---- |
| `FeishuWebhook:EnablePerformanceMonitoring`（全局 + 应用级） | 删除（先把耗时日志无条件降为 `Debug`，删开关不丢可诊断性） | `Logging:LogLevel:Mud.Feishu.Webhook=Debug` |
| `FeishuWebhook:Apps:{key}:AppKey` | `[Obsolete]`（由 `Apps` 字典键自动回填） | 直接使用 `Apps` 字典键 |
| `FeishuRedis:Event*` / `Nonce*` / `SeqId*` | `[Obsolete]`（仍作回落基座） | `FeishuDeduplication:Event/Nonce/SeqId` |
| `FeishuRedis:Deduplication:*`、`FeishuWebSocket:EventDeduplication:*` | `[Obsolete]`（仍作回落基座） | `FeishuDeduplication` |
| `FeishuWebSocket:Certificate:AllowSelfSignedCertificates` / `AllowCertificateNameMismatch` | `[Obsolete]` | `Certificate:Mode=Dev` |
| `FeishuDeduplication:Mode=Dev` 在生产环境 | 加固：启动期 `LogError` | — |

## 5. **明确不改**（复核结论，勿按早期草案执行）

| 项 | 早期草案 | 最终裁决 | 理由 |
| -- | -------- | -------- | ---- |
| `RateLimitOptions.TooManyRequestsStatusCode` / `TooManyRequestsMessage` | 改为 `internal const` + 固定文案 | **保留公开**，仅补文档 | 二者在 `FeishuRateLimitMiddleware` 中**真实生效**（拼消息 / 写状态码），是可用能力；改 `const` 属功能删减，破坏为正、收益为零 |
| `FeishuWebSocketOptions.EnableReconnectMetrics` | 并入「诊断开关清理」 | **保持现状** | 它不是日志开关：`false` 会把 `ReconnectState.TotalReconnectCount` 恒置 0，属公共契约可见值 |
| `FeishuAppConfig.TimeoutSeconds` → 增补 `TimeoutMs` 别名 | 统一为毫秒后缀 | **不改** | 会让同一 `FeishuApps` 元素内并存「秒语义」与「毫秒语义」两个键，人工换算事故高发；单位变更应作为独立 major 立项 |
| `DeduplicationOptions` / `EventDeduplicationOptions` | R5 删除 | **已完成 `[Obsolete]` 标注（R5.2.6）**，类与双读回落链保留至下个 major | 它们是双读期回落基座，`FeishuEventDeduplicator` 的构造重载与 Redis 回落链仍依赖；标注 `[Obsolete]` 后编译器自动在消费方产生 `CS0618` 提示 |
| `FeishuAppOptions` 注册 `IOptionsChangeTokenSource` | 「与 `TokenRecoveryOptions` 同形」注册 | **不注册** | 全仓库无 `IOptionsMonitor<FeishuAppOptions>` 消费方（消费方一律用 `IOptions<>`，其 `OptionsManager` 自带私有缓存、不观察变更令牌）；注册只会把「启动快照」伪装成「可热更」 |

## 6. 升级自检清单

```powershell
# 1) 静态审计：源码中不得出现已删除的配置键/属性名
powershell -File ./scripts/audit-config-keys.ps1 -Strict

# 2) 全门禁：构建 + 诊断白名单 + AOT 严格冒烟 + 逐工程/逐 TFM 测试 + 格式
powershell -File ./scripts/verify-build.ps1
```

应用侧自检：

1. `grep` 你的 `appsettings*.json`，确认没有 `EnableRequestLogging`；
2. 若配置了 `FeishuWebhook:Retry:*`，逐一核对第 2 节表格（这些值**升级后才会生效**）；
3. 若配置了 `FeishuWebSocket:Certificate:Mode=Dev`，确认该环境非生产（升级后它会真的放宽证书校验）；
4. 观察启动日志中的 `Warning`（`EnableRequestLogging` 迁移提示、`AutoRegisterEndpoint=false` 无效果提示、`CustomCallback` 与 `Mode` 不一致提示）。
