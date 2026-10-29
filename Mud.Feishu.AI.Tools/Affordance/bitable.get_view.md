## WHEN

- 已定位到某个视图，要确认它的名称与类型再决定是否套用取数。

## Avoid when

- 还不知道有哪些视图——用 `bitable.list_views` 枚举。
- 要按视图取记录内容——把 `view_id` 传给 `bitable.query_records`，本工具只回视图元数据。
- 想改视图配置——本域没有视图写工具。

## Prerequisites

- `view_id`（形如 vewXxx）来自 `bitable.list_views`；`app_token`、`table_id` 与该视图同源。
- 以 tenant 身份调用，需 bitable:app:readonly 权限。

## Tips

- 查不到时回执是 `found`=false 加 `view_id` 与 `message`，不抛错——按 `found` 分支，别当视图内容为空。
- 命中时回执只有 `view_id`/`view_name`/`view_type`；视图自带的过滤条件与隐藏字段不投影。
