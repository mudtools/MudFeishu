## WHEN

- 草稿内容已确认作废（写错收件人、需求已取消），且不需要再回看正文。

## Avoid when

- 只是想改内容或补附件——用 `mail.update_draft`，删了就得重头再写。
- 打算发出去——用 `mail.send_draft`，发送不等于删除。
- 还想留一份做参照——删除后草稿不可恢复。

## Prerequisites

- `draft_id` 来自 `mail.create_draft` 的返回；`user_mailbox_id` 用发件人邮箱地址。
- 先用 `dry_run=true` 预演，确认命中的是自己那封草稿。

## Tips

- 草稿是用户令牌面：身份未在 AllowedIdentities 放行 user 时会直接报权限错误。
- 删除只作用于草稿本身，不影响同一会话里已发送的邮件。
