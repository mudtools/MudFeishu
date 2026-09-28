日历（calendar.*）：时间一律用带时区的 RFC3339（如 `2026-10-01T14:00:00+08:00`），不要用自然语言或毫秒。`calendar.find_free_slots` 一次只查一个用户或一间会议室，多人请逐人调用；`calendar.create_event` 是写操作（默认不启用），同一业务意图复用同一个 idempotency_key 可防重复创建。
