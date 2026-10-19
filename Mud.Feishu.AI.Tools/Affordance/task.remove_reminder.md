## WHEN

- 提醒设错了时点或任务已不再需要被打扰，要收回那条提醒。

## Avoid when

- 只是想加另一个时点——每个任务最多 1 个提醒，先移除再 `task.set_reminder`，别堆叠。
- 任务是整个作废——用 `task.delete_task`，删任务比删提醒影响面更大，须先确认。

## Prerequisites

- 提醒基于截止时间计算：任务必须先有 due（创建或 `task.update_task` 设置），否则没有提醒可移除。
- `reminder_ids` 取自 `task.set_reminder` 返回的 reminders[].reminder_id；`task.tasklist_tasks` 只给任务摘要，不含提醒 ID。

## Tips

- 移除不存在的提醒平台也返回成功，所以"成功"不代表移除真的发生过。
- 提醒时刻按「截止时间 − relative_fire_minute 分钟」算，改了 due 等于改了提醒时刻。
