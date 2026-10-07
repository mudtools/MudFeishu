# 审批与任务

## 前置链
1. `approval.list_pending_tasks` 拿 task_id
2. 决策：`approval.approve_task` / `approval.reject_task` / `approval.transfer_task`

## 避坑
- **审批是双向的**：应拒就 `approval.reject_task`，不要为了"推进"而同意
- `approval.transfer_task` **不能转给自己**（空操作，流程不前进）
- 发起审批用 `approval.create_instance`，`form` 必须是 JSON **数组**字符串

## 示例
- "看看我有什么待批的" → `approval.list_pending_tasks`
- "这条不该我批，转给张三" → `contact.search_user` 拿 user_id → `approval.transfer_task`