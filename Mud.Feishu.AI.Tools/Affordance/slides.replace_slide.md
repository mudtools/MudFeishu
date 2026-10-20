## WHEN

- 重写某一页（整页 replacement），或只改页内某个元素（`block_id` 填 3 位 element ID）。

## Avoid when

- 要新增一页——`slides.add_slide`；要删页——`slides.delete_slide`。
- 还没读过这一页——先 `slides.get_slide`，凭记忆写 `block_id` 必错。

## Prerequisites

- `slide_id` + `parts`（JSON 数组字符串，1~200 项，action 取 block_replace 或 block_insert）。
- 需要 `slides:presentation:update` + `slides:presentation:write_only`。

## Tips

- 整页重建时，**新 XML 里没写的内容会从页面上消失**。
- 元素级替换需要带 id 的 XML：用 `slides.get_presentation` 时不要剥属性。
- 失败看 `failed_part_index` 与 `failed_reason`，其余项可能已生效，须复核后再重试。
