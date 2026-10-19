## WHEN

- 只有关键词、不确定消息落在哪个会话，需要跨可见会话定位消息。

## Avoid when

- 已知群 ID 要看最近聊了什么——那是 `im.get_history_messages`，按时间倒序。
- 已知消息 ID 要正文——那是 `im.get_message_content`。
- 要找群而不是找消息——那是 `im.search_chats`。

## Prerequisites

- `query` 必填，长度上限 50 字符。
- 缩小范围用 `chat_ids` / `from_ids`（字符串数组）或 `chat_type`（p2p / group）。
- 需 im:message:readonly，以应用（tenant）身份调用。

## Tips

- 命中内容是片段预览 `display_info`，按固定长度截断，全文要拿消息 ID 再回查。
- 定位信息在 `meta_data` 里：`message_id` / `chat_id` / `from_id` / `create_time`。
- `total` 是命中总数，不等于本页 `items` 的条数。
