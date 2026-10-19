## WHEN

- 只有**一个块**的文案要换（改标题措辞、订正一条待办）。
- 需要能安全重发：同参数重放不会叠出第二份效果。

## Avoid when

- 一批块一起改——用 `docx.update_blocks`，一次下发。
- 要在块底下加内容——用 `docx.create_block`；改文本不改变结构。
- 整篇换稿——用 `docx.replace_document`。

## Prerequisites

- `block_id` 与 `document_id` 必须同篇，且是**当前** ID：用 `docx.get_block` / `docx.get_document_blocks` 现取，别用上一轮记住的旧 ID。
- 目标块得是文本类块；表格与单元格不在这条路上。

## Tips

- `text` 整体替换块的文本元素，原有的加粗、链接、@提及等行内内容会被抹平成一个纯文本段。
- 改不动块类型和层级；要换形态就得删掉后用 `docx.create_block` 重建。
- 返回带文档版本号，可与 `docx.get_document_info` 对账确认改动落地。
