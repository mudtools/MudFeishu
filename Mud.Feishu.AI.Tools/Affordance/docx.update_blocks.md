## WHEN

- 同一篇文档里**若干离散块**的文案要一起改（术语统一替换、多处订正）。
- 手上已有一批块 ID，改动只涉及文本。

## Avoid when

- 只有一块——用 `docx.update_block`。
- 按位置清掉一段——那是 `docx.delete_blocks` 的索引区间语义；本工具按块 ID 定位，与顺序无关。
- 整篇换稿——用 `docx.replace_document`。

## Prerequisites

- `document_id` 配每块的块 ID，两者取自同一篇的 `docx.get_document_blocks` 或 `docx.get_raw_content`。
- `blocks` 每项必须同时给块 ID 与新文本；单次上限 200 条，超出要拆批。

## Tips

- 校验发生在调用下游之前：任何一条不合法就整批拒绝，不会留下改了一半的文档。
- 只暴露改文本这一个面，表格结构与富样式改不动。
- 返回只有成功条数、没有逐块回执，重要的批量改动事后复读一次确认。
