# Mud.Feishu.Redis

飞书事件订阅组件 Redis 分布式去重扩展。

## 功能特性

- **事件去重**: 基于 EventId 的分布式去重，防止重复处理同一事件
- **Nonce 去重**: 防止重放攻击，确保请求的唯一性
- **SeqID 去重**: WebSocket 二进制消息序列号去重，防止重复处理
- **原子性操作**: 使用 Redis SETNX + EXPIRE 确保去重操作的原子性
- **自动过期**: Redis 自动清理过期数据，无需手动维护
- **分布式支持**: 适用于多实例部署场景

## 安装

```bash
dotnet add package Mud.Feishu.Redis
```

## 快速开始

### 方式一：使用配置文件（推荐）

#### 1. 配置 Redis 连接

在 `appsettings.json` 中添加配置：

```json
{
  "Feishu": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "EventCacheExpiration": "24:00:00",
      "NonceTtl": "00:05:00",
      "SeqIdCacheExpiration": "24:00:00",
      "EventKeyPrefix": "feishu:event:",
      "NonceKeyPrefix": "feishu:nonce:",
      "SeqIdKeyPrefix": "feishu:seqid:",
      "ConnectTimeout": 5000,
      "SyncTimeout": 5000,
      "Ssl": false,
      "AllowAdmin": true
    }
  }
}
```

#### 2. 注册服务

```csharp
using Mud.Feishu.Redis.Extensions;

// 自动从配置文件读取 Redis 连接信息并注册所有去重服务
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisDeduplicators();
```

### 方式二：手动创建连接

```csharp
using Mud.Feishu.Redis.Extensions;
using StackExchange.Redis;

// 创建 Redis 连接
var redis = ConnectionMultiplexer.Connect("localhost:6379");

// 注册服务
services.AddFeishuRedisDeduplicators(redis);
```

### 单独注册

#### 1. 事件去重服务

```csharp
services.AddFeishuRedisEventDeduplicator(
    redis,
    cacheExpiration: TimeSpan.FromHours(24),
    keyPrefix: "feishu:event:"
);
```

#### 2. Nonce 去重服务

```csharp
services.AddFeishuRedisNonceDeduplicator(
    redis,
    nonceTtl: TimeSpan.FromMinutes(5),
    keyPrefix: "feishu:nonce:"
);
```

#### 3. SeqID 去重服务

```csharp
services.AddFeishuRedisSeqIDDeduplicator(
    redis,
    cacheExpiration: TimeSpan.FromHours(24),
    keyPrefix: "feishu:seqid:"
);
```

### 完整示例

#### 使用配置文件（推荐）

```csharp
var builder = WebApplication.CreateBuilder(args);

// 方式 1：自动从配置文件读取并注册
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisDeduplicators()
    .AddFeishuWebSocket(options =>
    {
        options.AppId = builder.Configuration["Feishu:AppId"];
        options.AppSecret = builder.Configuration["Feishu:AppSecret"];
    });

var app = builder.Build();
app.Run();
```

#### 手动创建连接

```csharp
var builder = WebApplication.CreateBuilder(args);

// 方式 2：手动创建 Redis 连接
var redis = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"));

// 注册飞书服务
builder.Services
    .AddFeishuRedisDeduplicators(redis)
    .AddFeishuWebSocket(options =>
    {
        options.AppId = builder.Configuration["Feishu:AppId"];
        options.AppSecret = builder.Configuration["Feishu:AppSecret"];
    });

var app = builder.Build();
app.Run();
```

## 配置选项

### RedisOptions 配置项

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ConnectionString` | string | "localhost:6379" | Redis 连接字符串 |
| `EventCacheExpiration` | TimeSpan | 24小时 | 事件去重缓存过期时间 |
| `NonceTtl` | TimeSpan | 5分钟 | Nonce 有效期 |
| `SeqIdCacheExpiration` | TimeSpan | 24小时 | SeqID 去重缓存过期时间 |
| `EventKeyPrefix` | string | "feishu:event:" | 事件去重键前缀 |
| `NonceKeyPrefix` | string | "feishu:nonce:" | Nonce 去重键前缀 |
| `SeqIdKeyPrefix` | string | "feishu:seqid:" | SeqID 去重键前缀 |
| `ConnectTimeout` | int | 5000ms | 连接超时时间 |
| `SyncTimeout` | int | 5000ms | 同步超时时间 |
| `Ssl` | bool | false | 是否启用 TLS/SSL |
| `AllowAdmin` | bool | true | 是否允许管理员操作 |

### 事件去重参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `redis` | IConnectionMultiplexer? | 从DI获取 | Redis 连接多路复用器（可选） |
| `cacheExpiration` | TimeSpan? | 从Options读取 | 缓存过期时间 |
| `keyPrefix` | string? | 从Options读取 | Redis 键前缀 |

### Nonce 去重参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `redis` | IConnectionMultiplexer? | 从DI获取 | Redis 连接多路复用器（可选） |
| `nonceTtl` | TimeSpan? | 从Options读取 | Nonce 有效期 |
| `keyPrefix` | string? | 从Options读取 | Redis 键前缀 |

### SeqID 去重参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `redis` | IConnectionMultiplexer? | 从DI获取 | Redis 连接多路复用器（可选） |
| `cacheExpiration` | TimeSpan? | 从Options读取 | 缓存过期时间 |
| `keyPrefix` | string? | 从Options读取 | Redis 键前缀 |

## Redis 数据结构

### 事件去重

- **Key**: `{keyPrefix}{eventId}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `cacheExpiration` 指定

### Nonce 去重

- **Key**: `{keyPrefix}{nonce}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `nonceTtl` 指定

### SeqID 去重

- **Key**: `{keyPrefix}{seqId}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `cacheExpiration` 指定
- **Sorted Set**: `{keyPrefix}set` (用于记录已处理的 SeqID)

## 常见问题

### 1. 如何更改默认的缓存过期时间？

在注册服务时传入自定义的 `cacheExpiration` 参数：

```csharp
services.AddFeishuRedisEventDeduplicator(
    redis,
    cacheExpiration: TimeSpan.FromDays(7)
);
```

### 2. 多个环境如何隔离数据？

使用不同的 `keyPrefix` 前缀：

```csharp
// 开发环境
services.AddFeishuRedisEventDeduplicator(
    redis,
    keyPrefix: "dev:feishu:event:"
);

// 生产环境
services.AddFeishuRedisEventDeduplicator(
    redis,
    keyPrefix: "prod:feishu:event:"
);
```

### 3. 如何监控 Redis 去重状态？

每个去重服务都提供了日志记录，可以通过日志查看去重状态：

```csharp
// 获取已处理的事件数量
var deduplicator = serviceProvider.GetRequiredService<IFeishuEventDistributedDeduplicator>();
var count = await deduplicator.GetCachedCountAsync();
```

## 许可证

MIT License
