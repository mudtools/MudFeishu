## WHEN

- 要快速表达"收到/同意"，用表情比再发一条消息更克制。

## Avoid when

- 想撤掉消息本身——那是 `im.revoke_message`，表情与消息是两个对象。
- 要删的是已贴错的表情——那是 `im.delete_reaction`。
- 不确定这表情贴过没有——先 `im.list_reactions` 看明细，平台不去重。

## Prerequisites

- `message_id` 是 om_ 开头的目标消息，来自事件上下文或 `im.get_history_messages`。
- `emoji_type` 是平台表情名（如 THUMBSUP），不是任意文字；空值会在入口被拒绝、不下发。

## Tips

- 本工具刻意没有幂等参数：同一表情调两次会在同一条消息上叠成两条。
- reaction_id 只出现在本次回执里，当场记下，之后删除要用它；错过只能靠 `im.batch_query_reactions` 重查明细。
