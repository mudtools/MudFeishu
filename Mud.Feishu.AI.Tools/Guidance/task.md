
# 任务（task.*）

## 路由优先级（先判断对象属于哪个域）
- 「待办」来自**任务清单** → `task.list_my_tasks`；来自**审批流程** → `approval.*`。
- 「提醒某人」若只是发消息，用 `im.*`；只有真的要建可跟踪事项才用 `task.create_task`。
- 「一批相关待办 / 项目待办池」→ 先 `task.tasklist_create` 建清单再 `task.*` 归集；单个零散待办不要建清单。
- 「找符合某条件的任务」→ `task.search_tasks`（用户身份，支持负责人/创建人/关注人/完成态/截止时间过滤），不要全量翻页再人肉筛。

## 前置链
读 `tasklist_page_list` → `tasklist_tasks` / `tasklist_get`；写 `create_task` / `update_task` / `complete_task` / `reopen` / `delete_task` / `create_subtask` / `add_comment` / `add_members`。
清单写族：`tasklist_create` → `tasklist_update` / `tasklist_delete` / `tasklist_member_add` / `tasklist_member_remove`（`tasklist_guid` 一律来自 `tasklist_page_list` 或 `tasklist_create`）。
提醒 `set_reminder` 前必须先有 `due`（提醒时刻 = 截止时间 − 提前分钟数）；`remove_reminder` 的 `reminder_ids` 来自任务详情。
创建前先 `contact.search_user` 拿 open_id（`assignee_ids` / `member_ids` 都是 open_id 列表）。

## 避坑
- `due` 用 RFC3339；`assignee_ids` 是 open_id 列表，不是姓名。
- **创建 / 子任务 / 加成员都要 `idempotency_key`**：重试时用同一值，否则会建出重复任务。
- `delete_task` 不可恢复，建议先 `dry_run`；`tasklist_delete` 同样不可恢复（但清单内任务本身不受影响）。
- 清单成员的 `type` 是 `user`（传 `open_id` 会被平台判非法）——`tasklist_member_add` 已按此固定，只需给 open_id 数组。
- 加关注人用 `task.add_members` 传 `role=follower`（本域没有独立的 followers 工具，别再找）。
- 「已完成」要改回未完成用 `task.reopen`（写 `completed_at=0`），它是 `complete_task` 的逆操作。
- 创建清单（必填/默认值）→ `feishu.guidance_read(task, creation-checklist)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
明天给张三建写周报任务 → `search_user` → `create_task`。
