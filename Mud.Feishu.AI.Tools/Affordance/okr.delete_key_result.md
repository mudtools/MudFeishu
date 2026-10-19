## WHEN

- 某个关键结果已作废或重复创建，需要从目标下移除。

## Avoid when

- 只是不再追踪它——停更即可，删除会丢掉它的进度记录。
- 整个目标都作废——用 `okr.delete_objective`（连带其下全部 KR，一次到位）。

## Prerequisites

- 先用 `okr.list_objectives` 确认目标，再用 `okr.list_key_results` 取 `key_result_id`。
- 需 OKR 内容写权限。

## Tips

- **不可恢复**，并且会带走该 KR 的进度历史。
- 建议先 `dry_run` 预演，核对"目标 ↔ KR"的对应关系后再执行。
