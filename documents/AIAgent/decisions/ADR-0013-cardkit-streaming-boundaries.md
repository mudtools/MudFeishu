# ADR-0013：CardKit 会话内流式回复的平台边界与失败语义

- 状态：已采纳（2026-10-26）
- 相关代码：`Mud.Feishu.AI/Channels/CardKitStreamMessageChannel.cs`
- 相关契约：`Mud.AI.Abstractions.Channels.IMessageChannel` / `IChannelTargetResolver` / `StreamingRequestContext`；
  `Mud.AI.Agent.Channels.BufferedMessageChannel`（分片缓冲基类）/ `StreamingChannelChain`（降级链）

## 背景

流式回复从「应用消息卡片流（feed card，工作台消息流）」切换为「CardKit 卡片实体 + 会话气泡」：
卡片正文由 markdown 元素承载，每次推送**全量文本**，平台自算增量并渲染打字机效果。
这条链路的约束全部来自平台（不是产品决策，也无法从代码读出来），因此外移到本文，
源码只保留一行链接与最短摘要。

## 决策一：协议三步固定为「建实体 → 投气泡 → 推全量」

1. `POST /open-apis/cardkit/v1/cards`：`type=card_json`，`data` 为卡片 JSON 2.0 且 `config.streaming_mode=true`；
2. `POST /open-apis/im/v1/messages`：`msg_type=interactive`，`content={"type":"card","data":{"card_id":…}}`
   （官方「共享卡片实体」形态）；
3. `PUT /open-apis/cardkit/v1/cards/{card_id}/elements/{element_id}/content`：推**全量**文本。

**卡片实体只能发送一次**，且发送方必须是创建方——故 Begin 失败后不可能「换一条消息重发同一张卡」，
只能整轮回退到编辑通道（降级链）或非流式回复（事件处理器）。

## 决策二：目标语义由通道自解释，不做平台 API 解析

`IMessageChannel.BeginAsync(appKey, target)` 的 `target` 语义由实现方定义：

| 场景 | 目标 | `receive_id_type` |
| --- | --- | --- |
| 会话事件（IM 消息） | `ConversationRequest.ChatId` | `chat_id` |
| 业务事件（审批 / 表格记录变更，p2p） | 操作人 `open_id` | `open_id` |
| 群聊业务事件（无 `ChatId`） | 不适用（返回 `null`，走非流式） | — |

判定依据是中立环境量 `StreamingRequestContext.Current`（管线进入通道前建立），**不做 ID 前缀猜测**：
目标是 `ChatId` 则 `chat_id`，否则为单聊操作人 `open_id`。环境量缺失（外部直调通道）时沿用 `chat_id`。

`ResolveStreamTarget` 是**同步**契约，通道内不可能 `await` 平台 API 反查 chat_id；
「业务事件要不要流式」因此只能由事件载荷已带的信息决定，而不是再发一次查询。

## 决策三：平台硬边界（登记，不防御）

| 边界 | 值 | 处置 |
| --- | --- | --- |
| 卡片 JSON 体积 | 30KB | 不截断、不加 header（预算全留给正文）；超限由平台拒绝，终态失败可观测 |
| `streaming_mode` 自动关闭 | 最后一次更新后 10 分钟 | **不主动关闭**：官方建议手动关，但终态钩子 `OnFlushed(messageId)` 无取消令牌，fire-and-forget 换不来等价收益 |
| 卡片实体有效期 | 14 天 | 会话流式回复生命周期远短于此；实体无删除接口，Begin 残留只能等过期 |
| 实体发送次数 | 1 次 | Begin 失败即整轮回退，不重发同一实体 |
| `sequence` | 同卡片**严格递增** | 飞书 300317 拒绝回退号/重复号；终态后状态移除，迟到写入直接丢弃（不重置为 1） |

## 决策四：失败语义分级

| 阶段 | 语义 | 可观测面 |
| --- | --- | --- |
| `BeginAsync` 建实体失败 | 向上抛（模型尚未调用，零重复成本） | 异常 → 降级链回退 |
| `BeginAsync` 投气泡失败 | 向上抛 + 计数 `channel_begin_orphaned` | 平台残留实体，唯一需单独区分的失败 |
| 单次增量失败 | 只记 Warning，不中断模型流 | `channel_update_failed`（用户最终仍看到完整回答） |
| **终态**落地失败 | Error 日志 + 计数 `channel_terminal_failed` | 用户永远看不到完整回答 |

**为什么终态失败不抛**：`BufferedMessageChannel.FlushAsync` / `WriteStreamAsync` 是 sealed override，
产品侧无法覆写；且基类契约明文要求「子类 `UpdateCoreAsync` 内的业务级失败自行记日志返回，不中断模型流」，
`OnFlushed` 在终态落地后**无条件**调用（void）。故产品侧的能力边界是**可观测**，不是**可抛出/可回滚**。
把 `Ok=false` 升级成 throw 会打断回合管线，属越界实现。

「补发一条非流式消息」这条更彻底的补偿需要上游给出「终态是否送达」的契约
（`FlushAsync` 返回 `StreamFlushOutcome` 或新增 `OnFlushCompleted(messageId, delivered)` 钩子），
已作为跨仓需求登记在 `.docs/AI/`；在拿到该契约前，本仓只做可观测。
