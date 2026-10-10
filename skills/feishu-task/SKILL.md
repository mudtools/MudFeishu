---
name: feishu-task
description: 飞书 task 域工具集：8 个工具（只读 1 / 写 7）。「待办」来自任务清单 → `task.list_my_tasks`；来自审批流程 → `approval.*`。
version: 1.0.0
---

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

## 命令（工具）清单
- `task.add_comment`（写）：为指定任务添加评论（content 必填，最长 3000 个 utf8 字符）。可通过 reply_to_comment_id 回复已有评论。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。
- `task.add_members`（写）：向指定任务添加成员（负责人或关注人，member_ids 为 open_id 数组，role 指定角色 assignee 或 follower）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。
- `task.complete_task`（写）：将任务标记为已完成（通过 update_task 设置 completed_at 字段）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。
- `task.create_subtask`（写）：为指定父任务创建子任务（summary 必填；接口功能除了额外需要父任务 GUID 外，和创建任务完全一致）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。
- `task.create_task`（写）：创建一条任务（summary 必填；due 为 RFC3339 时间由工具层转换为毫秒时间戳；assignee_ids 为被指派人 open_id 列表）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。
- `task.delete_task`（写）：删除指定任务（删除后任务无法再被获取到）。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。建议先 dry_run 预演确认。task_guid 来自 task.create_task 或 task.list_my_tasks。
- `task.list_my_tasks`（只读）：以用户令牌身份读取「我负责的」任务列表（按用户在任务界面的自定义排序返回；可翻页）。要求宿主已提供当前用户身份与该用户的用户令牌。只读，需 task:task:readonly。
- `task.update_task`（写）：更新任务信息（summary/description/due 等字段，至少传一个要更新的字段）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。

## 参考文档（细则）
- `references/creation-checklist.md`：`task/creation-checklist`
