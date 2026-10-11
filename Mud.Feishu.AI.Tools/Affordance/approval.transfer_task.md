## WHEN

- 这条审批**不该由我处理**（业务归属错了），或我无权判断该事项，需要移交给正确的审批人。

## Avoid when

- 事项本可自行处理——转交只让流程多流转一次，应先考虑 `approval.approve_task` / `approval.reject_task`。
- 只是想提醒某人关注，而不是移交处理权——那应当发消息沟通。

## Prerequisites

- 先用 `approval.list_pending_tasks` 拿到待办里的 `approval_code` 与 `instance_code`。
- 转交对象必须是该审批流程允许的审批人（转交后流程流转给被转交人）。

## Tips

- 转交**不产生审批结论**，只是换了处理人；流程仍停在原节点。
- 建议先 `dry_run` 预演，确认转交对象无误再真正执行。
