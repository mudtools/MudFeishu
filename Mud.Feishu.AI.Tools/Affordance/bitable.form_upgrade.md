## WHEN

- 确认表单仍是旧版，且用户明确要求升级为新版表单并选定布局。

## Avoid when

- 只想改表单名称或共享范围——用 `bitable.form_update`，本工具只做旧版升级这一件事。
- 调整题目的必填或顺序——用 `bitable.form_question_update`，与升级无关。
- 想另得一个表单——本工具只把现有 `form_id` 的表单升级，不会新建或复制表单。

## Prerequisites

- `form_name` 与 `display_mode` 都是必填参数；`display_mode` 只接受 traditional 与 one_question_per_page 两个字面值，其它值在 `dry_run` 前即被拒绝。
- `form_id` 与 `table_id` 先经 `bitable.list_tables`、`bitable.form_get` 链路核对。

## Tips

- **升级不可撤销**，执行后无法退回旧版表单，务必先 `dry_run` 预演。
- 结果返回升级后的表单 `form_id`，后续操作以它为准。
