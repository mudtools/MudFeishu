## WHEN

- 草稿已定稿要发出；或要在指定时刻发出而不是现在。

## Avoid when

- 内容还要改——先 `mail.update_draft`，发出这一步不可回退。
- 不需要留草稿、当场写完当场发——用 `mail.send_message`。
- 这封不发了——用 `mail.delete_draft`，别让它留在草稿箱等着被再次发出。

## Prerequisites

- `draft_id` 来自 `mail.create_draft`。
- `send_time` 只在定时时用：必须带时区（+08:00 或 Z 这类偏移）且至少晚于当前 5 分钟；不传即立即发送。

## Tips

- `send_time` 的格式与下限校验发生在调用下游之前，`dry_run` 预演同样会被拦下报错。
- 平台可用自动发送禁用策略拦住这封信（返回码看着是成功），本工具把它转成失败并附原因，别当成已送达。
- 发出后这封邮件即按已发送处置，`mail.delete_draft` 不再适用。
