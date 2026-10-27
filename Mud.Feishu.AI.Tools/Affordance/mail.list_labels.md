## WHEN

- 用户用名字指代标签（「加星标的那批」「项目归档」），需要先把名字换成 ID。
- 维护或核对邮箱有哪些标签可用。

## Avoid when

- 想按标签筛邮件——读邮件的入参只有文件夹与未读两个过滤位，本域没有标签过滤入参。
- 要邮件本身——用 `mail.list_messages` 或 `mail.search`。
- 想知道某封邮件挂了哪些标签——本工具只给标签清单，不回邮件归属。

## Prerequisites

- `user_mailbox_id` 必填（邮箱地址形态）。
- 需 mail:mailbox:readonly，以应用（tenant）身份调用。
- 标签 ID 的实际消费面在草稿与写侧入参（见 `mail.create_draft`），先在此查清可用值。

## Tips

- 回执是 `labels` 数组（每项 `label_id` 与 `name`）加一个 `total`。
- 未读数与标签颜色不进结果，别把它们当可用信息。
- 本工具不做翻页，结果只反映单次调用回的内容。
