## WHEN

- 写入前预审：把远端当前内容与你的草稿对比，确认只改预期的那几行。
- 怀疑文档被别人改过，需要看清差异再决定是否覆盖。

## Avoid when

- 只想知道「是否变了」——`drive.get_file_metas` 的更新时间更便宜。
- 两份都不是 Markdown（表格、幻灯片）——没有行级语义，比较无意义。
- 需要逐字符差异——本工具按行比较，行内修改会整行呈现。

## Prerequisites

- 左侧必填 `left_token`；右侧二选一：`right_token`（另一份文档/文件）或 `right_text`（草稿文本）。
- 两侧各有 `token_type`（`docx` 默认 / `file`），须与 token 实际类型一致。
- 需 drive:drive:readonly。

## Tips

- 输出是有界 unified diff（默认 3 行上下文）；`truncated=true` 时看 `diff_total_length` 判断规模。
- 超大文档会退化为前后缀粗略差异（`coarse=true`），此时只能看首尾变化。
- 只读工具，不会修改任何一侧。
