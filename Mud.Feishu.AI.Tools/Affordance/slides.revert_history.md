## WHEN

- 文稿被改坏，需要整体回到某个历史版本。

## Avoid when

- 只错了一页——`slides.get_slide` + `slides.replace_slide` 修那一页，回滚代价大得多。
- 还不知道有哪些版本——先 `slides.list_histories`。

## Prerequisites

- `history_version_id`：正整数字符串，取自 `slides.list_histories`，**不是** `revision_id`。
- 需要 `slides:presentation:update` + `slides:presentation:write_only`，且写工具已获宿主授权。

## Tips

- **整副覆盖**：回滚点之后的所有编辑（含他人改动）都会消失，先向用户确认再执行。
- 成功只代表任务已提交，须用 `slides.get_revert_status` 轮询确认落地。
