## WHEN
- 群里某条关键消息要让所有人持续看到，不能被后续聊天刷走。
- 群公告已经写好，需要把它钉在群顶部，保证后进群的人也看得到。

## Avoid when
- 要取消现有置顶——用 `chatgroup.delete_top_notice`。
- 要改公告内容本身——那是 `docx.set_chat_announcement`，本工具只决定顶部挂什么。
- 要把新内容发进群——用 `im.send_message`；置顶不产生消息。

## Prerequisites
- `action_type`=1 必须配已有的 `message_id`（从发送或回复消息的回执里取）；=2 不带消息 ID，但要求群已开通公告。
- `chat_id` 必填。

## Tips
- 漏配是硬错误：`action_type`=1 而不给 `message_id` 会被直接拦下，连 `dry_run` 都过不去。
- 置顶对全群成员可见，改动即刻生效。
- 想撤下时用 `chatgroup.delete_top_notice`，被置顶的消息本身不会丢。
