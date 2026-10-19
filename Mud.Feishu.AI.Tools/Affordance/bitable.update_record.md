## WHEN

- 修改某一条已存在记录的内容（翻状态、更正字段值），目标行可唯一定位。

## Avoid when

- 多条一起改——用 `bitable.record_batch_update`。
- 这一行要整体作废——用 `bitable.delete_record`；要新的一行——用 `bitable.add_record`。
- 不确定改哪条——先 `bitable.query_records` 定位 record_id，不要凭记忆猜。

## Prerequisites

- `record_id` 来自 `bitable.query_records` 结果；`app_token` 与 `table_id` 来自 `bitable.list_tables`。
- `fields` 只放要改的字段，字段名先经 `bitable.list_fields` 确认。

## Tips

- `fields` 必须是 JSON 对象；非法 JSON 在 `dry_run` 预演阶段同样会先报错，不会静默放过。
- 同一笔修改可能重试时传稳定 `idempotency_key`，重复请求不产生额外副作用。
