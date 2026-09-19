# 事件去重配置真相源对照表

> 文档性质：运维/集成必读。目标：消除「文档写了可配、绑定成功、运行时无效」。
> 修订：R5（2026-09）。R1 版本以旧键为「用户今天该配哪里」的表述**已作废**。

## 1. 唯一推荐入口：`FeishuDeduplication`

| 字段 | 说明 |
| ---- | ---- |
| `FeishuDeduplication:Mode` | `None` / `InMemory` / `Distributed` |
| `FeishuDeduplication:Profile` | `Default` / `HighReliability` / `HighAvailability` |
| `FeishuDeduplication:Event:Ttl` / `ProcessingTimeout` / `CleanupInterval` / `MaxCacheSize` | 事件去重条目 |
| `FeishuDeduplication:Event:KeyPrefix` | 事件键前缀 |
| `FeishuDeduplication:Nonce:Ttl` / `KeyPrefix` | Nonce 去重 |
| `FeishuDeduplication:SeqId:Ttl` / `KeyPrefix` / `ScopeKey` | SeqID 去重 |

**优先级**：字段级配置 > `Profile` 预设。`Distributed` 且配置了前缀时，三前缀必须互异（TMA2-20），由 `FeishuDeduplicationOptionsValidator` 强制。

## 2. 各通道的实际消费点（R5 复验）

| 通道 | 实际消费的配置 | 用户今天该配哪里 | 备注 |
| ---- | -------------- | ---------------- | ---- |
| WebSocket 内存/模式 | `FeishuDeduplication`（节存在时）→ 否则 `FeishuWebSocket:EventDeduplication:*` | **`FeishuDeduplication`**；未迁移时 `FeishuWebSocket:EventDeduplication` | Mode：None/InMemory/Distributed；默认 TTL 来自 `Consts`（48h） |
| Redis 分布式事件 | `FeishuDeduplication:Event:*`（节存在时字段级优先）→ 否则 `RedisOptions.EventCacheExpiration` / `EventKeyPrefix` | **`FeishuDeduplication:Event`** | 回落 `RedisOptions` 时会校验一致性；`FeishuRedis:Deduplication:CacheExpiration/KeyPrefix` **不生效**并 Warn |
| Redis Nonce / SeqID | `FeishuDeduplication:Nonce/SeqId:*` → 否则 `RedisOptions.Nonce*` / `SeqId*` | **`FeishuDeduplication:Nonce` / `:SeqId`** | 多租户必须三前缀互异（TMA2-20） |
| Webhook 默认内存 | `FeishuDeduplication`（节存在时）→ 否则 `IOptions<DeduplicationOptions>` → 否则 `Consts` 默认 | **`FeishuDeduplication`** | TTL/ProcessingTimeout 与 `Consts` 对齐（48h/10min） |

> **`IsConfiguredFromConfiguration` 语义（R5/X11）**：该属性是 SDK 内部状态位，表示
> 「`FeishuDeduplication` 节是否存在」，**面向宿主只读**（`internal set`）。上面所有「节存在时」
> 分支都由它判定；宿主无法在代码里伪造（下个 major 会改为直接判定节存在性）。

## 3. 默认值单一真相源（`Consts`，internal）

| 常量 | 值 | 含义 |
| ---- | -- | ---- |
| `Consts.DefaultCacheExpirationMs` | 48h | 事件去重 TTL |
| `Consts.DefaultCleanupIntervalMs` | 5min | 内存清理间隔 |
| `Consts.DefaultProcessingTimeoutMs` | 10min | 处理中超时 |
| `Consts.DefaultMaxCacheSize` | 100000 | 内存上限 |
| `Consts.DefaultEventKeyPrefix` | `feishu:event:` | 事件键前缀 |
| `Consts.DefaultNonceKeyPrefix` | `feishu:nonce:` | Nonce 键前缀 |
| `Consts.DefaultSeqIdKeyPrefix` | `feishu:seqid:` | SeqID 键前缀 |

> `Consts` 为 `internal`，宿主程序集不可直接引用；SDK 内部工厂与 Options 默认值必须引用这些常量，禁止重复魔法数。
> **R5/X14**：`DefaultDeduplicationRetryCount` / `DefaultDeduplicationInitialRetryDelayMs` /
> `DefaultDeduplicationMaxRetryDelayMs` 三个常量**零引用**，已删除（分布式失败语义在 R4 起不再被主路径消费）。

## 4. 旧键状态：**全部 deprecated，仅为兼容读取**

| 旧键 | 状态 | 迁移到 |
| ---- | ---- | ------ |
| `FeishuRedis:EventKeyPrefix` / `EventCacheExpiration` / `NonceKeyPrefix` / `NonceTtl` / `SeqIdKeyPrefix` / `SeqIdCacheExpiration` / `SeqIdScopeKey` / `AppKey` | **可绑定，仅作回落基座**；`FeishuDeduplication` 存在时被字段级覆盖 | `FeishuDeduplication:Event/Nonce/SeqId` |
| `FeishuRedis:Deduplication:ProcessingTimeout` / `CleanupInterval` / `MaxCacheSize` | **可绑定，仅作回落基座**（类型 `DeduplicationOptions`） | `FeishuDeduplication:Event:*` |
| `FeishuWebSocket:EventDeduplication:*` | **可绑定，仅作回落基座**（`FeishuDeduplication` 不存在时） | `FeishuDeduplication` |

> **R5 未删除 `DeduplicationOptions` / `EventDeduplicationOptions` 类型**：它们是双读期的回落基座
> （`FeishuEventDeduplicator` 的构造重载与 Redis 回落链依赖它们）。R5.2 起补 `[Obsolete]` 标记与迁移表，
> 删除留待下个 major。

## 5. 配置注册顺序（Webhook + Redis）

```text
AddFeishuRedisDeduplicators(config)   // 必须在 AddFeishuApp / Webhook Build 之前
    ↓ 注册 Redis 分布式 IFeishuEventDeduplicator（TryAddSingleton 先到先得）
    ↓ 同时调用 AddFeishuDeduplicationOptions(config) 绑定 FeishuDeduplication 节
CreateFeishuWebhookServiceBuilder(...).Build()
    ↓ TryAddSingleton 工厂仅在未注册时生效（内存实现）
```

颠倒顺序会导致 Webhook 内存去重覆盖 Redis 分布式实现（静默）。已有守卫 `EnsureFeishuAppNotRegistered` 保护 AppManager 注册顺序，去重实现仍以 **Redis 扩展先行** 为准。

## 6. 多租户模板（TMA2-20）

```jsonc
"FeishuDeduplication": {
  "Mode": "Distributed",
  "Event":  { "KeyPrefix": "t-a:feishu:event:",  "Ttl": "2.00:00:00" },
  "Nonce":  { "KeyPrefix": "t-a:feishu:nonce:",  "Ttl": "00:05:00" },
  "SeqId":  { "KeyPrefix": "t-a:feishu:seqid:" }
}
```

每个租户独立前缀，**禁止**三键相同或跨租户复用。

## 7. 行为变更提醒（跨版本累计）

1. **应用级 `EventHandlingTimeoutMs` 生效**（B1/R1）。升级前检查 `FeishuWebhook:Apps:{key}` 是否配置了过小超时；需保持旧全局语义时临时 `FeishuWebhook:LegacyGlobalTimeoutOnly=true`（R5 起已标 `[Obsolete]`，下个 major 删除）。
2. **Webhook 内存去重默认 TTL** 从实现私有 24h 对齐为 `Consts` 48h（B2/R1.2）。
3. Redis 路径无效 Dedup 键输出 **Warning**（不删除配置，不反转 `RedisOptions` 优先级）。
4. **`FeishuWebhook:Retry` 首次真正生效**（R5/X3）。此前该节 6 个字段恒为默认值，只有 `EnableRetry` 偶然生效；升级前请核对 `MaxRetryCount` / `RetryPollIntervalSeconds` 等是否与预期一致。
5. **`FeishuWebhook:AutoRegisterEndpoint` 与 `EnableRequestLogging` 无运行时效果**（R5/X4、X2）。前者已标 `[Obsolete]`（路由由 `app.UseFeishuWebhook()` 决定，配置 `false` 不会停止收事件，R5 起启动期会输出一次 Warning）；后者已删除，请改用 `Logging:LogLevel:Mud.Feishu.Webhook`。

## 8. 路线图

| 阶段 | 变化 |
| ---- | ---- |
| R1 | 文档 + Warn + Webhook 工厂对齐 `Consts` + B1 消费修复 |
| R2 | 新节 `FeishuDeduplication` 作为推荐真相源；旧键双读 |
| R3–R4 | 嵌套 Advanced 面；删除过时 Obsolete（major 评估） |
| **R5** | **本表重写为 unified 真相源**；`FeishuDeduplicationOptions.IsConfiguredFromConfiguration` 内部化；清理死配置（`EnableRequestLogging` 等）；补配置契约守卫与审计脚本门禁 |
| 下个 major | 删除 `DeduplicationOptions` / `EventDeduplicationOptions` / 各旧键；`IsConfiguredFromConfiguration` 改为直接判定节存在性 |
