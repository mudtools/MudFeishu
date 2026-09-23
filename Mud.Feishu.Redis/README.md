# Mud.Feishu.Redis

飞书事件订阅组件 Redis 分布式去重扩展。

## 功能特性

- **事件去重**: 基于 EventId 的分布式去重，使用 Redis Hash + Lua 脚本实现状态机模式（Processing → Completed），支持超时恢复和异常回滚
- **Nonce 去重**: 防止重放攻击，使用 Redis SET NX EX 确保请求唯一性
- **SeqID 去重**: WebSocket 二进制消息序列号去重，使用 Redis String + Sorted Set，写入时按**容量窗口**裁剪（`ZREMRANGEBYRANK`）并刷新 TTL，集合大小有确定性上界
- **多应用隔离**: **事件 / Nonce** 去重支持 `appKey` 参数，避免跨应用键冲突
- **SeqID 实例隔离**: `scopeKey` 构造参数（默认 `{AppKey}|{MachineName}`），多实例共享 Redis 时不互相判重
- **统一键构造**: `RedisKeyBuilder` 统一键构造，含分段转义、长度上限、空前缀护栏；**所有 SCAN 模式唯一出口为 `RedisKeyBuilder.Pattern`**（与键同源、段级精确）
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
    "Connection": {
      "ServerAddress": "localhost:6379",
      "Password": "",
      "ConnectTimeout": 5000,
      "SyncTimeout": 5000,
      "Ssl": false,
      "AbortOnConnectFail": true,
      "ConnectRetry": 3,
      "DefaultDatabase": 0
    },
    "Advanced": {
      "AllowAdmin": false
    },
    "EventCacheExpiration": "48:00:00",
    "NonceTtl": "00:10:00",
    "SeqIdCacheExpiration": "48:00:00",
    "SeqIdWindowCapacity": 100000,  // SeqID Sorted Set 容量窗口上界（必须 > 0）
    "EventKeyPrefix": "feishu:event:",
    "NonceKeyPrefix": "feishu:nonce:",
    "SeqIdKeyPrefix": "feishu:seqid:",
    "SeqIdScopeKey": "",       // 可选，为空时自动合成 {AppKey}|{MachineName}
    "AppKey": "default",       // 用于 SeqID scopeKey 合成
    "TokenKeyPrefix": "feishu" // 令牌键环境段：最终键 {TokenKeyPrefix}:{appKey}:token:…
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
    options.Connection.ServerAddress = "localhost:6379";
    options.Connection.Password = "your_password";
    options.Connection.ConnectRetry = 3;
    options.EventCacheExpiration = TimeSpan.FromHours(48);
    options.NonceTtl = TimeSpan.FromMinutes(5);
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
| `Connection.ServerAddress`      | string   | "localhost:6379" | Redis 服务器地址（host:port 或 redis://host:port 或 rediss://host:port） |
| `Connection.Password`           | string   | ""               | Redis 密码                                                               |
| `EventCacheExpiration`          | TimeSpan | 48 小时          | 事件去重缓存过期时间                                                     |
| `NonceTtl`                      | TimeSpan | 600 秒（10 分钟） | Nonce 有效期（建议 ≥ Webhook 时间戳容差的 2 倍）                          |
| `SeqIdCacheExpiration`          | TimeSpan | 48 小时          | SeqID 去重缓存过期时间                                                   |
| `SeqIdWindowCapacity`           | int      | 100000           | SeqID Sorted Set 容量窗口上界（成员数），必须 > 0，否则启动期校验失败     |
| `TokenKeyPrefix`                | string   | "feishu"         | 令牌键环境段：`{TokenKeyPrefix}:{appKey}:token:…`；空值兜底默认值，不能以 `*` 开头 |
| `EventKeyPrefix`                | string   | "feishu:event:"  | 事件去重键前缀                                                           |
| `NonceKeyPrefix`                | string   | "feishu:nonce:"  | Nonce 去重键前缀                                                         |
| `SeqIdKeyPrefix`                | string   | "feishu:seqid:"  | SeqID 去重键前缀                                                         |
| `Connection.ConnectTimeout`     | int      | 5000ms           | 连接超时时间（最小 1000ms）                                              |
| `Connection.SyncTimeout`        | int      | 5000ms           | 同步超时时间（最小 1000ms）                                              |
| `Connection.Ssl`                | bool     | false            | 是否启用 TLS/SSL                                                         |
| `Advanced.AllowAdmin`           | bool     | false            | 是否允许管理员操作（仅生产环境需要时启用）                               |
| `Connection.AbortOnConnectFail` | bool     | true             | 是否在连接失败时中止                                                     |
| `Connection.ConnectRetry`       | int      | 3                | 连接重试次数                                                             |
| `Connection.DefaultDatabase`    | int?     | null             | 默认数据库索引                                                           |
| `Advanced.ClientName`           | string?  | null             | 客户端名称（默认自动生成）                                               |

> ℹ️ **高级去重参数**：推荐使用统一节 `FeishuDeduplication`（`Event` / `Nonce` / `SeqId` 子节点）配置 TTL 与键前缀；`FeishuRedis:Deduplication` 子节仍可绑定 `ProcessingTimeout`（处理超时阈值，默认 10 分钟）、`CleanupInterval`、`MaxCacheSize`。注意：`CacheExpiration` 和 `KeyPrefix` 由上表中的 `EventCacheExpiration` / `EventKeyPrefix` 优先覆盖；`AllowProcessingOnFallback` / `MaxRetryCount` / `InitialRetryDelay` / `MaxRetryDelay` / `EnableVerboseLogging` 已在 R4 移除（Redis 主路径不消费），失败事件重试请改用 `FeishuWebhook:Retry`。

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

- **Key**: `feishu:event::{appKey}:{eventId}`（未传 `appKey` 时为 `feishu:event::{eventId}`）
- **Type**: Hash
- **Fields**:
  - `status` = `processing` | `completed`
  - `timestamp` = **Redis 服务端 Unix 秒**（Lua `redis.call('TIME')` 写入，消除实例间时钟漂移）
  - `timeout` = 该事件的处理超时秒数（用于 `GetStatusAsync` 判定，per-call 覆盖默认值）
- **TTL**: 由 `EventCacheExpiration` 指定

> 💡 键由 `RedisKeyBuilder` 分段构造，段间以 `:` 分隔，段内的 `:` 会被转义为 `\:`，因此 `{appKey}`/`{eventId}` 本身含 `:` 也不会产生键碰撞。
> 注意默认前缀自带尾 `:`，而 `Combine` 会再插入一级 `:`，故实际键为**双冒号**形态（如 `feishu:event::cli_a:evt1`）；
> 段内 `:` 转义示例：`appKey="a:b" + eventId="c"` → `feishu:event::a\:b:c`，与 `appKey="a" + eventId="b:c"` → `feishu:event::a:b\:c` 不会碰撞。

**核心 API**：

| 方法                       | 所属                               | 说明                               |
| -------------------------- | ---------------------------------- | ---------------------------------- |
| `TryMarkAsProcessingAsync` | `IFeishuEventDeduplicator`（接口） | 尝试标记事件为处理中，返回去重结果 |
| `MarkAsCompletedAsync`     | `IFeishuEventDeduplicator`（接口） | 标记事件为已完成                   |
| `RollbackProcessingAsync`  | `IFeishuEventDeduplicator`（接口） | 回滚处理中状态，允许重新处理       |
| `IsProcessedAsync`         | `IFeishuEventDeduplicator`（接口） | 检查事件是否已处理                 |
| `GetStatusAsync`           | `IFeishuEventDeduplicator`（接口） | 获取事件当前状态                   |
| `RemoveAsync`              | 仅 Redis 实现类                    | 手动移除去重标记                   |
| `RemoveRangeAsync`         | 仅 Redis 实现类                    | 批量移除去重标记                   |
| `GetCachedCountAsync`      | 仅 Redis 实现类                    | 获取缓存中的事件数量               |

> 💡 DI 注册绑定的是 `IFeishuEventDeduplicator` **接口**；`RemoveAsync` / `RemoveRangeAsync` / `GetCachedCountAsync` 仅在实现类 `RedisFeishuEventDistributedDeduplicator` 上提供，通过接口引用注入时不可调用，如需使用请解析具体实现类型。

### Nonce 去重（Nonce Deduplication）

使用 Redis SETNX + EXPIRE 实现原子性去重，防止重放攻击：

**Redis 数据结构**：

- **Key**: `feishu:nonce::{appKey}:{nonce}`（未传 `appKey` 时为 `feishu:nonce::{nonce}`）
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `NonceTtl` 指定

> 💡 键由 `RedisKeyBuilder` 分段构造，段间以 `:` 分隔，段内的 `:` 会被转义为 `\:`。

**核心 API**：

| 方法                  | 所属                                     | 说明                    |
| --------------------- | ---------------------------------------- | ----------------------- |
| `TryMarkAsUsedAsync`  | `IFeishuNonceDistributedDeduplicator`（接口） | 尝试标记 Nonce 为已使用 |
| `IsUsedAsync`         | `IFeishuNonceDistributedDeduplicator`（接口） | 检查 Nonce 是否已使用   |
| `RemoveAsync`         | 仅 Redis 实现类                          | 手动移除 Nonce 标记     |
| `RemoveRangeAsync`    | 仅 Redis 实现类                          | 批量移除 Nonce 标记     |
| `GetCachedCountAsync` | 仅 Redis 实现类                          | 获取缓存中的 Nonce 数量 |

> 💡 DI 注册绑定的是 `IFeishuNonceDistributedDeduplicator` **接口**；`RemoveAsync` / `RemoveRangeAsync` / `GetCachedCountAsync` 仅在实现类 `RedisFeishuNonceDistributedDeduplicator` 上提供，通过接口引用注入时不可调用，如需使用请解析具体实现类型。

### SeqID 去重（SeqID Deduplication）

使用 Redis String + Sorted Set 实现 WebSocket 消息序列号去重：

**Redis 数据结构**（键由 `RedisKeyBuilder.Combine` 产出，段间 `:`、段内 `:` 转义为 `\:`）：

- **String 键**: `feishu:seqid::{scopeKey}:{seqId}`（值 `"1"`，`SET ... EX ... NX`）
- **Sorted Set 键**: `feishu:seqid::{scopeKey}:set`（member = `seqId`，score = `SeqID`）
- **TTL**: 由 `SeqIdCacheExpiration` 指定，两者同生命周期

> 💡 **写入语义（容量窗口，R2-01）**：单 Lua 脚本原子执行
> `SET NX` + `ZADD` + `ZREMRANGEBYRANK key 0 -(capacity+1)` + `EXPIRE`——按**排名**裁剪，
> 仅保留分数最大的 `SeqIdWindowCapacity` 个成员（历史实现用**时间阈值**比较 SeqID 分数，
> 阈值 ≈1.79×10⁹ 恒大于任何真实 SeqID，导致成员写入即被清除、计数恒为 0）。

| 方法 | 语义 |
| --- | --- |
| `GetCacheCount()` | **当前窗口内成员数**（≤ `SeqIdWindowCapacity`） |
| `GetMaxProcessedSeqId()` | **窗口内真实最大值**（按 score 降序取首元素） |
| `ClearCacheAsync()` | 删除 `RedisKeyBuilder.Pattern(SeqIdKeyPrefix, scopeKey)` 命中的全部键，即 `feishu:seqid::{scopeKey}:*`（含 `:set`）；best-effort |

> ⚠️ `GetCacheCount()` **不再等于** String 键 TTL 窗口内的去重规模，**不可用于推断剩余去重空间**。
> 容量窗口只保证集合有界；TTL 由 String 键与 Sorted Set 各自承担。

> 💡 `scopeKey` 默认为 `{AppKey}|{MachineName}`（多实例共享 Redis 时不互相判重），可用 `RedisOptions.SeqIdScopeKey` 或 `FeishuDeduplication:SeqId:ScopeKey` 覆盖。
> 清理模式与键**同源**（统一经 `RedisKeyBuilder.Pattern`，且模式以**分隔符 + `*`** 结尾）：
> 历史缺陷用裸拼接缺少一级 `:`，模式恒不匹配实际键，**一个键都删不掉**；
> 修复后由 `FeishuWebSocketClient.ResetStateOnReconnectAsync` 在**每次 WebSocket 重连**时调用。

**核心 API**：

| 方法                      | 说明                    |
| ------------------------- | ----------------------- |
| `TryMarkAsProcessedAsync` | 尝试标记 SeqID 为已处理 |
| `IsProcessedAsync`        | 检查 SeqID 是否已处理   |
| `RollbackAsync`           | 回滚标记，允许重新处理  |
| `GetMaxProcessedSeqId`    | 获取窗口内最大已处理 SeqID |
| `GetCacheCount`           | 获取窗口内成员数        |
| `ClearCacheAsync`         | 清空本 `scopeKey` 的 SeqID 键（含 Sorted Set） |

> 💡 上表方法均为 `IFeishuSeqIDDeduplicator` **接口**成员，DI 注入的接口引用即可调用。

## 异常处理

所有 Redis 去重器在 Redis 异常时抛出 `FeishuRedisException`，消费侧可按 `FailureKind` 分类处理：

| `FeishuRedisFailureKind` | 含义 | 典型场景 | 建议策略 |
| --- | --- | --- | --- |
| `Connection` | 连接故障 | 网络中断、Redis 宕机 | 可降级到内存去重或拒绝请求 |
| `Timeout` | 操作超时 | 大 Value、网络延迟 | 重试或降级 |
| `Server` | 服务端/配置错误 | Cluster MOVED、权限拒绝 | 不重试，告警运维 |
| `InvalidArgument` | 无效参数 | 调用方传入非法键段（空段 / 段长 > 256 字符），由 `RedisKeyBuilder` 在构造键时抛出 | 修正调用参数，不重试 |

> ℹ️ **键前缀非法**（空或以 `*` 开头）抛**原生 `InvalidOperationException`**（配置类错误，非调用参数），
> 与 `InvalidArgument` 区分；启动期由 `RedisOptions.Validate()`（net6+ 经 `ValidateOnStart`）拦截。

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

- `RollbackProcessingAsync`（事件回滚，接口成员）
- `RemoveAsync` / `RemoveRangeAsync`（手动移除去重标记，仅 Redis 实现类提供）
- `GetCachedCountAsync`（获取缓存数量，仅 Redis 实现类提供）
- `ClearCacheAsync`（清空 SeqID 缓存，接口成员）

## 健康检查

注册 Redis 去重服务时，健康检查会自动注册到 ASP.NET Core 健康检查系统：

```csharp
// 健康检查已自动注册，标签为 "redis" 和 "feishu"
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);

// 映射健康检查端点
app.MapHealthChecks("/health");
```

健康检查会执行 Redis PING 命令，并返回延迟和连接端点信息。

**探活判据（R2-12）**：`PING` 命令成功即 `Healthy`；`data` 含 `latency`、`connectedEndpoints`、`totalEndpoints`。
端点级异常（副本不可达、集群拓扑变化）**只影响连通计数**，不会把 PING 正常的实例整体判为 `Unhealthy`。

**可选关闭（R2-12）**：库内默认会调用 `AddHealthChecks()`（对未使用健康检查的宿主产生隐式注册）。
如需自管注册：

```csharp
// 不注册到健康检查系统，仅注册 RedisHealthCheck 类型；由宿主自行 AddCheck<RedisHealthCheck>("feishu-redis")
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration, registerHealthCheck: false);
```

## 多应用隔离

**事件 / Nonce** 去重器支持 `appKey` 参数，在多应用场景下避免键冲突；
**SeqID** 使用 `scopeKey`（默认 `{AppKey}|{MachineName}`，见上文「SeqID 去重」）：

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
>
> ⚠️ **令牌键的环境隔离**：令牌键前缀为 `{TokenKeyPrefix}:{appKey}:token:…`（`TokenKeyPrefix` 默认 `feishu`）。
> 多环境共用同一 Redis 时，请为各环境配置不同的 `TokenKeyPrefix`（事件/Nonce/SeqID 的 `*KeyPrefix` 不受其影响）。
>
> ⚠️ **过期时间下限**：`EventCacheExpiration` / `SeqIdCacheExpiration` 的 setter 会把小于 1 分钟的值
> **静默钳制到 1 分钟**（配置 30 秒实际生效 1 分钟）；`SeqIdWindowCapacity` 非正值回落到默认值，
> 但启动期校验（`ValidateOnStart`）会对非正值直接失败。

### 3. 如何使用 TLS/SSL 连接 Redis？

在配置文件中启用 SSL：

```json
{
  "FeishuRedis": {
    "Connection": {
      "ServerAddress": "secure.redis.com:6380",
      "Password": "your_password",
      "Ssl": true
    }
  }
}
```

或使用 `rediss://` 协议：

```json
{
  "FeishuRedis": {
    "Connection": {
      "ServerAddress": "rediss://secure.redis.com:6380"
    }
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

- `Connection.ServerAddress` 不能为空，格式须为 `host:port` 或 `redis://host:port` 或 `rediss://host:port`
- `Connection.ConnectTimeout` 必须至少为 1000 毫秒
- `Connection.SyncTimeout` 必须至少为 1000 毫秒
- `Connection.ConnectRetry` 不能为负数

验证失败时应用将无法启动，并在日志中输出具体错误信息。

## 安全

### 令牌明文存储

Redis 令牌存储（`RedisTokenStore` / `RedisUserTokenStore`）默认以**明文**形式将 access token 和 refresh
token 持久化到 Redis。这意味着：

- 拥有 Redis 访问权限的人员可以直接读取令牌。
- Redis 数据落盘（RDB/AOF）或备份文件中包含明文令牌。

**如需加密存储**，请启用 `FeishuAppOptions.EnableTokenEncryption = true` 并注册 `IEncryptionProvider`：

```csharp
// 1. 开启令牌加密（FeishuAppOptions 经 IOptions 解析，通过 Configure 设置）
builder.Services.Configure<FeishuAppOptions>(options =>
{
    options.EnableTokenEncryption = true;
});

// 2. 注册 IEncryptionProvider（接口由 Mud.HttpUtils 提供，Mud.Feishu 不内置实现）
//    方式一：使用 Mud.HttpUtils 内置的 AES 加密提供程序（推荐）
builder.Services.AddMudHttpAesEncryption();

//    方式二：注册自定义实现（由消费方提供，需实现
//    Encrypt(string) / Decrypt(string) / EncryptBytes(byte[]) / DecryptBytes(byte[]) 四个成员）
builder.Services.AddSingleton<IEncryptionProvider, MyAesGcmEncryptionProvider>();
```

未启用加密时，启动期会产生 TMA-21 告警日志（Warning 级别），提示令牌以明文存储。若已启用加密但未注册 `IEncryptionProvider`，同样会告警并降级为明文存储。

### 键前缀护栏

`RedisKeyBuilder` 在构造键时强制：

- 前缀非空（空前缀 + `*` pattern 会退化为全库 SCAN，R-01 护栏）。
- 前缀不以 `*` 开头（通配符前缀导致 SCAN 匹配所有键）。
- 分段转义：段内的 `:` 替换为 `\:`，杜绝 `appKey="a:b"` 与 `appKey="a"` + `eventId="b:c"` 产生相同键（R-20）。
- 单段长度上限 256 字符；超限抛 `FeishuRedisException(InvalidArgument)`（调用方输入非法，不应重试）。
- SCAN 模式经 `RedisKeyBuilder.Pattern` 产出，且以**分隔符 + `*`** 结尾（段级精确匹配，
  避免 `scopeKey="a"` 的清理越界删除 `scopeKey="ab"` 的键）。

## 运维诊断

`AddFeishuRedisDeduplicators` / `AddFeishuRedisTokenStore` 会以单例注册诊断门面，
使宿主无需依赖具体实现类型即可观测三类键空间：

```csharp
using Mud.Feishu.Redis.Diagnostics;

var diagnostics = sp.GetRequiredService<IRedisDeduplicationDiagnostics>();
var snapshot = await diagnostics.GetSnapshotAsync(cancellationToken);

// snapshot.ServerTimeSeconds             Redis 服务端 Unix 秒（TIME 不可用时回落客户端时间）
// snapshot.EventDeduplicatorAvailable   事件去重器是否为 Redis 实现
// snapshot.EventCachedCount             事件键空间键数
// snapshot.NonceDeduplicatorAvailable / NonceCachedCount
// snapshot.SeqIdDeduplicatorAvailable / SeqIdCacheCount / SeqIdMaxProcessed / SeqIdScopeKey
```

三个去重器若被宿主替换为内存实现，对应 `*Available` 为 `false`（计数恒为 0，**不抛异常**）。

> ⚠️ **成本警告**：`EventCachedCount` / `NonceCachedCount` 为**全库 SCAN** + 服务端 `TIME`，属运维路径。
> **禁止在热路径或高频轮询（如每请求）中调用**；建议用于健康检查端点或人工排查。

## 可观测性

Redis 指标挂在既有 `Mud.Feishu` Meter 上（`Mud.Feishu.OpenTelemetry` 已自动 `AddMeter`），
宿主只需启用该 Meter：

| 指标 | 类型 | 单位 | 维度 |
| --- | --- | --- | --- |
| `feishu.redis.operation` | Counter | `{operation}` | `feishu.redis.command`、`feishu.dedup.type`、`outcome` |
| `feishu.redis.operation.duration` | Histogram | `ms` | `feishu.redis.command`、`feishu.dedup.type` |
| `feishu.redis.scan.keys` | Counter | `{key}` | `feishu.redis.command`、`outcome`（`scanned` / `deleted`） |

- `outcome` 的失败取值与 `FeishuRedisFailureKind` 一一对应（`connection` / `timeout` / `server` / `invalid_argument`），
  另有 `success`、`duplicate`、`timeout_recoverable`、`key_missing`。
- `feishu.redis.scan.keys` 仅在调用计数/清理类 API 时上报（**不引入任何周期性全库扫描**），
  `outcome=deleted` 的计数可直接暴露"清理恒删 0 个键"这类静默失效。

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter(FeishuMetrics.MeterName)); // "Mud.Feishu"
```

## 附录 A：能力 ↔ 实现 ↔ 测试 映射表

| 能力 | 实现文件 | 测试文件 | 关联审查编号 |
| --- | --- | --- | --- |
| 事件去重状态机 v2（Lua 原子化 + 服务端时钟） | `Services/RedisFeishuEventDistributedDeduplicator.cs` | `IntegrationTests/EventDeduplicatorIntegrationTests.cs`、`Tests/Services/RedisFeishuEventDistributedDeduplicatorTests.cs` | R-04/R-05/R-06/R-15/R-17/R-19 |
| Nonce 去重（SET NX EX + 分类异常） | `Services/RedisFeishuNonceDistributedDeduplicator.cs` | `IntegrationTests/NonceAndTokenStoreIntegrationTests.cs`、`Tests/Services/RedisFeishuNonceDistributedDeduplicatorTests.cs` | R-12/R-22 |
| SeqID 去重（scopeKey 隔离 + **容量窗口** Sorted Set） | `Services/RedisFeishuSeqIDDeduplicator.cs` | `IntegrationTests/SeqIDDeduplicatorIntegrationTests.cs`、`Tests/Services/RedisFeishuSeqIDDeduplicatorTests.cs` | R-01/R-07/**R2-01** |
| 统一键构造（转义 + 长度 + 护栏）与 **SCAN 模式单一出口** | `Services/RedisKeyBuilder.cs`（`Pattern`） | `Tests/ContractGuards/RedisKeyLayoutContractGuards.cs` | R-01/R-20/R-21/**R2-02** |
| 分类异常契约（含 `InvalidArgument` 真实产生） | `Mud.Feishu.Abstractions/Exceptions/FeishuRedisException.cs` | `Tests/ContractGuards/RedisKeyLayoutContractGuards.cs` | ADR-6/**R2-18** |
| Cluster 多节点扫描 | `Services/RedisStoreHelper.GetServers()` | `IntegrationTests/NonceAndTokenStoreIntegrationTests.cs` | R-10 |
| 令牌存储（per-app 键空间 + Cluster + 环境前缀） | `Services/RedisTokenStore.cs` / `Services/PerAppRedisTokenStoreFactory.cs` | `Tests/PerAppRedisTokenStoreKeyIsolationTests.cs`、`IntegrationTests/NonceAndTokenStoreIntegrationTests.cs` | R-09/R-10/R-23/**R2-04**/**R2-09** |
| 用户令牌存储 | `Services/RedisUserTokenStore.cs` | `Tests/Services/RedisUserTokenStoreTests.cs` | R-09/R-10 |
| **运维诊断门面** | `Diagnostics/IRedisDeduplicationDiagnostics.cs` | `Tests/ContractGuards/RedisKeyLayoutContractGuards.cs` | **R2-22** |
| **Redis 指标** | `Mud.Feishu.Abstractions/Metrics/FeishuMetrics.cs`、`Services/RedisMetricsHelper.cs` | `Mud.Feishu.OpenTelemetry.Tests` | **R2-21** |
| **连接选项装配（含 `rediss://` → TLS）** | `Services/RedisConnectionFactory.cs` | `Tests/ContractGuards/RedisKeyLayoutContractGuards.cs` | R-13/**R2-26** |
| Nonce 降级语义 | `NonceValidator.cs`（Webhook） | — | R-12/R-18 |
| 配置启动期校验 | `Configuration/RedisOptions.cs` / `RedisOptionsValidator.cs` | `Tests/Configuration/RedisOptionsValidatorTests.cs` | R-12/R-26 |

## 许可证

MIT License
