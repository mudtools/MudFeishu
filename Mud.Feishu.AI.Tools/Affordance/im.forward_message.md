## WHEN

- 要把一条已有消息**原样**递话给会话外的某人，不重写内容。

## Avoid when

- 对方需要整段讨论的上下文——那是 `im.forward_thread`，单条转发会丢话题串。
- 要在原消息下生成一条新回复——那是 `im.reply_message`，转发不产生新内容。

## Prerequisites

- `message_id` 是 omXxx 形态，来自事件上下文或 `im.search_messages` 的返回。
- `receive_id` 与 `receive_id_type` 必须配对（open_id / user_id / union_id，缺省 open_id）；非法类型提前拒绝。按姓名找人先 `contact.search_user`。

## Tips

- 转生在接收方产生的是一条**新** message_id，原消息不动；对转发副本的后续操作要用回执里的新 ID。
- 重试场景给稳定 `idempotency_key`：相同键不会重复转发。
