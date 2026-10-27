## WHEN

- 某条记录内容作废或填错，需要在多维表格里**永久移除**它。

## Avoid when

- 记录仍有留痕价值（历史核对 / 审计）——改用 `bitable.record_batch_update` 标记状态。
- 只是想清掉某个字段的值——更新该字段即可，不必删整条记录。

## Prerequisites

- 先用 `bitable.list_tables` 取 `app_token` 与 `table_id`；
- 再用 `bitable.query_records` 定位目标 `record_id`（确认它就是要删的那一条）。

## Tips

- **删除不可恢复**，平台不提供回收站。
- 执行前务必 `dry_run` 预演，逐字核对 `record_id`。
