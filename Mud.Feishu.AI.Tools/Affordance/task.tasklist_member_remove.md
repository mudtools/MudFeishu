## WHEN

- 人员转岗或项目结束，某人不该再看到这个清单及其任务。

## Avoid when

- 想降权而不是撤掉（owner → viewer）——本工具只负责移除，降权要用 `task.tasklist_member_add` 以新角色重加，先确认顺序再动手。
- 想连清单一起消失——用 `task.tasklist_delete`，逐个撤成员是多余动作。

## Prerequisites

- 先用 `task.tasklist_get` 取当前成员与角色，核对要移除的 `member_ids`（open_id 数组）。
- `member_ids` 的 ID 类型须与 `user_id_type`（默认 open_id）一致，混用会静默移不到人。

## Tips

- 移除只断掉这个清单的协作关系，成员名下的任务本身不受影响。
- 一次可传多个 open_id：批量撤人比逐次调用少一轮往返，但失败时定位也成倍放大。
