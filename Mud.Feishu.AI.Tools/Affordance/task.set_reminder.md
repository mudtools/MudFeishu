## WHEN

- 需要一条任务在截止前把人叫醒一次。

## Avoid when

- 任务还没有截止时间——提醒时刻完全由截止时间倒推，没截止时间就没有可设的提醒。
- 想再加一个时点——每个任务最多 1 个提醒，先 `task.remove_reminder` 再设，别堆叠。
- 只是想通知某人一句——发消息更轻，提醒不会离开任务。

## Prerequisites

- `task_guid` 来自 `task.create_task` 或 `task.list_my_tasks`，且该任务已设过截止时间。
- `relative_fire_minute` 是提前分钟数（30 即截止前半小时），不是时刻。

## Tips

- 响应里的 reminders 带的 ID 是 `task.remove_reminder` 唯一的 ID 来源，别丢掉这次返回。
- 本工具无幂等键；事后改截止时间等于改提醒时刻，不必重设。
