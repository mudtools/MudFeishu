## WHEN

- 块级修改前的第一步：读整篇文档的块序列——ID、类型、文本预览。

## Avoid when

- 只想通读内容——`docx.get_raw_content` 更省，这里每条文本只是预览。
- 要下钻某块的子块——用 `docx.list_block_children`；本工具的回执不带子块关系。

## Prerequisites

- `document_id`；知识库下的文档传 `wiki.get_node` 返回的 obj_token。
- 以 tenant 身份调用，需 docx:document:readonly 权限。

## Tips

- `items` 每条只有 `block_id`/`block_type`/`text`：`block_type` 是数值编码，与单块/子块读法的块型名称形态不同。
- `text` 只拼接文本元素并做截断，图片、提及等内容不体现在预览里。
- 默认单页，`has_more` 为真时带 `page_token` 续翻，或 `fetch_all` 在 `max_items` 预算内翻完。
