## WHEN

- 事情真的做完了，要让任务进入完成态、从待办里收掉。

## Avoid when

- 任务当初就不该存在——那是 `task.delete_task`；完成保留记录，删除不可恢复。
- 只想改标题或截止时间——那是 `task.update_task`。
- 做完又要重做——用 `task.update_task` 传 `completed=false`，两者互为逆操作。

## Prerequisites

- `task_guid` 来自 `task.create_task`、`task.list_my_tasks` 或 `task.tasklist_tasks`。
- 只需任务 ID：完成时间由工具层注入，模型给不了时间点。

## Tips

- 对已完成的任务重复调用不会把它改回未完成，只刷新完成时间戳。
- 想指定一个过去的完成时刻做不到，本工具面没有对应入参。
