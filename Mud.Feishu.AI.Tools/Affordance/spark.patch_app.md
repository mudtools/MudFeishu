## WHEN

- 应用已存在，只想改名称、描述、图标这层门面信息时用它。
- 改坏了要修参重放：PATCH 语义下相同参数重放结果一致，可安全重试。

## Avoid when

- 应用还没有——先用 `spark.create_app` 建出来。
- 要改谁能访问应用——用 `spark.update_app_visibility`，那是另一条轴。
- 想确认现状再决定改成什么——先 `spark.list_apps` 看当前名字。

## Prerequisites

- `app_id` 来自 `spark.list_apps` 或创建返回；需 spark:app 权限点，走 user 身份且宿主放行。
- `name`、`desc`、`icon_url` 至少给一个：全空的 PATCH 会被本地校验直接拒掉。

## Tips

- 先用 `dry_run` 预演查看字段摘要，再以相同参数去掉预演真正下发。
- 改完返回的 app 信息以平台回值为准，别把预演摘要当成修改结果。
