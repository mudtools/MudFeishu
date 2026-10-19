
# 审批（approval.*）

## 路由优先级（先判断对象属于哪个域）
- 出现「待办 / 待批」时先分域：来自**审批流程**才用 `approval.*`；来自**任务清单**用 `task.*`。
  两者都会说「待办」，混用是最高频的错域调用。
- 「要我批的」→ `approval.list_pending_tasks`；「我发起的、想看进展」→ `approval.get_instance`。

## 前置链
`approval.list_pending_tasks` 取 task_id → `approve_task` / `reject_task` / `transfer_task`。
「我发起的想收回来」→ `get_instance` 取 approval_code 与提交人 → `cancel_instance`；
只是想让某人看到进展 → `cc_instance`。

## 避坑
- **审批是双向的**：应拒就 reject，不要为推进而同意。
- `transfer_task` 不能转给自己（空操作）。
- **撤回只对「审批中」的实例有效**，且该审批定义须勾选过允许撤销——恒失败时先查这一项，不是参数写错。
- **`cc_instance` 只放开可见性**：被抄送人不会因此变成审批人。
- **`approval.cc_instance` 是用户身份工具**：宿主须在 `FeishuAgent:AllowedIdentities` 放行 `user`，否则启动即报错。
- 深层避坑 → `feishu.guidance_read(approval, decisions)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
有什么待批 → `list_pending_tasks`；这条不该我批 → `search_user` → `transfer_task`。
