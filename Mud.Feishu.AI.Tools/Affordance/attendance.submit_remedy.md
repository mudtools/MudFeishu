## WHEN
- 员工漏打卡，用户要求为指定日与时点提交补卡申请——本域写面只有这一个入口。
- 管理员集中处理组员的补卡诉求，逐条核对后正式发起。

## Avoid when
- 先确认是否已提过——用只读的 `attendance.query_my_remedys`，别用提交来试探。
- 只是查打卡记录——用 `attendance.query_my_flow` 或 `attendance.query_user_flow`。
- 想撤回或修改已提交的申请——工具面没有撤销类能力。

## Prerequisites
- `user_id`、`remedy_date`、`remedy_time`、`punch_no`、`work_type`、`reason` 缺一不可。
- 日期是 yyyyMMdd 整数、时刻是 HH:mm 字符串；`punch_no` 与 `work_type` 取值以该人考勤组配置为准。
- 本写工具默认不启用，先过宿主授权。

## Tips
- 没有幂等键：不确定上一次是否写进就先查询，直接重试等于重复申请。
- 落进去的是审批中的申请而非补卡完成；回执里的 approval_id 供用户跟进。
- `dry_run` 只回报字段摘要，不写入考勤系统。
