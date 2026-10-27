## WHEN

- 邮件要先留一份可复查的成品，或分几次补齐内容，暂不发出。
- 创建成功后拿到的 draft_id 交给 `mail.update_draft` 或 `mail.send` 接力。

## Avoid when

- 内容已确认、发完就走——用 `mail.send`，不必自己管中间态。
- 已有草稿要改——用 `mail.update_draft`，别建第二份近似草稿。
- 想把这段话说以后反复用——用 `mail.template_save`，草稿不会被复用。

## Prerequisites

- `user_mailbox_id` 传发件人的邮箱地址；`to`/`subject`/`body` 三项必填。
- 草稿族走用户令牌面：宿主未在 AllowedIdentities 放行 user 时报的是身份问题，改参数无用。

## Tips

- `dry_run` 只回显 `to`/`cc`/`bcc` 的条数与 `subject`/`body` 的长度，不外泄正文。
- 收件人与 `subject` 里出现换行会在下发前被拒（头注入防护），排版换行只能写进 `body`。
- 本工具没有附件入参；创建成功也不会有任何东西送达 `to`。
