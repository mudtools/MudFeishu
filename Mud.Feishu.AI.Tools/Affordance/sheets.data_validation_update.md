## WHEN

- 已有下拉的选项值要改，或它的生效区域需要整体重划。
- 一次给多个区域：`ranges` 是数组。

## Avoid when

- 区域上还没有校验——那是 `sheets.data_validation_create`。
- 要把限制撤掉——用 `sheets.data_validation_delete`，不要用空选项覆盖。

## Prerequisites

- `sheet_id` 来自 `sheets.list_sheets`，且 `ranges` 每项的工作表段与它一致。
- `ranges` 和 `condition_values` 都必须是非空数组，缺一即被本地校验拒掉。

## Tips

- 重写整条规则：生效区域以本次 `ranges` 为准，不是往旧规则上加一项。
- 回包的 `ranges` 只是入参回显，服务端真实面貌要用 `sheets.data_validation_get` 复核。
