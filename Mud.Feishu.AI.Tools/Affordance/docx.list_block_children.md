## WHEN

- 沿块树逐层下钻：列出一个父块的直接子块（不填 `block_id` 即顶层块）。

## Avoid when

- 要整篇平铺的块序列——用 `docx.get_document_blocks`，本工具一次只展开一层。
- 只看中一个已知块——用 `docx.get_block`，不必枚举。

## Prerequisites

- 起始 `block_id` 来自 `docx.get_document_blocks` 或上一轮的 `children`。
- 缺省 `block_id` 时以 `document_id` 本身作根块查询。
- 以 tenant 身份调用，需 docx:document:readonly 权限。

## Tips

- 每条块含 `block_id`/`block_type`/`text`，有子块时再带 `children`（仅 ID）。
- `block_type` 是块型名称字符串，不是数值编码。
- 默认单页，`has_more` 为真时带 `page_token` 续翻，或 `fetch_all` 在 `max_items` 预算内翻完。
