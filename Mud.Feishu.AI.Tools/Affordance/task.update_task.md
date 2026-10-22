## WHEN

- 已存在的任务要改标题、描述或截止时间。

## Avoid when

- 任务当初就不该存在——那是 `task.delete_task`，改状态不会让它消失。
- 要留一句说明或结论——那是 `task.add_comment`，覆盖描述会丢掉原文。
- 要换人或加人——那是 `task.add_members`，本工具没有成员入参。

## Prerequisites

- `task_guid` 来自 `task.create_task` 或 `task.list_my_tasks`。
- `summary`、`description`、`due`、`completed` 至少要给一个，全空会被直接拒；空标题等于没给，不构成一次更新。

## Tips

- 按字段更新：只有传了的字段被写，没传的保持原值，重复提交同一个值结果一致。
- `completed=true` 写当前完成时刻、`false` 清空完成时间（重新打开）；写的是 `completed_at` 字段。
- 改 `due` 会连带移动已设提醒的触发时刻，改完不必再重设提醒。
