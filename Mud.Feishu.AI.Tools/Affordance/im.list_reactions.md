## WHEN

- 要知道某个指定表情在一条消息上被谁、什么时候贴的。

## Avoid when

- 想知道这条消息上有哪些表情、各多少个——那是 `im.batch_query_reactions`。
- 要贴或撤表情——那是 `im.add_reaction` / `im.delete_reaction`。
- 想知道谁读过这条消息——那是 `im.get_message_read_users`。

## Prerequisites

- `message_id`（om_ 开头）来自 `im.send_message` 的回执或事件上下文。
- `reaction_type` 必填，一次只查一种表情；空白值在本地即被拒绝、不下发。
- 需 im:message（不是只读权限点），以应用（tenant）身份调用。

## Tips

- 单页返回、没有游标，只有 `returned_count`；条数等于页大小就意味着仍有明细未取到。
- `user_id_type` 只决定回复者 ID 的口径，默认 open_id，非法值在本地被拒绝。
- 明细键是 `reaction_id` / `emoji_type` / `operator_id` / `operator_type` / `action_time`。
