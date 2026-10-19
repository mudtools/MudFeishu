## WHEN

- 用户要求发起一条新的审批，你已有审批定义 Code 和要填报的表单内容。
- 上次被驳回后要重新提交：本工具发起的是全新实例。

## Avoid when

- 想收回审批中的旧实例——用 `approval.cancel_instance`，新发起不会动旧实例。
- 只是想让某人看到进展——用 `approval.cc_instance`，不必再发一条。

## Prerequisites

- approval_code 为审批定义 Code；form 必须是 JSON 数组字符串，按该定义的表单控件结构填写。
- user_id 可选，须为 user_id 类型，用于发起人数据权限校验。

## Tips

- 非法 JSON 或非数组会在下发前被本地拦截并附修复指引，不会惊动平台。
- 建议给稳定的 idempotency_key：重试撞同键返回 60012（该键已用过，不是故障）；省略则不保证幂等。
- 回执只有 instance_code，后续撤回或查进展全靠它。
