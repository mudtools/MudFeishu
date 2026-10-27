## WHEN

- 已锁定一条 message_id，要看它的正文与 @ 提到的人。

## Avoid when

- 还没有 ID，只知道是哪个群——先用 `im.get_history_messages` 定位。
- 想知道谁读过这条消息——那是 `im.get_message_read_users`。
- 想知道这条消息上贴了哪些表情——那是 `im.list_reactions`。

## Prerequisites

- `message_id`（omXxx）来自 `im.get_history_messages` 的回执或事件上下文。
- 需 im:message:readonly，以应用（tenant）身份调用。

## Tips

- 正文落在 `body` 键（历史列表用的是另一个键名），同样按预览长度截断，超长消息仍拿不到全文。
- `mentions` 只在有 @ 时给出，元素含 `key` / `id` / `name`。
- 回执只有 `message_id` / `msg_type` / `body` / `mentions` 四类键，发送者与时间要回列表取；无翻页游标。
