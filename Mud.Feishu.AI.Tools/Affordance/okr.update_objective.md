## WHEN

- 目标已在册，要改它的文字、备注、截止时间、得分或分类中的某几项。
- 只想动字段本身：目标仍留在原周期下，其下关键结果不受影响。

## Avoid when

- 要新建目标——那是 `okr.create_objective`，它挂在周期下。
- 改的是目标下某条关键结果——用 `okr.update_key_result`。
- 目标不再需要——那是 `okr.delete_objective`，会连带其下关键结果。

## Prerequisites

- `objective_id` 来自 `okr.list_objectives`；除它之外必须至少再传一个待更新字段，全空会被本地拒绝，连 `dry_run` 也过不去。
- `score` 是 0~1 的字符串小数；`category_id` 来自 `okr.list_categories`。

## Tips

- 传 `content` 就是整段替换目标文字（纯文本由工具层包装成块），不是追加一句。
- 换所属周期或改权重都做不到——参数面没有这两项。
- 改完先 `okr.get_objective` 读回，确认落库值与预期一致。
