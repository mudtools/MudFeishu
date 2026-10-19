## WHEN

- 待办里的这条任务你已核实内容、符合规定，要代表当前审批人给出「同意」结论。
- 实例后面还有其他审批人：同意只是把流程往前推一步。

## Avoid when

- 内容不符合规定该驳回——用 `approval.reject_task`，不要为推进流程而同意。
- 这条不该由你处理——用 `approval.transfer_task`；同意后结论就记在你名下。

## Prerequisites

- approval_code、instance_code、task_id、user_id 四者缺一不可；task_id/instance_code 只能取自 `approval.list_pending_tasks` 的结果。
- user_id 是当前审批人，类型要与查待办时用的 user_id_type 一致。

## Tips

- 同意后任务状态即变，重复提交同一 task_id 只会失败——回执 approved=true 即视为完成，别再重试。
- 本域没有「反悔同意」的工具：撤回只认提交人（`approval.cancel_instance` 要提交人的 user_id）。
