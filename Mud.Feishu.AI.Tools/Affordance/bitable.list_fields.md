## WHEN

- 写筛选、排序或记录载荷之前：先对齐表里真实存在的字段名、类型与主键标记。

## Avoid when

- 还不知道表 ID——从 `bitable.list_tables` 开始。
- 要看记录的值——那是 `bitable.query_records`，本工具只给结构。

## Prerequisites

- `table_id`（形如 tblXxx）来自 `bitable.list_tables`；可用 `view_id` 圈定某视图可见的字段。
- 以 tenant 身份调用，需 bitable:app:readonly 权限。

## Tips

- `items` 每条只有 `field_id`/`name`/`type`/`is_primary`/`ui_type`；字段属性细节（选项清单、格式配置）不投影。
- 后续筛选与取列都以这里的 `name` 原文为准，名字对不上就取不到该列。
- 默认单页，`has_more` 为真时用 `page_token` 续翻，或 `fetch_all` 自动翻完。
