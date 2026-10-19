## WHEN

- 本 Bot 刚发的文本消息有错字或错误数据，就地纠正：对方看到的是更新，不是刷屏。

## Avoid when

- 原消息是卡片——那是 `im.patch_card_message`，本工具固定按文本类型下发。
- 要让消息彻底消失——那是 `im.revoke_message`。
- 是要补充新内容而不是改旧消息——那是 `im.send_message`。

## Prerequisites

- `message_id` 必须是本 Bot 已发送的文本消息，来自 `im.send_message` 回执或事件上下文；他人的消息改不了。

## Tips

- PUT 整段覆盖、天然幂等：重复提交相同 `text` 结果一致，重试安全。
- 回执只回 message_id；要核实生效，用 `im.get_message_content` 回查。
- 拿不准目标消息时先 `dry_run` 预演，只回 method/path 与长度摘要，不触碰下游。
