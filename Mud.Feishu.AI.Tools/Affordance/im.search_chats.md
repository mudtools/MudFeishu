## WHEN

- 用户只记得群名的片段，需要用关键词反查是哪个群。

## Avoid when

- 已经拿到 chat_id 要群详情——那是 `im.get_chat`，本工具不回人数与群主。
- 要机器人在的群清单而不是按词匹配——那是 `chatgroup.list_chats`。
- 找的是消息内容而不是群——那是 `im.search_messages`。

## Prerequisites

- `query`（群名片段）必填。
- 需 im:chat:readonly，以应用（tenant）身份调用。
- 依赖宿主注册群管理能力，缺席时本工具不可用。

## Tips

- 每条只投影 `chat_id` / `name` / `description`，人数、群主与群设置都不在回执里。
- 默认单页，`has_more` 为真时用 `page_token` 续翻，`fetch_all` 才会自动翻完。
