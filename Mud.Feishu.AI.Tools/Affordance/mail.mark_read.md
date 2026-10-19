## WHEN

- 你已读完并处理完某封邮件，回写已读，避免用户邮箱堆积未读。
- 之前误标已读，要把这封放回未读。

## Avoid when

- 想让邮件从视野里消失——用 `mail.trash_message`，标已读不改变它的位置。
- 想清掉一份草稿——用 `mail.delete_draft`，草稿不在这工具的对象范围内。
- 想让以后的来信自动已读——用 `mail.rule_create` 的 mark_read 动作，本工具只管点名的这封。

## Prerequisites

- `message_id` 以 `mail.list_messages` 返回的 ID 为准；`user_mailbox_id` 传邮箱地址。
- 本工具是 tenant 令牌面，不需要为它单独放行 user。

## Tips

- 漏传 `read` 时按已读处理，不会反向把邮件标成未读；要放回未读必须显式给它传 false。
- 已读就是摘掉未读标记，同一封重复调用没有额外后果。
