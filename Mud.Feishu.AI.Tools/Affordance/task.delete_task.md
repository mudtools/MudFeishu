## WHEN

- 任务重复创建或已确认作废，需要彻底移除。

## Avoid when

- 任务只是做完了——用 `task.complete_task`，那才是自然终态且保留完成记录。
- 只是不想让它出现在我的列表里——完成或改负责人比删除更轻。

## Prerequisites

- 先用 `task.list_my_tasks` 取 `task_guid`（或使用 `task.create_task` 返回的 `task_guid`）。
- 需任务写权限。

## Tips

- 删除后任务**无法再被获取**，其子任务与评论一并不可访问。
- 删除**不可恢复**；建议先 `dry_run` 预演确认 `task_guid`。
