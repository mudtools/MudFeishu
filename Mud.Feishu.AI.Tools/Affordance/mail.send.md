## WHEN

- 已有一封存好的草稿要发出；或要在指定时刻发出而不是现在。
- 内容已在对话里确认过，一次发出，不需要留下可复查的草稿（不传 `draft_id`，直接给 `to`/`subject`/`body`）。

## Avoid when

- 内容还要改——先 `mail.update_draft`（或在现写现发前改好入参），发出这一步不可回退。
- 只想把内容存起来备查、暂不发出——用 `mail.create_draft`。
- 这封不发了——用 `mail.delete_draft`，别让它留在草稿箱等着被再次发出。
- 想沉淀这段话术反复用——用 `mail.template_save`，模板不会被自动套用。

## Prerequisites

- `user_mailbox_id` 是发件人邮箱地址；`draft_id` 来自 `mail.create_draft`。
- 两种用法**互斥**：给 `draft_id` 就不要再给 `to`/`subject`/`body`/`cc`/`bcc`（要改内容先 `mail.update_draft`）；不给 `draft_id` 则 `to`/`subject`/`body` 必填。
- `send_time` 只在定时时用：必须带时区（+08:00 或 Z 这类偏移）且至少晚于当前 5 分钟；不传即立即发送。
- 需用户令牌放行（AllowedIdentities 含 user），缺席时错误指向宿主配置而非入参。

## Tips

- 现写现发是"内部先建草稿再发送"：`dry_run` 预演的是建草稿这一步的下发摘要，看到它并不代表信已寄出。
- `send_time` 的格式与下限校验发生在调用下游之前，`dry_run` 预演同样会被拦下报错。
- 平台可用自动发送禁用策略拦住这封信（返回码看着是成功），本工具把它转成失败并附原因；现写现发分支会额外回传已建草稿的 `draft_id`，别当成已送达。
- 收件人与 `subject` 含换行会在本地被拒（头注入防护），换行只写在 `body` 里。
