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

| 层 | 键 | 隔离范围 | 行为 |
| --- | --- | --- | --- |
| 序号验证 `MessageSequenceValidator` | `SeqID` | **单连接实例**（重连时 `Reset()`） | 重复/回退检测（可配置跳跃阈值，默认禁用） |
| 事件去重 `IFeishuEventDeduplicator` | `event_id` + **AppKey** | **按 AppKey 隔离**（`TryMarkAsProcessingAsync`/`MarkAsCompletedAsync`/`RollbackProcessingAsync` 均带 AppKey） | 内存或 Redis（`EventDeduplication.Mode`） |
| 统一去重 `IUnifiedDeduplicationMiddleware` | `event_id` + `SeqID` | 由中间件实现决定 | 优先路径；命中 `ShouldSkip` → 回 ACK(200) |
| SeqID 去重 `IFeishuSeqIDDeduplicator` | `SeqID` | **进程内全局集合，无租户/应用隔离** | 未接入统一去重时的独立路径 |

> **口径纠正（R2/WS2-10）**：`IFeishuSeqIDDeduplicator`（`FeishuSeqIDDeduplicator` / `MemoryDeduplicator<ulong>`）
> 的键就是裸 `SeqID`——**不存在 `scopeKey` 隔离**。多实例部署时各实例各自去重（互不可见），
> 单实例内部也不区分租户：若同一进程内并存多条连接（当前 DI 形态为**非 keyed 单例**，
> 通常不可能），其 `SeqID` 会互相判重。
> 需要**真正具备隔离**的一层请用**事件级去重**（`event_id` + AppKey）。
> 多客户端同容器场景的接口演进（为 `IFeishuSeqIDDeduplicator` 增 `scopeKey`）已登记为后续项，本轮不实施。

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
- 派生上限为 `3 × MaxTextMessageSize`，以 `int.MaxValue` 饱和（R2/P2-1 修复此前会整型溢出为负数）。
  配置面另有上界 `MaxTextMessageSize ≤ 10MB`（启动期 `Validate()` fail-fast）。

## 5.1 分片超限的丢弃语义（R2/WS2-03 变更）

**"丢弃"必须在消息边界上完成。** WebSocket 是消息边界化的帧协议：一条消息由若干分片（`EndOfMessage=false`）
加最后一个 `EndOfMessage=true` 的分片组成。分片重组（`HandleFragmentedMessageAsync`）在超限时若直接
`return`，本条消息的**剩余分片**会在下一轮 `ReceiveAsync` 中被当作**新消息**消费——这些字节既不构成
合法 protobuf/JSON，又会污染 `MessageSequenceValidator` 游标与去重状态（`SequenceGapThreshold` 默认 0，
没有任何跳跃检测能发现它）。

因此现行为：

| 场景 | 行为 |
| --- | --- |
| 首帧或累积超限 | 记 `Error`（`ErrorType = FragmentSizeExceeded`）→ **排空至 `EndOfMessage`** → 丢弃 → 连接**保持可用** |
| 排空期间收到关闭帧 | 完成关闭握手并退出（连接即将终止，边界已无意义） |
| 排空期间连接失效/被取消 | 中止排空并返回（由接收循环的异常/取消路径收口） |
| 排空超过上界（1024 帧 / 64MB 仍无 `EndOfMessage`） | 判定协议层异常 → `Abort` 连接 → 接收循环异常退出 → 触发重连（重连会重置序号验证器与半包状态） |
| 收到关闭帧（正常终止） | **允许不排空**直接返回（这是唯一例外） |

排空上界的存在是必要的：恶意/异常对端可以持续投递超限分片让排空永不结束。

## 6. 背压对 ACK 的影响

背压前移后（见《架构与并发模型》第 6 节），槽位耗尽时接收循环挂起 → 当前帧既不读、也不回 ACK：

- 服务端视角：ACK 延迟 → 重投窗口变长（属预期语义："慢消费 = 反压"）。
- 调参建议：`MaxConcurrentHandlers` 保持 32 或更大；将其设为 1 会使认证响应等文本帧也可能被延迟。
- 观测手段：`feishu.websocket.backlog`（在途处理数）与健康检查的并发利用率（≥90% 判 Degraded）。
