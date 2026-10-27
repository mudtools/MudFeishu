## WHEN

- 已存在的日程要改内容或改期（标题、描述、起止时间），保留同一条记录。

## Avoid when

- 只是要加/看与会者——用 `calendar.add_event_attendees` 或 `calendar.list_event_attendees`，本工具不碰与会面。
- 日程作废——用 `calendar.delete_event`；不要用「改到很远的过去」来变相取消。
- 替自己应答邀请——那是 `calendar.rsvp`。

## Prerequisites

- `event_id` 与所属 `calendar_id` 先经 `calendar.list_events` 取准；除二者外至少再传一个要更新的字段。
- 改期时 `start` 与 `end` 必须**同时提供**且为带时区的 RFC3339（只给一个直接报错）。

## Tips

- 按字段增量更新：只有传入的字段会被改动，未传的保持原值。
- 改的是全体与会者共同可见的那条日程，不是副本；先 `dry_run` 核对下发的字段摘要再执行。
