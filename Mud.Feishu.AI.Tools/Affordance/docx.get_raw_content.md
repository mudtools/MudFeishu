## WHEN

- 只要全文纯文本读内容（摘要、找关键词、通读），不需要任何结构。

## Avoid when

- 要块 ID 定位编辑点——用 `docx.get_document_blocks`，本工具的回执没有 ID。
- 要可往返编辑的 Markdown——用 `markdown.fetch`（`token_type=docx`）。
- 只要标题和版本——`docx.get_document_info` 更省。

## Prerequisites

- `document_id`；知识库下的文档传 `wiki.get_node` 返回的 obj_token。
- `lang` 是整数枚举（可选，默认中文）。
- 以 tenant 身份调用，需 docx:document:readonly 权限。

## Tips

- 返回一整段纯文本：样式、图片、块边界全部丢失，不能反推块结构。
- 无分页：整篇受结果长度上限截断，疑似截断时改用块状读取核对。
