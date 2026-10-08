# 日历（calendar.*）

## 前置链
`find_free_slots` 查空闲 → `create_event`；`list_events` / `list_event_attendees` 查。

## 避坑
- **时间用带时区 RFC3339**，不要自然语言或毫秒。
- 多人**逐人**调用 `find_free_slots`。
- `delete_event` 会通知所有与会者。

## 示例
找共同空档 → 逐人查 → 取交集 → `create_event`。