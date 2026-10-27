## WHEN

- 用户要的是「某个文件夹里最近的一批邮件」，不带关键词。
- 需要一批 message_id 再逐封细读，或只关心未读量。

## Avoid when

- 有发件人或主题线索——用 `mail.search`，别全量列出再翻页。
- 要看主题与正文——那是 `mail.get_message`，本工具只回 ID。
- 要标签可用值——用 `mail.list_labels`；按标签筛在本域没有入参。

## Prerequisites

- `user_mailbox_id` 必填（邮箱地址形态）；`folder_id` 可选，如 INBOX / SENT / DRAFT。
- 需 mail:mailbox:readonly，以应用（tenant）身份调用。
- 宿主须启用 AddMailApi 的消息侧客户端，缺席时调用即报错。

## Tips

- `items` 每项只有一个 `message_id`，主题、发件人与时间都不回。
- 默认单页；`has_more` 为真时用 `page_token` 续翻，`fetch_all` 触顶时回 `truncated`。
- `only_unread` 是唯一的读取状态过滤，读过的邮件要靠回写状态后再筛。
