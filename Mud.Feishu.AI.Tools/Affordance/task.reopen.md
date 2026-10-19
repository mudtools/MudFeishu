## WHEN

- 已完成的任务又要重做（验收没过、情况变了），要把它拉回未完成态。

## Avoid when

- 任务当初就是错建的——那是 `task.delete_task`；重开会留下一条"完成过"的记录。
- 只想改内容或时间——那是 `task.update_task`。
- 目标是让它做完——那是 `task.complete_task`，两者互为逆操作。

## Prerequisites

- `task_guid` 来自 `task.create_task` 或 `task.list_my_tasks`。
- 只对已完成的任务有意义；对未完成任务重复调用不产生副作用。

## Tips

- 重开只写"完成时间为 0"这一个字段，评论、成员、截止时间都原样保留。
- 它与 `task.complete_task` 打的是同一个更新入口，方向相反，`dry_run` 能看清落的是哪个字段。
