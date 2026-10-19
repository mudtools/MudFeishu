## WHEN

- 要落库的只有一条新记录（开一张单、补一行台账）。

## Avoid when

- 一次要写多条——用 `bitable.record_batch_create`，别逐条调本工具。
- 目标行已存在、只是内容要改——用 `bitable.update_record`，不要新增重复行。
- 把 `fields` 传成 JSON 数组或标量——非对象会在本地校验阶段直接拒绝。

## Prerequisites

- `app_token` 与 `table_id` 来自 `bitable.list_tables`。
- 先经 `bitable.list_fields` 确认实际字段名：字段名拼错会在下发前被拒绝。

## Tips

- 同一笔写入可能重试时给稳定的 `idempotency_key`（如「单据号+动作」）；省略键则不保证幂等，重复调用会建出多条。
- 幂等键仅本工具内生效，其他工具的同名键不去重。
