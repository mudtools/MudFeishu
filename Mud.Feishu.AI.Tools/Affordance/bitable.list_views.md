## WHEN

- 取记录之前要确定用哪个视图：列出本表全部视图的 ID、名称与类型。

## Avoid when

- 手上已有 `view_id`、只要确认详情——用 `bitable.get_view`。
- 想直接看视图筛出的行——用 `bitable.query_records` 传 `view_id`，本工具不返回记录。

## Prerequisites

- `app_token` 与 `table_id`；两者来自 `bitable.list_tables`。
- 以 tenant 身份调用，需 bitable:app:readonly 权限。

## Tips

- 列表落在 `views` 数组（不是 `items`），每条只有 `view_id`/`view_name`/`view_type`。
- 本工具无翻页参数，一次回全量；`total` 给出视图条数，`has_more` 为真表示清单未取尽。
