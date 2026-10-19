## WHEN

- 手上已有 `draft_id`，要改正文/主题或补收件人，同时保留这封草稿。

## Avoid when

- 只是想把它发出去——用 `mail.send_draft`，发送之后再改不了这封。
- 这封不要了——用 `mail.delete_draft`，别用空内容覆盖。
- 还没有草稿——先 `mail.create_draft`，本工具不新建。

## Prerequisites

- `draft_id` 来自 `mail.create_draft`；`to`/`subject`/`body` 每次都要给全套。
- 与 `mail.mark_read` 那类 tenant 工具不同，本工具在用户令牌面上（须放行 user）。

## Tips

- 整段替换：不传 `cc`/`bcc` 就是这次不带抄送密送，旧值不会自动保留。
- `dry_run` 回显的路径已带 `draft_id`，可以先确认命中的是哪一封。
- 相同内容重复提交结果一致，可安全重放。
