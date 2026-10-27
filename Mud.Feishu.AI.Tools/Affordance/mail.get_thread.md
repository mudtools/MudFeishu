## WHEN

- 用户问的是「这一串往来最后谈到哪儿了」，需要线程归属与往来封数。
- 回复前先确认这条讨论线里实际有多少封邮件。

## Avoid when

- 要逐封正文——本工具只回数量与线程 ID，正文用 `mail.get_message`。
- 只有关键词、要先定位是哪封——那是 `mail.search`。
- 只想按文件夹翻最近邮件——用 `mail.list_messages`。

## Prerequisites

- `user_mailbox_id` 与 `thread_id`（形如 txxx）都必填。
- 线程 ID 在只读面上没有出口，通常来自写侧回执（`mail.send`、`mail.send`）或宿主上下文。
- 需 mail:mailbox:readonly，以应用（tenant）身份调用。

## Tips

- 命中时回 `found`、`thread_id`、`messages_count`，并附一条 `hint` 提示逐封读取。
- 未命中不报错：`found` 为 false 且原样回你传的 ID，应换 ID 而不是重试。
- 一次性展开整条会话会挤爆上下文，因此设计上只给计数。
