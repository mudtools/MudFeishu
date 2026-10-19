## WHEN

- 要盘点一篇文档上的讨论线索：谁发的、有没有结案、跟了多少楼。
- 准备回复或结案之前，需要先拿到 comment_id。

## Avoid when

- 要发表评论——用 `drive.add_comment`；跟帖 `drive.reply_comment`，结案 `drive.resolve_comment`。
- 想知道评论写了什么——本工具不回评论正文与被引用的原文。
- 要文档内容本身——那是 `docx.get_raw_content`。

## Prerequisites

- `file_token` 与 `file_type` 都必填（doc / docx / sheet / file / slides）。
- comment_id 只能从本工具的结果里拿。
- 需 drive:drive:readonly，以应用（tenant）身份调用。

## Tips

- 每条只投影 `comment_id`、`user_id`、`is_solved`、`reply_count`、`created_at`。
- 默认单页；`has_more` 为真时用 `page_token` 续翻，`fetch_all` 触顶回 `truncated`。
