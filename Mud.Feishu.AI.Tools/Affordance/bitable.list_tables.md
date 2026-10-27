## WHEN

- 进入陌生多维表格的第一步：清点这个应用里有哪些数据表，拿到可用的表 ID。

## Avoid when

- 已有表、要看列——用 `bitable.list_fields`，本工具不含表结构。
- 要按条件取记录——用 `bitable.query_records`，不是在这里筛。

## Prerequisites

- `app_token`（形如 bascnXxx）由调用方给出；知识库下的多维表格先经 `wiki.get_node` 取 token。
- 以 tenant（应用）身份调用，需 bitable:app:readonly 权限。

## Tips

- `items` 每条只有 `table_id`/`name`/`revision` 三个键；视图与字段要各自再取。
- 默认单页，`has_more` 为真时带 `page_token` 续翻，或用 `fetch_all` 在 `max_items` 预算内自动翻完。
