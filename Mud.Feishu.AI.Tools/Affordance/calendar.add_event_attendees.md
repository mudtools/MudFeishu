## WHEN

- 日程已存在，组织者要**新增受邀人**（每位被加入者都会收到日程邀请）。

## Avoid when

- 查已有参会名单——用 `calendar.list_event_attendees`，加人不回名单。
- 改时间/标题/描述——那是 `calendar.update_event`，与会者不在它的字段面里。
- 代参会人表态接受或拒绝——那是 `calendar.rsvp`，操作主体是受邀人本人而非日程侧。

## Prerequisites

- `event_id` 来自 `calendar.create_event` 或 `calendar.list_events`，并确认日历归属正确。
- `attendee_ids` 只接受成员 open_id 形态（工具固定按 user 类型下发）。

## Tips

- 加人**逐位发通知**，副作用直达个人；名单拿不准时先 `dry_run` 预演核对。
- 返回只是新增计数；个别人是否真的进席，回头用 `calendar.list_event_attendees` 复核。
