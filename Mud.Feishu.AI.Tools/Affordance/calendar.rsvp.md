## WHEN

- 当前身份收到某日程的邀请，要**替自己**表态接受、拒绝或暂定。

## Avoid when

- 你是日程侧要改时间/标题/取消——那是 `calendar.update_event` 或 `calendar.delete_event`，与应答是两回事。
- 要给别人加进日程——用 `calendar.add_event_attendees`。
- 只是看日程详情或谁参加了——用 `calendar.list_events` 或 `calendar.list_event_attendees`。

## Prerequisites

- 需 `calendar_id` 与 `event_id` 指向那条邀请本身；`rsvp_status` 取 accept、decline、tentative 之一（下发前校验）。

## Tips

- 本工具只更新**当前身份的参与状态**这一维，不改动日程本身的内容。
- 同一日程可改口：后一次应答覆盖前一次；先 `dry_run` 确认目标再落。
