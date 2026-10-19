## WHEN

- 已存在的任务要改标题、描述或截止时间。

## Avoid when

- 要改的是完成态——那是 `task.complete_task` / `task.reopen`，别在这里手写状态。
- 要留一句说明或结论——那是 `task.add_comment`，覆盖描述会丢掉原文。
- 要换人或加人——那是 `task.add_members`，本工具没有成员入参。

## Prerequisites

- `task_guid` 来自 `task.create_task` 或 `task.list_my_tasks`。
- `summary`、`description`、`due` 至少要给一个，全空会被直接拒；空标题等于没给，不构成一次更新。

## Tips

- 按字段更新：只有传了的字段被写，没传的保持原值，重复提交同一个值结果一致。
- 改 `due` 会连带移动已设提醒的触发时刻，改完不必再重设提醒。
