## WHEN

- 改某一页之前先读它的**完整** XML——整页重建与元素级替换都以它为输入。

## Avoid when

- 还不知道有哪些页——先 `slides.list_slides`。
- 要看整份结构与页序——`slides.get_presentation`。

## Prerequisites

- `slide_id` 与 `slide_number` 二选一（页码从 1 起）；同传或都不传会在本地被拒绝。
- 需要 `slides:presentation:read`。

## Tips

- 单页 XML **不截断**：截断过的片段回填给平台会静默丢内容。
- 页内 3 位 short element ID 只有本工具返回的 XML 里才有，元素级替换需要它。
