## WHEN

- 同一段话术反复手打，要沉淀成这个邮箱日后能取的模板。
- 想把默认收件人一起固化下来，起草时不必再抄一遍地址。

## Avoid when

- 现在就要发出去——用 `mail.send_message`，新建的模板不会被自动套用。
- 已有那份要换内容——用 `mail.template_update`，别再堆一份近似的。
- 要的是来信自动分拣——用 `mail.rule_create`，模板不产生任何自动动作。

## Prerequisites

- 只有 `name` 必填；`subject`/`template_content` 可留空，但空正文的模板日后取不出内容。
- `user_mailbox_id` 是模板归属的邮箱地址；模板族走 tenant 令牌面。

## Tips

- 默认按 HTML 存正文：纯文本话术要显式把 `is_plain_text_mode` 设为 true。
- `dry_run` 的摘要只给 `subject`/`template_content` 的长度，看不出排版是否成形。
- 创建只落模板本身；用它起草要再走 `mail.template_get` 与 `mail.create_draft`。
