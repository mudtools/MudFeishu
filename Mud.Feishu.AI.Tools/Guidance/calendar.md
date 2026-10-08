
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
