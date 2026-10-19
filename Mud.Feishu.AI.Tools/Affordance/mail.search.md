## WHEN

- 用户给的是自然语言线索（某人加上某主题），要反查是哪几封邮件。
- 需要按命中总量判断值不值得继续深挖。

## Avoid when

- 只要某文件夹的最近清单——用 `mail.list_messages`，不必起搜索。
- 已锁定 message_id 要正文——那是 `mail.get_message`。
- 要同一往来的其它邮件——用 `mail.get_thread`。

## Prerequisites

- `user_mailbox_id` 与 `query` 都必填。
- 该能力只有用户身份接口提供：需 mail:mailbox:readonly 并以用户（user）身份调用。
- 宿主须启用 AddMailApi 的 user 身份侧客户端并放行 user，缺席时调用即报错。

## Tips

- 每条只投影 `message_id` 与 `preview`，预览按固定长度截断，正文用 `mail.get_message` 取。
- 除 `has_more` 与 `page_token` 外还回 `total`，可据此判断要不要续翻。
- 结果不含线程归属与标签，跨封上下文要另外组装。
