## WHEN

- 已锁定某封邮件，要它的主题、收发件人与正文预览来回话。
- 决定要不要继续深挖（整条往来或附件）之前的单封核实。

## Avoid when

- 只有关键词、不知是哪封——先 `mail.search` 或 `mail.list_messages`。
- 要同一往来里其它邮件的全貌——用 `mail.get_thread`，本工具只看一封。
- 要取附件——用 `mail.get_attachment_url`，本回执不带附件字段。

## Prerequisites

- `message_id` 由 `mail.list_messages` 或 `mail.search` 给出；`user_mailbox_id` 是邮箱地址。
- 需 mail:mailbox:readonly，以应用（tenant）身份调用。
- 宿主须启用 AddMailApi 的消息侧客户端，缺席时调用即报错。

## Tips

- 投影固定：`subject`、`from`、`to`、`cc`、`body_preview`、`internal_date`、`folder_id`，收件人项内只有 `address`。
- `format` 只改下发请求，正文全文不进结果，读到的仍是预览。
- 查不到时回一个空 `message` 而非报错；密送与标签都不回。
