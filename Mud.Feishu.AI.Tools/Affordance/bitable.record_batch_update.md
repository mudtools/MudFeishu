## WHEN

- 多条已存在的记录要做同一批修改（批量翻状态、统一更正字段）。

## Avoid when

- 只改一条——用 `bitable.update_record`，单条链路更短。
- 这些记录还不存在、应新建——用 `bitable.record_batch_create`，本工具不会创建。
- 超过 1000 条——超上限会在本地校验即被拒绝，必须分批。

## Prerequisites

- `records` 每项的 record_id 必须来自 `bitable.query_records` 的真实结果；缺 record_id 或为空白的项在 `dry_run` 前即整批拒绝。
- 字段名先经 `bitable.list_fields` 确认，每项 fields 只放要改的字段。

## Tips

- 本工具没有幂等键：结果不确定时先用 `bitable.query_records` 复查实际值，再决定是否重试。
- 结果返回 record_ids 与 count，不区分每条的成败，大批量改后建议抽样核对。
