## WHEN

- 日程确实作废（会议取消、误建日程），且不再需要保留这条记录。

## Avoid when

- 只是要改时间或参与人——用 `calendar.update_event`：改期同样会通知与会者，且不会留下"删了又建"的两轮通知。
- 只是想确认某个时段是否空闲——用 `calendar.find_free_slots`，不必动日程。

## Prerequisites

- 先用 `calendar.list_events` 取目标 `event_id`（确认日历归属正确）。
- 需该日历的编辑权限。

## Tips

- 取消日程**会通知所有与会者**，这条通知无法撤回。
- 建议先 `dry_run` 预演，核对 `event_id` 与日历后再执行。
