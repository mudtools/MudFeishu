## WHEN

- 日历与时间窗都已确定，要把一条**新日程**落上日历。

## Avoid when

- 「约个会」通常先查空档——逐人 `calendar.find_free_slots` 取交集，再考虑创建。
- 日程已存在、只是时间或内容变了——那是 `calendar.update_event`，不要删了重建（`calendar.delete_event` 会向全体发取消通知）。

## Prerequisites

- `calendar_id` 是目标日历；`start`/`end` 必须是带时区的 RFC3339（end 晚于 start，在下发前校验）。

## Tips

- 本工具不接受与会者：建完再用 `calendar.add_event_attendees` 补人。
- 可能被重复提交时用 `idempotency_key`（稳定值，勿用随机数）：同键在同一应用与日历维度至多创建一次。
- `timezone`（默认 Asia/Shanghai）只兜底无时区偏移的时间；跨时区排期请自带偏移。
