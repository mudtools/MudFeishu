## WHEN

- 已有的关键结果要更新内容、截止时间或得分中的某几项，其余不动。
- 季度中途口径变了，只动这一条结果，不牵连同目标下的其他结果。

## Avoid when

- 要改的是所属目标的文字或分类——那是 `okr.update_objective`。
- 这条关键结果不该存在了——用 `okr.delete_key_result`。
- 要写进展文字或完成度——本域没有策展写进展的工具，改字段替代不了它。
- 要动的是指标的取值与单位——用 `okr.update_indicator`。

## Prerequisites

- `key_result_id` 来自 `okr.list_key_results`。
- 必须至少再传一个待更新字段（content / deadline / score），全空会被本地拒绝。
- 参数面没有归属字段，这条结果挂在哪个目标下改不动。

## Tips

- `score` 是 0~1 的字符串小数，越界或非数值直接本地报错，不会下发。
- 传 `content` 就是整段替换这条结果的文本，不是追加一句。
- 拿不准传了什么时先看 `dry_run` 的字段摘要，它不调用下游。
