# 任务（task.*）

## 前置链
`list_my_tasks` 读我负责的；写 `create_task` / `update_task` / `complete_task` / `delete_task` / `create_subtask` / `add_comment` / `add_members`。

## 避坑
- `due` 用 RFC3339；`assignee_ids` 是 open_id 列表。
- **创建 / 子任务 / 加成员都要 idempotency_key**。
- `delete_task` 不可恢复，建议先 `dry_run`。

## 示例
明天给张三建写周报任务 → `search_user` → `create_task`。