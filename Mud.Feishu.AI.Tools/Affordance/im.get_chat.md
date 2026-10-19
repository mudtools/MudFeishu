## WHEN

- 已拿到 chat_id，要看这个群的名称、简介、人数与群主归属。

## Avoid when

- 只有群名片段、还没有 ID——先用 `im.search_chats` 反查。
- 要成员名单——那是 `im.list_chat_members`，本工具只给计数。
- 只想确认当前身份在不在群里——那是 `chatgroup.is_in_chat`。

## Prerequisites

- `chat_id`（ocXxx）来自事件上下文或 `im.search_chats`。
- 需 im:chat:readonly，以应用（tenant）身份调用。
- 依赖宿主注册群管理能力，缺席时本工具不可用。

## Tips

- 回执把入参 `chat_id` 原样回显，其余只有 `name` / `description` / `user_count` / `owner_id` / `owner_id_type`。
- 单次调用、无翻页；`user_count` 是计数，不能用它判断某个人在不在群里。
