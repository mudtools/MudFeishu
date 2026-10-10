# 任务创建清单

## 前置链
1. `contact.search_user` 拿负责人 open_id（`assignee_ids` 不接受姓名）
2. `task.create_task` 传 `summary` + `assignee_ids` + 可选 `due` / `description`
3. 需要子任务 / 评论 / 成员时再分别 `create_subtask` / `add_comment` / `add_members`

## 避坑
- **必填只有 `summary` 与 `assignee_ids`**；其余（`due`、`description`、关注人）省略即用平台默认，
  不要为「填满」而臆造值。
- `due` 必须是 RFC3339（带时区）；毫秒时间戳与自然语言都会被拒。
- **创建 / 子任务 / 加成员都要 `idempotency_key`**：同一次意图重试必须复用同一值，
  否则会建出重复任务（这是本项目里最贵的错误类型——用户会看到两条一样的待办）。
- 想标记完成用 `complete_task`（走更新 `completed_at`），不要用 `update_task` 手写状态字段。
- 删任务前先 `dry_run=true` 确认对象：`delete_task` 不可恢复。

## 示例
- 「明天给张三建写周报任务」→ `search_user` → `create_task(assignee_ids=[open_id], due=<RFC3339>)`
- 「再补个子任务」→ `create_subtask`（复用同一 `idempotency_key` 语义：一次意图一个键）