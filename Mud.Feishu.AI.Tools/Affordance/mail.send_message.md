## WHEN

- 邮件内容已在对话里确认过，一次发出，不需要留下可复查的草稿。

## Avoid when

- 要先审再发、或分批补内容——走 `mail.create_draft` 再 `mail.send_draft`。
- 要定时发——本工具没有定时入参，改用 `mail.send_draft` 并带上定时时间。
- 想沉淀这段话术反复用——用 `mail.template_create`，模板不会被自动套用。

## Prerequisites

- `user_mailbox_id` 是发件人邮箱地址；`to`/`subject`/`body` 必填，收件人只认邮箱地址。
- 需用户令牌放行（AllowedIdentities 含 user），缺席时错误指向宿主配置而非入参。

## Tips

- 内部先建草稿再发送：`dry_run` 预演的是前一步的下发摘要，看到它并不代表信已寄出。
- 收件人与 `subject` 含换行会在本地被拒（头注入防护），换行只写在 `body` 里。
- 发出后无法用它改内容，要改只能另发一封；补发前先确认上一封的返回。
