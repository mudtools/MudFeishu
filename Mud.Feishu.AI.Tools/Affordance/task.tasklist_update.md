## WHEN

- 清单要改名：项目更名、口径调整、重名不好区分。

## Avoid when

- 想动清单的人——那是 `task.tasklist_member_add` 与 `task.tasklist_member_remove`。
- 清单整体作废——那是 `task.tasklist_delete`；改名可逆，删除不可恢复。
- 想改清单里某条任务的内容——那是任务层的 `task.update_task`。

## Prerequisites

- 清单 GUID 来自 `task.tasklist_page_list`，可先用 `task.tasklist_get` 核对现名。
- `name` 必填且最长 100 个字符；空值不是一次重命名。

## Tips

- 只下发名称这一个字段的更新，清单的其他属性在这里改不动。
- 同名重复提交结果一致；改名不动清单内的任务与成员关系。
