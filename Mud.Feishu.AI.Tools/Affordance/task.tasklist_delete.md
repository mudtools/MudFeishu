## WHEN

- 清单本身作废（重复建了、项目已终结），确认不再需要这个协作面。

## Avoid when

- 想清掉清单里的任务——本工具不是批量删任务的手段，删除任务要用 `task.delete_task`。
- 想改清单名或换所有者——用 `task.tasklist_update`，删除后无法复原。
- 只是某人不该再看这个清单——用 `task.tasklist_member_remove`，代价小得多。

## Prerequisites

- `tasklist_guid` 来自 `task.tasklist_page_list`；删前用 `task.tasklist_get` 核对清单名与所有者。
- 确认清单内任务的处理方式已和相关人对齐（删除后该清单不可再访问）。

## Tips

- 删除不可恢复，也没有回收站：误删只能重建清单，成员与分组关系要重新配。
- 该工具走租户令牌面，删除的是"这个清单"，不是调用者的个人视图。
