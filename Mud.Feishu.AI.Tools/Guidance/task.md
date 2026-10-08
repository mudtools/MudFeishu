
# 任务（task.*）

## 路由优先级（先判断对象属于哪个域）
- 「待办」来自**任务清单** → `task.list_my_tasks`；来自**审批流程** → `approval.*`。
- 「提醒某人」若只是发消息，用 `im.*`；只有真的要建可跟踪事项才用 `task.create_task`。

## 前置链
`list_my_tasks` 读我负责的；写 `create_task` / `update_task` / `complete_task` / `delete_task` / `create_subtask` / `add_comment` / `add_members`。
创建前先 `contact.search_user` 拿 open_id（`assignee_ids` 是 open_id 列表）。

## 避坑
- `due` 用 RFC3339；`assignee_ids` 是 open_id 列表，不是姓名。
- **创建 / 子任务 / 加成员都要 `idempotency_key`**：重试时用同一值，否则会建出重复任务。
- `delete_task` 不可恢复，建议先 `dry_run`。
- 创建清单（必填/默认值）→ `feishu.guidance_read(task, creation-checklist)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
明天给张三建写周报任务 → `search_user` → `create_task`。
