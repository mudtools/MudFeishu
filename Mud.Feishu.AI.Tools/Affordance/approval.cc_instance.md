## WHEN

- 想让相关人（主管、协同部门）看到这条审批的进展，但不应让他们参与审批动作。
- 审批还在流转，只需扩大可见范围——抄送不改变流程走向。

## Avoid when

- 想让别人来批——用 `approval.transfer_task`；被抄送人不会因此变成审批人。
- 想让流程停下——用 `approval.cancel_instance`；抄送完全不动流程。

## Prerequisites

- 用户身份工具：宿主须在 AllowedIdentities 放行 user，否则执行直接报不可用。
- instance_code 取自 `approval.list_pending_tasks` 或 `approval.get_instance`。
- cc_user_ids 不能为空数组；ID 类型须与 user_id_type 一致（默认 open_id）。

## Tips

- 被抄送人在审批中心「抄送我」里查看详情；留言 comment 上限 500 字。
- 回执给 cc_sent 与抄送人数，据此核对是否发给了全部目标。
