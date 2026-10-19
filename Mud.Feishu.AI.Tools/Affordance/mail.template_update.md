## WHEN

- 模板话术过时但还要留用：换主题、正文或默认收件人，保留它原来那份身份。

## Avoid when

- 这份不再需要——用 `mail.template_delete`（删了不可恢复）。
- 要另一种措辞并存——用 `mail.template_create` 新建，别覆盖原件。
- 想按它发信——先 `mail.template_get` 读正文，再走 `mail.create_draft`。

## Prerequisites

- `template_id` 来自 `mail.template_list`；改前先用 `mail.template_get` 把现有字段读回来。
- `name` 必填，整段替换语义下其余字段不传即被覆盖掉。

## Tips

- 平台响应体不回模板标识，回执里的 `template_id` 就是你传的那个，不能据此确认改到了哪一份。
- `dry_run` 摘要给出 `template_content` 的长度：显示为 0 意味着这次会把正文清空。
- 相同内容重复提交结果一致，改坏了可以用上一步读到的原文再覆盖回去。
