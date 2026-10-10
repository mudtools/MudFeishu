---
name: feishu-calendar
description: 飞书 calendar 域工具集：7 个工具（只读 3 / 写 4）。日程 / 会议室忙闲 → `calendar.*`；任务截止时间 → `task.*`。日程是他人可见的邀请，任务是我的待办。
version: 1.0.0
---

# 日历（calendar.*）

## 路由优先级（先判断对象属于哪个域）
- **日程 / 会议室忙闲** → `calendar.*`；**任务截止时间** → `task.*`。日程是他人可见的邀请，任务是我的待办。
- 「约个会」多数要先 `find_free_slots` 而不是直接 `create_event`。

## 前置链
`find_free_slots` 查空闲 → `create_event`；`list_events` / `list_event_attendees` 查；与会者用 `add_event_attendees`。

## 避坑
- **时间用带时区 RFC3339**（如 `2026-10-01T14:00:00+08:00`），不要自然语言或毫秒时间戳。
- 多人**逐人**调用 `find_free_slots`（接口不接受用户列表），再取交集。
- `delete_event` 会通知所有与会者，属不可静默的副作用。
- 时区与忙闲语义 → `feishu.guidance_read(calendar, free-busy)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
找三人共同空档 → 逐人 `find_free_slots` → 取交集 → `create_event`。

## 命令（工具）清单
- `calendar.add_event_attendees`（写）：向指定日程添加与会者（attendee_ids 为 open_id 数组，每位参会人会收到日程邀请）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。
- `calendar.create_event`（写）：在指定日历上创建一个日程（start/end 为 RFC3339 时间，如 2026-10-01T14:00:00+08:00）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。
- `calendar.delete_event`（写）：取消（删除）指定日程——取消日程会通知所有与会者。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。建议先 dry_run 预演确认。
- `calendar.find_free_slots`（只读）：查询一个用户主日历或一间会议室在指定时间窗内的忙闲（time_min/time_max 为 RFC3339；user_id 与 room_id 二选一）。多人场景请逐人调用。只读，需 calendar:calendar:readonly。
- `calendar.list_event_attendees`（只读）：分页列出指定日程的与会者（attendee_id/name/type/is_optional），可翻页。只读，需 calendar:calendar:readonly。
- `calendar.list_events`（只读）：列出指定日历上的日程（按开始时间返回，含 summary/start/end/event_id），可翻页。只读，需 calendar:calendar:readonly。
- `calendar.update_event`（写）：更新指定日程的信息（summary/description/start/end 等字段，至少传一个要更新的字段）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。start/end 为 RFC3339 时间。

## 参考文档（细则）
- `references/free-busy.md`：`calendar/free-busy`
