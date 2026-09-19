# 配置面变更日志（CHANGELOG-Config）

## R5（本批）

> 方案与复核结论：`.docs/配置面可用性修复与收敛方案-R5.md`
> 逐键迁移对照：`documents/Configuration/ConfigMigration-R5.md`

### R5.0 — 死配置止血（**breaking，但无行为影响**）

| 变更 | 替代 | 影响 |
| ---- | ---- | ---- |
| 删除 `FeishuWebhookOptions.EnableRequestLogging` | `Logging:LogLevel:Mud.Feishu.Webhook` | 该开关**从未被运行时读取**；配置了它的部署启动时会收到一次 `Warning` 迁移提示（不报错） |
| 删除 `FeishuAppWebhookOptions.Description` | 无 | 仅用于 `ToString()`，无行为影响 |
| 删除 `Consts.DefaultDeduplicationRetryCount` / `DefaultDeduplicationInitialRetryDelayMs` / `DefaultDeduplicationMaxRetryDelayMs` | 无 | `internal` 常量，且零引用 |

### R5.0 — 治理护栏

1. `scripts/audit-config-keys.ps1` 重构：分「精确死键」与「R4 宽口径启发式」两档，`-Strict` 只以精确档为判据；
   排除 `Demos\`；`allowlist` 由**整文件豁免**改为行内 `// audit-allow: <reason>`（匹配行或其上一行）。
   旧实现对 `FeishuWebhookOptions.cs` 的整文件豁免正是 `EnableRequestLogging` 存活至今的原因。
2. `-Strict` 接入 CI（`.github/workflows/dotnet-publish.yml` 独立 step）。
3. 新增契约守卫 `ConfigSurfaceContractGuards`（Abstractions / Webhook 各一份）：
   已删除属性不得复活、已删除常量不得复活、`SectionName` 必须被 `GetSection` 使用、Obsolete 成员必须仍可绑定、
   SDK 内部状态位不得对外可写、计划 Obsolete 的成员必须保持 Obsolete 标记。
4. `AGENTS.md` 新增「配置面治理（R5）」6 条条款，并修正其此前**与代码不符**的声明
   （原 `:331` 声称三个日志开关「已移除」，而 `EnableRequestLogging` 实际仍在）。

### R5.1 — 绑定与生效修复（**含行为变更**）

| 变更 | 类型 | 说明 |
| ---- | ---- | ---- |
| `FeishuAppOptions` 接入配置节绑定 | 新增能力 | 此前仅 `AddOptions<T>()`、**从未绑定任何节**，`SectionName` 为死常量；6 个行为开关现可经 `FeishuAppOptions` 节下发 |
| 新增 `FeishuAppOptionsValidator`（`ContextRetireDelaySeconds` 1–3600 秒，net6+ `ValidateOnStart`） | 新增 | 该约束此前只在运行期由 `FeishuAppContextRetirement` 抛出 |
| **`FeishuWebhook:Retry:*` 首次真正生效** | **行为变更** | 此前 6 个字段（`MaxRetryCount` / `InitialRetryDelaySeconds` / `RetryDelayMultiplier` / `MaxRetryDelaySeconds` / `RetryPollIntervalSeconds` / `MaxRetryPerPoll`）恒为默认值，仅 `EnableRetry` 偶然生效。**升级前请核对现有配置** |
| **`FeishuWebSocket:Certificate:Mode` 驱动运行时** | **行为变更（安全面放宽）** | `Mode=Dev` 此前无任何运行时效果，同时 `Validate` 的报错文案却引导用户改 `Mode=Dev` → 无效循环。现将优先级链固定为 `CustomCallback > ValidateServerCertificate=false > Mode=Dev > Mode=Strict`。**`Mode=Strict` / `Mode=Custom` / `ValidateServerCertificate=false` 三条路径行为不变**；「`Mode≠Custom` 且提供了 `CustomCallback`」保持既有「回调优先」语义并输出 Warning |
| 热更链补齐旧扁平键回填 | 修复 | 此前 `ApplyLegacyFlatKeys` 只在启动链执行，首次热更会以默认值重建上下文（如 `TimeOut=60` 回退为 30s） |
| `FeishuDeduplicationOptions.IsConfiguredFromConfiguration` 改 `internal set` | **源码级 breaking（仅外部程序集）** | 外部代码不能再写该 SDK 内部状态位（此前可在无配置节时伪造「新节有效」）。本仓库测试因 `InternalsVisibleTo` 不受影响 |

### R5.1 — 配置面审计判据说明

`FeishuWebhookOptions.AutoRegisterEndpoint` 与 `FeishuWebhookOptions.LegacyGlobalTimeoutOnly` 已标 `[Obsolete]`
（前者无运行时效果且属 X4，后者是 B1 的过渡闸且属 X9）。二者**仍可正常绑定**——Obsolete 只是下线预告，
不得顺带切断 appsettings 兼容；`AutoRegisterEndpoint=false` 时启动期会输出一次 Warning 说明其无效果。

## R4（**breaking / major**）

### 删除的公共 API（代码与 appsettings 均需迁移）

| 删除 | 替代 |
| ---- | ---- |
| `FeishuAppConfig.TimeOut` / `RetryCount` / `RetryDelayMs` | `TimeoutSeconds` / `HttpRetry.MaxAttempts` / `HttpRetry.DelayMs` |
| `FeishuAppConfig.CircuitBreaker*` 扁平 | `CircuitBreaker.*` |
| `FeishuAppConfig.EnableLogging` | `Logging:LogLevel:Mud.Feishu.Abstractions.TokenManager` |
| `DeduplicationOptions.AllowProcessingOnFallback` / 去重 `MaxRetryCount` / `InitialRetryDelay` / `MaxRetryDelay` | 主路径不消费；事件重试用 `FailedEventRetryOptions` |
| `DeduplicationOptions.EnableVerboseLogging` | `Logging:LogLevel` |
| `FeishuWebhookOptions.EnableBackgroundProcessing` | `EnableTokenBackgroundRefresh`（**null=不干预基座**，有应用时基座默认开启刷新） |
| `FeishuWebSocketOptions` 重连/证书扁平属性 + `EnableLogging` | `Reconnect.*` / `Certificate.*`；日志用 `Logging:LogLevel:Mud.Feishu.WebSocket` |
| `RedisOptions` 连接扁平属性 | `Connection.*` / `Advanced.*` |

### 行为变更

1. **Webhook 宿主令牌刷新默认**：`EnableTokenBackgroundRefresh=null` 时 **不再**把基座 `TokenRefreshBackgroundOptions.Enabled` 压成 false；有已注册应用时默认开启令牌后台刷新（与基座/D12 一致）。需关闭请显式 `"EnableTokenBackgroundRefresh": false`。
2. **配置 JSON 兼容**：Redis/WebSocket/FeishuApps 旧扁平键在 **配置绑定层** 回填嵌套属性（`ApplyLegacyFlatConnectionKeys` / `ApplyLegacyFlatKeys` / 应用级映射）；**代码 API 必须使用嵌套属性**。
3. 去重 HighReliability/HighAvailability 预设仅设置已消费字段（TTL/ProcessingTimeout）。

### 配置审计

```powershell
powershell -File ./scripts/audit-config-keys.ps1        # 告警
powershell -File ./scripts/audit-config-keys.ps1 -Strict # CI 阻断
```

### 治理文档

`AGENTS.md` 新增「配置面约定（R4）」：嵌套形状、日志 LogLevel、多租户三前缀、跨校验、安全默认。

## R2/R3

### 新增

| 能力 | 包 | 说明 |
| ---- | -- | ---- |
| `FeishuDeduplicationOptions` + `FeishuDeduplication` 节 | Abstractions | 统一去重真相源入口 |
| `AddFeishuDeduplicationOptions` / Profile API | Abstractions | B4 |
| `AddFeishuConfigurationConsistencyChecks` | Abstractions | WHF-03 组合层 |
| `CircuitBreakerOptions` / `HttpRetryOptions` | Abstractions | C2 |
| `WebSocketCertificateOptions` / `WebSocketReconnectOptions` / `CertificateValidationMode` | WebSocket | C3 |
| `RedisConnectionOptions` / `RedisAdvancedOptions` | Redis | C7 |

## R1

### 行为变更

- 应用级 `EventHandlingTimeoutMs` 生效；`FeishuWebhook:LegacyGlobalTimeoutOnly=true` 可回退全局-only。
- Webhook 内存去重默认对齐 Consts（48h/10min）。
- Redis 无效 Dedup 键 Warn（旧字段已删除后仅保留真相源文档说明）。


