# 配置面变更日志（CHANGELOG-Config）

## R4（本批，**breaking / major**）

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


