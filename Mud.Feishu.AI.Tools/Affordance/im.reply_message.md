## WHEN

- 这句话专门针对某条消息说，挂在它下面成串，避免打断主会话。

## Avoid when

- 没有特定指向的消息——那是 `im.send_message`。
- 只是把现成消息递给别人——那是 `im.forward_message`，回复会产生新内容。
- 事后要回看整串讨论——那是 `im.get_thread_messages`。

## Prerequisites

- `message_id` 是被回复的那条，来自事件上下文或 `im.get_history_messages`。
- `content` 必须是与 `msg_type` 匹配的 JSON 字符串（text 时形如 {"text":"…"}）；这是 im 域写面里唯一要自己拼内容 JSON 的工具。

## Tips

- `reply_in_thread` 留空即按当前会话自动判定：正处话题中会自动挂进话题；只有要脱离话题、直接回主会话时才显式传 false。
- `idempotency_key` 相同值在 1 小时内至多成功回复一条，省略即放弃这层保护。
