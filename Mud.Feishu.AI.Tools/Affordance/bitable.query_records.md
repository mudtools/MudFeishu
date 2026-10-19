## WHEN

- 读取面唯一能按字段内容筛选、排序、选列的入口。

## Avoid when

- 已有一批记录 ID 要精取——用 `bitable.get_records_by_ids`，本工具不接受 ID 名单。
- 只想套用某个视图的现成条件——直接传 `view_id` 即可，别再写一份等价的 `filter` 叠加收紧结果。

## Prerequisites

- `table_id` 来自 `bitable.list_tables`；写 `filter`/`sort` 前先 `bitable.list_fields` 核对字段原名。
- 以 tenant 身份调用，需 bitable:app:readonly 权限。

## Tips

- `items` 每条只有 `record_id` 与 `fields`，创建人、修改时间等元数据不投影。
- `filter`/`sort` 语法不合法在本地即被拒，不发请求；`field_names` 按字段名精确匹配，未命中的列会从 `fields` 里消失。
- 结果带 `total`；默认单页，`has_more` 为真时用 `page_token` 或 `fetch_all`。
