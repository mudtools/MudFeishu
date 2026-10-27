## WHEN

- 往已有文档正文续写一段或一批顶层块（补一节说明、追加记录）。

## Avoid when

- 要塞进某个块**底下**——`parent_block_id` 填该块 ID；结构化子树（多层级、带编号/嵌套）用 `docx.create_descendant_blocks`。
- 要改已有块的文案——用 `docx.update_blocks`，不是追加。
- 整篇换成新稿——用 `docx.replace_document`，别追加完再手删旧块。
- 只想先看 Markdown 会落成哪些块——用 `docx.import_markdown`（它不写入）。

## Prerequisites

- `document_id` 来自 `docx.create_document` 或已有文档；根块 ID 就等于它，不填 `parent_block_id` 即追加到顶层。
- 要定落点时 `index` 按执行时的子块序号算，先 `docx.list_block_children` 看现状。

## Tips

- `blocks` 与 `block_type`+`text` 互斥：给了 `blocks` 时单块简写被忽略，单次上限 50 块。
- 每块内容落成一个纯文本段，Markdown 式加粗/链接不生效。
- 重发复用同一 `idempotency_key`，否则整批内容会再插一遍。
