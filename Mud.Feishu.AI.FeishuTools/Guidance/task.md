任务（task.*）：`task.create_task` 创建任务（due 用 RFC3339，工具层负责转毫秒；assignee_ids 是 open_id 列表；idempotency_key 防重复创建）。`task.list_my_tasks` 读「我负责的」任务，依赖用户令牌与当前用户身份。
