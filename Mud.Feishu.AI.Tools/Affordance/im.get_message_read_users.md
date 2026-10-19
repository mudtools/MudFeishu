## WHEN

- 要确认某条已发消息被哪些人读到、各自什么时候读的。

## Avoid when

- 想知道这条消息写了什么——那是 `im.get_message_content`。
- 想看谁回了表情而不是谁读过——那是 `im.list_reactions`。
- 要群成员全量名单——那是 `im.list_chat_members`，两者口径不同。

## Prerequisites

- `message_id`（omXxx）来自发送回执或事件上下文；本工具只接受消息 ID，不接受群 ID。
- 需 im:message:readonly，以应用（tenant）身份调用。

## Tips

- 每条只给 `user_id` / `user_id_type` / `read_time`，没有姓名，要人名得再查通讯录。
- 读取时间投影为 `read_time`，不是接口里那个时间戳字段的原名。
- 默认单页，`fetch_all` 才聚合；`max_items` 仅在 `fetch_all` 为真时生效。
