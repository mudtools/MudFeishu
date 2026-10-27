---
name: feishu-task
description: 飞书 task 域工具集：18 个工具（只读 5 / 写 13）。「待办」来自任务清单 → `task.list_my_tasks`；来自审批流程 → `approval.*`。
version: 1.0.0
package_version: 3.0.3
---

# 任务（task.*）

## 路由优先级（先判断对象属于哪个域）
- 「待办」来自**任务清单** → `task.list_my_tasks`；来自**审批流程** → `approval.*`。
- 「提醒某人」若只是发消息，用 `im.*`；只有真的要建可跟踪事项才用 `task.create_task`。
- 「一批相关待办 / 项目待办池」→ 先 `task.tasklist_create` 建清单再 `task.*` 归集；单个零散待办不要建清单。
- 「找符合某条件的任务」→ `task.search_tasks`（用户身份，支持负责人/创建人/关注人/完成态/截止时间过滤），不要全量翻页再人肉筛。

## 前置链
读 `tasklist_page_list` → `tasklist_tasks` / `tasklist_get`；写 `create_task` / `update_task` / `complete_task` / `delete_task` / `add_comment` / `add_members`。
拆步骤用 `task.create_task` 传 `parent_task_guid`（同一入口，字段面与建任务一致）。
清单写族：`tasklist_create` → `tasklist_update` / `tasklist_delete` / `tasklist_member_add` / `tasklist_member_remove`（`tasklist_guid` 一律来自 `tasklist_page_list` 或 `tasklist_create`）。
提醒 `set_reminder` 前必须先有 `due`（提醒时刻 = 截止时间 − 提前分钟数）；`remove_reminder` 的 `reminder_ids` 来自任务详情。
创建前先 `contact.search_user` 拿 open_id（`assignee_ids` / `member_ids` 都是 open_id 列表）。

## 避坑
- `due` 用 RFC3339；`assignee_ids` 是 open_id 列表，不是姓名。
- **创建任务（含子任务）/ 加成员都要 `idempotency_key`**：重试时用同一值，否则会建出重复任务。
- `delete_task` 不可恢复，建议先 `dry_run`；`tasklist_delete` 同样不可恢复（但清单内任务本身不受影响）。
- 清单成员的 `type` 是 `user`（传 `open_id` 会被平台判非法）——`tasklist_member_add` 已按此固定，只需给 open_id 数组。
- 加关注人用 `task.add_members` 传 `role=follower`（本域没有独立的 followers 工具，别再找）。
- 「已完成」要改回未完成用 `task.update_task` 传 `completed=false`（写 `completed_at=0`），与 `complete_task` 互为逆操作。
- 创建清单（必填/默认值）→ `feishu.guidance_read(task, creation-checklist)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
明天给张三建写周报任务 → `search_user` → `create_task`。

## 命令（工具）清单
- `task.add_comment`（写）：为指定任务添加评论（content 必填，最长 3000 个 utf8 字符）。可通过 reply_to_comment_id 回复已有评论。写操作：需 task:task。
- `task.add_members`（写）：向指定任务添加成员（负责人或关注人，member_ids 为 open_id 数组，role 指定角色 assignee 或 follower）。写操作：需 task:task。
- `task.complete_task`（写）：将任务标记为已完成（通过 update_task 设置 completed_at 字段）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：需 task:task。
- `task.create_task`（写）：创建一条任务（summary 必填；due 为 RFC3339 时间由工具层转换为毫秒时间戳；assignee_ids 为被指派人 open_id 列表）。传 parent_task_guid 则创建该任务的子任务（同一入口，字段面一致）。写操作：需 task:task。
- `task.delete_task`（写）：删除指定任务（删除后任务无法再被获取到）。高危写（high-risk-write）：需 task:task。建议先 dry_run 预演确认。task_guid 来自 task.create_task 或 task.list_my_tasks。
- `task.list_my_tasks`（只读）：以用户令牌身份读取「我负责的」任务列表（按用户在任务界面的自定义排序返回；可翻页）。要求宿主已提供当前用户身份与该用户的用户令牌。只读，需 task:task:readonly。
- `task.remove_reminder`（写）：移除任务的提醒（reminder_ids 来自任务详情；移除不存在的提醒平台直接返回成功）。高危写（high-risk-write）：需 task:task。建议先 dry_run 预演确认。
- `task.search_tasks`（只读）：基于关键词与过滤条件搜索当前用户可见的任务（如'找关于合同的未完成任务'）。要求宿主已提供当前用户身份与该用户的用户令牌。只读，需 task:task:readonly。
- `task.set_reminder`（写）：为任务添加提醒（提醒时刻 = 截止时间 - relative_fire_minute 分钟；任务必须已有截止时间；每个任务最多 1 个提醒）。写操作：需 task:task。
- `task.tasklist_create`（写）：创建一个任务清单（清单用于组织同一项目的多个任务）。member_ids 为协作成员 open_id 列表（可选）。写操作：需 task:task。
- `task.tasklist_delete`（写）：删除指定清单（删除后清单不可恢复，清单内的任务本身不受影响）。高危写（high-risk-write）：需 task:task。建议先 dry_run 预演确认。tasklist_guid 来自 task.tasklist_page_list。
- `task.tasklist_get`（只读）：获取清单详情（清单名/所有者/成员）。tasklist_guid 来自 task.tasklist_page_list 或 task.tasklist_create。只读，需 task:task:readonly。
- `task.tasklist_member_add`（写）：向清单添加协作成员（member_ids 为 open_id 数组）。写操作：需 task:task。
- `task.tasklist_member_remove`（写）：从清单移除协作成员（member_ids 为 open_id 数组；移除不存在的成员平台直接返回成功）。高危写（high-risk-write）：需 task:task。建议先 dry_run 预演确认。
- `task.tasklist_page_list`（只读）：列出调用身份可读取的全部任务清单（返回清单摘要，可翻页）。只读，需 task:task:readonly。
- `task.tasklist_tasks`（只读）：分页列出指定清单内的任务（返回任务摘要；可按完成状态过滤，可翻页）。tasklist_guid 来自 task.tasklist_page_list。只读，需 task:task:readonly。
- `task.tasklist_update`（写）：重命名一个任务清单。tasklist_guid 来自 task.tasklist_page_list。写操作：需 task:task。
- `task.update_task`（写）：更新任务信息（summary/description/due/completed，至少传一个要更新的字段）。completed=true 标记完成、false 重新打开（与 task.complete_task 同一路径的简写入口）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：需 task:task。

## 参考文档（细则）
- `references/creation-checklist.md`：`task/creation-checklist`
- 正文里形如 `feishu.guidance_read(task, 主题)` 的指针，在文件形态下读本包的 `references/主题.md`（本包细则就是那份资产，无需进程内工具）
