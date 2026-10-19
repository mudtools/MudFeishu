## WHEN

- 眼前这批确认无用的邮件要移出收件箱，且日后还能在邮箱里找回。

## Avoid when

- 想改的只是读没读——用 `mail.mark_read`，本工具会把邮件挪位置。
- 想删掉草稿——用 `mail.delete_draft`，草稿与已收邮件的终态不同。
- 想让"以后这类邮件"自动进删除——用 `mail.rule_create` 的 delete 动作，本工具只处理点名的 ID。

## Prerequisites

- `message_ids` 来自 `mail.list_messages`，可一次给多个；批量前先 `mail.get_message` 核对是哪几封。
- 走 tenant 令牌面，权限点是 mail:mailbox。

## Tips

- 回执只给 trashed 与条数，不逐条说明命中情况：`message_ids` 抄错同样报成功而什么都没移走。
- 移入已删除文件夹不等于永久清除，也不触碰同一会话里的其他邮件。
