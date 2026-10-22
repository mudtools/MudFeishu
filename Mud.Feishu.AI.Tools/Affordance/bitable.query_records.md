## WHEN

- 读取面唯一能按字段内容筛选、排序、选列的入口。
- 手上已有一批记录 ID 要精取字段值——传 `record_ids`（单次最多 100 条）。

## Avoid when

- `record_ids` 与 `filter`/`view_id`/`sort` 不能同时给：前者按 ID 集合取、后者按条件筛，同时给出就无法判断"少返回的那条是被过滤掉了还是不存在"，执行端会直接拒绝。
- 只想套用某个视图的现成条件——直接传 `view_id` 即可，别再写一份等价的 `filter` 叠加收紧结果。

## Prerequisites

- `table_id` 来自 `bitable.list_tables`；写 `filter`/`sort` 前先 `bitable.list_fields` 核对字段原名。
- `record_ids` 形如 recXxx，必须与 `app_token`/`table_id` 同源；超过 100 个要分批。
- 以 tenant 身份调用，需 bitable:app:readonly 权限。

## Tips

- `items` 每条只有 `record_id` 与 `fields`，创建人、修改时间等元数据不投影。
- 条件检索：`filter`/`sort` 语法不合法在本地即被拒，不发请求；`field_names` 按字段名精确匹配，未命中的列会从 `fields` 里消失。
- 条件检索结果带 `total`；默认单页，`has_more` 为真时用 `page_token` 或 `fetch_all`。
- 按 ID 精取：不翻页、不回 `total`；不存在或读不到的 ID 进 `absent_record_ids` 而不是报错——统计前先核对它。
