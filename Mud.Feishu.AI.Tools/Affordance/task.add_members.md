## WHEN

- 给一条已存在的任务补负责人或关注人。

## Avoid when

- 任务还没建——`task.create_task` 能一次带上负责人，别建完再补一轮。
- 要让人看到整个清单的协作面——那是 `task.tasklist_member_add`，两层的成员不是同一回事。

## Prerequisites

- `task_guid` 来自 `task.create_task` 或 `task.list_my_tasks`。
- `member_ids` 只收 open_id，姓名不是 ID，先用 `contact.search_user` 反查。
- `role` 区分负责人与关注人，不填按负责人加。

## Tips

- 相同 `idempotency_key` 至多添加一次；省略时平台不去重，重复调用会把同一批人再加一遍。
- 结果只回 added_count，看不到每个人的角色落位，改派只能再调一次。
