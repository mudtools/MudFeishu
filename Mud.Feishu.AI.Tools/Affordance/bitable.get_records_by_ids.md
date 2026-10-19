## WHEN

- 手上已有一批记录 ID，要精取字段值——单次最多 100 条。

## Avoid when

- 想按条件筛——用 `bitable.query_records`，本工具不支持筛选式。
- 一次超过 100 个 ID——拆多批；执行端在发出请求前就整批拒绝。
- 要回写内容——用 `bitable.update_record` 或 `bitable.record_batch_update`。

## Prerequisites

- `app_token` 与 `table_id` 必须和这批记录同源；`record_ids` 形如 recXxx。
- 以 tenant 身份调用，需 bitable:app:readonly 权限。

## Tips

- `items` 每条只有 `record_id` 与 `fields`，没有创建时间与修改人等审计字段。
- 不存在或读不到的 ID 进 `absent_record_ids` 而不是报错——统计前先核对它。
- 回执没有翻页游标：一批一次取完。
