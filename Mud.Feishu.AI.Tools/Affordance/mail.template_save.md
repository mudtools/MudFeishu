## WHEN

- 同一段话术反复手打，要沉淀成这个邮箱日后能取的模板（不传 `template_id` 即新建）。
- 模板话术过时但还要留用：换主题、正文或默认收件人，保留它原来那份身份（传 `template_id` 即整段替换）。
- 想把默认收件人一起固化下来，起草时不必再抄一遍地址。

## Avoid when

- 现在就要发出去——用 `mail.send`，新建的模板不会被自动套用。
- 要另一种措辞并存——不传 `template_id` 新建，别覆盖原件。
- 这份不再需要——用 `mail.template_delete`（删了不可恢复）。
- 要的是来信自动分拣——用 `mail.rule_save`，模板不产生任何自动动作。

## Prerequisites

- 新建时只有 `name` 必填；`subject`/`template_content` 可留空，但空正文的模板日后取不出内容。
- 替换时 `template_id` 来自 `mail.template_list`，先用 `mail.template_get` 把现有字段读回来；整段替换语义下其余字段不传即被覆盖掉。
- `user_mailbox_id` 是模板归属的邮箱地址；模板族走 tenant 令牌面。

## Tips

- 默认按 HTML 存正文：纯文本话术要显式把 `is_plain_text_mode` 设为 true。
- `dry_run` 的摘要只给 `subject`/`template_content` 的长度，看不出排版是否成形；替换路径下显示为 0 意味着这次会把正文清空。
- 平台响应体不回模板标识，替换回执里的 `template_id` 就是你传的那个，不能据此确认改到了哪一份；相同内容重复提交结果一致，改坏了可以用上一步读到的原文再覆盖回去。
- 保存只落模板本身；用它起草要再走 `mail.template_get` 与 `mail.create_draft`。
