## WHEN

- 整个目标已废弃（周期结束未采用、建错），需要连同其下关键结果一起移除。

## Avoid when

- 只有个别关键结果作废——用 `okr.delete_key_result`（影响面小得多）。
- 只是想新开一个目标——用 `okr.create_objective`，旧目标保留的成本很低。

## Prerequisites

- 先用 `okr.list_objectives` 取目标 `objective_id`。
- 需 OKR 内容写权限。

## Tips

- **连带删除**其下全部关键结果与进度记录，且**不可恢复**。
- 建议先 `dry_run` 预演，核对将被删除的 KR 数量与预期一致。
