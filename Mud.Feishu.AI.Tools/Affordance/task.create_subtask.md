## WHEN

- 一件事要拆成能分别勾选的更小步骤，且父任务已经在手上。

## Avoid when

- 父任务还不存在——先 `task.create_task`，本工具必须有父 ID。
- 只想补一段说明或留个结论——那是 `task.add_comment`，子任务是新增节点。
- 要动父任务自己的标题或时间——那是 `task.update_task`。

## Prerequisites

- `task_guid` 填的是父任务 ID（来自 `task.create_task` 或 `task.list_my_tasks`）。
- `summary` 必填；`due` 若要给，必须是带时区的 RFC3339，自然语言会被拒。

## Tips

- 子任务面上没有直接指派负责人的入参，加人要建完再走 `task.add_members`。
- 返回的是子任务自己的 GUID；重试请复用同一个 `idempotency_key`，否则会挂出两个同名子任务。
