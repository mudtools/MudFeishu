## WHEN

- 群话题里的多轮讨论要整体移交给某人，一次带齐上下文。

## Avoid when

- 目标只是话题中的某一条消息——那是 `im.forward_message`，别拿单条转发凑。
- 要读话题内容而非移交——那是 `im.get_thread_messages`。
- 要在话题里继续发言——那是 `im.reply_message`；转发建的是副本。

## Prerequisites

- `thread_id` 是 omt_ 开头的话题 ID，来自事件上下文；它不是消息 ID，传 om_ 形态的值到不了话题。
- `receive_id_type` 仅支持 open_id / user_id / union_id（缺省 open_id），非法值在入口就被拒绝。

## Tips

- 回执给出接收方侧的**新** `thread_id`；后续对该话题副本的操作要用它，不是原话题 ID。
- 不传 `idempotency_key` 时，重试会把同一话题再转一遍。
