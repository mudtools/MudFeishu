## WHEN

- `slides.revert_history` 之后确认回滚是否真的落地（回滚是异步任务）。

## Avoid when

- 还没提交回滚——先 `slides.revert_history`（可 `dry_run` 预演）。
- 想挑回滚目标——`slides.list_histories`。

## Prerequisites

- `task_id` 来自 `slides.revert_history` 的返回。
- 需要 `slides:presentation:read`。

## Tips

- 按返回的 `poll_after_ms` 间隔轮询，不要连续空轮。
- `partial_failed` 时看 `failed_block_tokens`：文稿已不是目标版本，须如实转达。
- 「调用成功」不等于「已回滚完成」，别据此向用户宣布完成。
