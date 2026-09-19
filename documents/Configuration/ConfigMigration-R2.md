# 配置迁移指南（R2/R3）

对应实施方案：`.docs/配置面Bug修复与功能完善方案.md`（验证回写 §0A + 实施任务 §12）。

## R2：统一事件去重节 `FeishuDeduplication`

### 推荐新配置

```jsonc
{
  "FeishuDeduplication": {
    "Mode": "Distributed",                 // None | InMemory | Distributed
    "Profile": "Default",                  // Default | HighReliability | HighAvailability
    "Event": {
      "Ttl": "2.00:00:00",
      "ProcessingTimeout": "00:10:00",
      "CleanupInterval": "00:05:00",
      "KeyPrefix": "t-a:feishu:event:",
      "MaxCacheSize": 100000
    },
    "Nonce": { "Ttl": "00:05:00", "KeyPrefix": "t-a:feishu:nonce:" },
    "SeqId":  { "Ttl": "2.00:00:00", "KeyPrefix": "t-a:feishu:seqid:" }
  },
  "FeishuRedis": {
    "Connection": { /* R3 可选嵌套；旧扁平 ServerAddress 等仍可绑 */ "ServerAddress": "redis:6379" }
  }
}
```

### 旧键 → 新键映射

| 旧键 | 新键 | 双读期行为 |
| ---- | ---- | ---------- |
| `FeishuRedis:EventCacheExpiration` | `FeishuDeduplication:Event:Ttl` | 新节存在时新节字段优先；否则旧键 |
| `FeishuRedis:EventKeyPrefix` | `FeishuDeduplication:Event:KeyPrefix` | 同上 |
| `FeishuRedis:NonceTtl` | `FeishuDeduplication:Nonce:Ttl` | 同上 |
| `FeishuRedis:NonceKeyPrefix` | `FeishuDeduplication:Nonce:KeyPrefix` | 同上 |
| `FeishuRedis:SeqIdCacheExpiration` | `FeishuDeduplication:SeqId:Ttl` | 同上 |
| `FeishuRedis:SeqIdKeyPrefix` | `FeishuDeduplication:SeqId:KeyPrefix` | 同上 |
| `FeishuRedis:SeqIdScopeKey` | `FeishuDeduplication:SeqId:ScopeKey` | 同上 |
| `FeishuWebSocket:EventDeduplication:Mode` | `FeishuDeduplication:Mode` | 同上 |
| `FeishuWebSocket:EventDeduplication:CacheExpiration` | `FeishuDeduplication:Event:Ttl` | 同上 |
| `FeishuRedis:Deduplication:CacheExpiration/KeyPrefix` | 新节或 RedisOptions | Redis 路径仍被 RedisOptions 覆盖并 Warn |
| `FeishuRedis:Deduplication:AllowProcessingOnFallback` 等 | **不迁移** | Obsolete，主路径不消费 |

### 代码 Profile API

```csharp
services.AddFeishuDeduplicationOptions(configuration); // 绑定 FeishuDeduplication 节
// 或
services.AddFeishuDeduplicationOptions(DeduplicationProfile.HighReliability, o =>
{
    o.Event.Ttl = TimeSpan.FromHours(12); // 字段覆盖 Profile
});
```

> Redis 路径上 Profile 的 `AllowProcessingOnFallback` **不会生效**（B3）；Profile 仅影响已消费的 TTL/ProcessingTimeout 等。

### 跨包一致性（WHF-03）

```csharp
// 宿主同时引用 Webhook + Redis 时
services.AddFeishuConfigurationConsistencyChecks(o =>
{
    o.TimestampToleranceSeconds = webhookSection.GetValue<int>("TimestampToleranceSeconds");
    o.NonceTtlSeconds = redisSection.GetValue<int>("NonceTtlSeconds");
    o.FailOnValidationError = builder.Environment.IsProduction();
});
```

未注入 `TimestampToleranceSeconds` 时自动跳过（避免只装 Redis 误报）。

## R3：嵌套与 Advanced 面

### FeishuApps HTTP/熔断

```jsonc
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "...",
      "TimeoutSeconds": 30,
      "HttpRetry": { "MaxAttempts": 3, "DelayMs": 1000 },
      "CircuitBreaker": {
        "Enabled": true,
        "FailureThreshold": 20,
        "SamplingDurationSeconds": 60,
        "BreakDurationSeconds": 60,
        "MinimumThroughput": 10
      }
    }
  ]
}
```

旧扁平键（`TimeOut` / `RetryCount` / `CircuitBreakerEnabled` 等）**仍可绑定**（Obsolete 垫片，同存储）。

### WebSocket 证书/重连

```jsonc
{
  "FeishuWebSocket": {
    "AppKey": "default",
    "Certificate": {
      "Mode": "Strict",                 // Strict | Dev | Custom
      "AllowInsecureWebSocket": false,
      "ValidateServerCertificate": true,
      "AllowSelfSignedCertificates": false,
      "AllowCertificateNameMismatch": false
    },
    "Reconnect": {
      "Auto": true,
      "MaxAttempts": 5,
      "BaseDelayMs": 5000,
      "MaxDelayMs": 30000,
      "TotalBudget": "00:30:00",
      "Cooldown": "00:00:05",
      "MaxAuthRetryAttempts": 5
    }
  }
}
```

安全规则：
- `Mode=Strict` + 自签名/名称不匹配 → Validate **失败**
- `Mode=Custom` 无回调 → Validate **失败**
- 默认 Strict + wss + 校验证书，**不得回退**

## R4：Obsolete 属性已删除（代码 Breaking）

JSON 配置旧扁平键仍可通过绑定回填；**C# 代码必须使用嵌套 API**：

| 已删除属性 | 替代 |
| ---------- | ---- |
| `FeishuAppConfig.TimeOut` | `TimeoutSeconds` |
| `FeishuAppConfig.RetryCount` / `RetryDelayMs` | `HttpRetry.MaxAttempts` / `HttpRetry.DelayMs` |
| `FeishuAppConfig.CircuitBreaker*` 扁平 | `CircuitBreaker.*` |
| `FeishuAppConfig.EnableLogging` | `Logging:LogLevel:Mud.Feishu.Abstractions.TokenManager` |
| `FeishuWebhookOptions.EnableBackgroundProcessing` | `EnableTokenBackgroundRefresh`（null=不干预基座） |
| `FeishuWebSocketOptions.AutoReconnect` 等 | `Reconnect.*` |
| `FeishuWebSocketOptions.AllowSelfSignedCertificates` 等 | `Certificate.*` |
| `RedisOptions.ServerAddress` 等连接扁平键 | `Connection.*` / `Advanced.*` |
| `DeduplicationOptions.AllowProcessingOnFallback` 等 | 已删除（主路径不消费） |

### Webhook / TokenRefresh（R4）

```jsonc
"FeishuWebhook": {
  "EnableTokenBackgroundRefresh": null,  // null=不干预基座；true/false=显式覆盖
  "LegacyGlobalTimeoutOnly": false       // B1 过渡闸
}
```

### 日志开关

| 属性 | 替代 |
| ---- | ---- |
| `FeishuAppConfig.EnableLogging` | `Logging:LogLevel:Mud.Feishu.Abstractions.TokenManager` |
| `FeishuWebSocketOptions.EnableLogging` | `Logging:LogLevel:Mud.Feishu.WebSocket` |
| `FeishuWebhookOptions.EnableRequestLogging` | `Logging:LogLevel:Mud.Feishu.Webhook` |

### 配置审计

```powershell
powershell -File ./scripts/audit-config-keys.ps1
powershell -File ./scripts/audit-config-keys.ps1 -Strict   # CI 阻断
```

## 最小配置（目标态）

### 单应用 Webhook

```jsonc
{
  "FeishuApps": [
    { "AppKey": "default", "AppId": "cli_xxx", "AppSecret": "..." }
  ],
  "FeishuWebhook": {
    "Apps": {
      "default": {
        "VerificationToken": "...",
        "EncryptKey": "0123456789abcdef0123456789abcdef"
      }
    }
  }
}
```

### 多应用 + Redis

```jsonc
{
  "FeishuApps": [
    { "AppKey": "default", "AppId": "cli_aaa", "AppSecret": "..." },
    { "AppKey": "tenant-b", "AppId": "cli_bbb", "AppSecret": "...",
      "BaseUrl": "https://open.larksuite.com" }
  ],
  "FeishuWebSocket": { "AppKey": "default" },
  "FeishuRedis": { "ServerAddress": "redis:6379" },
  "FeishuDeduplication": {
    "Mode": "Distributed",
    "Event": { "KeyPrefix": "t-a:feishu:event:" },
    "Nonce": { "KeyPrefix": "t-a:feishu:nonce:" },
    "SeqId":  { "KeyPrefix": "t-a:feishu:seqid:" }
  }
}
```

## 回滚

1. 包版本回退（NuGet 消费方固定版本）。
2. 配置兼容：R2–R3 **旧扁平键仍绑定**，无需为回滚改配置。
3. B1 行为变更：`LegacyGlobalTimeoutOnly=true` 恢复全局-only 超时语义。
4. R4 删除 Obsolete 前至少保留一个 minor 双读窗口。

## 注册顺序

```text
AddFeishuRedisDeduplicators(config)  // 先于 App/Webhook
  → CreateFeishuWebhookServiceBuilder(...).Build()
  → AddFeishuDeduplicationOptions(config)  // 已由 Redis/WS/Webhook Builder 自动调用
```
