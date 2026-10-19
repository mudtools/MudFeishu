---
name: feishu-approval
description: 飞书 approval 域工具集：8 个工具（只读 2 / 写 6）。出现「待办 / 待批」时先分域：来自审批流程才用 `approval.*`；来自任务清单用 `task.*`。
version: 1.0.0
package_version: 3.0.3
---

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

## 命令（工具）清单
- `approval.approve_task`（写）：同意指定审批任务（需 approval_code/instance_code/task_id/user_id 四要素，task_id/instance_code 来自 approval.list_pending_tasks）。同意后审批流程流转到下一个审批人。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。
- `approval.cancel_instance`（写）：撤回一个审批实例（撤销后实例不再是'审批中/已通过'的正常状态）。需要 approval_code + instance_code + **审批提交人** user_id（三者缺一平台即拒绝；ID 类型与 user_id_type 一致）。⚠️ 前提是该审批定义在后台勾选了「允许撤销审批中的申请」或「允许撤销 x 天内通过的审批」，否则返回业务错误——这属于审批配置限制，不要重试。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutio…
- `approval.cc_instance`（写）：把当前审批实例抄送给若干用户（被抄送人可在审批中心「抄送我」里查看详情）。只放开可见性，**不改变流程、不更换审批人**——要让别人来批用 approval.transfer_task。instance_code 来自 approval.list_pending_tasks / approval.get_instance；cc_user_ids 的类型须与 user_id_type 一致。用户身份工具——宿主须在 AllowedIdentities 放行 user。写操作：默…
- `approval.create_instance`（写）：按审批定义 Code 发起一个审批实例，form 为审批表单 Value（JSON 数组字符串，按定义的表单控件结构填写；非 JSON 数组会在下发前被拒绝）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。
- `approval.get_instance`（只读）：获取审批实例详情（含表单/状态/审批流程节点）。instance_code 来自 approval.list_pending_tasks 或审批事件。只读，需 approval:approval:readonly。用户身份工具——宿主须在 AllowedIdentities 放行 user。
- `approval.list_pending_tasks`（只读）：查询审批待办任务列表（按用户 ID 过滤 PENDING 状态任务）。task_id/instance_code 可传给 approval.approve_task 完成同意操作。只读，需 approval:approval:readonly。
- `approval.reject_task`（写）：拒绝一个审批任务（拒绝后审批流程结束）。与 approval.approve_task 构成完整的双向决策面；task_id 来自 approval.list_pending_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。
- `approval.transfer_task`（写）：把一个审批任务转交给他人（转交后流程流转给被转交人）。适用于'这条不该我批/我无法判断'的场景。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。建议先 dry_run 预演确认转交对象。

## 参考文档（细则）
- `references/decisions.md`：`approval/decisions`
