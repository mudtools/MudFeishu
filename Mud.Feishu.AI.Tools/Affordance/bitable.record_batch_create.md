## WHEN

- 一次入库多条新记录（数据导入、批量补录），条数明确在 1000 以内。
- 只新增一条也走这里：`records` 里放 1 项即可，本工具是新增的唯一入口。

## Avoid when

- 这些记录已存在、要改内容——用 `bitable.record_batch_update`，本工具只创建。
- 超过 1000 条——本地校验在下发前就拒绝，分批调用并各带稳定 `idempotency_key`。

## Prerequisites

- 字段名先逐个经 `bitable.list_fields` 确认；`records` 每项都要包成 {"fields":{...}} 结构，不是裸字段对象。
- `app_token` 与 `table_id` 来自 `bitable.list_tables`。

## Tips

- 整批用同一 `idempotency_key` 重试不会重复创建；省略键则重试必然有重复写入风险。
- 结果返回 record_ids 与 count，逐批核对新建条数是否符合预期；新增单条时 record_ids 只有一项。
