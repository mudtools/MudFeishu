## WHEN

- 调整表单里某一道题：改标题、加描述、设必填、隐藏题目或调整题目位置。

## Avoid when

- 改表单整体的名称、描述或共享设置——用 `bitable.form_update`，那是表单级不是题目级。
- 不知道 field_id、要看题目全列表——用 `bitable.form_question_list`。
- 改这条题目背后的记录数据——用 `bitable.update_record`，本工具只动题目的展示属性。

## Prerequisites

- `field_id` 必须取自 `bitable.form_question_list` 的返回，别按题目标题反推。
- `form_id` 与 `table_id` 沿 `bitable.list_tables` 链路获取。

## Tips

- 可更属性一个都不传会被本地校验拒绝，至少给 `title` / `description` / `required` / `visible` / `pre_field_id` 之一。
- 补丁语义：未提供的属性保持现值，不会被重置。
- `pre_field_id` 的含义是「排到该字段之后」，用它做题目排序。
