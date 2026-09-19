# 配置面变更日志（CHANGELOG-Config）

## R2/R3（本批）

### 新增

| 能力 | 包 | 说明 |
| ---- | -- | ---- |
| `FeishuDeduplicationOptions` + `FeishuDeduplication` 节 | Abstractions | 统一去重真相源入口；节存在时对旧键字段级优先 |
| `AddFeishuDeduplicationOptions(configuration / DeduplicationProfile)` | Abstractions | 绑定 + Profile DI API（B4） |
| `AddFeishuConfigurationConsistencyChecks` | Abstractions | 组合层 WHF-03（NonceTtl ≥ TimestampTolerance），宿主注入容差 |
| `CircuitBreakerOptions` / `HttpRetryOptions` + `FeishuAppConfig` 嵌套 | Abstractions | C2/R3；`TimeoutSeconds` 与 `TimeOut` 同存储 |
| `WebSocketCertificateOptions` / `WebSocketReconnectOptions` / `CertificateValidationMode` | WebSocket | C3/R3；`ValidateCertificateOptions` 强制 Strict 安全组合 |
| Webhook/WS/Redis 工厂接线统一节 | 三工程 | 双读：新节优先，旧键仍可绑 |

### Obsolete（仍绑定，语义文档化）

| 属性 | 替代 |
| ---- | ---- |
| `DeduplicationOptions.AllowProcessingOnFallback` / 去重 `MaxRetryCount` / `InitialRetryDelay` / `MaxRetryDelay` | 主路径不消费；自定义分布式实现可用 |
| `FeishuWebhookOptions.EnableBackgroundProcessing` | `EnableTokenBackgroundRefresh` |
| `FeishuAppConfig.TimeOut` / `RetryCount` / `RetryDelayMs` / `CircuitBreaker*` 扁平 | `TimeoutSeconds` / `HttpRetry.*` / `CircuitBreaker.*` |
| `FeishuWebSocketOptions` 重连/证书扁平属性 | `Reconnect.*` / `Certificate.*` |

### 行为（延续 R1）

- 应用级 `EventHandlingTimeoutMs` 生效；`LegacyGlobalTimeoutOnly` 可回退。
- Webhook 内存去重默认对齐 Consts（48h/10min）。
- Redis 无效 Dedup 键 Warn。

### 文档

- 迁移指南：`documents/Configuration/ConfigMigration-R2.md`
- 最小配置：`documents/Configuration/appsettings.minimal.json`
- 真相源：`documents/Configuration/DeduplicationTruthSource.md`

### 门禁

```powershell
# 本机 agent 环境若 ProgramFiles 损坏，先修复环境变量再跑
$env:ProgramFiles = "C:\Program Files"
${env:ProgramFiles(x86)} = "C:\Program Files (x86)"
$env:MSBUILDDISABLENODEREUSE = "1"
powershell -File ./scripts/verify-build.ps1
```

## R1

### 行为变更

#### B1：应用级 `EventHandlingTimeoutMs` 开始真实生效

- **包**：`Mud.Feishu.Webhook`
- **兼容**：`null` / `-1` / `0` 继承全局。
- **升级动作**：检查应用级超时；需旧语义时 `FeishuWebhook:LegacyGlobalTimeoutOnly=true`。

#### B2：Webhook 内存去重默认 TTL/处理超时对齐 Consts

- 无参 DI 从 MemoryDeduplicator 24h/5min 对齐为 Consts 48h/10min。

### 诊断增强

- Redis 路径无效 Dedup 键 Warn（不删键、不反转 RedisOptions 优先级）。

## R4（规划，major）

- 删除已过期 Obsolete 属性与日志私有开关。
- 双读窗口结束后仅保留新键。

