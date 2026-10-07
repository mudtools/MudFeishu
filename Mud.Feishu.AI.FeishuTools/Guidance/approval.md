# 审批（approval.*）

## 前置链
`approval.list_pending_tasks` 取 task_id → `approve_task` / `reject_task` / `transfer_task`。

## 避坑
- **审批是双向的**：应拒就 reject，不要为推进而同意。
- `transfer_task` 不能转给自己（空操作）。
- 深层避坑 → `feishu.guidance_read(approval, decisions)`。

## 示例
有什么待批 → `list_pending_tasks`；该转给谁 → `transfer_task`。