## WHEN

- 某一页确认作废，需要从文稿中移除。

## Avoid when

- 只是内容错了——`slides.replace_slide` 改写它，别删。
- 想撤销误删——平台无回收站，只能 `slides.revert_history` 整副回滚，且会连带抹掉其后的编辑。

## Prerequisites

- `slide_id` 必须来自 `slides.list_slides` 的实际结果，不要凭页码猜测。
- 需要 `slides:presentation:update` + `slides:presentation:write_only`，且该高危写工具已获宿主授权。

## Tips

- **高危写、不可恢复**：调用前先 `dry_run=true` 逐字核对 `slide_id`。
- 一次只删一页，批量清理请逐页确认后再调用。
