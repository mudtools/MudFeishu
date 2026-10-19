## WHEN

- 上下文里只有话题 ID，要读这个话题串内的消息。

## Avoid when

- 要主会话或整个群的历史——那是 `im.get_history_messages`，入参是群 ID。
- 要把整条话题转走——那是 `im.forward_thread`，属写操作。
- 跨会话按关键词找消息——那是 `im.search_messages`。

## Prerequisites

- `thread_id`（omt_xxx）来自事件上下文或 `im.reply_message` 的返回。
- 需 im:message:readonly，以应用（tenant）身份调用。

## Tips

- 投影与群历史是同一套键：`message_id` / `create_time` / `sender_id` / `message_type` 加截断预览。
- 本工具没有时间区间参数，话题内取全量靠 `fetch_all` 聚合。
- `has_more` 为真时用回执的 `page_token` 续页；每页条数由绑定层固定。
