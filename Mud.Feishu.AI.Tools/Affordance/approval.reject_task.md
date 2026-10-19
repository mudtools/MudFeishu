## WHEN

- 这条申请违反制度或信息不实，你判断应当直接驳回它，而不是放行继续流转。
- 你对事项已有明确的否定结论，拒绝即终结本次审批流程。

## Avoid when

- 该由别人定夺——用 `approval.transfer_task` 移交处理权；拒绝不是推掉责任的手段。
- 内容其实合规——用 `approval.approve_task`；拒绝会让整条流程终止。

## Prerequisites

- 与同意同样要 approval_code、instance_code、task_id、user_id 四要素，后两个取自 `approval.list_pending_tasks`。
- comment 是拒绝理由，会展示给发起人；写明依据可减少来回沟通。

## Tips

- 拒绝后流程结束、不可逆；对方要再走只能 `approval.create_instance` 重新发起一条。
- 回执只有 rejected=true，不含后续节点信息；要核实例终态用 `approval.get_instance`。
