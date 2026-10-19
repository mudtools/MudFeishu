## WHEN

- 需要向妙搭数据表批量落新行：数据导入、定时补录这类场景。
- 想要"有则更新、无则新增"时也走本工具，但必须显式打开合并模式。

## Avoid when

- 记录已存在、要按条件改内容——用 `spark.update_table_records`。
- 只是想确认表里现在有什么——用 `spark.query_table_records`，别用写入试探。
- 多维表格（bitable）的记录不在本域——用 `bitable.add_record` 一族。

## Prerequisites

- `table_id` 是表名不是数字 ID：从 `spark.list_tables` 的结果里取准。
- `records_json` 每个元素是字段名到值的对象；必须是非空 JSON 数组，单次最多 500 条。

## Tips

- 缺省纯插入且不幂等：失败重试会造出重复行，需按主键收敛时传 `upsert`。
- 超过 500 条在下发前就被本地拒绝，请分批并逐批核对返回的 record_ids。
- `dry_run` 预演同样校验 `records_json` 的结构与条数，坏格式预演也会报错。
