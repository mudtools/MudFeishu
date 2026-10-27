## WHEN

- 已拿到 chat_id，要按创建时间倒序看这个群最近说了什么。

## Avoid when

- 要看某条消息的完整正文——本工具只给预览，用 `im.get_message_content`。
- 跨会话按关键词找消息——那是 `im.search_messages`。
- 消息发在话题里——仍用本工具，传 `container_type=thread` 与 `container_id=<thread_id>`。

## Prerequisites

- `container_id` 来自事件上下文或 `im.search_chats`：`container_type=chat`（默认）时为 chat_id（ocXxx），`=thread` 时为 thread_id（omt_xxx）。
- `start_time` / `end_time` 须为带时区的 RFC3339，格式不合在本地即被拒绝、不下发；话题容器不支持这两个过滤。
- 需 im:message:readonly，以应用（tenant）身份调用。

## Tips

- 每条只投影 `message_id` / `create_time` / `sender_id` / `message_type` / `content`，正文按预览长度截断。
- 话题容器（`container_type=thread`）没有时间区间参数，取全量靠 `fetch_all`；thread_id（omt_xxx）来自事件上下文或 `im.reply_message` 的返回。
- 默认单页；`has_more` 为真时带 `page_token` 续翻，或用 `fetch_all` 在预算内自动翻完。
- 每页条数由绑定层固定，参数面调不了。
