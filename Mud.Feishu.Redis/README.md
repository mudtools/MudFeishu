# Mud.Feishu.Redis

飞书事件订阅组件 Redis 分布式去重扩展。

## 功能特性

- **事件去重**: 基于 EventId 的分布式去重，使用 Redis Hash + Lua 脚本实现状态机模式（Processing → Completed），支持超时恢复和异常回滚
- **Nonce 去重**: 防止重放攻击，使用 Redis SET NX EX 确保请求唯一性
- **SeqID 去重**: WebSocket 二进制消息序列号去重，使用 Redis Sorted Set 支持范围查询，含 TTL 与写入时裁剪（有界增长）
- **多应用隔离**: 所有去重器支持 `appKey` 参数，避免跨应用事件冲突
- **SeqID 实例隔离**: `scopeKey` 构造参数，多实例共享 Redis 时不互相判重
- **统一键构造**: `RedisKeyBuilder` 统一四类键构造，含分段转义、长度上限、空前缀护栏
- **可分类异常**: `FeishuRedisException` + `FeishuRedisFailureKind`，消费侧可区分连接故障与服务端错误
- **健康检查**: 内置 Redis 健康检查，自动注册到 ASP.NET Core 健康检查系统
- **配置验证**: 启动时自动验证配置有效性（`ValidateOnStart`），敏感信息掩码输出
- **原子性操作**: 使用 Lua 脚本确保去重操作的原子性，服务端时钟消除时钟漂移
- **自动过期**: Redis 自动清理过期数据，无需手动维护
- **分布式支持**: 适用于多实例部署场景，Cluster 下多节点聚合扫描
- **AOT 兼容**: net8.0+ 原生 AOT 支持，源生成 JSON 序列化，无反射依赖

## 安装

```bash
dotnet add package Mud.Feishu.Redis
```

## 快速开始

### 配置 Redis 连接

在 `appsettings.json` 中添加配置：

```json
{
  "FeishuRedis": {
    "ServerAddress": "localhost:6379",
    "Password": "",
    "EventCacheExpiration": "48:00:00",
    "NonceTtl": "00:05:00",
    "SeqIdCacheExpiration": "48:00:00",
    "EventKeyPrefix": "feishu:event:",
    "NonceKeyPrefix": "feishu:nonce:",
    "SeqIdKeyPrefix": "feishu:seqid:",
    "SeqIdScopeKey": "",  // 可选，为空时自动合成 {AppKey}|{MachineName}
    "AppKey": "default",  // 用于 SeqID scopeKey 合成
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "Ssl": false,
    "AllowAdmin": false,
    "AbortOnConnectFail": true,
    "ConnectRetry": 3,
    "DefaultDatabase": 0
  }
}
```

### 注册服务

#### 方式一：从配置文件注册所有去重服务

```csharp
using Mud.Feishu.Redis.Extensions;

// 自动从配置文件读取 Redis 连接信息并注册所有去重服务
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);
```

#### 方式二：代码配置注册所有去重服务

```csharp
// 通过代码配置 Redis 连接信息
builder.Services.AddFeishuRedisDeduplicators(options =>
{
    options.ServerAddress = "localhost:6379";
    options.Password = "your_password";
    options.EventCacheExpiration = TimeSpan.FromHours(48);
    options.NonceTtl = TimeSpan.FromMinutes(5);
    options.ConnectRetry = 3;
});
```

#### 方式三：自定义配置节名称

```csharp
// 使用自定义配置节名称
builder.Services.AddFeishuRedisDeduplicators(
    builder.Configuration,
    sectionName: "MyRedisConfig");
```

### 完整示例

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册 Redis 去重服务（从配置文件读取）
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);

// 注册 WebSocket 事件订阅
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();

// 健康检查已自动注册，可通过端点查看
app.MapHealthChecks("/health");

app.Run();
```

## 配置选项

### RedisOptions 配置项

| 参数                   | 类型     | 默认值           | 说明                                                                     |
| ---------------------- | -------- | ---------------- | ------------------------------------------------------------------------ |
| `ServerAddress`        | string   | "localhost:6379" | Redis 服务器地址（host:port 或 redis://host:port 或 rediss://host:port） |
| `Password`             | string   | ""               | Redis 密码                                                               |
| `EventCacheExpiration` | TimeSpan | 48 小时          | 事件去重缓存过期时间                                                     |
| `NonceTtl`             | TimeSpan | 5 分钟           | Nonce 有效期                                                             |
| `SeqIdCacheExpiration` | TimeSpan | 48 小时          | SeqID 去重缓存过期时间                                                   |
| `EventKeyPrefix`       | string   | "feishu:event:"  | 事件去重键前缀                                                           |
| `NonceKeyPrefix`       | string   | "feishu:nonce:"  | Nonce 去重键前缀                                                         |
| `SeqIdKeyPrefix`       | string   | "feishu:seqid:"  | SeqID 去重键前缀                                                         |
| `ConnectTimeout`       | int      | 5000ms           | 连接超时时间（最小 1000ms）                                              |
| `SyncTimeout`          | int      | 5000ms           | 同步超时时间（最小 1000ms）                                              |
| `Ssl`                  | bool     | false            | 是否启用 TLS/SSL                                                         |
| `AllowAdmin`           | bool     | false            | 是否允许管理员操作（仅生产环境需要时启用）                               |
| `AbortOnConnectFail`   | bool     | true             | 是否在连接失败时中止                                                     |
| `ConnectRetry`         | int      | 3                | 连接重试次数                                                             |
| `DefaultDatabase`      | int?     | null             | 默认数据库索引                                                           |
| `ClientName`           | string?  | null             | 客户端名称（默认自动生成）                                               |

> ℹ️ **高级去重参数**：在 `FeishuRedis:Deduplication` 子节下可配置 `ProcessingTimeout`（处理超时阈值，默认 10 分钟）。注意：`CacheExpiration` 和 `KeyPrefix` 由上表中的 `EventCacheExpiration` / `EventKeyPrefix` 优先覆盖。

## 去重服务详解

### 事件去重（Event Deduplication）

事件去重采用 **状态机模式**，使用 Redis Hash 存储事件状态，Lua 脚本保证原子性：

```
Pending → Processing → Completed
              ↓            ↑
         (超时恢复)    (正常完成)
              ↓
         (异常回滚 → Pending)
```

**状态说明**：

| 状态                 | 说明                           |
| -------------------- | ------------------------------ |
| `Pending`            | 初始状态，事件尚未开始处理     |
| `Processing`         | 事件正在处理中，其他实例将跳过 |
| `Completed`          | 事件处理完成，永久跳过         |
| `TimeoutRecoverable` | 处理中超时，允许重新处理       |

**Redis 数据结构**：

- **Key**: `{keyPrefix}{appKey}:{eventId}`
- **Type**: Hash
- **Fields**: `status` (processing/completed), `timestamp` (UTC 时间)
- **TTL**: 由 `EventCacheExpiration` 指定

**核心 API**：

| 方法                       | 说明                               |
| -------------------------- | ---------------------------------- |
| `TryMarkAsProcessingAsync` | 尝试标记事件为处理中，返回去重结果 |
| `MarkAsCompletedAsync`     | 标记事件为已完成                   |
| `RollbackProcessingAsync`  | 回滚处理中状态，允许重新处理       |
| `IsProcessedAsync`         | 检查事件是否已处理                 |
| `GetStatusAsync`           | 获取事件当前状态                   |
| `RemoveAsync`              | 手动移除去重标记                   |
| `RemoveRangeAsync`         | 批量移除去重标记                   |
| `GetCachedCountAsync`      | 获取缓存中的事件数量               |

### Nonce 去重（Nonce Deduplication）

使用 Redis SETNX + EXPIRE 实现原子性去重，防止重放攻击：

**Redis 数据结构**：

- **Key**: `{keyPrefix}{appKey}:{nonce}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `NonceTtl` 指定

**核心 API**：

| 方法                  | 说明                    |
| --------------------- | ----------------------- |
| `TryMarkAsUsedAsync`  | 尝试标记 Nonce 为已使用 |
| `IsUsedAsync`         | 检查 Nonce 是否已使用   |
| `RemoveAsync`         | 手动移除 Nonce 标记     |
| `RemoveRangeAsync`    | 批量移除 Nonce 标记     |
| `GetCachedCountAsync` | 获取缓存中的 Nonce 数量 |

### SeqID 去重（SeqID Deduplication）

使用 Redis String + Sorted Set 实现 WebSocket 消息序列号去重：

**Redis 数据结构**：

- **Key**: `{keyPrefix}{scopeKey}{seqId}` (String 类型，记录已处理状态)
- **Sorted Set**: `{keyPrefix}{scopeKey}set` (记录所有已处理的 SeqID，支持范围查询)
- **TTL**: 由 `SeqIdCacheExpiration` 指定（写入时刷新，Sorted Set 同生命周期）

> 💡 `scopeKey` 默认为 `{AppKey}|{MachineName}`，多实例共享 Redis 时不互相判重。可通过 `RedisOptions.SeqIdScopeKey` 自定义。
> Sorted Set 在写入时执行 `ZREMRANGEBYSCORE` 裁剪过期成员，确保集合有界增长。

**核心 API**：

| 方法                      | 说明                    |
| ------------------------- | ----------------------- |
| `TryMarkAsProcessedAsync` | 尝试标记 SeqID 为已处理 |
| `IsProcessedAsync`        | 检查 SeqID 是否已处理   |
| `GetMaxProcessedSeqId`    | 获取最大已处理 SeqID    |
| `GetCacheCount`           | 获取缓存数量            |
| `ClearCacheAsync`         | 清空缓存                |

## 异常处理

所有 Redis 去重器在 Redis 异常时抛出 `FeishuRedisException`，消费侧可按 `FailureKind` 分类处理：

| `FeishuRedisFailureKind` | 含义 | 典型场景 | 建议策略 |
| --- | --- | --- | --- |
| `Connection` | 连接故障 | 网络中断、Redis 宕机 | 可降级到内存去重或拒绝请求 |
| `Timeout` | 操作超时 | 大 Value、网络延迟 | 重试或降级 |
| `Server` | 服务端/配置错误 | Cluster MOVED、权限拒绝 | 不重试，告警运维 |

```csharp
try
{
    var result = await deduplicator.TryMarkAsProcessingAsync(eventId);
}
catch (FeishuRedisException ex)
{
    if (ex.FailureKind == FeishuRedisFailureKind.Connection)
    {
        // 可降级到内存去重
        logger.LogWarning(ex, "Redis 连接故障，降级到内存去重");
    }
    else
    {
        // 服务端/配置类错误不降级
        throw;
    }
}
```

> ⚠️ 本组件不提供内置的 Redis 故障降级到内存去重的能力。如需降级，由上层（Webhook `NonceFailureMode` / WS 异常处理）决定策略。

### best-effort API

以下 API 设计为 best-effort（失败记日志、不抛出）：

- `RollbackProcessingAsync`（事件回滚）
- `RemoveAsync` / `RemoveRangeAsync`（手动移除去重标记）
- `GetCachedCountAsync`（获取缓存数量）
- `ClearCacheAsync`（清空 SeqID 缓存）

## 健康检查

注册 Redis 去重服务时，健康检查会自动注册到 ASP.NET Core 健康检查系统：

```csharp
// 健康检查已自动注册，标签为 "redis" 和 "feishu"
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);

// 映射健康检查端点
app.MapHealthChecks("/health");
```

健康检查会执行 Redis PING 命令，并返回延迟和连接端点信息。

## 多应用隔离

所有去重器支持 `appKey` 参数，在多应用场景下避免事件冲突：

```csharp
// 标记事件为处理中（指定应用）
var result = await deduplicator.TryMarkAsProcessingAsync(eventId, appKey: "hr-app");

// 标记事件为已完成（指定应用）
await deduplicator.MarkAsCompletedAsync(eventId, appKey: "hr-app");

// 检查事件是否已处理（指定应用）
var isProcessed = await deduplicator.IsProcessedAsync(eventId, appKey: "hr-app");
```

Redis 键格式：`{keyPrefix}{appKey}:{eventId}`，不同应用的事件互不干扰。

## 常见问题

### 1. 如何更改默认的缓存过期时间？

在配置文件中修改：

```json
{
  "FeishuRedis": {
    "EventCacheExpiration": "7.00:00:00",
    "NonceTtl": "00:10:00",
    "SeqIdCacheExpiration": "7.00:00:00"
  }
}
```

### 2. 多个环境如何隔离数据？

通过配置文件设置不同的键前缀：

开发环境 `appsettings.Development.json`:

```json
{
  "FeishuRedis": {
    "EventKeyPrefix": "dev:feishu:event:",
    "NonceKeyPrefix": "dev:feishu:nonce:",
    "SeqIdKeyPrefix": "dev:feishu:seqid:"
  }
}
```

生产环境 `appsettings.Production.json`:

```json
{
  "FeishuRedis": {
    "EventKeyPrefix": "prod:feishu:event:",
    "NonceKeyPrefix": "prod:feishu:nonce:",
    "SeqIdKeyPrefix": "prod:feishu:seqid:"
  }
}
```

> 💡 键前缀非空且不以 `*` 开头，否则启动期校验失败（R-01 护栏）。

### 3. 如何使用 TLS/SSL 连接 Redis？

在配置文件中启用 SSL：

```json
{
  "FeishuRedis": {
    "ServerAddress": "secure.redis.com:6380",
    "Password": "your_password",
    "Ssl": true
  }
}
```

或使用 `rediss://` 协议：

```json
{
  "FeishuRedis": {
    "ServerAddress": "rediss://secure.redis.com:6380"
  }
}
```

### 4. 如何处理事件处理超时？

事件去重器内置了超时恢复机制。当事件处于 Processing 状态超过 `processingTimeout`（默认 10 分钟）时，`TryMarkAsProcessingAsync` 会返回 `TimeoutRecoverable` 结果，允许重新处理：

```csharp
var result = await deduplicator.TryMarkAsProcessingAsync(eventId);

if (result.Status == DeduplicationStatus.TimeoutRecoverable)
{
    // 事件之前处理超时，可以安全地重新处理
    _logger.LogWarning("事件 {EventId} 处理超时，重新处理", result.EventId);
}
```

### 5. 如何回滚事件处理状态？

当事件处理过程中发生异常时，可以回滚状态，允许后续重新处理：

```csharp
try
{
    var result = await deduplicator.TryMarkAsProcessingAsync(eventId);
    if (result.IsDuplicate)
        return;

    // 处理业务逻辑...
    await ProcessBusinessLogicAsync();

    // 标记为已完成
    await deduplicator.MarkAsCompletedAsync(eventId);
}
catch (Exception ex)
{
    // 回滚处理状态，允许重新处理
    await deduplicator.RollbackProcessingAsync(eventId);
    throw;
}
```

### 6. 配置验证失败怎么办？

服务注册时会自动验证配置有效性，常见验证规则：

- `ServerAddress` 不能为空，格式须为 `host:port` 或 `redis://host:port` 或 `rediss://host:port`
- `ConnectTimeout` 必须至少为 1000 毫秒
- `SyncTimeout` 必须至少为 1000 毫秒
- `ConnectRetry` 不能为负数

验证失败时应用将无法启动，并在日志中输出具体错误信息。

## 安全

### 令牌明文存储

Redis 令牌存储（`RedisTokenStore` / `RedisUserTokenStore`）默认以**明文**形式将 access token 和 refresh
token 持久化到 Redis。这意味着：

- 拥有 Redis 访问权限的人员可以直接读取令牌。
- Redis 数据落盘（RDB/AOF）或备份文件中包含明文令牌。

**如需加密存储**，请启用 `FeishuAppOptions.EnableTokenEncryption = true` 并注册 `IEncryptionProvider`：

```csharp
builder.Services.AddFeishuApp(options =>
{
    options.EnableTokenEncryption = true;
});
// 注册 IEncryptionProvider 实现（如 AES-GCM）
builder.Services.AddSingleton<IEncryptionProvider, AesEncryptionProvider>();
```

未启用加密时，启动期会产生 TMA-21 告警日志（Warning 级别），提示令牌以明文存储。

### 键前缀护栏

`RedisKeyBuilder` 在构造键时强制：

- 前缀非空（空前缀 + `*` pattern 会退化为全库 SCAN，R-01 护栏）。
- 前缀不以 `*` 开头（通配符前缀导致 SCAN 匹配所有键）。
- 分段转义：段内的 `:` 替换为 `\:`，杜绝 `appKey="a:b"` 与 `appKey="a"` + `eventId="b:c"` 产生相同键（R-20）。
- 单段长度上限 256 字节，防止超长键 DoS。

## 附录 A：能力 ↔ 实现 ↔ 测试 映射表

| 能力 | 实现文件 | 测试文件 | 关联审查编号 |
| --- | --- | --- | --- |
| 事件去重状态机 v2（Lua 原子化 + 服务端时钟） | `RedisFeishuEventDistributedDeduplicator.cs` | `EventDeduplicatorIntegrationTests.cs` | R-04/R-05/R-06/R-15/R-17/R-19 |
| Nonce 去重（SET NX EX + 分类异常） | `RedisFeishuNonceDistributedDeduplicator.cs` | `NonceAndTokenStoreIntegrationTests.cs` | R-12/R-22 |
| SeqID 去重（scopeKey 隔离 + 有界 Sorted Set） | `RedisFeishuSeqIDDeduplicator.cs` | `SeqIDDeduplicatorIntegrationTests.cs` | R-01/R-07/R-08 |
| 统一键构造（转义 + 长度 + 护栏） | `RedisKeyBuilder.cs` | `SeqIDDeduplicatorIntegrationTests.cs` | R-01/R-20/R-21 |
| 分类异常契约 | `FeishuRedisException.cs` | `EventDeduplicatorIntegrationTests.cs` | ADR-6 |
| Cluster 多节点扫描 | `RedisStoreHelper.GetServers()` | `NonceAndTokenStoreIntegrationTests.cs` | R-10 |
| 令牌存储（per-app 键空间 + Cluster） | `RedisTokenStore.cs` / `PerAppRedisTokenStoreFactory.cs` | `NonceAndTokenStoreIntegrationTests.cs` | R-09/R-10/R-23 |
| 用户令牌存储 | `RedisUserTokenStore.cs` | `NonceAndTokenStoreIntegrationTests.cs` | R-09/R-10 |
| Nonce 降级语义 | `NonceValidator.cs`（Webhook） | — | R-12/R-18 |
| 配置启动期校验 | `RedisOptions.cs` / `RedisOptionsValidator.cs` | — | R-12/R-26 |
| 连接字符串原生解析 | `RedisFeishuServiceBuilderExtensions.cs` | — | R-13 |

## 许可证

MIT License
