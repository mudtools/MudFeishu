## WHEN

- 明确知道要在**哪一个块底下**插入一个子块（某标题下补一段、某待办下加一条）。
- 落点要精确到父块内序号，而不是文档末尾。

## Avoid when

- 只是往正文尾部续写——用 `docx.append_blocks`，父块可以缺省。
- 一次要建一整棵多层结构——用 `docx.create_descendant_blocks`，别连环插。
- 只是改已有块的文案——用 `docx.update_block`。

## Prerequisites

- `block_id` 是父块真实 ID，须与 `document_id` 同篇；它来自 `docx.get_document_blocks` / `docx.list_block_children` 或上一跳返回的新块 ID。
- 可建的块型是文本类闭集：表格、单元格、引用容器建不出来。

## Tips

- `block_type` 传错时错误信息直接列出可用值，照抄即可，别猜别名。
- `text` 落成一个纯文本段：加粗、链接、@提及这类行内内容不会保留。
- 每次调用都插一个新块；重复触发等于重复插入，跨重试固定 `idempotency_key`。
