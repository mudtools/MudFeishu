## WHEN

- 要看群公告当前的内容：公告按文档块形态存储，逐块读取其结构与文本预览。

## Avoid when

- 读普通文档正文——那是 `docx.get_document_blocks` 一族，本工具只认群公告。
- 要改公告文本——用 `docx.set_chat_announcement`，文档侧块写工具不适用于公告。

## Prerequisites

- `chat_id`（ocXxx）来自 `chatgroup.list_chats` 或 `im.search_chats`。
- 以 tenant 身份调用，需 docx:document:readonly 权限。

## Tips

- `items` 每条含 `block_id`/`block_type`/`text`，有子块时再带 `children`；`block_type` 是块型名称字符串。
- 要更新的块用这里的 `block_id` 定位传给更新工具；`text` 是截断预览，不能当公告全文引用。
- 默认单页，`has_more` 为真时带 `page_token` 续翻，或 `fetch_all` 自动翻完。
