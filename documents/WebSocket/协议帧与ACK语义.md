# WebSocket 协议帧与 ACK 语义

> 适用范围：`Mud.Feishu.WebSocket` 的二进制帧处理链路（`BinaryMessageProcessor` / `FrameBuilder` / `MessageRouter`）。
> 目的：明确"什么帧会回 ACK、回的 code 是什么、服务端会如何反应"，以及去重与重投的关系。

## 1. 帧结构（ProtoBuf，`EventProtoData`）

| 字段 | 说明 |
| --- | --- |
| `Service` / `Method` | `Method = 0` 表示控制帧（Ping/Pong），其余为数据帧 |
| `PayloadType` / `PayloadEncoding` | 数据帧载荷为 JSON（`event`）；ACK 回帧的 `PayloadType = "ack"`、`PayloadEncoding = "json"` |
| `SeqID` | 服务端全局递增序号（**全局计数器**，跨应用共享 → 跳跃属正常，默认不做跳跃检测） |
| `LogID` / `LogIDNew` | 服务端日志标识，ACK 原样回传 |
| `Headers` | ACK 回帧追加 `biz_rt`（本端业务处理耗时，ms） |

## 2. 控制帧

| 类型 | 处理 | 是否回 ACK |
| --- | --- | --- |
| `Ping` | 忽略（服务端不应向客户端发 Ping） | 否 |
| `Pong` | 解析 `ClientConfig`（仅应用 `PingInterval`，钳制 5–30 秒）→ `PongReceived` | 否 |
| 其他 | 记录日志（Debug） | 否 |

> 设计约束：`HeartbeatManager` **不据 Pong 判死**（对齐 Python SDK `_ping_loop`）。连接断开由接收循环异常与
> `ClientProtocolKeepAliveInterval`（协议级 Ping/Pong 超时）检测。

## 3. 数据帧处理与 ACK

```text
接收 → 帧反序列化
  ├─ 失败（ProtoException）→ 回退按 JSON 解析（有大小上限）→ 不回 ACK
  ├─ 序号验证失败（Duplicate/Rollback）→ 回 ACK(200)
  ├─ 统一去重命中（EventId / SeqID）          → 回 ACK(200)
  ├─ 处理成功                                 → MarkCompleted → 回 ACK(200)
  ├─ 处理失败（路由返回 false / 处理器异常）  → Rollback     → 回 ACK(500)
  └─ 已解析帧之后的未预期异常                 → Rollback(SeqID) → 回 ACK(500)
```

| code | 含义 | 服务端行为 |
| --- | --- | --- |
| **200** | 已受理（含"因去重而跳过"） | 不再重投 |
| **500** | 处理失败 | **重投**该事件 |

**幂等要求（重要）**：由于失败会触发服务端重投，业务处理器**必须幂等**；同时 SDK 侧通过
`IUnifiedDeduplicationMiddleware`（EventId + SeqID 双重去重）降低重复投递的落地概率。

## 4. 去重链路

| 层 | 键 | 行为 |
| --- | --- | --- |
| 序号验证 `MessageSequenceValidator` | `SeqID` | 重复/回退检测（可配置跳跃阈值，默认禁用） |
| 事件去重 `IFeishuEventDeduplicator` | `event_id` | 内存或 Redis（`EventDeduplication.Mode`） |
| 统一去重 `IUnifiedDeduplicationMiddleware` | `event_id` + `SeqID` | 优先路径；命中 `ShouldSkip` → 回 ACK(200) |
| SeqID 去重 `IFeishuSeqIDDeduplicator` | `SeqID`（含 scopeKey 隔离） | 未接入统一去重时的独立路径 |

失败回滚：处理失败时回滚 `SeqID` 标记（`RollbackAsync`），使服务端重投能够被重新处理。

## 5. 消息大小限制（收发同源）

| 配置 | 维度 | 默认 | 作用点 |
| --- | --- | --- | --- |
| `MessageSizeLimits.MaxTextMessageSize` | 字符 | 1,048,576 | 文本发送的**字符**校验（既有契约） |
| `MessageSizeLimits.MaxTextMessageBytes` | UTF-8 字节 | 0（= 3 × 字符上限） | 文本发送 **+** 分片文本接收（收发同源） |
| `MessageSizeLimits.MaxBinaryMessageSize` | 字节 | 10MB | 二进制发送 + 二进制接收（含分片与 JSON 回退路径） |

- 默认派生值（3 × 字符上限）恰好等于旧"字符语义"的字节上界 ⇒ 属**放宽**，现有一切合法消息继续通过。
- 默认配置下"字符校验先失败"是常态：字节分支只在显式收紧 `MaxTextMessageBytes` 时才会触发。
- 超限的二进制消息在写入内存流**之前**被拦截并丢弃（不回 ACK；服务端会按超时重投）。

## 6. 背压对 ACK 的影响

背压前移后（见《架构与并发模型》第 6 节），槽位耗尽时接收循环挂起 → 当前帧既不读、也不回 ACK：

- 服务端视角：ACK 延迟 → 重投窗口变长（属预期语义："慢消费 = 反压"）。
- 调参建议：`MaxConcurrentHandlers` 保持 32 或更大；将其设为 1 会使认证响应等文本帧也可能被延迟。
- 观测手段：`feishu.websocket.backlog`（在途处理数）与健康检查的并发利用率（≥90% 判 Degraded）。
