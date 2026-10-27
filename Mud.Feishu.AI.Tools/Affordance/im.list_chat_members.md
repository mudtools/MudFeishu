## WHEN

- 已拿到 chat_id，要这个群的成员 ID 名单，即谁在群里。

## Avoid when

- 只想确认当前身份在不在群里——那是 `chatgroup.is_in_chat`，只要一个布尔答案。
- 要群名、简介或群主——那是 `im.get_chat`。
- 要按姓名拿某个人的 open_id——那是 `contact.search_user`。

## Prerequisites

- `chat_id`（ocXxx）来自事件上下文或 `im.search_chats`。
- 需 im:chat:readonly，以应用（tenant）身份调用。

## Tips

- 每条只投影 `member_id` / `name` / `tenant_key`；`member_total` 是成员总数，不是本页条数。
- 回执不带 ID 类型字段，把 `member_id` 传给别的工具前先确认它是哪种口径。
- 默认单页，`has_more` 为真时用 `page_token` 续翻；`max_items` 仅在 `fetch_all` 为真时生效。
