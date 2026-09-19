# 事件去重配置真相源对照表（R1）

> 文档性质：运维/集成必读。目标：消除「文档写了可配、绑定成功、运行时无效」。
> 阶段：R1（不改公共 API 形状）。R2 统一节 `FeishuDeduplication` 落地后本表将扩展映射列。

## 现状：按通道选配置面

| 通道 | 实际消费的配置 | 用户今天该配哪里 | 备注 |
| ---- | -------------- | ---------------- | ---- |
| WebSocket 内存/模式 | `FeishuWebSocket:EventDeduplication:*` → `FeishuEventDeduplicator` 工厂 | **`FeishuWebSocket:EventDeduplication`** | Mode：None/InMemory/Distributed；默认 TTL 来自 Consts（48h） |
| Redis 分布式事件 | `RedisOptions.EventCacheExpiration` / `EventKeyPrefix` **优先** | **`FeishuRedis:EventCacheExpiration` / `EventKeyPrefix`** | `FeishuRedis:Deduplication:CacheExpiration/KeyPrefix` **不生效**，将被覆盖并 Warn |
| Redis Nonce / SeqID | `RedisOptions.NonceTtl/NonceKeyPrefix/SeqId*` | **`FeishuRedis:Nonce*` / `SeqId*`** | 多租户必须三前缀互异（TMA2-20） |
| Webhook 默认内存 | Builder 工厂：`IOptions<DeduplicationOptions>` 若已注册则用之；否则 **Consts 默认** | 代码 `Configure<DeduplicationOptions>` 或未来 `FeishuDeduplication` | R1 起 TTL/ProcessingTimeout 与 Consts 对齐（48h/10min），不再是 MemoryDeduplicator 的 24h/5min 私有默认 |

## 默认值单一真相源（Consts）

| 常量 | 值 | 含义 |
| ---- | -- | ---- |
| `Consts.DefaultCacheExpirationMs` | 48h | 事件去重 TTL |
| `Consts.DefaultCleanupIntervalMs` | 5min | 内存清理间隔 |
| `Consts.DefaultProcessingTimeoutMs` | 10min | 处理中超时 |
| `Consts.DefaultMaxCacheSize` | 100000 | 内存上限 |
| `Consts.DefaultEventKeyPrefix` | `feishu:event:` | 事件键前缀 |
| `Consts.DefaultNonceKeyPrefix` | `feishu:nonce:` | Nonce 键前缀 |
| `Consts.DefaultSeqIdKeyPrefix` | `feishu:seqid:` | SeqID 键前缀 |

> `Consts` 为 `internal`，宿主程序集不可直接引用；SDK 内部工厂与 Options 默认必须引用这些常量，禁止重复魔法数。

## 伪可配置字段（绑定成功但当前主路径不消费）

| 字段 | 出现位置 | Redis 主路径 | 处置 |
| ---- | -------- | ------------ | ---- |
| `AllowProcessingOnFallback` | `DeduplicationOptions` / `FeishuRedis:Deduplication` | **不消费** | R1 Warn；R2 迁 Advanced 或删除 |
| `MaxRetryCount`（去重） | 同上 | **不消费** | 同上；**勿与** `FailedEventRetryOptions.MaxRetryCount`（事件重试，已消费）混淆 |
| `InitialRetryDelay` / `MaxRetryDelay` | 同上 | **不消费** | 同上 |

## 配置注册顺序（Webhook + Redis）

```text
AddFeishuRedisDeduplicators(config)   // 必须在 AddFeishuApp / Webhook Build 之前
    ↓ 注册 Redis 分布式 IFeishuEventDeduplicator（TryAddSingleton 先到先得）
CreateFeishuWebhookServiceBuilder(...).Build()
    ↓ TryAddSingleton 工厂仅在未注册时生效（内存实现）
```

颠倒顺序会导致 Webhook 内存去重覆盖 Redis 分布式实现（静默）。已有守卫 `EnsureFeishuAppNotRegistered` 保护 AppManager 注册顺序，去重实现仍以 **Redis 扩展先行** 为准。

## 多租户模板（TMA2-20）

```jsonc
"FeishuRedis": {
  "EventKeyPrefix": "t-a:feishu:event:",
  "NonceKeyPrefix": "t-a:feishu:nonce:",
  "SeqIdKeyPrefix":  "t-a:feishu:seqid:",
  "EventCacheExpiration": "2.00:00:00",
  "NonceTtl": "00:05:00"
}
```

每个租户独立前缀，禁止三键相同或跨租户复用。

## R1 行为变更提醒

1. **应用级 `EventHandlingTimeoutMs` 开始生效**（B1）。升级前检查 `FeishuWebhook:Apps:{key}` 是否配置了过小超时；需保持旧语义时临时 `FeishuWebhook:LegacyGlobalTimeoutOnly=true`。
2. **Webhook 内存去重默认 TTL** 从实现私有 24h 对齐为 Consts 48h（B2/R1.2）。
3. Redis 路径无效 Dedup 键将输出 **Warning**（不删除配置，不反转 RedisOptions 优先级）。

## 路线图

| 阶段 | 变化 |
| ---- | ---- |
| R1（本表） | 文档 + Warn + Webhook 工厂对齐 Consts + B1 消费修复 |
| R2 | 新节 `FeishuDeduplication` 作为推荐真相源；旧键 Obsolete 双读；映射表见方案 §4.1 |
| R3–R4 | 嵌套 Advanced 面；删除过时 Obsolete（major 评估） |
