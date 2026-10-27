## WHEN

- 需要把符合条件的一批既有记录统一改字段：状态修正、取值迁移。
- 语义是按条件命中更新，不是编辑单条——一次调用覆盖全部命中行。

## Avoid when

- 要新增行——用 `spark.add_table_records`，本工具不创建记录。
- 不确定会命中多少行——先用 `spark.query_table_records` 带同样条件试查，再决定要不要改。
- 改应用名称或图标——用 `spark.patch_app`，别在记录面绕路。

## Prerequisites

- `filter` 必填且非空白（PostgREST 语法，如 age=gt.10），缺失会被本地拒绝，防止无条件刷全表。
- `record_json` 是待改字段构成的 JSON 对象；`table_id` 同样是表名。

## Tips

- 相同 `filter` 加 `record_json` 重放结果一致，重试是安全的。
- 调用后用返回的 record_ids 核对命中数量，与预期不符立即停手排查条件。
- `dry_run` 预演也先过 `filter` 与 `record_json` 的本地校验。
